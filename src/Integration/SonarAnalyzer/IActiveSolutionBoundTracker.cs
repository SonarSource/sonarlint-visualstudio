//-----------------------------------------------------------------------
// <copyright file="IActiveSolutionBoundTracker.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace SonarLint.VisualStudio.Integration.SonarAnalyzer
{
    /// <summary>
    /// Allows checking if the current Visual Studio solution is bound to a SonarQube project or not
    /// </summary>
    public interface IActiveSolutionBoundTracker : IDisposable
    {
        /// <summary>
        /// Returns whether the active solution is bound to a SonarQube project
        /// </summary>
        bool IsActiveSolutionBound { get; }
    }
}
