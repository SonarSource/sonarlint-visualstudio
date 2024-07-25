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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.SLCore.NodeJS.Notifications
{
    public interface IUnsupportedNodeVersionNotificationService
    {
        void Show(string languageName, string minVersion, string currentVersion = null);
    }

    [Export(typeof(IUnsupportedNodeVersionNotificationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class UnsupportedNodeVersionNotificationService : IUnsupportedNodeVersionNotificationService
    {
        private readonly INotificationService notificationService;
        private readonly IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction;
        private readonly IBrowserService browserService;

        private const string NotificationId = "sonarlint.nodejs.min.version.not.found";

        [ImportingConstructor]
        public UnsupportedNodeVersionNotificationService(INotificationService notificationService,
            IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction,
            IBrowserService browserService)
        {
            this.notificationService = notificationService;
            this.doNotShowAgainNotificationAction = doNotShowAgainNotificationAction;
            this.browserService = browserService;
        }

        public void Show(string languageName, string minVersion, string currentVersion = null)
        {
            notificationService.ShowNotification(new VisualStudio.Core.Notifications.Notification(
                id: NotificationId,
                message: string.Format(NotificationStrings.NotificationUnsupportedNode, languageName, minVersion, currentVersion ?? NotificationStrings.NotificationNoneVersion),
                actions: new INotificationAction[]
                {
                    new NotificationAction(NotificationStrings.NotificationShowMoreInfoAction, _ => ShowMoreInfo(), false),
                    doNotShowAgainNotificationAction
                }));
        }

        private void ShowMoreInfo()
        {
            browserService.Navigate(DocumentationLinks.LanguageSpecificRequirements_JsTs);
        }
    }
}
