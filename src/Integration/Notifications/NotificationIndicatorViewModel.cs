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

using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public class NotificationIndicatorViewModel : ViewModelBase, INotificationIndicatorViewModel
    {
        private readonly ITimer autocloseTimer;
        private readonly IThreadHandling threadHandling;

        private string text;
        private bool hasUnreadEvents;
        private bool isVisible;
        private bool isToolTipVisible;
        private bool areNotificationsEnabled;

        public ObservableCollection<SonarQubeNotification> NotificationEvents { get; }

        public ICommand NavigateToNotification { get; }

        public NotificationIndicatorViewModel(IServerNotificationsTelemetryManager telemetryManager, IBrowserService vsBrowserService)
            : this(telemetryManager, vsBrowserService, ThreadHandling.Instance,
                  new TimerWrapper { AutoReset = false, Interval = 3000 /* 3 sec */})
        {
        }

        // For testing
        internal NotificationIndicatorViewModel(IServerNotificationsTelemetryManager telemetryManager, 
            IBrowserService vsBrowserService,
            IThreadHandling threadHandling, 
            ITimer autocloseTimer)
        {
            this.threadHandling = threadHandling;
            this.autocloseTimer = autocloseTimer;

            NotificationEvents = new ObservableCollection<SonarQubeNotification>();
            text = BuildToolTipText();

            autocloseTimer.Elapsed += OnAutocloseTimerElapsed;
            ClearUnreadEventsCommand = new RelayCommand(() => HasUnreadEvents = false);

            NavigateToNotification = new DelegateCommand(parameter =>
            {
                var notification = (SonarQubeNotification) parameter;
                vsBrowserService.Navigate(notification.Link.ToString());
                IsToolTipVisible = false;
                telemetryManager.NotificationClicked(notification.Category);
            });
        }

        public ICommand ClearUnreadEventsCommand { get; }

        public string ToolTipText
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
                ToolTipText = BuildToolTipText();
            }
        }

        public bool IsIconVisible
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

        public bool AreNotificationsEnabled
        {
            get
            {
                return areNotificationsEnabled;
            }
            set
            {
                SetAndRaisePropertyChanged(ref areNotificationsEnabled, value);
            }
        }

        public bool IsToolTipVisible
        {
            get
            {
                return isToolTipVisible;
            }
            set
            {
                SetAndRaisePropertyChanged(ref isToolTipVisible, value);

                // If the tooltip was closed manually, stop the timer
                if (!isToolTipVisible)
                {
                    autocloseTimer.Stop();
                }
            }
        }

        public void SetNotificationEvents(IEnumerable<SonarQubeNotification> events)
        {
            if (events == null ||
                !events.Any() ||
                !AreNotificationsEnabled ||
                !isVisible)
            {
                return;
            }

            threadHandling.RunOnUIThread(() =>
                {
                    NotificationEvents.Clear();

                    foreach (var ev in events)
                    {
                        NotificationEvents.Add(ev);
                    }

                    HasUnreadEvents = true;
                    IsToolTipVisible = true;
                    autocloseTimer.Start();
                });
        }

        private void OnAutocloseTimerElapsed(object sender, EventArgs e)
        {
            IsToolTipVisible = false;
        }

        private string BuildToolTipText()
        {
            const string noUnreadEvents = "You have no unread events.";
            if (!HasUnreadEvents ||
                NotificationEvents.Count == 0)
            {
                return noUnreadEvents;
            }

            return string.Format("You have {0} unread event{1}.",
                NotificationEvents.Count, NotificationEvents.Count == 1 ? "" : "s");
        }
    }
}
