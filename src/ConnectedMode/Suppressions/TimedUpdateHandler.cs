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
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.Core.SystemAbstractions;

namespace SonarLint.VisualStudio.ConnectedMode.Suppressions
{
    [Export(typeof(TimedUpdateHandler))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TimedUpdateHandler : IDisposable
    {
        private const double MillisecondsToWaitBetweenRefresh = 1000 * 60 * 10; // 10 minutes

        private readonly ITimer refreshTimer;
        private readonly ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater;
        private readonly IServerHotspotStoreUpdater serverHotspotStoreUpdater;

        private bool disposed;

        [ImportingConstructor]
        public TimedUpdateHandler(ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater,
            IServerHotspotStoreUpdater serverHotspotStoreUpdater) 
            : this(suppressionIssueStoreUpdater, serverHotspotStoreUpdater, new TimerFactory()) { }

        internal /* for testing */ TimedUpdateHandler(ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater,
            IServerHotspotStoreUpdater serverHotspotStoreUpdater, ITimerFactory timerFactory)
        {
            this.suppressionIssueStoreUpdater = suppressionIssueStoreUpdater;
            this.serverHotspotStoreUpdater = serverHotspotStoreUpdater;

            refreshTimer = timerFactory.Create();
            refreshTimer.AutoReset = true;
            refreshTimer.Interval = MillisecondsToWaitBetweenRefresh;
            refreshTimer.Elapsed += OnRefreshTimerElapsed;

            refreshTimer.Start();
        }

        private void OnRefreshTimerElapsed(object sender, TimerEventArgs e)
        {
            suppressionIssueStoreUpdater.UpdateAllServerSuppressionsAsync().Forget();
            serverHotspotStoreUpdater.UpdateAllServerHotspotsAsync().Forget();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                refreshTimer.Elapsed -= OnRefreshTimerElapsed;
                refreshTimer.Dispose();
                disposed = true;
            }
        }
    }
}
