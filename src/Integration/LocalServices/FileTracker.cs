/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.File;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.Integration.LocalServices;

[Export(typeof(IFileTracker))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class FileTracker : IFileTracker
{
    private readonly ISLCoreServiceProvider serviceProvider;
    private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IThreadHandling threadHandling;
    private readonly IClientFileDtoFactory clientFileDtoFactory;
    private readonly ILogger logger;

    [ImportingConstructor]
    public FileTracker(ISLCoreServiceProvider serviceProvider, IActiveConfigScopeTracker activeConfigScopeTracker,
        IThreadHandling threadHandling, IClientFileDtoFactory clientFileDtoFactory, ILogger logger)
    {
        this.serviceProvider = serviceProvider;
        this.activeConfigScopeTracker = activeConfigScopeTracker;
        this.threadHandling = threadHandling;
        this.clientFileDtoFactory = clientFileDtoFactory;
        this.logger = logger;
    }

    public void AddFiles(params string[] addedFiles)
    {
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFilesChangedAsync([], addedFiles)).Forget();
    }

    public void RemoveFiles(params string[] removedFiles)
    {
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFilesChangedAsync(removedFiles, [])).Forget();
    }

    public void RenameFiles(string[] beforeRenameFiles, string[] afterRenameFiles)
    {
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFilesChangedAsync(beforeRenameFiles, afterRenameFiles))
            .Forget();
    }

    private Task NotifySlCoreFilesChangedAsync(string[] removedFiles, string[] addedFiles)
    {
        if (serviceProvider.TryGetTransientService(out IFileRpcSLCoreService fileRpcSlCoreService))
        {
            var rootPath = activeConfigScopeTracker.Current.RootPath;
            var configScopeId = activeConfigScopeTracker.Current.Id;
            var clientFiles = addedFiles.Select(fp => clientFileDtoFactory.Create(fp, configScopeId, rootPath)).ToList();
            var removedFileUris = removedFiles.Select(f => new FileUri(f)).ToList();

            fileRpcSlCoreService.DidUpdateFileSystem(new DidUpdateFileSystemParams(
                removedFileUris, clientFiles));
        }
        else
        {
            logger.WriteLine(Strings.FileTrackerGetRpcServiceError);
        }

        return Task.CompletedTask;
    }
}
