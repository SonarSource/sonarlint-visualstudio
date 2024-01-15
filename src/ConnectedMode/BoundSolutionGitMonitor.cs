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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode
{
    /// <summary>
    /// Higher-level class that raises Git events for bound solutions
    /// </summary>
    /// <remarks>
    /// This class handles changing the repo being monitored when a solution is opened/closed
    /// </remarks>
    [Export(typeof(IBoundSolutionGitMonitor))]
    // Singleton - stateful.
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class BoundSolutionGitMonitor : IBoundSolutionGitMonitor, IDisposable
    {
        public event EventHandler HeadChanged;

        /// <summary>
        /// Factory to create a new object that will monitor the local git repo
        /// for relevant changes
        /// </summary>
        internal /* for testing */ delegate IGitEvents GitEventFactory(string repoPathRoot);

        private readonly IGitWorkspaceService gitWorkspaceService;
        private readonly GitEventFactory createLocalGitMonitor;
        private readonly ILogger logger;

        private IGitEvents currentRepoEvents;
        private bool disposedValue;

        [ImportingConstructor]
        public BoundSolutionGitMonitor(IGitWorkspaceService gitWorkspaceService, ILogger logger)
            : this(gitWorkspaceService, logger, CreateGitEvents)
        {
        }

        internal /* for testing */ BoundSolutionGitMonitor(IGitWorkspaceService gitWorkspaceService,
            ILogger logger,
            GitEventFactory gitEventFactory)
        {
            this.gitWorkspaceService = gitWorkspaceService;
            this.logger = logger;
            createLocalGitMonitor = gitEventFactory;

            Refresh();
        }

        private static IGitEvents CreateGitEvents(string repoRootPath) => new GitEventsMonitor(repoRootPath);

        private void OnHeadChanged(object sender, EventArgs e)
        {
            // Forward the notification that the local repo head has changed
            try
            {
                logger.LogVerbose(Resources.GitMonitor_GitBranchChanged);
                HeadChanged?.Invoke(this, e);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.GitMonitor_EventError, ex);
            }
        }

        public void Refresh()
        {
            CleanupLocalGitEventResources();

            var rootPath = gitWorkspaceService.GetRepoRoot();

            if (rootPath == null)
            {
                logger.LogVerbose(Resources.GitMonitor_NoRepo);
            }
            else
            {
                logger.LogVerbose(Resources.GitMonitor_MonitoringRepoStarted, rootPath);
                // Avoid one potential race condition - initialize first, then set
                // the class-level variable.
                var local = createLocalGitMonitor(rootPath);
                local.HeadChanged += OnHeadChanged;

                currentRepoEvents = local;
            }
        }

        private void CleanupLocalGitEventResources()
        {
            // Avoid potential race condition - copy the variable
            // and clean up the copy
            var local = currentRepoEvents;

            if (local != null)
            {
                logger.LogVerbose(Resources.GitMonitor_MonitoringRepoStopped);
                local.HeadChanged -= OnHeadChanged;
                (local as IDisposable)?.Dispose();
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
