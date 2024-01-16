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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode.Migration.FileProviders
{
    [Export(typeof(IFileProvider))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal partial class XmlFileProvider : IFileProvider
    {
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IRoslynProjectProvider projectProvider;
        private readonly ILogger logger;    
        private readonly IThreadHandling threadHandling;
        private readonly IFileFinder fileFinder;

        // We only search for non-project files - we get the Roslyn project file paths
        // from the VSWorkspace.
        // Corner case: this means that we won't clean projects that are in the solution
        // but unloaded, since they won't appear in the VsWorkspace
        internal static readonly string[] FileSearchPatterns = new string[] {
            "*.ruleset",
            "*.props",
            "*.targets"
        };

        [ImportingConstructor]
        public XmlFileProvider(
            ISolutionInfoProvider solutionInfoProvider,
            IRoslynProjectProvider projectProvider,
            ILogger logger,
            IThreadHandling threadHandling)
            : this(solutionInfoProvider, projectProvider, logger, threadHandling, new FileFinder(logger))
        {
        }

        internal /* for testing */ XmlFileProvider(
            ISolutionInfoProvider solutionInfoProvider,
            IRoslynProjectProvider projectProvider,
            ILogger logger,
            IThreadHandling threadHandling,
            IFileFinder fileFinder)
        {
            this.solutionInfoProvider = solutionInfoProvider;
            this.projectProvider = projectProvider;
            this.logger = logger;
            this.threadHandling = threadHandling;
            this.fileFinder = fileFinder;
        }

        public async Task<IEnumerable<string>> GetFilesAsync(CancellationToken token)
        {
            var roslynProjectFiles = GetRoslynProjectFilePaths();
            // If there are no Roslyn projects then there is nothing to clean up
            if (roslynProjectFiles.Length == 0)
            {
                logger.LogMigrationVerbose("No Roslyn projects: nothing to clean");
                return Enumerable.Empty<string>();
            }

            var allFiles = new HashSet<string>(roslynProjectFiles, StringComparer.OrdinalIgnoreCase);

            // Make sure we are doing the disk search on a background thread
            await threadHandling.SwitchToBackgroundThread();

            await AddFilesUnderSolutionFolderAsync(allFiles);
            AddFilesInProjectFolders(allFiles, roslynProjectFiles);

            return allFiles.ToList();
        }

        private string[] GetRoslynProjectFilePaths()
        {
            var roslynProjects = projectProvider.Get();
            return roslynProjects.Where(x => x.FilePath != null).Select(x => x.FilePath).ToArray();
        }

        private async Task AddFilesUnderSolutionFolderAsync(HashSet<string> allFiles)
        {
            var solutionDir = await solutionInfoProvider.GetSolutionDirectoryAsync();
            if (solutionDir == null)
            {
                logger.LogMigrationVerbose("Unable to locate the solution folder.");
                return;
            }

            var files = fileFinder.GetFiles(solutionDir, SearchOption.AllDirectories, FileSearchPatterns);
            AddToResults(allFiles, files);
        }

        private void AddFilesInProjectFolders(HashSet<string> allFiles, string[] projectFilePaths)
        {
            foreach(var projectFilePath in projectFilePaths)
            {
                var projectDirectorynfo = Directory.GetParent(projectFilePath);

                // Only search the top-level of project directory.
                // If the project directory is under the solution folder then this will find duplicates
                // which will be filtered out.
                var files = fileFinder.GetFiles(projectDirectorynfo.FullName, SearchOption.TopDirectoryOnly, FileSearchPatterns);
                AddToResults(allFiles, files);
            }
        }

        private static void AddToResults(HashSet<string> results, IEnumerable<string> newFiles)
        {
            foreach(var newFile in newFiles)
            {
                results.Add(newFile);
            }
        }
    }
}
