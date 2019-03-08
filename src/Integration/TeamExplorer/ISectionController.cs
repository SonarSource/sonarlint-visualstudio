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
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    /// <summary>
    /// Representation of the connect section
    /// </summary>
    internal interface ISectionController
    {
        /// <summary>
        /// The progress host
        /// </summary>
        /// <remarks>return <see cref="View"/> when the view specific host is the one to use</remarks>
        IProgressControlHost ProgressHost { get; }

        /// <summary>
        /// <see cref="ConnectSectionView"/>
        /// </summary>
        IConnectSectionView View { get; }

        /// <summary>
        /// <see cref="ConnectSectionViewModel"/>
        /// </summary>
        IConnectSectionViewModel ViewModel { get; }

        /// <summary>
        /// The notifications service to use
        /// </summary>
        ///<remarks>return <see cref="ViewModel"/> when the view model specific implementation is the one to use</remarks>
        IUserNotification UserNotifications { get; }

        ICommand ConnectCommand { get; }

        ICommand ReconnectCommand { get; }

        ICommand<BindCommandArgs> BindCommand { get; }

        ICommand<string> BrowseToUrlCommand { get; }

        ICommand<ProjectViewModel> BrowseToProjectDashboardCommand { get; }

        ICommand<ConnectionInformation> RefreshCommand { get; }

        ICommand DisconnectCommand { get; }

        ICommand<ServerViewModel> ToggleShowAllProjectsCommand { get; }
    }
}
