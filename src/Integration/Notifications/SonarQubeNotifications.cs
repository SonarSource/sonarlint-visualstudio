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
using System.Timers;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    [Export(typeof(ISonarQubeNotifications))]
    internal class SonarQubeNotifications : ViewModelBase, ISonarQubeNotifications
    {
        private string balloonTipText;
        private string text;
        private bool hasEvents;
        private bool isVisible;
        private bool isBalloonTooltipVisible;

        private DateTimeOffset lastRequestDate = DateTimeOffset.MinValue;
        private readonly Timer timer;
        private readonly IStateManager stateManager;
        private readonly ISonarQubeServiceWrapper sonarQubeService;
        private readonly System.Threading.CancellationTokenSource cancellation =
            new System.Threading.CancellationTokenSource();

        public event EventHandler ShowDetails;

        [ImportingConstructor]
        [ExcludeFromCodeCoverage] // Do not unit test MEF constructor
        internal SonarQubeNotifications(IHost host)
            : this(host.SonarQubeService, host.VisualStateManager,
                  new Timer { Interval = 60000 /* 60sec */ })
        {
        }

        internal SonarQubeNotifications(ISonarQubeServiceWrapper sonarQubeService,
            IStateManager stateManager, Timer timer)
        {
            this.timer = timer;
            this.sonarQubeService = sonarQubeService;
            this.stateManager = stateManager;

            timer.Elapsed += OnTimerElapsed;
            ShowDetailsCommand = new RelayCommand(ShowDetailsCommandExecuted);
        }

        public ICommand ShowDetailsCommand { get; }

        public string BalloonTipText
        {
            get
            {
                return balloonTipText;
            }
            set
            {
                SetAndRaisePropertyChanged(ref balloonTipText, value);
            }
        }

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                SetAndRaisePropertyChanged(ref text, value);
            }
        }

        public bool HasEvents
        {
            get
            {
                return hasEvents;
            }

            set
            {
                SetAndRaisePropertyChanged(ref hasEvents, value);
            }
        }

        public bool IsVisible
        {
            get
            {
                return isVisible;
            }
            set
            {
                SetAndRaisePropertyChanged(ref isVisible, value);
            }
        }

        public bool IsBalloonTooltipVisible
        {
            get
            {
                return isBalloonTooltipVisible;
            }
            set
            {
                SetAndRaisePropertyChanged(ref isBalloonTooltipVisible, value);
            }
        }

        private void ShowDetailsCommandExecuted()
        {
            ShowDetails?.Invoke(this, EventArgs.Empty);
        }

        public void Start()
        {
            IsVisible = true;

            timer.Start();
        }

        public void Stop()
        {
            cancellation.Cancel();

            timer.Stop();

            IsVisible = false;
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            UpdateMessage();
        }

        private void UpdateMessage()
        {
            var serverConnection = ThreadHelper.Generic.Invoke(() => stateManager?.GetConnectedServers().FirstOrDefault());
            var projectKey = stateManager?.BoundProjectKey;

            if (serverConnection == null ||
                projectKey == null)
            {
                // TODO: delete me
                var demoMode = true;
                if (demoMode)
                {
                    HasEvents = true;
                    BalloonTipText = "Weeeeeeeee have sssssssssome messagessssssssssss fooooor youuuuuuuuusssssss..........";
                }

                return;
            }

            NotificationEvent[] events;
            var isSuccess = sonarQubeService.TryGetNotificationEvents(serverConnection, cancellation.Token,
                projectKey, lastRequestDate, out events);

            if (isSuccess && events != null)
            {
                lastRequestDate = events.Max(ev => ev.Date);
                HasEvents = true;
                BalloonTipText = string.Join(Environment.NewLine + Environment.NewLine,
                    events.Select(ev => ev.Message));
            }
        }

        public void Dispose()
        {
            timer.Elapsed -= OnTimerElapsed;

            Stop();
        }
    }
}
