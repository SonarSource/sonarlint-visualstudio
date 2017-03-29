/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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
    /// Command handler to set the &lt;SonarQubeTestProject/&gt; project property to a specific value.
    /// </summary>
    internal class ProjectTestPropertySetCommand : VsCommandBase
    {
        public const string PropertyName = Constants.SonarQubeTestProjectBuildPropertyKey;
        private readonly IProjectPropertyManager propertyManager;

        private readonly bool? commandPropertyValue;

        internal /*for testing purposes*/ bool? CommandPropertyValue => this.commandPropertyValue;

        /// <summary>
        /// Construct a new command handler to set the &lt;SonarQubeTestProject/&gt;
        /// project property to a specified value.
        /// </summary>
        /// <param name="setPropertyValue">Value this instance will set the project properties to.</param>
        public ProjectTestPropertySetCommand(IServiceProvider serviceProvider, bool? setPropertyValue)
            : base(serviceProvider)
        {
            this.propertyManager = this.ServiceProvider.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");

            this.commandPropertyValue = setPropertyValue;
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.propertyManager != null, "Should not be invokable with no property manager");

            IList<Project> projects = this.propertyManager
                                          .GetSelectedProjects()
                                          .ToList();

            Debug.Assert(projects.Any(), "No projects selected");
            Debug.Assert(projects.All(x => Language.ForProject(x).IsSupported), "Unsupported projects");

            foreach (Project project in projects)
            {
                this.propertyManager.SetBooleanProperty(project, PropertyName, this.commandPropertyValue);
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
                IList<bool?> properties = projects.Select(x =>
                    this.propertyManager.GetBooleanProperty(x, PropertyName)).ToList();

                command.Enabled = true;
                command.Visible = true;
                // Checked if all projects have the same value, and that value is
                // the same as the value this instance is responsible for.
                command.Checked = properties.AllEqual() && (properties.First() == this.commandPropertyValue);
            }
        }
    }
}
