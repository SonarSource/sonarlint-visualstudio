//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsProjectSystemHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE80;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableVsProjectSystemHelper : IProjectSystemHelper
    {
        private readonly IServiceProvider serviceProvider;

        public IDictionary<Project, Microsoft.Build.Evaluation.Project> MsBuildProjectMapping { get; } = new Dictionary<Project, Microsoft.Build.Evaluation.Project>();

        public ConfigurableVsProjectSystemHelper(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        #region IVsProjectSystemHelper
        IServiceProvider IProjectSystemHelper.ServiceProvider
        {
            get
            {
                return this.serviceProvider;
            }
        }

        Project IProjectSystemHelper.GetSolutionItemsProject()
        {
            return this.SolutionItemsProject;
        }

        IEnumerable<Project> IProjectSystemHelper.GetSolutionManagedProjects()
        {
            return this.ManagedProjects ?? Enumerable.Empty<Project>();
        }

        bool IProjectSystemHelper.IsFileInProject(Project project, string file)
        {
            return this.IsFileInProjectAction?.Invoke(project, file) ?? false;
        }

        void IProjectSystemHelper.AddFileToProject(Project project, string file)
        {
            bool addFileToProject = !project.ProjectItems.OfType<ProjectItem>().Any(pi => StringComparer.OrdinalIgnoreCase.Equals(pi.Name, file));
            if (addFileToProject)
            {
                project.ProjectItems.AddFromFile(file);
            }
        }

        Solution2 IProjectSystemHelper.GetCurrentActiveSolution()
        {
            return this.CurrentActiveSolution;
        }

        Microsoft.Build.Evaluation.Project IProjectSystemHelper.GetEquivalentMSBuildProject(EnvDTE.Project project)
        {
            if (this.MsBuildProjectMapping.ContainsKey(project))
            {
                return this.MsBuildProjectMapping[project];
            }

            return null;
        }

        #endregion

        #region Test helpers

        public Project SolutionItemsProject { get; set; }

        public IEnumerable<Project> ManagedProjects { get; set; }

        public Func<Project, string, bool> IsFileInProjectAction { get; set; }

        public Solution2 CurrentActiveSolution { get; set; }

        #endregion
    }
}
