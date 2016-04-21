//-----------------------------------------------------------------------
// <copyright file="ISectionController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    /// <summary>
    /// Representation of the connect section
    /// </summary>
    public interface ISectionController
    {
        /// <summary>
        /// The progress host
        /// </summary>
        /// <remarks>return <see cref="View"/> when the view specific host is the one to use</remarks>
        IProgressControlHost ProgressHost { get; }

        /// <summary>
        /// <see cref="ConnectSectionView"/>
        /// </summary>
        ConnectSectionView View { get; }

        /// <summary>
        /// <see cref="ConnectSectionViewModel"/>
        /// </summary>
        ConnectSectionViewModel ViewModel { get; }

        /// <summary>
        /// The notifications service to use
        /// </summary>
        ///<remarks>return <see cref="ViewModel"/> when the view model specific implementation is the one to use</remarks>
        IUserNotification UserNotifications { get; }

        ICommand ConnectCommand { get; }

        ICommand BindCommand { get; }

        ICommand BrowseToUrlCommand { get; }

        ICommand BrowseToProjectDashboardCommand { get; }

        ICommand RefreshCommand { get; }

        ICommand DisconnectCommand { get; }

        ICommand ToggleShowAllProjectsCommand { get; }
    }
}
