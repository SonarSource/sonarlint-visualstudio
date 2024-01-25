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
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    /// <summary>
    /// Listens to binding changes and triggers fetching of taint vulnerabilities from the connected server.
    /// Doesn't do initial sync - only triggers the fetch when the binding changes.
    /// </summary>
    internal interface ITaintIssuesBindingMonitor : IDisposable
    {
    }

    [Export(typeof(ITaintIssuesBindingMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintIssuesBindingMonitor : ITaintIssuesBindingMonitor
    {
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IFolderWorkspaceMonitor folderWorkspaceMonitor;
        private readonly ITaintIssuesSynchronizer taintIssuesSynchronizer;

        [ImportingConstructor]
        public TaintIssuesBindingMonitor(IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IFolderWorkspaceMonitor folderWorkspaceMonitor,
            ITaintIssuesSynchronizer taintIssuesSynchronizer)
        {
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.folderWorkspaceMonitor = folderWorkspaceMonitor;
            this.taintIssuesSynchronizer = taintIssuesSynchronizer;

            folderWorkspaceMonitor.FolderWorkspaceInitialized += FolderWorkspaceInitializedEvent_FolderWorkspaceInitialized;
            activeSolutionBoundTracker.SolutionBindingChanged += ActiveSolutionBoundTracker_SolutionBindingChanged;
            activeSolutionBoundTracker.SolutionBindingUpdated += ActiveSolutionBoundTracker_SolutionBindingUpdated;
        }

        private async void FolderWorkspaceInitializedEvent_FolderWorkspaceInitialized(object sender, EventArgs e)
        {
            await Sync();
        }

        private async void ActiveSolutionBoundTracker_SolutionBindingUpdated(object sender, EventArgs e)
        {
            await Sync();
        }

        private async void ActiveSolutionBoundTracker_SolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            await Sync();
        }

        private async Task Sync()
        {
            await taintIssuesSynchronizer.SynchronizeWithServer();
        }

        public void Dispose()
        {
            folderWorkspaceMonitor.FolderWorkspaceInitialized -= FolderWorkspaceInitializedEvent_FolderWorkspaceInitialized;
            activeSolutionBoundTracker.SolutionBindingChanged -= ActiveSolutionBoundTracker_SolutionBindingChanged;
            activeSolutionBoundTracker.SolutionBindingUpdated -= ActiveSolutionBoundTracker_SolutionBindingUpdated;
        }
    }
}
