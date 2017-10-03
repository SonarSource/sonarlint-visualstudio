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
            model.MonitorEvents();

            model.ToolTipText = "test";

            model.ShouldRaisePropertyChangeFor(x => x.ToolTipText);
        }

        [TestMethod]
        public void HasUnreadEvents_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.HasUnreadEvents = true;

            model.ShouldRaisePropertyChangeFor(x => x.HasUnreadEvents);
        }

        [TestMethod]
        public void IsIconVisible_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.IsIconVisible = true;

            model.ShouldRaisePropertyChangeFor(x => x.IsIconVisible);
        }

        [TestMethod]
        public void AreNotificationsEnabled_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.AreNotificationsEnabled = true;

            model.ShouldRaisePropertyChangeFor(x => x.AreNotificationsEnabled);
        }

        [TestMethod]
        public void IsToolTipVisible_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.IsToolTipVisible = true;

            model.ShouldRaisePropertyChangeFor(x => x.IsToolTipVisible);
        }

        [TestMethod]
        public void HasUnreadEvents_WithNo_Events_UpdatesTooltipText()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.IsIconVisible = true;
            model.AreNotificationsEnabled = true;
            model.IsToolTipVisible = true;
            model.ShouldRaisePropertyChangeFor(x => x.IsToolTipVisible);

            model.ToolTipText.Should().Be("You have no unread events.");
        }

        [TestMethod]
        public void HasUnreadEvents_WithEvents_UpdatesTooltipText()
        {
            var timerMock = new Mock<ITimer>();
            var model = new NotificationIndicatorViewModel(a => a(), timerMock.Object);
            model.MonitorEvents();

            model.IsIconVisible = true;
            model.AreNotificationsEnabled = true;
            model.IsToolTipVisible = true;
            model.SetNotificationEvents(testEvents);

            model.ShouldRaisePropertyChangeFor(x => x.IsToolTipVisible);

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

        private NotificationIndicatorViewModel SetupModelWithNotifications(bool areEnabled,
            bool areVisible, SonarQubeNotification[] events)
        {
            var timerMock = new Mock<ITimer>();
            var model = new NotificationIndicatorViewModel(a => a(), timerMock.Object);
            model.AreNotificationsEnabled = areEnabled;
            model.IsIconVisible = areVisible;
            model.SetNotificationEvents(events);

            return model;
        }
    }
}
