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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Notifications.UnitTests
{
    [TestClass]
    public class SonarQubeNotifications_HasUnreadEvents
    {
        private SonarQubeNotifications notifications;

        [TestInitialize]
        public void TestInitialize()
        {
            notifications = new SonarQubeNotifications(
                new ConfigurableSonarQubeServiceWrapper(), new ConfigurableStateManager(), new Mock<ITimer>().Object);
        }

        [TestMethod]
        public void HasUnreadEvents_Raises_PropertyChanged()
        {
            // Arrange
            notifications.MonitorEvents();
            // Act
            notifications.HasUnreadEvents = true;
            // Assert
            notifications.ShouldRaisePropertyChangeFor(x => x.HasUnreadEvents);
        }

        [TestMethod]
        public void HasUnreadEvents_True_Sets_Text()
        {
            // NOTE: this case should not happen

            // Arrange
            notifications.HasUnreadEvents.Should().BeFalse(); // check default value

            // Act
            notifications.HasUnreadEvents = true;

            // Assert
            notifications.Text.Should().Be("You have no unread events.");
        }

        [TestMethod]
        public void HasUnreadEvents_True_NotificationEvents_Some_Sets_Text()
        {
            // Arrange
            notifications.HasUnreadEvents.Should().BeFalse(); // check default value
            notifications.NotificationEvents.Add(new NotificationEvent { });
            notifications.NotificationEvents.Add(new NotificationEvent { });

            // Act
            notifications.HasUnreadEvents = true;

            // Assert
            notifications.Text.Should().Be("You have 2 unread events.");
        }

        [TestMethod]
        public void HasUnreadEvents_False_Sets_Text()
        {
            // Arrange
            notifications.HasUnreadEvents = true;

            // Act
            notifications.HasUnreadEvents = false;

            // Assert
            notifications.Text.Should().Be("You have no unread events.");
        }

        [TestMethod]
        public void HasUnreadEvents_False_NotificationEvents_Some_Sets_Text()
        {
            // Arrange
            notifications.HasUnreadEvents = true;
            notifications.NotificationEvents.Add(new NotificationEvent { });
            notifications.NotificationEvents.Add(new NotificationEvent { });

            // Act
            notifications.HasUnreadEvents = false;

            // Assert
            notifications.Text.Should().Be("You have no unread events.");
        }
    }
}
