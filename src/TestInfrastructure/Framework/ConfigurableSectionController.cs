/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Windows.Input;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSectionController : ISectionController
    {
        #region ISectionController

        public ICommand<BindCommandArgs> BindCommand
        {
            get;
            set;
        }

        public ICommand ConnectCommand
        {
            get;
            set;
        }

        public ICommand DisconnectCommand
        {
            get;
            set;
        }

        public ICommand ReconnectCommand
        {
            get;
            set;
        }

        public IProgressControlHost ProgressHost
        {
            get;
            set;
        }

        public ICommand<ConnectionInformation> RefreshCommand
        {
            get;
            set;
        }

        public ICommand<ServerViewModel> ToggleShowAllProjectsCommand
        {
            get;
            set;
        }

        public IUserNotification UserNotifications
        {
            get;
            set;
        }

        public IConnectSectionViewModel ViewModel
        {
            get;
            set;
        }

        public IConnectSectionView View
        {
            get;
            set;
        }

        public ICommand<string> BrowseToUrlCommand
        {
            get;
            set;
        }

        public ICommand<ProjectViewModel> BrowseToProjectDashboardCommand
        {
            get;
            set;
        }

        #endregion ISectionController

        #region Test helpers

        public static ConfigurableSectionController CreateDefault()
        {
            var section = new ConfigurableSectionController();
            section.ViewModel = new ConnectSectionViewModel();
            section.View = new ConnectSectionView();
            section.ProgressHost = new ConfigurableProgressControlHost();
            section.UserNotifications = new ConfigurableUserNotification();
            section.BindCommand = new RelayCommand<BindCommandArgs>(args => { });
            section.ConnectCommand = new RelayCommand(() => { });
            section.DisconnectCommand = new RelayCommand(() => { });
            section.ReconnectCommand = new RelayCommand(() => { });
            section.RefreshCommand = new RelayCommand<ConnectionInformation>(c => { });
            section.BrowseToUrlCommand = new RelayCommand<string>(url => { });
            section.BrowseToProjectDashboardCommand = new RelayCommand<ProjectViewModel>(vm => { });
            section.ToggleShowAllProjectsCommand = new RelayCommand<ServerViewModel>(vm => { });
            return section;
        }

        #endregion Test helpers
    }
}
