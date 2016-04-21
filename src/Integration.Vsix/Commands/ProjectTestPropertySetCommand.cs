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
        private readonly bool? setPropertyValue;

        public ProjectTestPropertySetCommand(IServiceProvider serviceProvider, bool? setPropertyValue)
            : base(serviceProvider)
        {
            this.projectSystem = this.ServiceProvider.GetMefService<IHost>()?.GetService<IProjectSystemHelper>();
            this.setPropertyValue = setPropertyValue;
        }

        protected override void InvokeInternal()
        {
            IList<Project> projects = this.projectSystem.GetSelectedProjects().ToList();

            Debug.Assert(projects.All(x => Language.ForProject(x).IsSupported), "Unsupported projects");

            foreach (Project project in projects)
            {
                this.SetTestProperty(project, this.setPropertyValue);
            }
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;

            IDictionary<Language, Project> projects = this.projectSystem
                                                          .GetSelectedProjects()
                                                          .ToDictionary(x => Language.ForProject(x), x => x);

            if (projects.Any() && projects.Keys.All(x => x.IsSupported))
            {
                IList<bool?> properties = projects.Values
                                                  .Select(this.GetTestProperty)
                                                  .ToList();
                
                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && (properties.First() == this.setPropertyValue);
            }
        }

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
    }
}