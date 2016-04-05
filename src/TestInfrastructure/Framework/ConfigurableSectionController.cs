//-----------------------------------------------------------------------
// <copyright file="ConfigurableSectionController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSectionController : ISectionController
    {
        #region  ISectionController
        public ICommand BindCommand
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

        public IProgressControlHost ProgressHost
        {
            get;
            set;
        }

        public ICommand RefreshCommand
        {
            get;
            set;
        }

        public ICommand ToggleShowAllProjectsCommand
        {
            get;
            set;
        }

        public IUserNotification UserNotifications
        {
            get;
            set;
        }

        public ConnectSectionViewModel ViewModel
        {
            get;
            set;
        }

        public ConnectSectionView View
        {
            get;
            set;
        }

        public ICommand BrowseToUrlCommand
        {
            get;
            set;
        }

        public ICommand BrowseToProjectDashboardCommand
        {
            get;
            set;
        }
        #endregion

        public static ConfigurableSectionController CreateDefault()
        {
            var section = new ConfigurableSectionController();
            section.ViewModel = new ConnectSectionViewModel();
            section.View = new ConnectSectionView();
            section.ProgressHost = new ConfigurableProgressControlHost();
            section.UserNotifications = new ConfigurableUserNotification();
            section.BindCommand = new RelayCommand(() => { });
            section.ConnectCommand = new RelayCommand(() => { });
            section.DisconnectCommand = new RelayCommand(() => { });
            section.RefreshCommand = new RelayCommand(() => { });
            section.BrowseToUrlCommand = new RelayCommand(() => { });
            section.BrowseToProjectDashboardCommand = new RelayCommand(() => { });
            section.ToggleShowAllProjectsCommand = new RelayCommand(() => { });
            return section;
        }
    }
}
