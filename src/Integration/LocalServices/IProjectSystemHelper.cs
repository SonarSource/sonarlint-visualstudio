//-----------------------------------------------------------------------
// <copyright file="IProjectSystemHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Wrapper service to abstract the VS related API handling
    /// </summary>
    internal interface IProjectSystemHelper : ILocalService
    {
        /// <summary>
        /// Returns the current active solution
        /// </summary>
        Solution2 GetCurrentActiveSolution();

        /// <summary>
        /// Return the 'Solution Items' folder which internally treated as project
        /// </summary>
        Project GetSolutionItemsProject();

        /// <summary>
        /// Checks whether a file is in the project
        /// </summary>
        bool IsFileInProject(Project project, string file);

        /// <summary>
        /// Adds a file to project
        /// </summary>
        void AddFileToProject(Project project, string file);

        /// <summary>
        /// Adds a file with specific item type to the project
        /// </summary>
        void AddFileToProject(Project project, string file, string itemType);

        /// <summary>
        /// Returns all what VS considers as a projects in a solution
        /// </summary>
        IEnumerable<Project> GetSolutionProjects();

        /// <summary>
        /// Returns only the filtered project based on a common filter. 
        /// This should only be called after we connected to a SonarQube server, 
        /// since some of the filtering is SonarQube server instance specific.
        /// <seealso cref="IProjectSystemFilter"/> which is used internally.
        /// </summary>
        IEnumerable<Project> GetFilteredSolutionProjects();

        /// <summary>
        /// Returns the <seealso cref="IVsHierarchy"/> for a <see cref="Project"/>
        /// </summary>
        IVsHierarchy GetIVsHierarchy(Project dteProject);
    }
}
