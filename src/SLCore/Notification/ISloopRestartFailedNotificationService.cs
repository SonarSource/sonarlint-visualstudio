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

using System;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.SLCore.Notification
{
    public interface ISloopRestartFailedNotificationService
    {
        void Show(Action act);
    }

    [Export(typeof(ISloopRestartFailedNotificationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class SloopRestartFailedNotificationService : ISloopRestartFailedNotificationService
    {
        private readonly INotificationService notificationService;

        private const string NotificationId = "sonarlint.sloop.restart.failed";

        [ImportingConstructor]
        public SloopRestartFailedNotificationService(INotificationService notificationService)
        {
            this.notificationService = notificationService;
        }

        public void Show(Action act)
        {
            var notification = new VisualStudio.Core.Notifications.Notification(
                id: NotificationId,
                message: SLCoreStrings.SloopRestartFailedNotificationService_GoldBarMessage,
                actions: new[]
                {
                    new NotificationAction(SLCoreStrings.SloopRestartFailedNotificationService_Restart, _ => act(), true)
                },
                showOncePerSession: false
            );

            notificationService.ShowNotification(notification);
        }
    }
}
