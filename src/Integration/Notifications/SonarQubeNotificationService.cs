/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration.Resources;
using SonarQube.Client.Services;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public sealed class SonarQubeNotificationService : ISonarQubeNotificationService, IDisposable
    {
        private readonly ITimer timer;
        private readonly ISonarQubeService sonarQubeService;
        private readonly ILogger logger;

        private CancellationTokenSource cancellation;
        private DateTimeOffset lastCheckDate;
        private string projectKey;

        public INotificationIndicatorViewModel Model { get; }

        public NotificationData GetNotificationData() =>
            new NotificationData
            {
                IsEnabled = Model.AreNotificationsEnabled,
                LastNotificationDate = lastCheckDate
            };

        public SonarQubeNotificationService(ISonarQubeService sonarQubeService, INotificationIndicatorViewModel model,
            ITimer timer, ILogger logger)
        {
            this.sonarQubeService = sonarQubeService;
            this.timer = timer;
            this.timer.Elapsed += OnTimerElapsed;
            this.logger = logger;

            Model = model;
        }

        public async Task StartAsync(string projectKey, NotificationData notificationData)
        {
            if (projectKey == null)
            {
                throw new ArgumentNullException(nameof(projectKey));
            }

            this.projectKey = projectKey;
            cancellation = new CancellationTokenSource();
            Model.AreNotificationsEnabled = notificationData?.IsEnabled ?? true;
            lastCheckDate = GetLastCheckedDate(notificationData);

            timer.Start();
            await UpdateEvents(true);
        }

        public void Stop()
        {
            cancellation?.Cancel();
            timer.Stop();
            Model.IsIconVisible = false;
        }

        private async Task UpdateEvents(bool isFirstRequest = false)
        {
            // Query server even if notifications are disabled, query the server to know
            // if the icon should be shown (so the notifications can be re-enabled).
            if (!sonarQubeService.IsConnected ||
                (!Model.AreNotificationsEnabled && Model.IsIconVisible))
            {
                return;
            }

            try
            {
                var events = await sonarQubeService.GetNotificationEventsAsync(projectKey,
                    lastCheckDate, cancellation.Token);
                if (events == null)
                {
                    // Notifications are not supported on SonarQube
                    logger.WriteLine(Strings.Notifications_NotSupported);
                    Stop();
                    return;
                }

                // First request is only to detect if notifications are enabled on the server.
                // Even if there are notifications, do not show them as it could be easy to miss
                // (this code is executed on solution load, when a lot of things happen in the UI).
                if (!isFirstRequest)
                {
                    if (events.Count > 0)
                    {
                        lastCheckDate = events.Max(ev => ev.Date);
                    }
                    Model.SetNotificationEvents(events);
                }

                Model.IsIconVisible = true;
            }
            catch (Exception ex)
            {
                logger.WriteLine($"Failed to fetch notifications: {ex.Message}");
            }
        }

        private async void OnTimerElapsed(object sender, EventArgs e)
        {
            Debug.Assert(cancellation != null,
                "Cancellation token should not be null if the timer is active - check StartAsync has been called");

            await UpdateEvents();
        }

        public void Dispose()
        {
            timer.Elapsed -= OnTimerElapsed;
            Stop();
        }

        private static DateTimeOffset GetLastCheckedDate(NotificationData notificationData)
        {
            var oneDayAgo = DateTimeOffset.Now.AddDays(-1);

            if (notificationData == null ||
                notificationData.LastNotificationDate < oneDayAgo)
            {
                return oneDayAgo;
            }
            else
            {
                return notificationData.LastNotificationDate;
            }
        }
    }
}
