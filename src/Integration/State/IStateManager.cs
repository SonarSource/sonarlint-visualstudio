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
using System.Collections.Generic;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.State
{
    /// <summary>
    /// Manages the view model state (also encapsulates it)
    /// </summary>
    internal interface IStateManager
    {
        /// <summary>
        /// The underlying managed visual state
        /// </summary>
        /// <remarks>The state should not be manipulated directly, it exposed only for data binding purposes</remarks>
        TransferableVisualState ManagedState { get; }

        /// <summary>
        /// Event fired when <see cref="IsBusy"/> is changed. The arguments will include the new value.
        /// </summary>
        event EventHandler<bool> IsBusyChanged;

        /// <summary>
        /// Event fired when the SonarQube project binding of the solution changes.
        /// </summary>
        event EventHandler BindingStateChanged;

        bool IsBusy { get; set; }

        bool HasBoundProject { get; }

        bool IsConnected { get; }

        string BoundProjectKey { get; set; }

        IEnumerable<ConnectionInformation> GetConnectedServers();

        ConnectionInformation GetConnectedServer(SonarQubeProject project);

        void SetProjects(ConnectionInformation connection, IEnumerable<SonarQubeProject> projects);

        void SetBoundProject(SonarQubeProject project);

        void ClearBoundProject();

        void SyncCommandFromActiveSection();
    }
}
