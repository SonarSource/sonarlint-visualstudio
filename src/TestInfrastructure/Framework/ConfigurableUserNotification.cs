//-----------------------------------------------------------------------
// <copyright file="ConfigurableUserNotification.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.TeamExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls;

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

        #region  IUserNotification
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

        #endregion

        #region Test helpers

        public void AssertNoShowErrorMessages()
        {
            Assert.AreEqual(0, this.showErrorRequests.Count, "Unexpected messages: {0}", string.Join(", ", this.showErrorRequests));
        }

        public void AssertSingleErrorMessage(string expected)
        {
            Assert.AreEqual(1, this.showErrorRequests.Count, "Unexpected messages: {0}", string.Join(", ", this.showErrorRequests));
            Assert.AreEqual(expected, this.showErrorRequests[0], "Unexpected message");
        }

        public void AssertNotification(Guid notificationId, string expected)
        {
            string message = this.AssertNotification(notificationId);
            Assert.AreEqual(expected, message, "Unexpected message");
        }

        public string AssertNotification(Guid notificationId)
        {
            Notification notification;
            Assert.IsTrue(this.notifications.TryGetValue(notificationId, out notification), "Unexpected notificationId: {0}", notificationId);
            return notification.Message;
        }

        public void AssertNoNotification(Guid notificationId)
        {
            Notification notification;
            Assert.IsFalse(this.notifications.TryGetValue(notificationId, out notification), "Unexpected notification: {0}", notification?.Message);
        }

        #endregion
    }
}
