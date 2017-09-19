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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
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

        private readonly INotifyIcon notifyIcon;
        private readonly ITimer timer;
        private const string iconPath = "pack://application:,,,/SonarLint;component/Resources/sonarqube_green.ico";
        private const string tooltipTitle = "SonarQube notification";
        private string message;
        private DateTimeOffset lastRequestDate = DateTimeOffset.MinValue;
        private readonly ISonarQubeServiceWrapper sqServiceWrapper;
        private readonly IStateManager stateManager;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        [ImportingConstructor]
        [ExcludeFromCodeCoverage] // Do not unit test MEF constructor
        internal SonarQubeNotifications(IHost host)
            : this(host.SonarQubeService, host.VisualStateManager, new TaskbarNotifyIcon(),
                  new TimerWrap())
        {
        }

        internal SonarQubeNotifications(ISonarQubeServiceWrapper sonarQubeService,
            IStateManager stateManager, INotifyIcon notifyIcon, ITimer timer,
            double timerIntervalMilliseconds = 60000 /* 60sec */)
        {
            this.notifyIcon = notifyIcon;
            this.timer = timer;
            this.sqServiceWrapper = sonarQubeService;
            this.stateManager = stateManager;

            notifyIcon.Click += (s, e) => ShowNofitication();
            notifyIcon.DoubleClick += (s, e) => OnShowDetails(EventArgs.Empty);
            notifyIcon.BalloonTipClicked += (s, e) => OnShowDetails(EventArgs.Empty);
            notifyIcon.Icon = new Icon(Application.GetResourceStream(new Uri(iconPath)).Stream);

            timer.Elapsed += OnTimerElapsed;
            timer.Interval = timerIntervalMilliseconds;
        }

        public void Start()
        {
            var serverConnection = ThreadHelper.Generic.Invoke(() => stateManager.GetConnectedServers().FirstOrDefault());

            timer.Start();
        }

        public void Stop()
        {
            cancellation.Cancel();
            timer.Elapsed -= OnTimerElapsed;
            timer.Stop();
            notifyIcon.Icon.Dispose();
            notifyIcon.Dispose();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateMessage();
            ShowNofitication();
        }

        private void UpdateMessage()
        {
            var serverConnection = ThreadHelper.Generic.Invoke(() =>
                stateManager?.GetConnectedServers().FirstOrDefault());
            var projectKey = stateManager?.BoundProjectKey;

            if (serverConnection == null ||
                projectKey == null)
            {
                return;
            }

            NotificationEvent[] events;
            var isSuccess = sqServiceWrapper.TryGetNotificationEvents(serverConnection, cancellation.Token,
                projectKey, lastRequestDate, out events);

            if (isSuccess && events != null)
            {
                message = string.Join(Environment.NewLine + Environment.NewLine,
                    events.Select(ev => ev.Message));
                lastRequestDate = events.Max(ev => ev.Date);
            }
        }

        private void ShowNofitication()
        {
            if (message != string.Empty)
            {
                notifyIcon.Visible = true;

                ThreadHelper.Generic.Invoke(() =>
                    notifyIcon?.ShowBalloonTip((int)TimeSpan.FromSeconds(10).TotalMilliseconds,
                        tooltipTitle, message));
            }
        }

        private void OnShowDetails(EventArgs e)
        {
            ShowDetails?.Invoke(this, e);
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
