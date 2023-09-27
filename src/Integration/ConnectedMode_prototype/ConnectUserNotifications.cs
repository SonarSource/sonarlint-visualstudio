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

using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.ConnectedMode_prototype
{
    // TE: TODO - there is an existing generic INotificationService service which uses the IInfoBarManager
    // internally. However, it's currently not generic enough and would need some refactoring to be used
    // here:
    // * it's hard-coded to the put the gold bar on the main window.
    // * (?) it assumes only one notification is active at a time (?)
    // * it has special logic about only showing some notifications once per session which isn't appropriate here.
    [Export(typeof(IUserNotification))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class ConnectUserNotifications : IUserNotification, IDisposable
    {
        // Hard-coded tool window guid
        private static readonly Guid NotificationToolWindowGuid = ConnectedModeToolWindow.ToolWindowId;

        private readonly IInfoBarManager infoBarManager;
        private readonly IThreadHandling threadHandling;
        private readonly ILogger logger;

        private Tuple<IInfoBar, INotification> activeNotification;

        [ImportingConstructor]
        public ConnectUserNotifications(IInfoBarManager infoBarManager,
            ILogger logger,
            IThreadHandling threadHandling)
        { 
            this.infoBarManager = infoBarManager;
            this.threadHandling = threadHandling;
        }

        public bool HideNotification(Guid id)
        {
            // TODO - always removing the current infobar i.e. currently ignores the supplied id
            RemoveExistingInfoBar();
            return true;
        }

        public void ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand)
            => ShowNotification(message, notificationId, associatedCommand, true);

        public void ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand)
            => ShowNotification(message, notificationId, associatedCommand, false);

        public void ShowNotification(string message, Guid notificationId, ICommand associatedCommand, bool isError)
        {
            threadHandling.RunOnUIThread2(() =>
            {
                RemoveExistingInfoBar();

                // TE - TODO - the format of the messages displayed in the Team Explorer is different from the gold bar format.
                // The Team Explorer version can embed actions in the text, whereas our implementation expects the actions
                // to be at the end. However, we own the IUserNotification interface, so we can change it easily.
                var notification = new Notification(notificationId.ToString(),
                    isError ? "Error: " : "Warning: ",
                    new INotificationAction[] { new NotificationAction(message, x => associatedCommand.Execute(x), false) }
                    );
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

                if (matchingAction?.ShouldDismissAfterAction == true)
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

            threadHandling.RunOnUIThread2(() =>
            {
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
            });
        }

        // TE - TODO - just using the INotification interface for convenience here as a quick and dirty
        // hack i.e. so we can just copy and reuse most of the code from the NotificationService.
        private void ShowInfoBar(INotification notification)
        {
            try
            {
                var buttonTexts = notification.Actions.Select(x => x.CommandText).ToArray();

                var infoBar = infoBarManager.AttachInfoBarWithButtons(NotificationToolWindowGuid, notification.Message,
                    buttonTexts,
                    SonarLintImageMoniker.OfficialSonarLintMoniker);

                activeNotification = new Tuple<IInfoBar, INotification>(infoBar, notification);
                activeNotification.Item1.ButtonClick += CurrentInfoBar_ButtonClick;
                activeNotification.Item1.Closed += CurrentInfoBar_Closed;
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
