using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
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
        }

        protected override void InvokeInternal()
        {
            foreach (Project project in this.projectSystem.GetSelectedProjects())
            {
                this.SetIsExcluded(project, !this.GetIsExcluded(project));
            }
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;

            IList<bool> properties = this.projectSystem.GetSelectedProjects()
                                         .Select(this.GetIsExcluded)
                                         .ToList();

            if (properties.Any())
            {
                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && properties.First();
            }
        }

        private bool GetIsExcluded(Project dteProject)
        {
            string propertyString;
            bool propertyValue;
            bool hasProperty = this.projectSystem.TryGetProjectProperty(dteProject, PropertyName, out propertyString);
            if (hasProperty && bool.TryParse(propertyString, out propertyValue))
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
                this.projectSystem.RemoveProjectProperty(dteProject, PropertyName);
            }
        }

    }
}