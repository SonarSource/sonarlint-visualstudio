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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Migration
{
    [Export(typeof(IFileProvider))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class MSBuildFileProvider : IFileProvider
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private readonly IFileSystem fileSystem;

        internal const string FileSearchPattern = "*.ruleset,*.props,*.targets,*.csproj,*.vbpproj";

        [ImportingConstructor]
        public MSBuildFileProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ILogger logger,
            IThreadHandling threadHandling)
            : this(serviceProvider, logger, threadHandling, new FileSystem())
        {
        }

        internal /* for testing */ MSBuildFileProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ILogger logger,
            IThreadHandling threadHandling,
            IFileSystem fileSystem)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.threadHandling = threadHandling;
            this.fileSystem = fileSystem;
        }

        public async Task<IEnumerable<string>> GetFilesAsync(CancellationToken token)
        {
            var solutionDir = await GetSolutionDirectoryAsync();
            if (solutionDir == null)
            {
                LogVerbose("Unable to locate the solution folder.");
                return Enumerable.Empty<string>();
            }

            return FindFiles(solutionDir);
        }

        private async Task<string> GetSolutionDirectoryAsync()
        {
            string solutionDir = null;

            await threadHandling.RunOnUIThread(() => {
                var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
                // If there isn't an open solution the returned hresult will indicate an error
                // and the returned solution name will be null. We'll just ignore the hresult.
                solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionDirectory, out var solutionDirAsObject);

                solutionDir = solutionDirAsObject as string;

            });
            return solutionDir;
        }

        private string[] FindFiles(string rootFolder)
        {
            LogVerbose("Searching for files... Root directory: " + rootFolder);
            var files = fileSystem.Directory.GetFiles(rootFolder, FileSearchPattern, SearchOption.AllDirectories);
            LogVerbose("Number of matching files: " + files.Length);
            return files;
        }

        private void LogVerbose(string text) => logger.LogVerbose("[Migration] " + text);
    }
}
