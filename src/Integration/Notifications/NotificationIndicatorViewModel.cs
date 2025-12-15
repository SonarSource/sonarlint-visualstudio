/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SmartNotification;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.Notifications;

public sealed class NotificationIndicatorViewModel : ViewModelBase, INotificationIndicatorViewModel, IDisposable
{
    private readonly ISmartNotificationService smartNotificationService;
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly ITimer autocloseTimer;
    private readonly IThreadHandling threadHandling;
    private bool areNotificationsEnabled;
    private bool hasUnreadEvents;
    private bool isCloud;
    private bool isToolTipVisible;
    private bool isVisible;

    private string text;

    public ObservableCollection<SmartNotification> NotificationEvents { get; }

    public ICommand ClearUnreadEventsCommand { get; }

    public NotificationIndicatorViewModel(ISmartNotificationService smartNotificationService, IBrowserService vsBrowserService, IActiveSolutionBoundTracker activeSolutionBoundTracker)
        : this(smartNotificationService, vsBrowserService, activeSolutionBoundTracker, ThreadHandling.Instance, new TimerWrapper { AutoReset = false, Interval = 3000 /* 3 sec */ })
    {
        // Nothing to do
    }

    // For testing
    internal NotificationIndicatorViewModel(
        ISmartNotificationService smartNotificationService,
        IBrowserService vsBrowserService,
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IThreadHandling threadHandling,
        ITimer autocloseTimer)
    {
        this.smartNotificationService = smartNotificationService;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.threadHandling = threadHandling;
        this.autocloseTimer = autocloseTimer;

        smartNotificationService.NotificationReceived += OnNotificationReceived;
        activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
        NotificationEvents = [];
        text = BuildToolTipText();

        autocloseTimer.Elapsed += OnAutocloseTimerElapsed;
        ClearUnreadEventsCommand = new RelayCommand(() => HasUnreadEvents = false);

        NavigateToNotification = new DelegateCommand(parameter =>
        {
            var notification = (SmartNotification) parameter;
            vsBrowserService.Navigate(notification.Link);
            IsToolTipVisible = false;
        });
    }

    public void Dispose()
    {
        smartNotificationService.NotificationReceived -= OnNotificationReceived;
        activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;
    }

    public ICommand NavigateToNotification { get; }

    public string ToolTipText
    {
        get => text;
        set => SetAndRaisePropertyChanged(ref text, value);
    }

    public bool HasUnreadEvents
    {
        get => hasUnreadEvents;

        set
        {
            SetAndRaisePropertyChanged(ref hasUnreadEvents, value);
            ToolTipText = BuildToolTipText();
        }
    }

    public bool IsIconVisible
    {
        get => isVisible;
        set => SetAndRaisePropertyChanged(ref isVisible, value);
    }

    public bool AreNotificationsEnabled
    {
        get => areNotificationsEnabled;
        set => SetAndRaisePropertyChanged(ref areNotificationsEnabled, value);
    }

    public bool IsToolTipVisible
    {
        get => isToolTipVisible;
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

    public bool IsCloud
    {
        get => isCloud;
        set => SetAndRaisePropertyChanged(ref isCloud, value);
    }

    public void SetNotificationEvents(IEnumerable<SmartNotification> events)
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

    private void OnAutocloseTimerElapsed(object sender, EventArgs e) => IsToolTipVisible = false;

    private string BuildToolTipText()
    {
        const string noUnreadEvents = "You have no unread events.";
        if (!HasUnreadEvents || NotificationEvents.Count == 0)
        {
            return noUnreadEvents;
        }

        return string.Format("You have {0} unread event{1}.",
            NotificationEvents.Count, NotificationEvents.Count == 1 ? "" : "s");
    }

    private void OnNotificationReceived(object sender, NotificationReceivedEventArgs args)
    {
        SetNotificationEvents([args.Notification]);
    }

    private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs args)
    {
        IsIconVisible = args.Configuration?.Project?.ServerConnection != null;
        IsCloud = args.Configuration?.Project?.ServerConnection is ServerConnection.SonarCloud;
        AreNotificationsEnabled = args.Configuration?.Project?.ServerConnection?.Settings.IsSmartNotificationsEnabled ?? false;
    }
}
