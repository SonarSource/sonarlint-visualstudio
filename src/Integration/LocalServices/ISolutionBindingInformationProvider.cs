//-----------------------------------------------------------------------
// <copyright file="ISolutionBindingInformationProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// SonarQube-bound project discovery
    /// </summary>
    internal interface ISolutionBindingInformationProvider : ILocalService
    {
        /// <summary>
        /// Return all the SonarQube bound projects in the current solution.
        /// It's up to the caller to make sure that the solution is fully loaded before calling this method.
        /// </summary>
        /// <remarks>Filtered out projects are not considered, <seealso cref="IProjectSystemHelper.GetFilteredSolutionProjects"/></remarks>
        /// <returns>Will always return an instance, never a null</returns>
        IEnumerable<Project> GetBoundProjects();

        /// <summary>
        /// Return all the SonarQube unbound projects in the current solution. 
        /// It's up to the caller to make sure that the solution is fully loaded before calling this method.
        /// </summary>
        /// <remarks>Filtered out projects are not considered, <seealso cref="IProjectSystemHelper.GetFilteredSolutionProjects"/></remarks>
        /// <returns>Will always return an instance, never a null</returns>
        IEnumerable<Project> GetUnboundProjects();
    }
}
