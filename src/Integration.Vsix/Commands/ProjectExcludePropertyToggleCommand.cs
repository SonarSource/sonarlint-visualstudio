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
        }

        protected override void InvokeInternal()
        {
            IList<Project> projects = this.projectSystem.GetSelectedProjects().ToList();

            Debug.Assert(projects.All(x =>Language.ForProject(x).IsSupported), "Unsupported projects");

            foreach (Project project in projects)
            {
                this.SetIsExcluded(project, !this.GetIsExcluded(project));
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
                IList<bool> properties = projects.Values
                                                 .Select(this.GetIsExcluded)
                                                 .ToList();

                command.Enabled = true;
                command.Visible = true;
                command.Checked = properties.AllEqual() && properties.First();
            }
        }

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

    }
}