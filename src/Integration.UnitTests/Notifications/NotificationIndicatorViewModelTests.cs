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
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Notifications.UnitTests
{
    [TestClass]
    public class NotificationIndicatorViewModelTests
    {
        private static SonarQubeNotification testEvent = new SonarQubeNotification
        {
                Category = "foo",
                Message = "foo",
                Link = new Uri("http://foo.com"),
                Date = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.FromHours(2))
            };

        private static SonarQubeNotification[] testEvents = new SonarQubeNotification[] { testEvent };

        [TestMethod]
        public void Text_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.TooltipText = "test";

            model.ShouldRaisePropertyChangeFor(x => x.TooltipText);
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
        public void IsBalloonTooltipVisible_Raises_PropertyChanged()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.IsBalloonTooltipVisible = true;

            model.ShouldRaisePropertyChangeFor(x => x.IsBalloonTooltipVisible);
        }

        [TestMethod]
        public void HasUnreadEvents_WithNo_Events_UpdatesTooltipText()
        {
            var model = new NotificationIndicatorViewModel();
            model.MonitorEvents();

            model.IsIconVisible = true;
            model.AreNotificationsEnabled = true;
            model.IsBalloonTooltipVisible = true;
            model.ShouldRaisePropertyChangeFor(x => x.IsBalloonTooltipVisible);

            model.TooltipText.Should().Be("You have no unread events.");
        }

        [TestMethod]
        public void HasUnreadEvents_WithEvents_UpdatesTooltipText()
        {
            var model = new NotificationIndicatorViewModel(a => a());
            model.MonitorEvents();

            model.IsIconVisible = true;
            model.AreNotificationsEnabled = true;
            model.IsBalloonTooltipVisible = true;
            model.SetNotificationEvents(new[] { testEvent} );

            model.ShouldRaisePropertyChangeFor(x => x.IsBalloonTooltipVisible);

            model.TooltipText.Should().Be("You have 1 unread event.");
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
            var model = new NotificationIndicatorViewModel(a => a());
            model.AreNotificationsEnabled = areEnabled;
            model.IsIconVisible = areVisible;
            model.SetNotificationEvents(events);

            return model;
        }
    }
}
