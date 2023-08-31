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

using System.ComponentModel.Composition;
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Core
{
    public interface IGitWorkspaceService
    {
        /// <summary>
        /// Returns the file path to the current Git repository root
        /// Or null if not a git repo
        /// </summary>
        string GetRepoRoot();
    }

    [Export(typeof(IGitWorkspaceService))]
    internal class GitWorkSpaceService : IGitWorkspaceService
    {
        private readonly ILogger logger;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IFileSystem fileSystem;

        private const string gitFolder = ".git";

        [ImportingConstructor]
        public GitWorkSpaceService(ISolutionInfoProvider solutionInfoProvider, ILogger logger) : this(solutionInfoProvider, logger, new FileSystem())
        { }

        internal GitWorkSpaceService(ISolutionInfoProvider solutionInfoProvider, ILogger logger, IFileSystem fileSystem)
        {
            this.solutionInfoProvider = solutionInfoProvider;
            this.logger = logger;
            this.fileSystem = fileSystem;
        }

        public string GetRepoRoot()
        {
            var workspaceRoot = solutionInfoProvider.GetSolutionDirectory();
            string gitRoot;

            if (workspaceRoot == null)
            {
                return null;
            }

            var currentDir = new DirectoryInfo(workspaceRoot);

            do
            {
                gitRoot = Path.Combine(currentDir.FullName, gitFolder);

                if (fileSystem.Directory.Exists(gitRoot))
                {
                    return currentDir.FullName;
                }

                currentDir = currentDir.Parent;
            }
            while (currentDir != null && fileSystem.Directory.Exists(currentDir.FullName));

            logger.WriteLine(CoreStrings.NoGitFolder);
            return null;
        }
    }
}
