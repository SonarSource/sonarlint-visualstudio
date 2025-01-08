/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core.ConfigurationScope;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Taint
{
    /// <summary>
    /// Listens to binding changes and triggers fetching of taint vulnerabilities from the connected server.
    /// Doesn't do initial sync - only triggers the fetch when the binding changes.
    /// </summary>
    internal interface ITaintIssuesBindingMonitor : IDisposable;

    [Export(typeof(ITaintIssuesBindingMonitor))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaintIssuesConfigurationScopeMonitor : ITaintIssuesBindingMonitor
    {
        private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
        private readonly ITaintIssuesSynchronizer taintIssuesSynchronizer;

        [ImportingConstructor]
        public TaintIssuesConfigurationScopeMonitor(IActiveConfigScopeTracker activeConfigScopeTracker,
            ITaintIssuesSynchronizer taintIssuesSynchronizer)
        {
            this.activeConfigScopeTracker = activeConfigScopeTracker;
            this.taintIssuesSynchronizer = taintIssuesSynchronizer;

            this.activeConfigScopeTracker.CurrentConfigurationScopeChanged += ActiveConfigScopeTrackerOnCurrentConfigurationScopeChanged;
        }

        private void ActiveConfigScopeTrackerOnCurrentConfigurationScopeChanged(object sender, EventArgs e) =>
            taintIssuesSynchronizer.UpdateTaintVulnerabilitiesAsync(activeConfigScopeTracker.Current).Forget();

        public void Dispose() =>
            activeConfigScopeTracker.CurrentConfigurationScopeChanged -= ActiveConfigScopeTrackerOnCurrentConfigurationScopeChanged;
    }
}
