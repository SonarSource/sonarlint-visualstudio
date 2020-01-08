/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System.Collections.Generic;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration
{
    // "GetUnboundProjects" is used by the error list controller in connected mode (legacy & new)
    // to find projects that have been added to a bound solution but do not have the ruleset
    // set correctly.
    // It is also used by the BindingProcessImpl to find projects that required project-level binding.

    internal interface IUnboundProjectFinder
    {
        /// <summary>
        /// Return all of the projects in the current solution for which the required solution-level or project-level configuration is missing
        /// </summary>
        /// <remarks>
        /// It's up to the caller to make sure that the solution is fully loaded before calling this method.
        /// Not all projects required project-level configuration e.g. Cpp projects. However, they still require
        /// solution-level configuration (e.g. a  file contains the rules configuration).
        /// Internally projects are filtered using the <see cref="IProjectSystemFilter"/> service.
        /// <seealso cref="IProjectSystemHelper.GetFilteredSolutionProjects"/></remarks>
        /// <returns>Will always return an instance, never a null</returns>
        IEnumerable<Project> GetUnboundProjects();
    }
}
