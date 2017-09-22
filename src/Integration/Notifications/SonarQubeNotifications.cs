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
using System.Threading.Tasks;
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
        private bool hasEvents;
        private bool isVisible;
        private bool isBalloonTooltipVisible;

        private DateTimeOffset lastRequestDate = DateTimeOffset.MinValue;
        private readonly Timer timer;
        private readonly IWebBrowser webBrowser;
        private readonly IStateManager stateManager;
        private readonly ISonarQubeServiceWrapper sonarQubeService;
        private readonly CancellationTokenSource cancellation = new CancellationTokenSource();

        [ImportingConstructor]
        [ExcludeFromCodeCoverage] // Do not unit test MEF constructor
        internal SonarQubeNotifications(IHost host, IWebBrowser webBrowser)
            : this(host.SonarQubeService, webBrowser, host.VisualStateManager,
                  new Timer { Interval = 20000 /* should be 60sec */ })
        {
        }

        internal SonarQubeNotifications(ISonarQubeServiceWrapper sonarQubeService,
            IWebBrowser webBrowser, IStateManager stateManager, Timer timer)
        {
            this.timer = timer;
            this.webBrowser = webBrowser;
            this.sonarQubeService = sonarQubeService;
            this.stateManager = stateManager;

            timer.Elapsed += OnTimerElapsed;
            ShowDetailsCommand = new RelayCommand(ShowDetailsCommandExecuted);
            NotificationEvents = new ObservableCollection<NotificationEvent>();
        }

        public ICommand ShowDetailsCommand { get; }

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

                UpdateTooltipText();

                if (value)
                {
                    AnimateBalloonTooltip();
                }
            }
        }

        private async void AnimateBalloonTooltip()
        {
            IsBalloonTooltipVisible = true;
            await System.Threading.Tasks.Task.Delay(4000);
            IsBalloonTooltipVisible = false;
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

        public ObservableCollection<NotificationEvent> NotificationEvents { get; }

        private void ShowDetailsCommandExecuted()
        {
            var url = "http://peach.sonarsource.com"; // TODO: use real url
            webBrowser.NavigateTo(url);
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
            ThreadHelper.Generic.Invoke(() => UpdateMessage());
        }

        private void UpdateMessage()
        {
            var events = GetNotificationEvents();

            // TODO: Remove this code
            if (events.Length == 0)
            {
                events = new[]
                {
                    new NotificationEvent
                    {
                        Date = DateTimeOffset.UtcNow,
                        Message = "Quality gate is red (it was green).",
                        Link = new Uri("http://peach.sonarsource.com")
                    },
                    new NotificationEvent
                    {
                        Date = DateTimeOffset.UtcNow,
                        Message = "You have 15 new issues.",
                        Link = new Uri("http://peach.sonarsource.com")
                    }
                };
            }

            if (events.Length > 0)
            {
                NotificationEvents.Clear();
                Array.ForEach(events, NotificationEvents.Add);

                HasEvents = true;
            }
        }

        private void UpdateTooltipText()
        {
            var count = NotificationEvents.Count == 0
                ? "no"
                : NotificationEvents.Count.ToString();

            var isNew = HasEvents ? "new " : string.Empty;

            Text = $"You have {count} {isNew}events.";
        }

        private NotificationEvent[] GetNotificationEvents()
        {
            var connection = ThreadHelper.Generic.Invoke(() => stateManager?.GetConnectedServers().FirstOrDefault());
            var projectKey = stateManager?.BoundProjectKey;

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
            timer.Elapsed -= OnTimerElapsed;

            Stop();
        }
    }
}
