using System;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics;
using EnvDTE;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class ProjectSonarLintMenuCommand : VsCommandBase
    {
        private readonly IProjectSystemHelper projectSystem;

        public ProjectSonarLintMenuCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.projectSystem = this.ServiceProvider.GetMefService<IHost>()?.GetService<IProjectSystemHelper>();
            Debug.Assert(this.projectSystem != null, $"Failed to get {nameof(IProjectSystemHelper)}");
        }

        protected override void InvokeInternal()
        {
            // Do nothing; this is a menu only.
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
                command.Enabled = true;
                command.Visible = true;
            }
        }
    }
}
