using System;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;
using System.Linq;

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
            foreach (Project project in this.projectSystem.GetSelectedProjects())
            {
                this.SetTestProperty(project, this.setPropertyValue);
            }
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;

            IList<bool?> properties = this.projectSystem.GetSelectedProjects()
                                                        .Select(this.GetTestProperty)
                                                        .ToList();
            if (properties.Any())
            {
                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && (properties.First() == this.setPropertyValue);
            }
        }

        private bool? GetTestProperty(Project dteProject)
        {
            string propertyString;
            bool propertyValue;
            bool hasProperty = this.projectSystem.TryGetProjectProperty(dteProject, PropertyName, out propertyString);
            if (hasProperty && bool.TryParse(propertyString, out propertyValue))
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
                this.projectSystem.RemoveProjectProperty(dteProject, PropertyName);
            }
        }
    }
}