//-----------------------------------------------------------------------
// <copyright file="ProjectExcludePropertyToggleCommand.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class ProjectExcludePropertyToggleCommand : VsCommandBase
    {
        private const string PropertyName = Constants.SonarQubeExcludeBuildPropertyKey;

        private readonly IProjectSystemHelper projectSystem;

        public ProjectExcludePropertyToggleCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.projectSystem = this.ServiceProvider.GetMefService<IHost>()?.GetService<IProjectSystemHelper>();
            Debug.Assert(this.projectSystem != null, $"Failed to get {nameof(IProjectSystemHelper)}");
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.projectSystem != null, "Should not be invokable with no project system");

            IList<Project> projects = this.projectSystem.GetSelectedProjects().ToList();
            Debug.Assert(projects.Any(), "No projects selected");
            Debug.Assert(projects.All(x => Language.ForProject(x).IsSupported), "Unsupported projects");

            if (projects.Count == 1 || projects.Select(this.GetIsExcluded).AllEqual()) 
            {
                // Single project, or multiple projects & consistent property values
                foreach (Project project in projects)
                {
                    // Toggle value
                    this.SetIsExcluded(project, !this.GetIsExcluded(project));
                }
            }
            else
            {
                // Multiple projects & mixed property values
                foreach (Project project in projects)
                {
                    // Set excluded = true
                    this.SetIsExcluded(project, true);
                }
            }
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;
            if (this.projectSystem == null)
            {
                return;
            }

            IList<Project> projects = this.projectSystem.GetSelectedProjects()
                                                        .ToList();

            if (projects.Any() && projects.All(x => Language.ForProject(x).IsSupported))
            {
                IList<bool> properties = projects.Select(this.GetIsExcluded)
                                                 .ToList();

                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && properties.First();
            }
        }

        #region Property helpers

        private bool GetIsExcluded(Project dteProject)
        {
            string propertyString = this.projectSystem.GetProjectProperty(dteProject, PropertyName);

            bool propertyValue;
            if (bool.TryParse(propertyString, out propertyValue))
            {
                return propertyValue;
            }

            return false;
        }

        private void SetIsExcluded(Project dteProject, bool value)
        {
            if (value)
            {
                this.projectSystem.SetProjectProperty(dteProject, PropertyName, true.ToString());
            }
            else
            {
                this.projectSystem.ClearProjectProperty(dteProject, PropertyName);
            }
        }

        #endregion
    }
}