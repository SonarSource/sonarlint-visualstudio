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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Command handler to toggle the &lt;SonarQubeExclude/&gt; project property.
    /// </summary>
    /// <remarks>Will toggle between removing the property, and setting it to 'true'.</remarks>
    internal class ProjectExcludePropertyToggleCommand : VsCommandBase
    {
        public const string PropertyName = Constants.SonarQubeExcludeBuildPropertyKey;

        private readonly IProjectPropertyManager propertyManager;

        public ProjectExcludePropertyToggleCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.propertyManager = this.ServiceProvider.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.propertyManager != null, "Should not be invokable with no property manager");

            IList<Project> projects = this.propertyManager
                                          .GetSelectedProjects()
                                          .ToList();

            Debug.Assert(projects.Any(), "No projects selected");
            Debug.Assert(projects.All(x => Language.ForProject(x).IsSupported), "Unsupported projects");

            if (projects.Count == 1 ||
                projects.Select(x => this.propertyManager.GetBooleanProperty(x, PropertyName)).AllEqual())
            {
                // Single project, or multiple projects & consistent property values
                foreach (Project project in projects)
                {
                    this.ToggleProperty(project);
                }
            }
            else
            {
                // Multiple projects & mixed property values
                foreach (Project project in projects)
                {
                    this.propertyManager.SetBooleanProperty(project, PropertyName, true);
                }
            }
        }

        private void ToggleProperty(Project project)
        {
            bool currentValue = this.propertyManager.GetBooleanProperty(project, PropertyName)
                                                            .GetValueOrDefault(false);
            if (currentValue)
            {
                this.propertyManager.SetBooleanProperty(project, PropertyName, null);
            }
            else
            {
                this.propertyManager.SetBooleanProperty(project, PropertyName, true);
            }
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;
            if (this.propertyManager == null)
            {
                return;
            }

            IList<Project> projects = this.propertyManager
                                          .GetSelectedProjects()
                                          .ToList();

            if (projects.Any() && projects.All(x => Language.ForProject(x).IsSupported))
            {
                IList<bool> properties = projects.Select(x =>
                    this.propertyManager.GetBooleanProperty(x, PropertyName) ?? false).ToList();

                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && properties.First();
            }
        }
    }
}
