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
using System.Windows.Input;
using FluentAssertions;
using Microsoft.TeamFoundation.Controls;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableUserNotification : IUserNotification
    {
        private class Notification
        {
            public NotificationType Type { get; }

            public string Message { get; }

            public Notification(NotificationType type, string message)
            {
                this.Type = type;
                this.Message = message;
            }
        }

        private readonly List<string> showErrorRequests = new List<string>();
        private readonly IDictionary<Guid, Notification> notifications = new Dictionary<Guid, Notification>();

        #region IUserNotification

        void IUserNotification.ShowBusy()
        {
            throw new NotImplementedException();
        }

        void IUserNotification.HideBusy()
        {
            throw new NotImplementedException();
        }

        void IUserNotification.ShowError(string errorMessage)
        {
            this.showErrorRequests.Add(errorMessage);
        }

        void IUserNotification.ShowException(Exception ex, bool clearOtherNotifications)
        {
            throw new NotImplementedException();
        }

        void IUserNotification.ShowMessage(string message)
        {
            throw new NotImplementedException();
        }

        void IUserNotification.ShowWarning(string warningMessage)
        {
            throw new NotImplementedException();
        }

        void IUserNotification.ClearNotifications()
        {
            this.showErrorRequests.Clear();
            this.notifications.Clear();
        }

        bool IUserNotification.HideNotification(Guid id)
        {
            return this.notifications.Remove(id);
        }

        void IUserNotification.ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.notifications[notificationId] = new Notification(NotificationType.Error, message);
        }

        void IUserNotification.ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.notifications[notificationId] = new Notification(NotificationType.Warning, message);
        }

        #endregion IUserNotification

        #region Test helpers

        public void AssertNoShowErrorMessages()
        {
            this.showErrorRequests.Should().BeEmpty("Unexpected messages: {0}", string.Join(", ", this.showErrorRequests));
        }

        public void AssertSingleErrorMessage(string expected)
        {
            this.showErrorRequests.Should().HaveCount(1, "Unexpected messages: {0}", string.Join(", ", this.showErrorRequests));
            this.showErrorRequests[0].Should().Be(expected, "Unexpected message");
        }

        public void AssertNotification(Guid notificationId, string expected)
        {
            string message = this.AssertNotification(notificationId);
            message.Should().Be(expected, "Unexpected message");
        }

        public string AssertNotification(Guid notificationId)
        {
            Notification notification;
            this.notifications.TryGetValue(notificationId, out notification).Should().BeTrue("Unexpected notificationId: {0}", notificationId);
            return notification.Message;
        }

        public void AssertNoNotification(Guid notificationId)
        {
            Notification notification;
            this.notifications.TryGetValue(notificationId, out notification).Should().BeFalse("Unexpected notification: {0}", notification?.Message);
        }

        #endregion Test helpers
    }
}