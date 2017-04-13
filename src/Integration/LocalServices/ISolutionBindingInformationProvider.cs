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
        /// <remarks>Internally projects are filtered using the <see cref="IProjectSystemFilter"/> service.
        /// <seealso cref="IProjectSystemHelper.GetFilteredSolutionProjects"/></remarks>
        /// <returns>Will always return an instance, never a null</returns>
        IEnumerable<Project> GetBoundProjects();

        /// <summary>
        /// Return all the SonarQube unbound projects in the current solution.
        /// It's up to the caller to make sure that the solution is fully loaded before calling this method.
        /// </summary>
        /// <remarks>Internally projects are filtered using the <see cref="IProjectSystemFilter"/> service.
        /// <seealso cref="IProjectSystemHelper.GetFilteredSolutionProjects"/></remarks>
        /// <returns>Will always return an instance, never a null</returns>
        IEnumerable<Project> GetUnboundProjects();

        /// <summary>
        /// Returns whether the solution is bound to SonarQube
        /// </summary>
        bool IsSolutionBound();
    }
}
