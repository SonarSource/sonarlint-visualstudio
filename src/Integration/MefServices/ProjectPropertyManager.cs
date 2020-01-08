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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IProjectPropertyManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectPropertyManager : IProjectPropertyManager
    {
        private readonly IProjectSystemHelper projectSystem;

        [ImportingConstructor]
        public ProjectPropertyManager(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.projectSystem = host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
        }

        #region IProjectPropertyManager

        public IEnumerable<Project> GetSelectedProjects()
        {
            return this.projectSystem
                .GetSelectedProjects();
        }

        public bool? GetBooleanProperty(Project project, string propertyName)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            string propertyString = this.projectSystem.GetProjectProperty(project, propertyName);

            bool propertyValue;
            if (bool.TryParse(propertyString, out propertyValue))
            {
                return propertyValue;
            }

            return null;
        }

        public void SetBooleanProperty(Project project, string propertyName, bool? value)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (value.HasValue)
            {
                this.projectSystem.SetProjectProperty(project, propertyName, value.Value.ToString());
            }
            else
            {
                this.projectSystem.ClearProjectProperty(project, propertyName);
            }
        }

        #endregion
    }
}
