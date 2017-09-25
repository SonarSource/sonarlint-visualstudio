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
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        private DateTimeOffset lastCheckDate;
        private string projectKey;

        public INotificationIndicatorViewModel Model { get; private set; }

        public NotificationData GetNotificationData() =>
            new NotificationData
                {
                    IsEnabled = Model.AreNotificationsEnabled,
                    LastNotificationDate = lastCheckDate
                };

        public SonarQubeNotificationService(ISonarQubeService sonarQubeService,
            INotificationIndicatorViewModel model, ITimer timer)
        {
            this.timer = timer;
            this.sonarQubeService = sonarQubeService;
            Model = model;

            timer.Elapsed += OnTimerElapsed;
        }

        public async Task StartAsync(string projectKey, NotificationData notificationData)
        {
            if (projectKey == null)
            {
                throw new ArgumentNullException(nameof(projectKey));
            }

            this.projectKey = projectKey;
            InitializeModel(notificationData);

            await UpdateEvents();
            timer.Start();
        }

        private void InitializeModel(NotificationData notificationData)
        {
            Model.AreNotificationsEnabled = notificationData?.IsEnabled ?? true;

            var oneDayAgo = DateTimeOffset.Now.AddDays(-1);
            if (notificationData == null ||
                notificationData.LastNotificationDate < oneDayAgo)
            {
                lastCheckDate = oneDayAgo;
            }
            else
            {
                lastCheckDate = notificationData.LastNotificationDate;
            }
        }

        public void Stop()
        {
            cancellation.Cancel();

            timer.Stop();
            Model.IsIconVisible = false;
        }

        private async Task UpdateEvents()
        {
            if (!sonarQubeService.IsConnected)
            {
                return;
            }

            var events = await GetNotificationEvents();
            if (events == null)
            {
                Stop();
                return;
            }
            Model.IsIconVisible = true;
            Model.SetNotificationEvents(events);
        }

        private async void OnTimerElapsed(object sender, EventArgs e)
        {
            await UpdateEvents();
        }

        private async Task<IList<SonarQubeNotification>> GetNotificationEvents()
        {
            IList<SonarQubeNotification> events = null;

            try
            {
                events = await sonarQubeService.GetNotificationEventsAsync(projectKey,
                    lastCheckDate, cancellation.Token);
            }
            catch (Exception ex)
            {
                VsShellUtils.WriteToSonarLintOutputPane(
                    Microsoft.VisualStudio.Shell.ServiceProvider.GlobalProvider,
                    $"Failed to fetch notifications : {ex.Message}");
            }

            if (events != null && events.Count > 0)
            {
                lastCheckDate = events.Max(ev => ev.Date);
            }

            return events;
        }

        public void Dispose()
        {
            timer.Elapsed -= OnTimerElapsed;
            Stop();
        }
    }
}
