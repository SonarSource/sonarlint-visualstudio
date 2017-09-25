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
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Notifications
{
    public class NotificationIndicatorViewModel : ViewModelBase, INotificationIndicatorViewModel
    {
        private string text;
        private bool hasUnreadEvents;
        private bool isVisible;
        private bool isBalloonTooltipVisible;
        private bool areNotificationsEnabled;
        private readonly ITimer autocloseTimer;
        private readonly Action<Action> uiThreadInvoker;

        public ObservableCollection<SonarQubeNotification> NotificationEvents { get; }

        // For testing
        public NotificationIndicatorViewModel()
            : this (ThreadHelper.Generic.Invoke)
        {
        }

        public NotificationIndicatorViewModel(Action<Action> uiThreadInvoker)
        {
            this.uiThreadInvoker = uiThreadInvoker;

            NotificationEvents = new ObservableCollection<SonarQubeNotification>();
            text = GetTooltipText();

            autocloseTimer = new TimerWrapper { Interval = 3000, AutoReset = false };
            autocloseTimer.Elapsed += OnAutocloseTimerElapsed;

            ClearUnreadEventsCommand = new RelayCommand(() => HasUnreadEvents = false);
        }

        public ICommand ClearUnreadEventsCommand { get; }

        public string TooltipText
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
                TooltipText = GetTooltipText();
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

        public bool IsBalloonTooltipVisible
        {
            get
            {
                return isBalloonTooltipVisible;
            }
            set
            {
                SetAndRaisePropertyChanged(ref isBalloonTooltipVisible, value);

                // If the tooltip was closed manually, stop the timer
                if (!isBalloonTooltipVisible)
                {
                    autocloseTimer.Stop();
                }
            }
        }

        public void SetNotificationEvents(IList<SonarQubeNotification> events)
        {
            if (events == null ||
                events.Count == 0 ||
                !AreNotificationsEnabled ||
                !isVisible)
            {
                return;
            }

            uiThreadInvoker(() =>
                {
                    NotificationEvents.Clear();

                    foreach (var ev in events)
                    {
                        NotificationEvents.Add(ev);
                    }

                    HasUnreadEvents = true;
                    IsBalloonTooltipVisible = true;
                    autocloseTimer.Start();
                });
        }

        private void OnAutocloseTimerElapsed(object sender, EventArgs e)
        {
            IsBalloonTooltipVisible = false;
        }

        private string GetTooltipText()
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
