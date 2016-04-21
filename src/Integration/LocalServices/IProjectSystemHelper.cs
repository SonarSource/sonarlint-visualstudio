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
    public interface IProjectSystemHelper : ILocalService
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

        /// <summary>
        /// Returns the currently selected projects in the active solution.
        /// </summary>
        IEnumerable<Project> GetSelectedProjects();

        /// <summary>
        /// Get the value of a given MSBuild project property.
        /// </summary>
        /// <param name="propertyName">Name of the property to get</param>
        /// <param name="value">The returned value of the property</param>
        /// <remarks>
        /// The out parameter <paramref name="value"/> will be null in two cases:<br/>
        ///  1. The property does not exist (method returns false), or<br/>
        ///  2. The property exists but has not been set (method returns true)
        /// </remarks>
        /// <returns>True if the property exists, false if it does not</returns>
        bool TryGetProjectProperty(Project dteProject, string propertyName, out string value);

        /// <summary>
        /// Set the value of the given MSBuild project property.
        /// </summary>
        /// <remarks>The property is created if it does not already exist</remarks>
        /// <param name="propertyName">Name of the property to set</param>
        void SetProjectProperty(Project dteProject, string propertyName, string value);

        /// <summary>
        /// Remove an MSBuild project project if it exists.
        /// </summary>
        /// <param name="propertyName">Name of the property to remove</param>
        void RemoveProjectProperty(Project dteProject, string propertyName);
    }
}
