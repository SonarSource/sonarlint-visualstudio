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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.TypeScript.Notifications
{
    internal interface IUnsupportedNodeVersionNotificationService
    {
        void Show();
    }

    [Export(typeof(IUnsupportedNodeVersionNotificationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class UnsupportedNodeVersionNotificationService : IUnsupportedNodeVersionNotificationService
    {
        private readonly INotificationService notificationService;
        private readonly IBrowserService browserService;
        private readonly INotification notification;

        private const string NotificationId = "sonarlint.nodejs.min.version.not.found.14.17";

        [ImportingConstructor]
        public UnsupportedNodeVersionNotificationService(INotificationService notificationService, 
            IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction,
            IBrowserService browserService)
        {
            this.notificationService = notificationService;
            this.browserService = browserService;

            notification = new Notification(
                id: NotificationId,
                message: Resources.NotificationUnsupportedNode,
                actions: new INotificationAction[]
                {
                    new NotificationAction(Resources.NotificationShowMoreInfoAction, _ => ShowMoreInfo(), false),
                    doNotShowAgainNotificationAction
                }
            );
        }

        public void Show()
        {
            notificationService.ShowNotification(notification);
        }

        private void ShowMoreInfo()
        {
            browserService.Navigate("https://docs.sonarsource.com/sonarlint/visual-studio/getting-started/requirements/#nodejs-prerequisites-for-js-and-ts");
        }
    }
}
