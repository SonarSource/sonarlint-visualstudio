/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
    internal interface IProjectPropertyManager
    {
        /// <summary>
        /// Get all currently selected projects in the Solution Explorer.
        /// </summary>
        /// <returns></returns>
        IEnumerable<Project> GetSelectedProjects();

        /// <summary>
        /// Get the project property value with the given name.
        /// </summary>
        /// <remarks>Will only look in unconditional ItemGroups</remarks>
        /// <param name="project">Project to get the property value for</param>
        /// <param name="propertyName">Property name</param>
        /// <returns>Null if the property is undefined or not a boolean, true/false otherwise</returns>
        bool? GetBooleanProperty(Project project, string propertyName);

        /// <summary>
        /// Set the project property value with the given name.
        /// </summary>
        /// <remarks>
        /// Will only look in unconditional ItemGroups.<para/>
        /// Using null as the <paramref name="value"/> will clear the property.
        /// </remarks>
        /// <param name="project">Project to set the property value for</param>
        /// <param name="propertyName">Property name</param>
        /// <param name="value">Property value</param>
        void SetBooleanProperty(Project project, string propertyName, bool? value);
    }
}