//-----------------------------------------------------------------------
// <copyright file="IUserNotification.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
