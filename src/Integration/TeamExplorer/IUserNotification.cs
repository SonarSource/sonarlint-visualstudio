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
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    /// <summary>
    /// Show notifications to the user
    /// </summary>
    internal interface IUserNotification
    {
        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowBusy"/>
        /// </summary>
        void ShowBusy();

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.HideBusy"/>
        /// </summary>
        void HideBusy();

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowError(string)"/>
        /// </summary>
        void ShowError(string errorMessage);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowException(Exception, bool)"/>
        /// </summary>
        void ShowException(Exception ex, bool clearOtherNotifications = true);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowMessage(string)"/>
        /// </summary>
        void ShowMessage(string message);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ShowWarning(string)"/>
        /// </summary>
        void ShowWarning(string warningMessage);

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.ClearNotifications"/>
        /// </summary>
        void ClearNotifications();

        /// <summary>
        /// <see cref="TeamFoundation.Controls.WPF.TeamExplorer.TeamExplorerSectionViewModelBase.HideNotification(Guid)"/>
        /// </summary>
        bool HideNotification(Guid id);

        void ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand);

        void ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand);
    }
}
