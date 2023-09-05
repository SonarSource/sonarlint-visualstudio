/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Telemetry;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Notifications.UnitTests
{
    [TestClass]
    public class NotificationIndicatorViewModelTests
    {
        private static SonarQubeNotification[] testEvents =
            new []
            {
                new SonarQubeNotification("foo", "foo", new Uri("http://foo.com"),
                    new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)))
            };

        [TestMethod]
        public void Text_Raises_PropertyChanged()
        {
            var model = CreateTestSubject();
            var monitor = model.Monitor();

            model.ToolTipText = "test";

            model.ToolTipText.Should().Be("test");
            monitor.Should().RaisePropertyChangeFor(x => x.ToolTipText);
        }

        [TestMethod]
        public void HasUnreadEvents_Raises_PropertyChanged()
        {
            var model = CreateTestSubject();
            var monitor = model.Monitor();

            model.HasUnreadEvents = true;

            model.HasUnreadEvents.Should().BeTrue();
            monitor.Should().RaisePropertyChangeFor(x => x.HasUnreadEvents);
        }

        [TestMethod]
        public void IsIconVisible_Raises_PropertyChanged()
        {
            var model = CreateTestSubject();
            var monitor = model.Monitor();

            model.IsIconVisible = true;

            model.IsIconVisible.Should().BeTrue();
            monitor.Should().RaisePropertyChangeFor(x => x.IsIconVisible);
        }

        [TestMethod]
        public void AreNotificationsEnabled_Raises_PropertyChanged()
        {
            var model = CreateTestSubject();
            var monitor = model.Monitor();

            model.AreNotificationsEnabled = true;

            model.AreNotificationsEnabled.Should().BeTrue();
            monitor.Should().RaisePropertyChangeFor(x => x.AreNotificationsEnabled);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void AreNotificationsEnabled_UpdatesTelemetry(bool areEnabled)
        {
            var telemetryManager = new Mock<IServerNotificationsTelemetryManager>();
            var model = CreateTestSubject(telemetryManager: telemetryManager.Object);

            model.AreNotificationsEnabled = areEnabled;

            telemetryManager.Verify(x=> x.NotificationsToggled(areEnabled));
            telemetryManager.VerifyNoOtherCalls();

            model.AreNotificationsEnabled = !areEnabled;

            telemetryManager.Verify(x => x.NotificationsToggled(!areEnabled));
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IsToolTipVisible_Raises_PropertyChanged()
        {
            var model = CreateTestSubject();
            var monitor = model.Monitor();

            model.IsToolTipVisible = true;

            model.IsToolTipVisible.Should().BeTrue();
            monitor.Should().RaisePropertyChangeFor(x => x.IsToolTipVisible);
        }

        [TestMethod]
        public void IsToolTipVisible_False_Stops_Timer()
        {
            // Arrange
            var timerMock = new Mock<ITimer>();

            var model = CreateTestSubject(timerMock.Object);
            model.IsToolTipVisible = true;

            // Act
            model.IsToolTipVisible = false;

            timerMock.Verify(x => x.Stop(), Times.Once);
        }

        [TestMethod]
        public void HasUnreadEvents_WithNo_Events_UpdatesTooltipText()
        {
            var model = CreateTestSubject();
            var monitor = model.Monitor();

            model.IsIconVisible = true;
            model.AreNotificationsEnabled = true;
            model.IsToolTipVisible = true;
            monitor.Should().RaisePropertyChangeFor(x => x.IsToolTipVisible);

            model.ToolTipText.Should().Be("You have no unread events.");
        }

        [TestMethod]
        public void HasUnreadEvents_WithEvents_UpdatesTooltipText()
        {
            var timerMock = new Mock<ITimer>();
            var model = CreateTestSubject(timerMock.Object);
            var monitor = model.Monitor();

            model.IsIconVisible = true;
            model.AreNotificationsEnabled = true;
            model.IsToolTipVisible = true;
            model.SetNotificationEvents(testEvents);

            monitor.Should().RaisePropertyChangeFor(x => x.IsToolTipVisible);

            model.ToolTipText.Should().Be("You have 1 unread event.");
        }

        [TestMethod]
        public void SetNotificationEvents_SetEvents_SetsHasUnreadEvents()
        {
            var model = SetupModelWithNotifications(false, false, testEvents);
            model.HasUnreadEvents.Should().BeFalse();

            model = SetupModelWithNotifications(false, true, testEvents);
            model.HasUnreadEvents.Should().BeFalse();

            model = SetupModelWithNotifications(true, false, testEvents);
            model.HasUnreadEvents.Should().BeFalse();

            model = SetupModelWithNotifications(true, true, new SonarQubeNotification[0]);
            model.HasUnreadEvents.Should().BeFalse();

            model = SetupModelWithNotifications(true, true, null);
            model.HasUnreadEvents.Should().BeFalse();

            model = SetupModelWithNotifications(true, true, testEvents);
            model.HasUnreadEvents.Should().BeTrue();
        }

        [TestMethod]
        public void SetNotificationEvents_SetEvents_UpdatesTelemetry()
        {
            var telemetryManager = new Mock<IServerNotificationsTelemetryManager>();
            var model = CreateTestSubject(telemetryManager: telemetryManager.Object);
            model.AreNotificationsEnabled = true;
            model.IsIconVisible = true;

            var events = new[]
            {
                CreateNotification("category1"),
                CreateNotification("category2"),
                CreateNotification("category1"),
                CreateNotification("category3"),
            };

            model.SetNotificationEvents(events);

            telemetryManager.Verify(x=> x.NotificationReceived("category1"), Times.Exactly(2));
            telemetryManager.Verify(x=> x.NotificationReceived("category2"), Times.Once());
            telemetryManager.Verify(x=> x.NotificationReceived("category3"), Times.Once());

            telemetryManager.Reset();
            model.SetNotificationEvents(Enumerable.Empty<SonarQubeNotification>());

            telemetryManager.Verify(x => x.NotificationReceived(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HasUnreadEvents_RunOnUIThread()
        {
            var threadHandling = new Mock<IThreadHandling>();
            var model = CreateTestSubject(threadHandling: threadHandling.Object);
            model.AreNotificationsEnabled = true;
            model.IsIconVisible = true;

            var events = new[]
            {
                CreateNotification("category1")
            };

            model.SetNotificationEvents(events);

            threadHandling.Verify(x => x.RunOnUIThread(It.IsAny<Action>()), Times.Once);
        }

        [TestMethod]
        public void NavigateToNotification_NotificationNavigated()
        {
            var vsBrowserService = new Mock<IBrowserService>();
            var testSubject = CreateTestSubject(vsBrowserService: vsBrowserService.Object);

            var notification = CreateNotification("test", "http://localhost:2000");
            testSubject.NavigateToNotification.Execute(notification);

            vsBrowserService.Verify(x=> x.Navigate("http://localhost:2000/"), Times.Once());
        }

        [TestMethod]
        public void NavigateToNotification_TelemetrySent()
        {
            var telemetryManager = new Mock<IServerNotificationsTelemetryManager>();
            var testSubject = CreateTestSubject(telemetryManager: telemetryManager.Object);

            var notification = CreateNotification("test");
            testSubject.NavigateToNotification.Execute(notification);

            telemetryManager.Verify(x => x.NotificationClicked("test"), Times.Once());
        }

        [TestMethod]
        public void NavigateToNotification_TooltipClosed()
        {
            var testSubject = CreateTestSubject();

            testSubject.IsToolTipVisible = true;

            var notification = CreateNotification("test");
            testSubject.NavigateToNotification.Execute(notification);

            testSubject.IsToolTipVisible.Should().BeFalse();
        }

        private SonarQubeNotification CreateNotification(string category, string url = "http://localhost") => new SonarQubeNotification(category, "test", new Uri(url), DateTimeOffset.Now);

        [TestMethod]
        public void ClearUnreadEventsCommand_Sets_HasUnreadEvents_False()
        {
            // Arrange
           var model = SetupModelWithNotifications(true, true, testEvents);
            model.HasUnreadEvents.Should().BeTrue();

            // Act
            model.ClearUnreadEventsCommand.Execute(null);

            // Assert
            model.HasUnreadEvents.Should().BeFalse();
        }

        private NotificationIndicatorViewModel SetupModelWithNotifications(bool areEnabled, bool areVisible, SonarQubeNotification[] events)
        {
            var model = CreateTestSubject();
            model.AreNotificationsEnabled = areEnabled;
            model.IsIconVisible = areVisible;
            
            model.SetNotificationEvents(events);

            return model;
        }

        private NotificationIndicatorViewModel CreateTestSubject(ITimer timer = null, 
            IServerNotificationsTelemetryManager telemetryManager = null,
            IBrowserService vsBrowserService = null,
            IThreadHandling threadHandling = null)
        {
            timer ??= Mock.Of<ITimer>();
            telemetryManager ??= Mock.Of<IServerNotificationsTelemetryManager>();
            vsBrowserService ??= Mock.Of<IBrowserService>();
            threadHandling ??= new NoOpThreadHandler();

            return new NotificationIndicatorViewModel(telemetryManager, vsBrowserService, threadHandling, timer);
        }
    }
}
