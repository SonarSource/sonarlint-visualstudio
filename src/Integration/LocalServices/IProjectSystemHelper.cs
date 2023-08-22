/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Wrapper service to abstract the VS related API handling
    /// </summary>
    public interface IProjectSystemHelper : ILocalService, IVsHierarchyLocator
    {
        /// <summary>
        /// Returns all what VS considers as a projects in a solution
        /// </summary>
        IEnumerable<Project> GetSolutionProjects();

        /// <summary>
        /// Returns the <seealso cref="IVsHierarchy"/> for a <see cref="Project"/>
        /// </summary>
        IVsHierarchy GetIVsHierarchy(Project dteProject);

        /// <summary>
        /// Returns the <see cref="Project"/> referenced by the project <seealso cref="IVsHierarchy"/>
        /// </summary>
        Project GetProject(IVsHierarchy projectHierarchy);

        /// <summary>
        /// Returns the currently selected projects in the active solution.
        /// </summary>
        IEnumerable<Project> GetSelectedProjects();

        /// <summary>
        /// Get the value of a given MSBuild project property.
        /// </summary>
        /// <param name="propertyName">Name of the property to get</param>
        /// <returns>The value of the property or null if the property does not exist/has not been set.</returns>
        string GetProjectProperty(Project dteProject, string propertyName);

        /// <summary>
        /// Get the value of a given MSBuild project property for the specified build configuration (e.g. "Debug")
        /// </summary>
        /// <param name="propertyName">Name of the property to get</param>
        /// <returns>The value of the property or null if the property does not exist/has not been set.</returns>
        string GetProjectProperty(Project dteProject, string propertyName, string configuration);

        /// <summary>
        /// Set the value of the given MSBuild project property.
        /// </summary>
        /// <remarks>The property is created if it does not already exist</remarks>
        /// <param name="propertyName">Name of the property to set</param>
        void SetProjectProperty(Project dteProject, string propertyName, string value);

        /// <summary>
        /// Remove an MSBuild project if it exists.
        /// </summary>
        /// <remarks>This does not remove the property from the project, it only removes the value.</remarks>
        /// <param name="propertyName">Name of the property to remove</param>
        void ClearProjectProperty(Project dteProject, string propertyName);

        /// <summary>
        /// Return all project 'kinds' (GUIDs) for the given project.
        /// </summary>
        /// <returns>Project kinds GUIDs for the project</returns>
        IEnumerable<Guid> GetAggregateProjectKinds(IVsHierarchy hierarchy);

        /// <summary>
        /// Returns a flag indicating whether there is fully-opened solution or not.
        /// </summary>
        bool IsSolutionFullyOpened();
    }
}
