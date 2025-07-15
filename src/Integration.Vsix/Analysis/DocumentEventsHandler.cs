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
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.File;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis;

public interface IDocumentEventsHandler : IDisposable, IRequireInitialization;

[Export(typeof(IDocumentEventsHandler))]
[PartCreationPolicy(CreationPolicy.Shared)]
public sealed class DocumentEventsHandler : IDocumentEventsHandler
{
    private readonly IDocumentTracker documentTracker;
    private readonly IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater;
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IActiveCompilationDatabaseTracker activeCompilationDatabaseTracker;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;
    private bool disposed;

    [ImportingConstructor]
    public DocumentEventsHandler(
        IDocumentTracker documentTracker,
        IVcxCompilationDatabaseUpdater vcxCompilationDatabaseUpdater,
        ISLCoreServiceProvider serviceProvider,
        IActiveConfigScopeTracker activeConfigScopeTracker,
        IInitializationProcessorFactory initializationProcessorFactory,
        IActiveCompilationDatabaseTracker activeCompilationDatabaseTracker,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.documentTracker = documentTracker;
        this.vcxCompilationDatabaseUpdater = vcxCompilationDatabaseUpdater;
        this.serviceProvider = serviceProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.activeCompilationDatabaseTracker = activeCompilationDatabaseTracker;
        this.threadHandling = threadHandling;
        this.logger = logger.ForVerboseContext(nameof(DocumentEventsHandler));

        InitializationProcessor = initializationProcessorFactory.CreateAndStart<DocumentEventsHandler>([activeCompilationDatabaseTracker], async () =>
        {
            if (disposed)
            {
                return;
            }

            activeConfigScopeTracker.CurrentConfigurationScopeChanged += ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
            activeCompilationDatabaseTracker.DatabaseChanged += ActiveCompilationDatabaseTracker_DatabaseChanged;

            documentTracker.DocumentOpened += OnDocumentOpened;
            documentTracker.DocumentClosed += OnDocumentClosed;
            documentTracker.DocumentSaved += OnDocumentSaved;
            documentTracker.OpenDocumentRenamed += OnOpenDocumentRenamed;

            var openDocuments = documentTracker.GetOpenDocuments();
            await AddFilesToCompilationDatabaseAsync(openDocuments);
            NotifySlCoreFilesOpened(activeConfigScopeTracker.Current, openDocuments);
        });
    }

    private void ActiveCompilationDatabaseTracker_DatabaseChanged(object sender, EventArgs e) =>
        threadHandling
            .RunOnBackgroundThread(() => AddFilesToCompilationDatabaseAsync(documentTracker.GetOpenDocuments()))
            .Forget();

    private void ActiveConfigScopeTracker_CurrentConfigurationScopeChanged(object sender, ConfigurationScopeChangedEventArgs e)
    {
        if (!e.DefinitionChanged)
        {
            return;
        }

        threadHandling.RunOnBackgroundThread(() =>
        {
            NotifySlCoreFilesOpened(activeConfigScopeTracker.Current, documentTracker.GetOpenDocuments().ToArray());
        }).Forget();
    }

    public IInitializationProcessor InitializationProcessor { get; }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (InitializationProcessor.IsFinalized)
        {
            activeConfigScopeTracker.CurrentConfigurationScopeChanged -= ActiveConfigScopeTracker_CurrentConfigurationScopeChanged;
            activeCompilationDatabaseTracker.DatabaseChanged -= ActiveCompilationDatabaseTracker_DatabaseChanged;
            documentTracker.DocumentOpened -= OnDocumentOpened;
            documentTracker.DocumentClosed -= OnDocumentClosed;
            documentTracker.DocumentSaved -= OnDocumentSaved;
            documentTracker.OpenDocumentRenamed -= OnOpenDocumentRenamed;
        }
    }

    private void OnOpenDocumentRenamed(object sender, DocumentRenamedEventArgs args) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            if (ShouldUpdateVcxCompilationDatabase(args.Document.DetectedLanguages))
            {
                await vcxCompilationDatabaseUpdater.RenameFileAsync(args.OldFilePath, args.Document.FullPath);
            }

            var currentConfigurationScope = activeConfigScopeTracker.Current;
            NotifySlCoreFileClosed(args.OldFilePath, currentConfigurationScope);
            NotifySlCoreFilesOpened(currentConfigurationScope, args.Document);
        }).Forget();

    private void OnDocumentClosed(object sender, DocumentEventArgs args) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            if (ShouldUpdateVcxCompilationDatabase(args.Document.DetectedLanguages))
            {
                await vcxCompilationDatabaseUpdater.RemoveFileAsync(args.Document.FullPath);
            }

            NotifySlCoreFileClosed(args.Document.FullPath, activeConfigScopeTracker.Current);
        }).Forget();

    private void OnDocumentOpened(object sender, DocumentOpenedEventArgs args) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            await AddFilesToCompilationDatabaseAsync(args.Document);

            NotifySlCoreFilesOpened(activeConfigScopeTracker.Current, args.Document);
        }).Forget();

    /// <summary>
    /// Due to the fact that we can't react to project/file properties changes, regenerating the compilation database entry on file save is needed
    /// Additionally, this is a workaround that can deal with the renaming bug described in https://sonarsource.atlassian.net/browse/SLVS-2170
    /// </summary>
    private void OnDocumentSaved(object sender, DocumentSavedEventArgs args) =>
        threadHandling.RunOnBackgroundThread(async () =>
        {
            await AddFilesToCompilationDatabaseAsync(args.Document);
        }).Forget();

    private async Task AddFilesToCompilationDatabaseAsync(params Document[] documents)
    {
        foreach (var document in documents.Where(document => ShouldUpdateVcxCompilationDatabase(document.DetectedLanguages)))
        {
            await vcxCompilationDatabaseUpdater.AddFileAsync(document.FullPath);
        }
    }

    private void NotifySlCoreFilesOpened(ConfigurationScope configurationScope, params Document[] openDocuments)
    {
        if (VerifyConfigurationScopeInitialized(configurationScope) && GetFileRpcSlCoreServiceOrNull() is { } fileRpcSlCoreService)
        {
            foreach (var openDocument in openDocuments)
            {
                fileRpcSlCoreService.DidOpenFile(new(configurationScope.Id, new FileUri(openDocument.FullPath)));
            }
        }
    }

    private void NotifySlCoreFileClosed(string filePath, ConfigurationScope currentConfigurationScope)
    {
        if (VerifyConfigurationScopeInitialized(currentConfigurationScope))
        {
            GetFileRpcSlCoreServiceOrNull()?.DidCloseFile(new DidCloseFileParams(currentConfigurationScope.Id, new FileUri(filePath)));
        }
    }

    private IFileRpcSLCoreService GetFileRpcSlCoreServiceOrNull()
    {
        if (!serviceProvider.TryGetTransientService(out IFileRpcSLCoreService service))
        {
            logger.LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
        }
        return service;
    }

    private bool VerifyConfigurationScopeInitialized(ConfigurationScope currentConfigurationScope)
    {
        if (currentConfigurationScope?.Id is not null)
        {
            return true;
        }
        logger.LogVerbose(SLCoreStrings.ConfigScopeNotInitialized);
        return false;
    }

    private bool ShouldUpdateVcxCompilationDatabase(IEnumerable<AnalysisLanguage> fileLanguages) =>
        fileLanguages.Contains(AnalysisLanguage.CFamily) && activeCompilationDatabaseTracker.CurrentDatabase?.DatabaseType == CompilationDatabaseType.VCX;
}
