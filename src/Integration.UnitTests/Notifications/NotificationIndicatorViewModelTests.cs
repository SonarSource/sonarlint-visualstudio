/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
            var model = new NotificationIndicatorViewModel();
            var monitor = model.Monitor();

            model.ToolTipText = "test";

            model.ToolTipText.Should().Be("test");
            monitor.Should().RaisePropertyChangeFor(x => x.ToolTipText);
        }

        [TestMethod]
        public void HasUnreadEvents_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            var monitor = model.Monitor();

            model.HasUnreadEvents = true;

            model.HasUnreadEvents.Should().BeTrue();
            monitor.Should().RaisePropertyChangeFor(x => x.HasUnreadEvents);
        }

        [TestMethod]
        public void IsIconVisible_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            var monitor = model.Monitor();

            model.IsIconVisible = true;

            model.IsIconVisible.Should().BeTrue();
            monitor.Should().RaisePropertyChangeFor(x => x.IsIconVisible);
        }

        [TestMethod]
        public void AreNotificationsEnabled_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            var monitor = model.Monitor();

            model.AreNotificationsEnabled = true;

            model.AreNotificationsEnabled.Should().BeTrue();
            monitor.Should().RaisePropertyChangeFor(x => x.AreNotificationsEnabled);
        }

        [TestMethod]
        public void IsToolTipVisible_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
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

            var model = new NotificationIndicatorViewModel(a => a(), timerMock.Object)
            {
                IsToolTipVisible = true
            };

            // Act
            model.IsToolTipVisible = false;

            timerMock.Verify(x => x.Stop(), Times.Once);
        }

        [TestMethod]
        public void HasUnreadEvents_WithNo_Events_UpdatesTooltipText()
        {
            var model = new NotificationIndicatorViewModel();
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
            var model = new NotificationIndicatorViewModel(a => a(), timerMock.Object);
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

        private NotificationIndicatorViewModel SetupModelWithNotifications(bool areEnabled,
            bool areVisible, SonarQubeNotification[] events)
        {
            var timerMock = new Mock<ITimer>();
            var model = new NotificationIndicatorViewModel(a => a(), timerMock.Object)
            {
                AreNotificationsEnabled = areEnabled,
                IsIconVisible = areVisible
            };
            model.SetNotificationEvents(events);

            return model;
        }
    }
}
