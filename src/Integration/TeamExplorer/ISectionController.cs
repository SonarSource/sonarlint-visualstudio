﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.WPF;

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

        ICommand<BindCommandArgs> BindCommand { get; }

        ICommand<string> BrowseToUrlCommand { get; }

        ICommand<ProjectViewModel> BrowseToProjectDashboardCommand { get; }

        ICommand<ServerViewModel> ToggleShowAllProjectsCommand { get; }
    }
}
