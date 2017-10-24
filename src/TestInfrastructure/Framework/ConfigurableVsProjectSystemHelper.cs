/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableVsProjectSystemHelper : IProjectSystemHelper
    {
        private readonly IServiceProvider serviceProvider;
        private bool isSolutionFullyOpened;

        public ConfigurableVsProjectSystemHelper(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        #region IVsProjectSystemHelper

        Project IProjectSystemHelper.GetSolutionItemsProject(bool createOnNull)
        {
            return this.SolutionItemsProject;
        }

        public Project GetSolutionFolderProject(string solutionFolderName, bool createOnNull)
        {
            return this.SolutionItemsProject;
        }

        IEnumerable<Project> IProjectSystemHelper.GetSolutionProjects()
        {
            return this.Projects ?? Enumerable.Empty<Project>();
        }

        IEnumerable<Project> IProjectSystemHelper.GetFilteredSolutionProjects()
        {
            return this.FilteredProjects ?? Enumerable.Empty<Project>();
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

        void IProjectSystemHelper.AddFileToProject(Project project, string file, string itemType)
        {
            bool addFileToProject = !project.ProjectItems.OfType<ProjectItem>().Any(pi => StringComparer.OrdinalIgnoreCase.Equals(pi.Name, file));
            if (addFileToProject)
            {
                var item = project.ProjectItems.AddFromFile(file);
                Property itemTypeProperty = VsShellUtils.FindProperty(item.Properties, Constants.ItemTypePropertyKey);
                if (itemTypeProperty != null)
                {
                    itemTypeProperty.Value = itemType;
                }
            }
        }

        public void RemoveFileFromProject(Project project, string fileName)
        {
            var projectItem = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(pi => StringComparer.OrdinalIgnoreCase.Equals(pi.Name, fileName));
            if (projectItem != null)
            {
                projectItem.Remove();
            }
        }

        Solution2 IProjectSystemHelper.GetCurrentActiveSolution()
        {
            return this.CurrentActiveSolution;
        }

        IVsHierarchy IProjectSystemHelper.GetIVsHierarchy(Project dteProject)
        {
            if (this.SimulateIVsHierarchyFailure)
            {
                return null;
            }

            return dteProject as IVsHierarchy;
        }

        public IEnumerable<Project> GetSelectedProjects()
        {
            return this.SelectedProjects ?? Enumerable.Empty<Project>();
        }

        public string GetProjectProperty(Project dteProject, string propertyName)
        {
            var projMock = dteProject as ProjectMock;
            if (projMock == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith($"Only expecting {nameof(ProjectMock)}");
            }

            return projMock.GetBuildProperty(propertyName);
        }

        public void SetProjectProperty(Project dteProject, string propertyName, string value)
        {
            var projMock = dteProject as ProjectMock;
            if (projMock == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith($"Only expecting {nameof(ProjectMock)}");
            }

            projMock.SetBuildProperty(propertyName, value);
        }

        public void ClearProjectProperty(Project dteProject, string propertyName)
        {
            var projMock = dteProject as ProjectMock;
            if (projMock == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith($"Only expecting {nameof(ProjectMock)}");
            }

            projMock.ClearBuildProperty(propertyName);
        }

        public IEnumerable<Guid> GetAggregateProjectKinds(IVsHierarchy hierarchy)
        {
            ProjectMock dteProject = hierarchy as ProjectMock;
            if (dteProject == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith($"Only expecting {nameof(ProjectMock)} type");
            }

            return dteProject.GetAggregateProjectTypeGuids();
        }

        public bool IsSolutionFullyOpened()
        {
            return this.isSolutionFullyOpened;
        }

        #endregion IVsProjectSystemHelper

        #region Test helpers

        public Project SolutionItemsProject { get; set; }

        public IEnumerable<Project> Projects { get; set; }

        public IEnumerable<Project> FilteredProjects { get; set; }

        public IEnumerable<Project> SelectedProjects { get; set; }

        public Func<Project, string, bool> IsFileInProjectAction { get; set; }

        public Solution2 CurrentActiveSolution { get; set; }

        public bool SimulateIVsHierarchyFailure { get; set; }

        public void SetIsSolutionFullyOpened(bool isFullyOpened)
        {
            this.isSolutionFullyOpened = isFullyOpened;
        }

        #endregion Test helpers
    }
}