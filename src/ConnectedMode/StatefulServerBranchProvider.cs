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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode
{
    [Export(typeof(IStatefulServerBranchProvider))]
    // note: this class does not seem to be safe from concurrent updates
    internal sealed class StatefulServerBranchProvider : IStatefulServerBranchProvider, IDisposable
    {
        private readonly IServerBranchProvider serverBranchProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private bool disposedValue;
        private string selectedBranch;

        [ImportingConstructor]
        public StatefulServerBranchProvider(
            IServerBranchProvider serverBranchProvider,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            ILogger logger,
            IThreadHandling threadHandling)
        {
            this.serverBranchProvider = serverBranchProvider;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.logger = logger;
            this.threadHandling = threadHandling;

            activeSolutionBoundTracker.PreSolutionBindingChanged += OnPreSolutionBindingChanged;
            activeSolutionBoundTracker.PreSolutionBindingUpdated += OnPreSolutionBindingUpdated;
        }

        private void OnPreSolutionBindingUpdated(object sender, EventArgs e)
        {
            logger.LogVerbose(Resources.StatefulBranchProvider_BindingUpdated);
            selectedBranch = null;
        }

        private void OnPreSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            logger.LogVerbose(Resources.StatefulBranchProvider_BindingChanged);
            selectedBranch = null;
        }

        public async Task<string> GetServerBranchNameAsync(CancellationToken token)
        {
            if (selectedBranch == null)
            {
                logger.LogVerbose(Resources.StatefulBranchProvider_CacheMiss);

                // Note: we're using null to indicate that a refresh is required.
                // However, the serverBranchProvider will return null in some cases e.g. standalone mode, not a git repo.
                // In these cases we expect the serverBranchProvider to return quickly so the impact of making unnecessary
                // calls is not significant.
                selectedBranch = await DoGetServerBranchNameAsync(token);
            }
            else
            {
                logger.LogVerbose(Resources.StatefulBranchProvider_CacheHit);
            }

            logger.WriteLine(Resources.StatefulBranchProvider_ReturnValue, selectedBranch ?? Resources.NullBranchName);
            return selectedBranch;
        }

        private Task<string> DoGetServerBranchNameAsync(CancellationToken token)
        {
            return threadHandling.RunOnBackgroundThread(() =>
            {
                return serverBranchProvider.GetServerBranchNameAsync(token);
            });
        }

        #region IDisposable

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    activeSolutionBoundTracker.PreSolutionBindingChanged -= OnPreSolutionBindingChanged;
                    activeSolutionBoundTracker.PreSolutionBindingUpdated -= OnPreSolutionBindingUpdated;
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

        #endregion IDisposable
    }
}
