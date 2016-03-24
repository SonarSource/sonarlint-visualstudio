//-----------------------------------------------------------------------
// <copyright file="IRuleSetConflictsController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Provides the UX for RuleSet conflict - detection and auto-fix
    /// </summary>
    /// <seealso cref="IConflictsManager"/>
    /// <seealso cref="IRuleSetInspector"/>
    internal interface IRuleSetConflictsController : ILocalService
    {
        /// <summary>
        /// Checks whether the current solution has projects with conflicts RuleSets. 
        /// The check is against the solution level RuleSet (if solution is bound).
        /// </summary>
        /// <returns>Whether has conflicts (in which case there will be a UX to auto-fix them as well)</returns>
        bool CheckForConflicts();

        /// <summary>
        /// Clears any UX that was activated part of <see cref="CheckForConflicts"/>
        /// </summary>
        void Clear();
    }
}
