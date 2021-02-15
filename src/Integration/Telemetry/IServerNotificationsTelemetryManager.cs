/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Telemetry
{
    internal interface IServerNotificationsTelemetryManager
    {
        void QualityGateNotificationReceived();
        void QualityGateNotificationClicked();
        void NewIssueNotificationReceived();
        void NewIssueNotificationClicked();
        void NotificationsToggled(bool areNotificationsEnabled);
    }

    [Export(typeof(IServerNotificationsTelemetryManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerNotificationsTelemetryManager : IServerNotificationsTelemetryManager
    {
        private readonly ITelemetryDataRepository telemetryRepository;

        [ImportingConstructor]
        public ServerNotificationsTelemetryManager(ITelemetryDataRepository telemetryRepository)
        {
            this.telemetryRepository = telemetryRepository;
        }

        void IServerNotificationsTelemetryManager.QualityGateNotificationReceived()
        {
            Debug.Assert(telemetryRepository.Data != null);

            ++telemetryRepository.Data.ServerNotifications.ServerNotificationCounters.QualityGateNotificationCounter.ReceivedCount;
            telemetryRepository.Save();
        }

        void IServerNotificationsTelemetryManager.QualityGateNotificationClicked()
        {
            Debug.Assert(telemetryRepository.Data != null);

            ++telemetryRepository.Data.ServerNotifications.ServerNotificationCounters.QualityGateNotificationCounter.ClickedCount;
            telemetryRepository.Save();
        }

        void IServerNotificationsTelemetryManager.NewIssueNotificationReceived()
        {
            Debug.Assert(telemetryRepository.Data != null);

            ++telemetryRepository.Data.ServerNotifications.ServerNotificationCounters.NewIssuesNotificationCounter.ReceivedCount;
            telemetryRepository.Save();
        }

        void IServerNotificationsTelemetryManager.NewIssueNotificationClicked()
        {
            Debug.Assert(telemetryRepository.Data != null);

            ++telemetryRepository.Data.ServerNotifications.ServerNotificationCounters.NewIssuesNotificationCounter.ClickedCount;
            telemetryRepository.Save();
        }

        void IServerNotificationsTelemetryManager.NotificationsToggled(bool areNotificationsEnabled)
        {
            Debug.Assert(telemetryRepository.Data != null);

            telemetryRepository.Data.ServerNotifications.IsDisabled = !areNotificationsEnabled;
            telemetryRepository.Save();
        }
    }
}
