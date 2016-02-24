//-----------------------------------------------------------------------
// <copyright file="ConnectSectionViewModel.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    internal class ConnectSectionViewModel : TeamExplorerSectionViewModelBase,
                                        IUserNotification /* Most of it implemented by TeamExplorerSectionViewModelBase */
    {
        private ObservableCollection<ServerViewModel> connectedServers;
        private ObservableCollection<ProjectViewModel> boundProjects;
        private ICommand connectCommand;
        private ICommand bindCommand;

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

        public ObservableCollection<ServerViewModel> ConnectedServers
        {
            get { return this.connectedServers; }
            set { this.SetAndRaisePropertyChanged(ref this.connectedServers, value); }
        }

        public ObservableCollection<ProjectViewModel> BoundProjects
        {
            get { return this.boundProjects; }
            set { this.SetAndRaisePropertyChanged(ref this.boundProjects, value); }
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

        #endregion
    }
}
