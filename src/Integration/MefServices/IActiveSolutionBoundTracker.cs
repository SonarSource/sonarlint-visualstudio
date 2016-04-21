//-----------------------------------------------------------------------
// <copyright file="IActiveSolutionBoundTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Allows checking if the current Visual Studio solution is bound to a SonarQube project or not
    /// </summary>
    public interface IActiveSolutionBoundTracker
    {
        /// <summary>
        /// Returns whether the active solution is bound to a SonarQube project
        /// </summary>
        bool IsActiveSolutionBound { get; }

        /// <summary>
        /// Event to notify subscribers when the binding status of a solution have changed.
        /// This occurs when a new solution is opened, or the SonarQube binding status of the solution changes.
        /// </summary>
        event EventHandler<bool> SolutionBindingChanged;
    }
}
