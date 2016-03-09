//-----------------------------------------------------------------------
// <copyright file="IStateManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Service;
using System.Collections.Generic;

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
        /// <remarks>The state should not be manipulated directly, it exposed only for databinding purposes</remarks>
        TransferableVisualState ManagedState { get; }

        bool IsBusy { get; set; }

        string BoundProjectKey { get; set; }

        void SetProjects(ConnectionInformation connection, IEnumerable<ProjectInformation> projects);

        void SetBoundProject(ProjectInformation project);

        void ClearBoundProject();

        void SyncCommandFromActiveSection();
    }
}
