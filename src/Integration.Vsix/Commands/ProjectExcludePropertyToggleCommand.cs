using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class ProjectExcludePropertyToggleCommand : VsCommandBase
    {
        private readonly IDictionary<string, bool> props = new Dictionary<string, bool>();

        public ProjectExcludePropertyToggleCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override void InvokeInternal()
        {
            Project selectedProject = this.GetSelectedSingleProject();
            if (selectedProject != null)
            {
                // todo: implement
                if (!this.props.ContainsKey(selectedProject.UniqueName))
                {
                    this.props.Add(selectedProject.UniqueName, false);
                }

                this.props[selectedProject.UniqueName] = !this.props[selectedProject.UniqueName];
            }
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = false;
            command.Visible = false;

            Project selectedProject = this.GetSelectedSingleProject();
            if (selectedProject != null)
            {
                command.Enabled = true;
                command.Visible = true;

                // todo: implement
                if (!this.props.ContainsKey(selectedProject.UniqueName))
                {
                    this.props.Add(selectedProject.UniqueName, false);
                }

                command.Checked = this.props[selectedProject.UniqueName];
            }
        }

        private Project GetSelectedSingleProject()
        {
            var dte = this.ServiceProvider.GetService<DTE>();
            if (dte == null)
            {
                return null;
            }

            var selectedProjects = new List<Project>();
            foreach (object projectObj in dte.ActiveSolutionProjects as Array ?? new object[0])
            {
                var project = projectObj as Project;
                if (project != null)
                {
                    selectedProjects.Add(project);
                }
            }

            if (selectedProjects.Count == 1)
            {
                return selectedProjects[0];
            }

            return null;
        }
    }
}