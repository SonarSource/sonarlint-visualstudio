//-----------------------------------------------------------------------
// <copyright file="IConflictsManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Handlers conflict resolution at the solution level
    /// </summary>
    public interface IConflictsManager : ILocalService
    {
        /// <summary>
        /// Checks the current solution for conflicts
        /// </summary>
        /// <returns>Not null. Will be empty when there are no conflicts</returns>
        /// <remarks>This method is supposed to run just after the solution was bound to SonarQube project. Other cases may return invalid results</remarks>
        IReadOnlyList<ProjectRuleSetConflict> GetCurrentConflicts();
    }
}
