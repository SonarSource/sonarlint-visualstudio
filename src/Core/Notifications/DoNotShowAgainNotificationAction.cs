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

namespace SonarLint.VisualStudio.Core.Notifications
{
    public interface IDoNotShowAgainNotificationAction : INotificationAction
    {
    }

    [Export(typeof(IDoNotShowAgainNotificationAction))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class DoNotShowAgainNotificationAction : IDoNotShowAgainNotificationAction
    {
        private readonly IDisabledNotificationsStorage disabledNotificationsStorage;

        [ImportingConstructor]
        public DoNotShowAgainNotificationAction(IDisabledNotificationsStorage disabledNotificationsStorage)
        {
            this.disabledNotificationsStorage = disabledNotificationsStorage;
        }

        public string CommandText => CoreStrings.Notifications_DontShowAgainAction;

        public bool ShouldDismissAfterAction => true;

        public Action<INotification> Action => 
            notification =>
            {
                if (notification == null)
                {
                    throw new ArgumentNullException(nameof(notification));
                }
                
                disabledNotificationsStorage.DisableNotification(notification.Id);
            };
    }
}
