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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Timers;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.WPF;

using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    [Export(typeof(ISonarQubeNotifications))]
    internal class SonarQubeNotifications : ViewModelBase, ISonarQubeNotifications
    {
        private string text = "You have no events.";
        private bool hasUnreadEvents;
        private bool isVisible;
        private bool isBalloonTooltipVisible;

        private DateTimeOffset lastRequestDate = DateTimeOffset.MinValue;
        private readonly ITimer notificationsTimer;
        private readonly ITimer autocloseTimer;
        private readonly IStateManager stateManager;
        private readonly ISonarQubeServiceWrapper sonarQubeService;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();
        private bool isEnabled = true;

        [ImportingConstructor]
        [ExcludeFromCodeCoverage] // Do not unit test MEF constructor
        internal SonarQubeNotifications(IHost host)
            : this(host.SonarQubeService, host.VisualStateManager,
                    new TimerWrapper { Interval = 20000 /* should be 60sec */ },
                    new TimerWrapper { Interval = 3000 })
        {
        }

        internal SonarQubeNotifications(ISonarQubeServiceWrapper sonarQubeService,
            IStateManager stateManager, ITimer notificationsTimer, ITimer autocloseTimer)
        {
            this.sonarQubeService = sonarQubeService;
            this.stateManager = stateManager;

            this.notificationsTimer = notificationsTimer;
            notificationsTimer.Elapsed += OnTimerElapsed;

            this.autocloseTimer = autocloseTimer;
            autocloseTimer.Elapsed += OnAutocloseTimerElapsed;

            NotificationEvents = new ObservableCollection<NotificationEvent>();

            ClearUnreadEventsCommand = new RelayCommand(() =>
            {
                HasUnreadEvents = false;
            });
        }

        public ICommand ClearUnreadEventsCommand { get; }

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

        public bool HasUnreadEvents
        {
            get
            {
                return hasUnreadEvents;
            }
            set
            {
                SetAndRaisePropertyChanged(ref hasUnreadEvents, value);

                UpdateTooltipText();
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

        public bool IsEnabled
        {
            get
            {
                return isEnabled;
            }
            set
            {
                SetAndRaisePropertyChanged(ref isEnabled, value);
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

                if (isBalloonTooltipVisible)
                {
                    autocloseTimer.Start();
                }
                else
                {
                    autocloseTimer.Stop();
                }
            }
        }

        public ObservableCollection<NotificationEvent> NotificationEvents { get; }

        public void Start()
        {
            IsVisible = true;

            notificationsTimer.Start();
        }

        public void Stop()
        {
            cancellation.Cancel();

            notificationsTimer.Stop();

            IsVisible = false;
        }

        private void OnTimerElapsed(object sender, EventArgs e)
        {
            var events = GetNotificationEvents();

            ThreadHelper.Generic.Invoke(() => SetNotificationEvents(events));
        }

        private void OnAutocloseTimerElapsed(object sender, EventArgs e)
        {
            IsBalloonTooltipVisible = false;
        }

        private void SetNotificationEvents(NotificationEvent[] events)
        {
            //if (events.Length > 0)
            {
                NotificationEvents.Clear();
                Array.ForEach(events, NotificationEvents.Add);

                HasUnreadEvents = true;
                IsBalloonTooltipVisible = true;
            }
        }

        private void UpdateTooltipText()
        {
            Text = NotificationEvents.Count > 0 && HasUnreadEvents
                ? $"You have {NotificationEvents.Count} unread events."
                : "You have no unread events.";
        }

        private NotificationEvent[] GetNotificationEvents()
        {
            var connection = ThreadHelper.Generic.Invoke(() => stateManager.GetConnectedServers().FirstOrDefault());
            var projectKey = stateManager.BoundProjectKey;

            NotificationEvent[] events = null;

            if (connection != null && projectKey != null)
            {
                if (sonarQubeService.TryGetNotificationEvents(connection, cancellation.Token, projectKey,
                    lastRequestDate, out events))
                {
                    lastRequestDate = events.Max(ev => ev.Date);
                }
            }

            return events ?? new NotificationEvent[0];
        }

        public void Dispose()
        {
            notificationsTimer.Elapsed -= OnTimerElapsed;

            Stop();

            notificationsTimer.Dispose();
        }
    }
}
