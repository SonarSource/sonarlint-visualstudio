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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Branch;

namespace SonarLint.VisualStudio.ConnectedMode
{
    [Export(typeof(IStatefulServerBranchProvider))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class StatefulServerBranchProvider : IStatefulServerBranchProvider, IDisposable
    {
        private readonly IServerBranchProvider serverBranchProvider;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IActiveConfigScopeTracker activeConfigScopeTracker;
        private readonly ISLCoreServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly IThreadHandling threadHandling;
        private bool disposedValue;
        private string selectedBranch;
        private readonly IAsyncLock asyncLock;

        [ImportingConstructor]
        public StatefulServerBranchProvider(
            IServerBranchProvider serverBranchProvider,
            IActiveSolutionBoundTracker activeSolutionBoundTracker,
            IActiveConfigScopeTracker activeConfigScopeTracker,
            ISLCoreServiceProvider serviceProvider,
            ILogger logger,
            IThreadHandling threadHandling,
            IAsyncLockFactory asyncLockFactory)
        {
            this.serverBranchProvider = serverBranchProvider;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.activeConfigScopeTracker = activeConfigScopeTracker;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.threadHandling = threadHandling;
            asyncLock = asyncLockFactory.Create();

            activeSolutionBoundTracker.PreSolutionBindingUpdated += OnPreSolutionBindingUpdated;
            activeSolutionBoundTracker.PreSolutionBindingChanged += OnPreSolutionBindingChanged;
        }

        private void OnPreSolutionBindingUpdated(object sender, EventArgs e)
        {
            logger.LogVerbose(Resources.StatefulBranchProvider_BindingUpdated);
            SafeClearSelectedBranchCache();

            NotifySlCoreBranchChange();
        }

        private void SafeClearSelectedBranchCache()
        {
            using (asyncLock.Acquire())
            {
                selectedBranch = null;
            }
        }

        private void OnPreSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs e)
        {
            logger.LogVerbose(Resources.StatefulBranchProvider_BindingChanged);
            SafeClearSelectedBranchCache();

            if (e.Configuration.Mode.IsInAConnectedMode())
            {
                NotifySlCoreBranchChange();
            }
        }

        private void NotifySlCoreBranchChange() =>
            threadHandling.RunOnBackgroundThread(() =>
            {
                if (!serviceProvider.TryGetTransientService(out ISonarProjectBranchSlCoreService sonarProjectBranchSlCoreService))
                {
                    logger.LogVerbose(SLCoreStrings.ServiceProviderNotInitialized);
                    return;
                }
                if (activeConfigScopeTracker.Current == null)
                {
                    return;
                }
                sonarProjectBranchSlCoreService.DidVcsRepositoryChange(new DidVcsRepositoryChangeParams(activeConfigScopeTracker.Current.Id));
            }).Forget();

        public async Task<string> GetServerBranchNameAsync(CancellationToken token)
        {
            using (await asyncLock.AcquireAsync())
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
            }

            logger.WriteLine(Resources.StatefulBranchProvider_ReturnValue, selectedBranch ?? Resources.NullBranchName);
            return selectedBranch;
        }

        private Task<string> DoGetServerBranchNameAsync(CancellationToken token) => threadHandling.RunOnBackgroundThread(() => serverBranchProvider.GetServerBranchNameAsync(token));

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
