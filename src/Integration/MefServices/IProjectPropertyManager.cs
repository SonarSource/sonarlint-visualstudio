//-----------------------------------------------------------------------
// <copyright file="IProjectPropertyManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration
{
    public interface IProjectPropertyManager
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