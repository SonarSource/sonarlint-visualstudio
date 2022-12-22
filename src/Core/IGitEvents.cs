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
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core.Binding;

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

    /// <summary>
    /// Low-level Git repo monitoring class.
    /// Monitors the repo it is given and raises events when interesting changes occur.
    /// Knowns nothing about binding.
    /// </summary>
    internal class GitRepoMonitor : IGitEvents
    {
        public event EventHandler HeadChanged;

        private readonly FileSystemWatcher fileSystemWatcher;

        public GitRepoMonitor(string repoRootPath)
        {
            // Create the file system watcher
        }
    }

    /// <summary>
    /// Higher-level class that only raises Git events for bound solutions
    /// </summary>
    [Export(typeof(IGitEvents))]
    [Export(typeof(IBoundSolutionObserver))]
    // Singleton - stateful.
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class BoundSolutionGitMonitor : IGitEvents, IBoundSolutionObserver, IDisposable
    {
        /// <summary>
        /// Factory to create a new object that will monitor the local git repo
        /// for relevant changes
        /// </summary>
        internal /* for testing */ delegate IGitEvents GitEventFactory(string repoPathRoot);

        public event EventHandler HeadChanged;

        private readonly IGitWorkspaceService gitWorkspaceService;
        private readonly GitEventFactory createLocalGitMonitor;

        private IGitEvents currentRepoEvents;
        private bool disposedValue;

        [ImportingConstructor]
        public BoundSolutionGitMonitor(IGitWorkspaceService gitWorkspaceService)
            : this(gitWorkspaceService, CreateGitEvents)
        {
        }

        internal /* for testing */ BoundSolutionGitMonitor(IGitWorkspaceService gitWorkspaceService,
            GitEventFactory gitEventFactory)
        {
            this.gitWorkspaceService = gitWorkspaceService;
            createLocalGitMonitor = gitEventFactory;
        }

        private static IGitEvents CreateGitEvents(string repoRootPath) => new GitRepoMonitor(repoRootPath);

        public Task OnSolutionBindingChangedAsync()
        {
            Refresh();
            return Task.CompletedTask;
        }

        private void OnHeadChanged(object sender, EventArgs e)
        {
            // Forward the event
            HeadChanged?.Invoke(this, e);
        }

        private void Refresh()
        {
            CleanupLocalGitEventResources();

            var rootPath = gitWorkspaceService.GetRepoRoot();

            if (rootPath != null)
            {
                currentRepoEvents = createLocalGitMonitor(rootPath);
                currentRepoEvents.HeadChanged += OnHeadChanged;
            }
        }

        private void CleanupLocalGitEventResources()
        {
            if (currentRepoEvents != null)
            {
                currentRepoEvents.HeadChanged -= OnHeadChanged;
                (currentRepoEvents as IDisposable)?.Dispose();
            }
            currentRepoEvents = null;
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CleanupLocalGitEventResources();
                }

                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion // IDisposable
    }
}
