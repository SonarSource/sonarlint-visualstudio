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
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Timers;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SystemInterface.Timers;
using SystemWrapper.Timers;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    [Export(typeof(ISonarQubeNotifications))]
    internal class SonarQubeNotifications : ISonarQubeNotifications
    {
        public event EventHandler ShowDetails;

        private INotifyIcon notifyIcon;
        private readonly ITimer timer;
        private const string iconPath = "pack://application:,,,/SonarLint;component/Resources/sonarqube_green.ico";
        private const string tooltipTitle = "SonarQube notification";
        private string message;
        private DateTimeOffset lastRequestDate = DateTimeOffset.MinValue;
        private readonly ISonarQubeServiceWrapper sonarQubeService;
        private readonly IStateManager stateManager;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        [ImportingConstructor]
        [ExcludeFromCodeCoverage] // Do not unit test MEF constructor
        internal SonarQubeNotifications(IHost host)
            : this(host.SonarQubeService, host.VisualStateManager, new StatusBarIconWrapper(),
                  new TimerWrap())
        {
        }

        internal SonarQubeNotifications(ISonarQubeServiceWrapper sonarQubeService,
            IStateManager stateManager, INotifyIcon notifyIcon, ITimer timer,
            double timerIntervalMilliseconds = 60000 /* 60sec */)
        {
            this.notifyIcon = notifyIcon;
            this.timer = timer;
            this.sonarQubeService = sonarQubeService;
            this.stateManager = stateManager;

            timer.Elapsed += OnTimerElapsed;
            timer.Interval = timerIntervalMilliseconds;
        }

        public void Start()
        {
            notifyIcon.BalloonTipClick += OnBalloonTipClick;
            notifyIcon.IsVisible = true;

            var serverConnection = ThreadHelper.Generic.Invoke(() => stateManager.GetConnectedServers().FirstOrDefault());

            timer.Start();
        }

        private void OnBalloonTipClick(object sender, EventArgs e)
        {
            ShowDetails?.Invoke(this, e);
        }

        public void Stop()
        {
            cancellation.Cancel();

            timer.Stop();

            notifyIcon.BalloonTipClick -= OnBalloonTipClick;
            notifyIcon.IsVisible = false;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateMessage();
        }

        private void UpdateMessage()
        {
            var serverConnection = ThreadHelper.Generic.Invoke(() =>
                stateManager?.GetConnectedServers().FirstOrDefault());
            var projectKey = stateManager?.BoundProjectKey;

            if (serverConnection == null ||
                projectKey == null)
            {
                var demoMode = true;
                if (demoMode)
                {
                    notifyIcon.HasEvents = true;
                    notifyIcon.BalloonTipText = "We have some messagesssssssssssssssss for youuuuuuuuuuuuuuu";
                }

                return;
            }

            NotificationEvent[] events;
            var isSuccess = sonarQubeService.TryGetNotificationEvents(serverConnection, cancellation.Token,
                projectKey, lastRequestDate, out events);

            if (isSuccess && events != null)
            {
                message = string.Join(Environment.NewLine + Environment.NewLine,
                    events.Select(ev => ev.Message));
                lastRequestDate = events.Max(ev => ev.Date);
                notifyIcon.HasEvents = true;
                notifyIcon.BalloonTipText = message;
            }
        }

        public void Dispose()
        {
            timer.Elapsed -= OnTimerElapsed;

            Stop();
        }
    }
}
