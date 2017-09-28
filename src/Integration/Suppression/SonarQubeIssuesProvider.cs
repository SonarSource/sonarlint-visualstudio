/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Suppression
{
    [Export(typeof(ISonarQubeIssuesProvider))]
    public sealed class SonarQubeIssuesProvider : ISonarQubeIssuesProvider, IDisposable
    {
        private const double MillisecondsToWaitBetweenRefresh = 1000 * 60 * 60 * 1; // 1 hour

        private readonly IHost host;
        private readonly System.Timers.Timer refreshTimer;
        private readonly IActiveSolutionBoundTracker solutionBoundTacker;

        private IList<SonarQubeIssue> cachedSuppressedIssues;
        private bool isDisposed;
        private CancellationTokenSource cancellationTokenSource;

        [ImportingConstructor]
        internal SonarQubeIssuesProvider(IHost host, IActiveSolutionBoundTracker solutionBoundTacker)
        {
            // Ideally we would have the bound project as part of IActiveSolutionBoundTracker.SolutionBindingChanged event
            // args and we could get rid of the dependency to IHost here.
            this.host = host;
            this.solutionBoundTacker = solutionBoundTacker;
            this.solutionBoundTacker.SolutionBindingChanged += OnSolutionBoundChanged;

            // TODO: Use mockable timer
            refreshTimer = new System.Timers.Timer { AutoReset = true, Interval = MillisecondsToWaitBetweenRefresh };
            refreshTimer.Elapsed += OnRefreshTimerElapsed;

            if (this.solutionBoundTacker.IsActiveSolutionBound)
            {
                SynchronizeSuppressedIssues();
                refreshTimer.Start();
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            refreshTimer.Dispose();
            this.solutionBoundTacker.SolutionBindingChanged -= OnSolutionBoundChanged;
            this.isDisposed = true;
        }

        public IEnumerable<SonarQubeIssue> GetSuppressedIssues(string projectGuid, string filePath)
        {
            // TODO: Block the call while the cache is being built + handle multi-threading
            return this.cachedSuppressedIssues.Where(x => x.FilePath == filePath && x.ModuleKey == BuildModuleKey(projectGuid));
        }

        private string BuildModuleKey(string projectGuid)
        {
            var sonarQubeProjectKey = this.host.VisualStateManager.BoundProjectKey;
            return $"{sonarQubeProjectKey}:{sonarQubeProjectKey}:{projectGuid}";
        }

        private async void OnRefreshTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await SynchronizeSuppressedIssues();
        }

        private async void OnSolutionBoundChanged(object sender, bool e)
        {
            if (solutionBoundTacker.IsActiveSolutionBound)
            {
                await SynchronizeSuppressedIssues();
                refreshTimer.Start();
            }
            else
            {
                refreshTimer.Stop();
            }
        }

        private async Task SynchronizeSuppressedIssues()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            // TODO: Handle race conditions
            this.cachedSuppressedIssues = await this.host.SonarQubeService.GetSuppressedIssuesAsync(
                this.host.VisualStateManager.BoundProjectKey, cancellationTokenSource.Token);
        }
    }
}
