//-----------------------------------------------------------------------
// <copyright file="ConnectSectionViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using SonarLint.VisualStudio.Integration.State;
using System;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    public class ConnectSectionViewModel : TeamExplorerSectionViewModelBase,
                                        IUserNotification /* Most of it implemented by TeamExplorerSectionViewModelBase */
    {
        private TransferableVisualState state;
        private ICommand connectCommand;
        private ICommand bindCommand;
        private ICommand browseToUrl;

        public ConnectSectionViewModel()
        {
            this.Title = Resources.Strings.ConnectSectionTitle;
            this.IsExpanded = true;
            this.IsVisible = true;
        }

        #region IUserNotification

        public void ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.ShowNotification(message, NotificationType.Error, NotificationFlags.NoTooltips/*No need for them since we don't use hyperlinks*/, associatedCommand, notificationId);
        }

        public void ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.ShowNotification(message, NotificationType.Warning, NotificationFlags.NoTooltips/*No need for them since we don't use hyperlinks*/, associatedCommand, notificationId);
        }

        #endregion

        #region Properties

        public TransferableVisualState State
        {
            get { return this.state; }
            set { this.SetAndRaisePropertyChanged(ref this.state, value); }
        }

        #endregion

        #region Commands

        public ICommand ConnectCommand
        {
            get { return this.connectCommand; }
            set { SetAndRaisePropertyChanged(ref this.connectCommand, value); }
        }

        public ICommand BindCommand
        {
            get { return this.bindCommand; }
            set { SetAndRaisePropertyChanged(ref this.bindCommand, value); }
        }

        public ICommand BrowseToUrlCommand
        {
            get { return this.browseToUrl; }
            set { SetAndRaisePropertyChanged(ref this.browseToUrl, value); }
        }

        #endregion
    }
}
