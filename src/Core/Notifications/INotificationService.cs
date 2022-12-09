﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Linq;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.Core.Notifications
{
    public interface INotificationService : IDisposable
    {
        void ShowNotification(INotification notification);
    }

    [Export(typeof(INotificationService))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class NotificationService : INotificationService
    {
        /// <summary>
        /// Taken from "ToolWindowGuids80.ErrorList"
        /// </summary>
        internal static readonly Guid ErrorListToolWindowGuid = new Guid("D78612C7-9962-4B83-95D9-268046DAD23A");

        private readonly IInfoBarManager infoBarManager;
        private readonly IDisabledNotificationsStorage notificationsStorage;
        private readonly IThreadHandling threadHandling;
        private readonly ILogger logger;

        private readonly HashSet<string> oncePerSessionNotifications = new HashSet<string>();

        private Tuple<IInfoBar, INotification> activeNotification;

        [ImportingConstructor]
        public NotificationService(IInfoBarManager infoBarManager, 
            IDisabledNotificationsStorage notificationsStorage,
            IThreadHandling threadHandling, 
            ILogger logger)
        {
            this.infoBarManager = infoBarManager;
            this.notificationsStorage = notificationsStorage;
            this.threadHandling = threadHandling;
            this.logger = logger;
        }

        public void ShowNotification(INotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }

            if (notificationsStorage.IsNotificationDisabled(notification.Id))
            {
                logger.LogVerbose($"[NotificationService] notification '{notification.Id}' will not be shown: notification is blocked.");
                return;
            }

            if (oncePerSessionNotifications.Contains(notification.Id))
            {
                logger.LogVerbose($"[NotificationService] notification '{notification.Id}' will not be shown: notification has already been displayed.");
                return;
            }

            threadHandling.RunOnUIThread(() =>
            {
                RemoveExistingInfoBar();
                ShowInfoBar(notification);
            });
        }

        private void CurrentInfoBar_ButtonClick(object sender, InfoBarButtonClickedEventArgs e)
        {
            try
            {
                var notification = activeNotification.Item2;
                var matchingAction = notification.Actions.FirstOrDefault(x => x.CommandText == e.ClickedButtonText);

                matchingAction?.Action(notification);
                
                if(matchingAction?.ShouldDismissAfterAction == true)
                {
                    RemoveExistingInfoBar();
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CoreStrings.Notifications_FailedToExecuteAction, ex);
            }
        }

        private void CurrentInfoBar_Closed(object sender, EventArgs e)
        {
            RemoveExistingInfoBar();
        }

        private void RemoveExistingInfoBar()
        {
            if (activeNotification == null)
            {
                return;
            }

            try
            {
                activeNotification.Item1.ButtonClick -= CurrentInfoBar_ButtonClick;
                activeNotification.Item1.Closed -= CurrentInfoBar_Closed;
                infoBarManager.DetachInfoBar(activeNotification.Item1);
                activeNotification = null;
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CoreStrings.Notifications_FailedToRemove, ex);
            }
        }

        private void ShowInfoBar(INotification notification)
        {
            try
            {
                var buttonTexts = notification.Actions.Select(x => x.CommandText).ToArray();

                var infoBar = infoBarManager.AttachInfoBarWithButtons(ErrorListToolWindowGuid,
                    notification.Message,
                    buttonTexts,
                    SonarLintImageMoniker.OfficialSonarLintMoniker);

                activeNotification = new Tuple<IInfoBar, INotification>(infoBar, notification);
                activeNotification.Item1.ButtonClick += CurrentInfoBar_ButtonClick;
                activeNotification.Item1.Closed += CurrentInfoBar_Closed;

                oncePerSessionNotifications.Add(notification.Id);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CoreStrings.Notifications_FailedToDisplay, notification.Id, ex);
            }
        }

        public void Dispose()
        {
            RemoveExistingInfoBar();
        }
    }
}
