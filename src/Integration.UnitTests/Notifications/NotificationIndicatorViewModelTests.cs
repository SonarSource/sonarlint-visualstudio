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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SmartNotification;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Notifications;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests.Notifications;

[TestClass]
public class NotificationIndicatorViewModelTests
{
    private static readonly SmartNotification[] TestEvents =
    [
        new("foo", "http://foo.com", ["SCOPE_ID"], "foo", "connectionId", new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)))
    ];
    private ISmartNotificationService smartNotificationService;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IBrowserService browserService;
    private NotificationIndicatorViewModel testSubject;
    private IThreadHandling threadHandling;
    private ITimer timer;

    [TestInitialize]
    public void TestInitialize()
    {
        timer = Substitute.For<ITimer>();
        smartNotificationService = Substitute.For<ISmartNotificationService>();
        browserService = Substitute.For<IBrowserService>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        threadHandling = new NoOpThreadHandler();
        testSubject = new NotificationIndicatorViewModel(smartNotificationService, browserService, activeSolutionBoundTracker, threadHandling, timer);
    }

    [TestMethod]
    public void Ctor_SubscribesToEvents()
    {
        smartNotificationService.ReceivedWithAnyArgs(1).NotificationReceived += Arg.Any<EventHandler<NotificationReceivedEventArgs>>();
        activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged += Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
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

        SetupModelWithNotifications(true, true, []);
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
        var notificationViewModel = new NotificationIndicatorViewModel(smartNotificationService, browserService, activeSolutionBoundTracker, mockThreadHandling, timer);
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

        browserService.Received(1).Navigate("http://localhost:2000");
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

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        testSubject.Dispose();

        smartNotificationService.ReceivedWithAnyArgs(1).NotificationReceived -= Arg.Any<EventHandler<NotificationReceivedEventArgs>>();
        activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
    }

    [TestMethod]
    public void NotificationReceived_SetsNotificationEvents()
    {
        testSubject.AreNotificationsEnabled = true;
        testSubject.IsIconVisible = true;
        var smartNotification = new SmartNotification("Test message", "http://localhost:9000/project", ["scope1"], "QUALITY_GATE", "connectionId", DateTimeOffset.Now);

        smartNotificationService.NotificationReceived += Raise.EventWith(new NotificationReceivedEventArgs(smartNotification));

        testSubject.NotificationEvents.Should().HaveCount(1);
        testSubject.NotificationEvents[0].Category.Should().Be("QUALITY_GATE");
        testSubject.NotificationEvents[0].Text.Should().Be("Test message");
        testSubject.NotificationEvents[0].Link.Should().Be("http://localhost:9000/project");
        testSubject.HasUnreadEvents.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(SonarLintMode.Connected)]
    [DataRow(SonarLintMode.LegacyConnected)]
    public void SolutionBindingChanged_BindingToCloud_IsCloudIsTrue(SonarLintMode sonarLintMode)
    {
        var cloudBindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarCloud("my org"), sonarLintMode);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(SonarLintMode.Connected)]
    [DataRow(SonarLintMode.LegacyConnected)]
    public void SolutionBindingChanged_BindingToServer_IsCloudIsFalse(SonarLintMode sonarLintMode)
    {
        var cloudBindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarQube(new Uri("C:\\")), sonarLintMode);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeFalse();
    }

    [TestMethod]
    public void SolutionBindingChanged_Standalone_IsCloudIsFalse()
    {
        var cloudBindingConfiguration = new BindingConfiguration(null, SonarLintMode.Standalone, string.Empty);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(SonarLintMode.Connected)]
    [DataRow(SonarLintMode.LegacyConnected)]
    public void SolutionBindingChanged_BoundToServer_IsIconVisibleIsTrue(SonarLintMode sonarLintMode)
    {
        var bindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarQube(new Uri("http://localhost")), sonarLintMode);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(bindingConfiguration));

        testSubject.IsIconVisible.Should().BeTrue();
    }

    [TestMethod]
    public void SolutionBindingChanged_Standalone_IsIconVisibleIsFalse()
    {
        var standaloneConfiguration = new BindingConfiguration(null, SonarLintMode.Standalone, string.Empty);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(standaloneConfiguration));

        testSubject.IsIconVisible.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SolutionBindingChanged_BoundToServer_AreNotificationsEnabledReflectsSettings(bool isSmartNotificationsEnabled)
    {
        var serverConnection = new ServerConnection.SonarQube(new Uri("http://localhost"), new ServerConnectionSettings(isSmartNotificationsEnabled));
        var bindingConfiguration = CreateBindingConfiguration(serverConnection, SonarLintMode.Connected);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(bindingConfiguration));

        testSubject.AreNotificationsEnabled.Should().Be(isSmartNotificationsEnabled);
    }

    [TestMethod]
    public void SolutionBindingChanged_Standalone_AreNotificationsEnabledIsFalse()
    {
        var standaloneConfiguration = new BindingConfiguration(null, SonarLintMode.Standalone, string.Empty);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(standaloneConfiguration));

        testSubject.AreNotificationsEnabled.Should().BeFalse();
    }

    private static BindingConfiguration CreateBindingConfiguration(ServerConnection serverConnection, SonarLintMode mode) =>
        new(new BoundServerProject("my solution", "my project", serverConnection), mode, string.Empty);

    private static SmartNotification CreateNotification(string category, string url = "http://localhost") => new("test", url, [], category, "connectionId", DateTimeOffset.Now);

    private void SetupModelWithNotifications(bool areEnabled, bool areVisible, SmartNotification[] events)
    {
        testSubject.AreNotificationsEnabled = areEnabled;
        testSubject.IsIconVisible = areVisible;

        testSubject.SetNotificationEvents(events);
    }
}
