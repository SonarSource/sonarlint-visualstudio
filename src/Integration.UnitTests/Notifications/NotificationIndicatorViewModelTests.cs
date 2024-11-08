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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Notifications;

[TestClass]
public class NotificationIndicatorViewModelTests
{
    private static readonly SonarQubeNotification[] TestEvents =
    [
        new("foo", "foo", new Uri("http://foo.com"), new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)))
    ];
    private IBrowserService browserService;
    private NotificationIndicatorViewModel testSubject;
    private IThreadHandling threadHandling;
    private ITimer timer;

    [TestInitialize]
    public void TestInitialize()
    {
        timer = Substitute.For<ITimer>();
        browserService = Substitute.For<IBrowserService>();
        threadHandling = new NoOpThreadHandler();
        testSubject = new NotificationIndicatorViewModel(browserService, threadHandling, timer);
    }

    [TestMethod]
    public void Text_Raises_PropertyChanged()
    {
        var monitor = testSubject.Monitor();

        testSubject.ToolTipText = "test";

        testSubject.ToolTipText.Should().Be("test");
        monitor.Should().RaisePropertyChangeFor(x => x.ToolTipText);
    }

    [TestMethod]
    public void HasUnreadEvents_Raises_PropertyChanged()
    {
        var monitor = testSubject.Monitor();

        testSubject.HasUnreadEvents = true;

        testSubject.HasUnreadEvents.Should().BeTrue();
        monitor.Should().RaisePropertyChangeFor(x => x.HasUnreadEvents);
    }

    [TestMethod]
    public void IsIconVisible_Raises_PropertyChanged()
    {
        var monitor = testSubject.Monitor();

        testSubject.IsIconVisible = true;

        testSubject.IsIconVisible.Should().BeTrue();
        monitor.Should().RaisePropertyChangeFor(x => x.IsIconVisible);
    }

    [TestMethod]
    public void AreNotificationsEnabled_Raises_PropertyChanged()
    {
        var monitor = testSubject.Monitor();

        testSubject.AreNotificationsEnabled = true;

        testSubject.AreNotificationsEnabled.Should().BeTrue();
        monitor.Should().RaisePropertyChangeFor(x => x.AreNotificationsEnabled);
    }

    [TestMethod]
    public void IsToolTipVisible_Raises_PropertyChanged()
    {
        var monitor = testSubject.Monitor();

        testSubject.IsToolTipVisible = true;

        testSubject.IsToolTipVisible.Should().BeTrue();
        monitor.Should().RaisePropertyChangeFor(x => x.IsToolTipVisible);
    }

    [TestMethod]
    public void IsToolTipVisible_False_Stops_Timer()
    {
        testSubject.IsToolTipVisible = true;

        testSubject.IsToolTipVisible = false;

        timer.Received(1).Stop();
    }

    [TestMethod]
    public void HasUnreadEvents_WithNo_Events_UpdatesTooltipText()
    {
        var monitor = testSubject.Monitor();

        testSubject.IsIconVisible = true;
        testSubject.AreNotificationsEnabled = true;
        testSubject.IsToolTipVisible = true;
        monitor.Should().RaisePropertyChangeFor(x => x.IsToolTipVisible);

        testSubject.ToolTipText.Should().Be("You have no unread events.");
    }

    [TestMethod]
    public void HasUnreadEvents_WithEvents_UpdatesTooltipText()
    {
        var monitor = testSubject.Monitor();

        testSubject.IsIconVisible = true;
        testSubject.AreNotificationsEnabled = true;
        testSubject.IsToolTipVisible = true;
        testSubject.SetNotificationEvents(TestEvents);

        monitor.Should().RaisePropertyChangeFor(x => x.IsToolTipVisible);

        testSubject.ToolTipText.Should().Be("You have 1 unread event.");
    }

    [TestMethod]
    public void SetNotificationEvents_SetEvents_SetsHasUnreadEvents()
    {
        SetupModelWithNotifications(false, false, TestEvents);
        testSubject.HasUnreadEvents.Should().BeFalse();

        SetupModelWithNotifications(false, true, TestEvents);
        testSubject.HasUnreadEvents.Should().BeFalse();

        SetupModelWithNotifications(true, false, TestEvents);
        testSubject.HasUnreadEvents.Should().BeFalse();

        SetupModelWithNotifications(true, true, new SonarQubeNotification[0]);
        testSubject.HasUnreadEvents.Should().BeFalse();

        SetupModelWithNotifications(true, true, null);
        testSubject.HasUnreadEvents.Should().BeFalse();

        SetupModelWithNotifications(true, true, TestEvents);
        testSubject.HasUnreadEvents.Should().BeTrue();
    }

    [TestMethod]
    public void HasUnreadEvents_RunOnUIThread()
    {
        var mockThreadHandling = Substitute.For<IThreadHandling>();
        var notificationViewModel = new NotificationIndicatorViewModel(browserService, mockThreadHandling, timer);
        notificationViewModel.AreNotificationsEnabled = true;
        notificationViewModel.IsIconVisible = true;

        var events = new[] { CreateNotification("category1") };

        notificationViewModel.SetNotificationEvents(events);

        mockThreadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
    }

    [TestMethod]
    public void NavigateToNotification_NotificationNavigated()
    {
        var notification = CreateNotification("test", "http://localhost:2000");

        testSubject.NavigateToNotification.Execute(notification);

        browserService.Received(1).Navigate("http://localhost:2000/");
    }

    [TestMethod]
    public void NavigateToNotification_TooltipClosed()
    {
        testSubject.IsToolTipVisible = true;

        var notification = CreateNotification("test");
        testSubject.NavigateToNotification.Execute(notification);

        testSubject.IsToolTipVisible.Should().BeFalse();
    }

    [TestMethod]
    public void ClearUnreadEventsCommand_Sets_HasUnreadEvents_False()
    {
        SetupModelWithNotifications(true, true, TestEvents);
        testSubject.HasUnreadEvents.Should().BeTrue();

        testSubject.ClearUnreadEventsCommand.Execute(null);

        testSubject.HasUnreadEvents.Should().BeFalse();
    }

    private static SonarQubeNotification CreateNotification(string category, string url = "http://localhost") => new(category, "test", new Uri(url), DateTimeOffset.Now);

    private void SetupModelWithNotifications(bool areEnabled, bool areVisible, SonarQubeNotification[] events)
    {
        testSubject.AreNotificationsEnabled = areEnabled;
        testSubject.IsIconVisible = areVisible;

        testSubject.SetNotificationEvents(events);
    }
}
