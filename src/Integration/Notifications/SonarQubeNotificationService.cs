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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using SonarQube.Client.Models;
using SonarQube.Client.Services;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public sealed class SonarQubeNotificationService : ISonarQubeNotificationService, IDisposable
    {
        private readonly ITimer timer;
        private readonly ISonarQubeService sonarQubeService;
        private readonly ISonarLintOutput sonarLintOutput;

        private CancellationTokenSource cancellation;
        private DateTimeOffset lastCheckDate;
        private string projectKey;
        private bool isFirstServerQuery;

        public INotificationIndicatorViewModel Model { get; }

        public NotificationData GetNotificationData() =>
            new NotificationData
            {
                IsEnabled = Model.AreNotificationsEnabled,
                LastNotificationDate = lastCheckDate
            };

        public SonarQubeNotificationService(ISonarQubeService sonarQubeService, INotificationIndicatorViewModel model,
            ITimer timer, ISonarLintOutput sonarLintOutput)
        {
            this.sonarQubeService = sonarQubeService;

            Model = model;

            this.timer = timer;
            this.timer.Elapsed += OnTimerElapsed;

            this.sonarLintOutput = sonarLintOutput;
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

            isFirstServerQuery = true;
            timer.Start();
            await UpdateEvents();
        }

        public void Stop()
        {
            cancellation?.Cancel();
            timer.Stop();
            Model.IsIconVisible = false;
        }

        private async Task UpdateEvents()
        {
            if (!sonarQubeService.IsConnected ||
                !Model.AreNotificationsEnabled)
            {
                return;
            }

            try
            {
                var events = await GetNotificationEvents();
                if (events == null)
                {
                    // Notifications are not supported on SonarQube
                    Stop();
                    return;
                }

                if (!isFirstServerQuery)
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
                sonarLintOutput.Write($"Failed to fetch notifications : {ex.Message}");
            }

            isFirstServerQuery = false;
        }

        private async void OnTimerElapsed(object sender, EventArgs e)
        {
            Debug.Assert(cancellation != null,
                "Cancellation token should not be null if the timer is active - check StartAsync has been called");

            await UpdateEvents();
        }

        private async Task<IList<SonarQubeNotification>> GetNotificationEvents()
        {
            return await sonarQubeService.GetNotificationEventsAsync(projectKey,
                lastCheckDate, cancellation.Token);
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
