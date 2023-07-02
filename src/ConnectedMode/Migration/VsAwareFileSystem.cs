/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.ComponentModel.Composition;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IVsAwareFileSystem))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class VsAwareFileSystem : IVsAwareFileSystem
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IFileSystem fileSystem;
        private readonly IThreadHandling threadHandling;
        private IVsQueryEditQuerySave2 queryFileOperation;

        [ImportingConstructor]
        public VsAwareFileSystem([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ILogger logger,
            IThreadHandling threadHandling)
            : this(serviceProvider, logger, threadHandling, new FileSystem())
        { }

        internal /* for testing */ VsAwareFileSystem(IServiceProvider serviceProvider,
            ILogger logger,
            IThreadHandling threadHandling,
            IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.fileSystem = fileSystem;
            this.threadHandling = threadHandling;
        }

        public Task DeleteFolderAsync(string folderPath)
        {
            // TODO - error handling
            logger.LogMigrationVerbose($"Deleting directory: {folderPath}");
            fileSystem.Directory.Delete(folderPath, true);
            return Task.CompletedTask;
        }

        public Task<string> LoadAsTextAsync(string filePath)
        {
            // TODO - error handling
            logger.LogMigrationVerbose($"Reading file: {filePath}");
            var content = fileSystem.File.ReadAllText(filePath);
            return Task.FromResult(content);
        }

        public Task SaveAsync(string filePath, string text)
        {
            // TODO - error handling
            logger.LogMigrationVerbose($"Saving file: {filePath}");
            fileSystem.File.WriteAllText(filePath, text);
            return Task.CompletedTask;
        }

        public async Task BeginChangeBatchAsync()
        {
            logger.LogMigrationVerbose("Beginning batch of file changes...");

            queryFileOperation = await GetSccServiceAsync();
        }

        public Task EndChangeBatchAsync()
        {
            logger.LogMigrationVerbose("File changes complete");
            return Task.CompletedTask;
        }

        private async Task<IVsQueryEditQuerySave2> GetSccServiceAsync()
        {
            IVsQueryEditQuerySave2 sccService = null;
            await threadHandling.RunOnUIThread( () =>
            {
                sccService = serviceProvider.GetService(typeof(SVsQueryEditQuerySave)) as IVsQueryEditQuerySave2;
            });

            return sccService;
        }
    }
}
