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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.ConnectedMode.Install;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Issue;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.QualityProfile;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.ConnectedMode
{
    [ExcludeFromCodeCoverage]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(BoundSolutionUIContext.GuidString, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid("dd3427e0-7bb2-4a51-b00a-ddae2c32c7ef")]
    public sealed class ConnectedModePackage : AsyncPackage
    {
        private SSESessionManager sseSessionManager;
        private IIssueServerEventsListener issueServerEventsListener;
        private IQualityProfileServerEventsListener qualityProfileServerEventsListener;
        private ServerSuppressionsChangedHandler serverSuppressionsChangedHandler;
        private BoundSolutionUpdateHandler boundSolutionUpdateHandler;
        private TimedUpdateHandler timedUpdateHandler;
        private LocalSuppressionsChangedHandler localSuppressionsChangedHandler;
        private ImportBeforeInstallTrigger importBeforeInstallTrigger;
        private IHotspotDocumentClosedHandler hotspotDocumentClosedHandler;
        private IHotspotSolutionClosedHandler hotspotSolutionClosedHandler;
        private ILocalHotspotStoreMonitor hotspotStoreMonitor;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var componentModel = await GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            var logger = componentModel.GetService<ILogger>();

            logger.WriteLine(Resources.Package_Initializing);

            LoadServicesAndDoInitialUpdates(componentModel);

            issueServerEventsListener = componentModel.GetService<IIssueServerEventsListener>();
            issueServerEventsListener.ListenAsync().Forget();

            qualityProfileServerEventsListener = componentModel.GetService<IQualityProfileServerEventsListener>();
            qualityProfileServerEventsListener.ListenAsync().Forget();

            serverSuppressionsChangedHandler = componentModel.GetService<ServerSuppressionsChangedHandler>();
            boundSolutionUpdateHandler = componentModel.GetService<BoundSolutionUpdateHandler>();
            timedUpdateHandler = componentModel.GetService<TimedUpdateHandler>();
            localSuppressionsChangedHandler = componentModel.GetService<LocalSuppressionsChangedHandler>();

            hotspotDocumentClosedHandler = componentModel.GetService<IHotspotDocumentClosedHandler>();

            hotspotSolutionClosedHandler = componentModel.GetService<IHotspotSolutionClosedHandler>();

            hotspotStoreMonitor = componentModel.GetService<ILocalHotspotStoreMonitor>();
            await hotspotStoreMonitor.InitializeAsync();

            logger.WriteLine(Resources.Package_Initialized);
        }

        /// <summary>
        /// Trigger an initial update of suppressions (These classes might have missed the initial solution binding
        /// event from the ActiveSolutionBoundTracker)
        /// See https://github.com/SonarSource/sonarlint-visualstudio/issues/3886
        /// </summary>
        private void LoadServicesAndDoInitialUpdates(IComponentModel componentModel)
        {
            sseSessionManager = componentModel.GetService<SSESessionManager>();
            sseSessionManager.CreateSessionIfInConnectedMode();
            importBeforeInstallTrigger = componentModel.GetService<ImportBeforeInstallTrigger>();
            importBeforeInstallTrigger.TriggerUpdateAsync().Forget();
            var updater = componentModel.GetService<ISuppressionIssueStoreUpdater>();
            updater.UpdateAllServerSuppressionsAsync().Forget();
            var hotspotsUpdater = componentModel.GetService<IServerHotspotStoreUpdater>();
            hotspotsUpdater.UpdateAllServerHotspotsAsync().Forget();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                sseSessionManager?.Dispose();
                issueServerEventsListener?.Dispose();
                serverSuppressionsChangedHandler?.Dispose();
                boundSolutionUpdateHandler?.Dispose();
                timedUpdateHandler?.Dispose();
                localSuppressionsChangedHandler?.Dispose();
                importBeforeInstallTrigger?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
