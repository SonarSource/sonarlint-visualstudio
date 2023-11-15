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
using System.IO;
using System.IO.Abstractions;

namespace SonarLint.VisualStudio.Core
{
    /// <summary>
    /// Raises events for changes to a single git repo
    /// </summary>
    public interface IGitEvents
    {
        /// <summary>
        /// Raised when the current head changes
        /// </summary>
        event EventHandler HeadChanged;
    }

    public sealed class GitEventsMonitor : IGitEvents, IDisposable
    {
        public event EventHandler HeadChanged;
        private IFileSystemWatcher fileSystemWatcher;
        private bool disposed = false;

        private const string GitFolder = ".git";
        private const string HEADFile = "HEAD";

        public GitEventsMonitor(string repoFolder) : this(repoFolder, new FileSystemWatcherFactory())
        {
        }

        internal GitEventsMonitor(string repoFolder, IFileSystemWatcherFactory fileSystemFactory)
        {
            WatchGitEvents(repoFolder, fileSystemFactory);
        }

        private void WatchGitEvents(string repoFolder, IFileSystemWatcherFactory fileSystemFactory)
        {
            if (repoFolder == null) { return; }

            var gitFolderPath = Path.Combine(repoFolder, GitFolder);

            fileSystemWatcher = fileSystemFactory.FromPath(gitFolderPath);

            fileSystemWatcher.Filter = HEADFile;
            //When you change head Git does not change existing head file instead it creates a temp file first and renames it to "head" after deleting the original file.
            //Because of that we are listening the "renamed" event instead of "changed"
            fileSystemWatcher.Renamed += HeadFileChanged;

            fileSystemWatcher.EnableRaisingEvents = true;
        }

        private void HeadFileChanged(object sender, FileSystemEventArgs e)
        {
            HeadChanged?.BeginInvoke(this, EventArgs.Empty, null, null);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    fileSystemWatcher?.Dispose();
                }

                disposed = true;
            }
        }
    }
}
