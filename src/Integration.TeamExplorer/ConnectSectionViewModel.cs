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
using System.Windows.Input;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.WPF;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    // TE: Simplified replacement for TeamExplorerSectionViewModelBase, implementing just the methods
    // required by our code.
    internal class DummyTeamExplorerSectionViewModelBase : SonarLint.VisualStudio.Integration.WPF.ViewModelBase
    {
        public bool IsBusy { get; set; }
        public bool IsExpanded { get; set; }
        public bool IsVisible { get; set; }
        public string Title { get; set; }
    }

    internal class ConnectSectionViewModel : DummyTeamExplorerSectionViewModelBase,
                                        IConnectSectionViewModel,
                                        IUserNotification
    {

        // Replacement for Microsoft.TeamFoundation.Controls.NotificationType
        private enum NotificationType
        {
            Information = 0,
            Warning = 1,
            Error = 2
        }

        private TransferableVisualState state;
        private ICommand connectCommand;
        private ICommand<BindCommandArgs> bindCommand;
        private ICommand<string> browseToUrl;

        public ConnectSectionViewModel()
        {
            this.Title = Resources.Strings.ConnectSectionTitle;
            this.IsExpanded = true;
            this.IsVisible = true;
        }

        #region IUserNotification

        public void ShowNotificationError(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.ShowNotification(message, NotificationType.Error, associatedCommand, notificationId);
        }

        public void ShowNotificationWarning(string message, Guid notificationId, ICommand associatedCommand)
        {
            this.ShowNotification(message, NotificationType.Warning, associatedCommand, notificationId);
        }

        public bool HideNotification(Guid id)
        {
            // TODO - TE: implement notifications
            // This method was provided by TeamExplorerSectionViewModelBase. We need to provide an alternative implementation.
            return true;
        }

        private void ShowNotification(string message, NotificationType notificationType,
            ICommand associatedCommand, Guid notificationId)
        {
            // TODO - TE: implement notifications
            // This method was provided by TeamExplorerSectionViewModelBase. We need to provide an alternative implementation.
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

        public ICommand<BindCommandArgs> BindCommand
        {
            get { return this.bindCommand; }
            set { SetAndRaisePropertyChanged(ref this.bindCommand, value); }
        }

        public ICommand<string> BrowseToUrlCommand
        {
            get { return this.browseToUrl; }
            set { SetAndRaisePropertyChanged(ref this.browseToUrl, value); }
        }

        #endregion
    }
}
