/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.File;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis;

public interface IDocumentEventsHandler : IDisposable
{
}

[Export(typeof(IDocumentEventsHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
public sealed class DocumentEventsHandler : IDocumentEventsHandler
{
    private readonly IDocumentTracker documentTracker;
    private readonly IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater;
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private IFileRpcSLCoreService fileRpcSlCoreService;
    private bool disposed;

    [ImportingConstructor]
    public DocumentEventsHandler(
        IDocumentTracker documentTracker,
        IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater,
        ISLCoreServiceProvider serviceProvider,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.documentTracker = documentTracker;
        this.vcxCompilationDatabaseUpdater = vcxCompilationDatabaseUpdater;
        this.serviceProvider = serviceProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.threadHandling = threadHandling;
        this.logger = logger.ForContext(nameof(DocumentEventsHandler));
        this.documentTracker.DocumentOpened += OnDocumentOpened;
        this.documentTracker.DocumentClosed += OnDocumentClosed;
        this.documentTracker.DocumentSaved += OnDocumentSaved;
        this.documentTracker.OpenDocumentRenamed += OnOpenDocumentRenamed;

        documentTracker.GetOpenDocuments().ToList().ForEach(AddFileToCompilationDatabase);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        documentTracker.DocumentOpened -= OnDocumentOpened;
        documentTracker.DocumentClosed -= OnDocumentClosed;
        documentTracker.DocumentSaved -= OnDocumentSaved;
        documentTracker.OpenDocumentRenamed -= OnOpenDocumentRenamed;
    }

    private void OnOpenDocumentRenamed(object sender, DocumentRenamedEventArgs args)
    {
        if (args.Document.DetectedLanguages.Contains(AnalysisLanguage.CFamily))
        {
            vcxCompilationDatabaseUpdater.RenameFileAsync(args.OldFilePath, args.Document.FullPath).Forget();
        }
        threadHandling.RunOnBackgroundThread(() =>
        {
            NotifySlCoreFileClosed(args.OldFilePath);
            NotifySlCoreFileOpened(args.Document.FullPath);
        }).Forget();
    }

    private void OnDocumentClosed(object sender, DocumentEventArgs args)
    {
        if (args.Document.DetectedLanguages.Contains(AnalysisLanguage.CFamily))
        {
            vcxCompilationDatabaseUpdater.RemoveFileAsync(args.Document.FullPath).Forget();
        }
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFileClosed(args.Document.FullPath)).Forget();
    }

    private void OnDocumentOpened(object sender, DocumentEventArgs args)
    {
        AddFileToCompilationDatabase(args.Document);
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFileOpened(args.Document.FullPath)).Forget();
    }

    /// <summary>
    /// Due to the fact that we can't react to project/file properties changes, regenerating the compilation database entry on file save is needed
    /// Additionally, this is a workaround that can deal with the renaming bug described in https://sonarsource.atlassian.net/browse/SLVS-2170
    /// </summary>
    private void OnDocumentSaved(object sender, DocumentSavedEventArgs args) => AddFileToCompilationDatabase(args.Document);

    private void AddFileToCompilationDatabase(Document document)
    {
        if (document.DetectedLanguages.Contains(AnalysisLanguage.CFamily))
        {
            vcxCompilationDatabaseUpdater.AddFileAsync(document.FullPath).Forget();
        }
    }

    private void NotifySlCoreFileOpened(string filePath)
    {
        if (activeConfigScopeTracker.Current?.Id is null)
        {
            logger.WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
            return;
        }
        GetFileRpcSlCoreService()?.DidOpenFile(new DidOpenFileParams(activeConfigScopeTracker.Current.Id, new FileUri(filePath)));
    }

    private void NotifySlCoreFileClosed(string filePath)
    {
        if (activeConfigScopeTracker.Current?.Id is null)
        {
            logger.WriteLine(SLCoreStrings.ConfigScopeNotInitialized);
            return;
        }
        GetFileRpcSlCoreService()?.DidCloseFile(new DidCloseFileParams(activeConfigScopeTracker.Current.Id, new FileUri(filePath)));
    }

    private IFileRpcSLCoreService GetFileRpcSlCoreService()
    {
        if (fileRpcSlCoreService != null)
        {
            return fileRpcSlCoreService;
        }
        if (!serviceProvider.TryGetTransientService(out IFileRpcSLCoreService service))
        {
            logger.WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
        }
        fileRpcSlCoreService = service;
        return fileRpcSlCoreService;
    }
}
