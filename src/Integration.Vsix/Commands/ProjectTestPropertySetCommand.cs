using System;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class ProjectTestPropertySetCommand : VsCommandBase
    {
        private static readonly IDictionary<string, bool?> s_props = new Dictionary<string, bool?>();

        private readonly bool? setPropertyValue;

        public ProjectTestPropertySetCommand(IServiceProvider serviceProvider, bool? setPropertyValue)
            : base(serviceProvider)
        {
            this.setPropertyValue = setPropertyValue;
        }

        protected override void InvokeInternal()
        {
            Project selectedProject = this.GetSelectedSingleProject();
            if (selectedProject != null)
            {
                // todo: implement
                if (!s_props.ContainsKey(selectedProject.UniqueName))
                {
                    s_props.Add(selectedProject.UniqueName, null);
                }

                s_props[selectedProject.UniqueName] = this.setPropertyValue;
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
                if (!s_props.ContainsKey(selectedProject.UniqueName))
                {
                    s_props.Add(selectedProject.UniqueName, null);
                }

                command.Checked = s_props[selectedProject.UniqueName] == this.setPropertyValue;
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