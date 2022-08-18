/*
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
using System.Linq;
using SonarLint.VisualStudio.Core.InfoBar;

namespace SonarLint.VisualStudio.Core.Notifications
{
    public interface INotificationService : IDisposable
    {
        void ShowNotification(INotification notification);
    }
    
    public sealed class NotificationService : INotificationService
    {
        /// <summary>
        /// Taken from "ToolWindowGuids80.ErrorList"
        /// </summary>
        internal static readonly Guid ErrorListToolWindowGuid = new Guid("D78612C7-9962-4B83-95D9-268046DAD23A");

        private readonly IInfoBarManager infoBarManager;
        private readonly IThreadHandling threadHandling;

        private Tuple<IInfoBar, INotification> activeNotification;

        public NotificationService(IInfoBarManager infoBarManager, IThreadHandling threadHandling)
        {
            this.infoBarManager = infoBarManager;
            this.threadHandling = threadHandling;
        }

        public void ShowNotification(INotification notification)
        {
            if (notification == null)
            {
                throw new ArgumentNullException(nameof(notification));
            }
            // todo: check if already shown

            threadHandling.RunOnUIThread(() =>
            {
                RemoveExistingInfoBar();

                var buttonTexts = notification.Actions.Select(x => x.CommandText).ToArray();

                var infoBar = infoBarManager.AttachInfoBarWithButtons(ErrorListToolWindowGuid,
                    notification.Message,
                    buttonTexts,
                    SonarLintImageMoniker.OfficialSonarLintMoniker);

                activeNotification = new Tuple<IInfoBar, INotification>(infoBar, notification); 
                activeNotification.Item1.ButtonClick += CurrentInfoBar_ButtonClick;
                activeNotification.Item1.Closed += CurrentInfoBar_Closed;
            });
        }

        private void CurrentInfoBar_ButtonClick(object sender, InfoBarButtonClickedEventArgs e)
        {
            var matchingAction = activeNotification.Item2.Actions.FirstOrDefault(x => x.CommandText == e.ClickedButtonText);

            matchingAction?.Action();

            // todo: handle "do not show again"
            // todo: who is responsible for adding "do not show again" action?
        }

        private void CurrentInfoBar_Closed(object sender, EventArgs e)
        {
            RemoveExistingInfoBar();
        }

        private void RemoveExistingInfoBar()
        {
            if (activeNotification != null)
            {
                activeNotification.Item1.ButtonClick -= CurrentInfoBar_ButtonClick;
                activeNotification.Item1.Closed -= CurrentInfoBar_Closed;
                infoBarManager.DetachInfoBar(activeNotification.Item1);
                activeNotification = null;
            }
        }

        public void Dispose()
        {
            RemoveExistingInfoBar();
        }
    }
}
