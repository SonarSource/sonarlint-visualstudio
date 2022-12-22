/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Raises events for changes to the git repo
    /// </summary>
    public interface IGitEvents
    {
        /// <summary>
        /// Raised when the current head changes
        /// </summary>
        event EventHandler HeadChanged;
    }

    public class GitEventsMonitor : IGitEvents
    {
        public event EventHandler HeadChanged;
        private IFileSystemWatcher fileSystemWatcher;

        private const string GitFolder = ".git";
        private const string HEADFile = "HEAD";


        public GitEventsMonitor(IGitWorkspaceService gitWorkspaceService) : this(gitWorkspaceService, new FileSystemWatcherFactory())
        {

        }

        internal GitEventsMonitor(IGitWorkspaceService gitWorkspaceService, IFileSystemWatcherFactory fileSystemFactory)
        {
            WatchGitEvents(gitWorkspaceService, fileSystemFactory);
        }

        private void WatchGitEvents(IGitWorkspaceService gitWorkspaceService, IFileSystemWatcherFactory fileSystemFactory)
        {
            var repoFolder = gitWorkspaceService.GetRepoRoot();
            
            if(repoFolder == null) { return; }

            var gitFolderPath = Path.Combine(repoFolder, GitFolder);

            fileSystemWatcher = fileSystemFactory.FromPath(gitFolderPath);

            fileSystemWatcher.Filter = HEADFile;
            
            fileSystemWatcher.Changed += HeadFileChanged;

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void HeadFileChanged(object sender, FileSystemEventArgs e)
        {
            HeadChanged.Invoke(this, EventArgs.Empty);
        }
    }
}
