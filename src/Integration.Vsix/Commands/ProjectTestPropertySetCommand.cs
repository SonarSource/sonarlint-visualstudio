//-----------------------------------------------------------------------
// <copyright file="ProjectTestPropertySetCommand.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class ProjectTestPropertySetCommand : VsCommandBase
    {
        private const string PropertyName = Constants.SonarQubeTestProjectBuildPropertyKey;

        private readonly IProjectSystemHelper projectSystem;
        private readonly bool? commandPropertyValue;

        internal /*for testing purposes*/ bool? CommandPropertyValue => this.commandPropertyValue;

        public ProjectTestPropertySetCommand(IServiceProvider serviceProvider, bool? setPropertyValue)
            : base(serviceProvider)
        {
            this.projectSystem = this.ServiceProvider.GetMefService<IHost>()?.GetService<IProjectSystemHelper>();
            Debug.Assert(this.projectSystem != null, $"Failed to get {nameof(IProjectSystemHelper)}");

            this.commandPropertyValue = setPropertyValue;
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.projectSystem != null, "Should not be invokable with no project system");

            IList<Project> projects = this.projectSystem.GetSelectedProjects().ToList();
            Debug.Assert(projects.Any(), "No projects selected");
            Debug.Assert(projects.All(x => Language.ForProject(x).IsSupported), "Unsupported projects");

            foreach (Project project in projects)
            {
                this.SetTestProperty(project, this.commandPropertyValue);
            }
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;

            IList<Project> projects = this.projectSystem.GetSelectedProjects()
                                                        .ToList();

            if (projects.Any() && projects.All(x => Language.ForProject(x).IsSupported))
            {
                IList<bool?> properties = projects.Select(this.GetTestProperty)
                                                  .ToList();
                
                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && (properties.First() == this.commandPropertyValue);
            }
        }

        #region Property helpers

        private bool? GetTestProperty(Project dteProject)
        {
            string propertyString = this.projectSystem.GetProjectProperty(dteProject, PropertyName);

            bool propertyValue;
            if (bool.TryParse(propertyString, out propertyValue))
            {
                return propertyValue;
            }

            return null;
        }

        private void SetTestProperty(Project dteProject, bool? value)
        {
            if (value.HasValue)
            {
                this.projectSystem.SetProjectProperty(dteProject, PropertyName, value.Value.ToString());
            }
            else
            {
                this.projectSystem.ClearProjectProperty(dteProject, PropertyName);
            }
        }

        #endregion
    }
}