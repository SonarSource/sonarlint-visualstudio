﻿/*
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
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.File;

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

    public void AddFiles(params SourceFile[] addedFiles)
    {
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFilesChanged([], addedFiles)).Forget();
    }

    public void RemoveFiles(params string[] removedFiles)
    {
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFilesChanged(removedFiles, [])).Forget();
    }

    public void RenameFiles(string[] beforeRenameFiles, SourceFile[] afterRenameFiles)
    {
        threadHandling.RunOnBackgroundThread(() => NotifySlCoreFilesChanged(beforeRenameFiles, afterRenameFiles))
            .Forget();
    }

    private void NotifySlCoreFilesChanged(string[] removedFiles, SourceFile[] addedFiles)
    {
        if (serviceProvider.TryGetTransientService(out IFileRpcSLCoreService fileRpcSlCoreService) && activeConfigScopeTracker.Current is {} configScope) 
        {
            var clientFiles = addedFiles.Select(sourceFile => clientFileDtoFactory.Create(configScope.Id, configScope.RootPath, sourceFile)).ToList();
            var removedFileUris = removedFiles.Select(f => new FileUri(f)).ToList();

            fileRpcSlCoreService.DidUpdateFileSystem(new DidUpdateFileSystemParams(
                removedFileUris, clientFiles));
        }
        else
        {
            logger.WriteLine("[{0}] {1}", nameof(FileTracker), SLCoreStrings.ServiceProviderNotInitialized);
        }
    }
}
