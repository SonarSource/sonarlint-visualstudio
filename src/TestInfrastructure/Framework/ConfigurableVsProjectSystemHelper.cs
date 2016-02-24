//-----------------------------------------------------------------------
// <copyright file="ConfigurableVsProjectSystemHelper.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using SonarLint.VisualStudio.Integration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableVsProjectSystemHelper : IProjectSystemHelper
    {
        private readonly IServiceProvider serviceProvider;

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
        #endregion

        #region Test helpers

        public Project SolutionItemsProject { get; set; }

        public IEnumerable<Project> ManagedProjects { get; set; }

        public Func<Project, string, bool> IsFileInProjectAction { get; set; }

        #endregion
    }
}
