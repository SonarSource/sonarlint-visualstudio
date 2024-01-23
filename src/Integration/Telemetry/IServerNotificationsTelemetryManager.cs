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
using System.Diagnostics;
using SonarLint.VisualStudio.Integration.Telemetry.Payload;

namespace SonarLint.VisualStudio.Integration.Telemetry
{
    public interface IServerNotificationsTelemetryManager
    {
        void NotificationsToggled(bool areNotificationsEnabled);
        void NotificationReceived(string category);
        void NotificationClicked(string category);
    }

    [Export(typeof(IServerNotificationsTelemetryManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerNotificationsTelemetryManager : IServerNotificationsTelemetryManager
    {
        private readonly ITelemetryDataRepository telemetryRepository;
        private static object Lock = new Object();

        [ImportingConstructor]
        public ServerNotificationsTelemetryManager(ITelemetryDataRepository telemetryRepository)
        {
            this.telemetryRepository = telemetryRepository;
        }

        void IServerNotificationsTelemetryManager.NotificationsToggled(bool areNotificationsEnabled)
        {
            Debug.Assert(telemetryRepository.Data != null);

            telemetryRepository.Data.ServerNotifications.IsDisabled = !areNotificationsEnabled;
            telemetryRepository.Save();
        }

        public void NotificationReceived(string category)
        {
            Debug.Assert(telemetryRepository.Data != null);

            var counter = GetOrCreateCounter(category);
            counter.ReceivedCount++;

            telemetryRepository.Save();
        }

        public void NotificationClicked(string category)
        {
            Debug.Assert(telemetryRepository.Data != null);

            var counter = GetOrCreateCounter(category);
            counter.ClickedCount++;

            telemetryRepository.Save();
        }

        private ServerNotificationCounter GetOrCreateCounter(string category)
        {
            var countersDictionary = telemetryRepository.Data.ServerNotifications.ServerNotificationCounters;

            if (!countersDictionary.ContainsKey(category))
            {
                lock (Lock)
                {
                    if (!countersDictionary.ContainsKey(category))
                    {
                        countersDictionary[category] = new ServerNotificationCounter();
                    }
                }
            }

            return countersDictionary[category];
        }
    }
}
