/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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

        #endregion IVsProjectSystemHelper

        #region Test helpers

        public Project SolutionItemsProject { get; set; }

        public IEnumerable<Project> Projects { get; set; }

        public IEnumerable<Project> FilteredProjects { get; set; }

        public IEnumerable<Project> SelectedProjects { get; set; }

        public Func<Project, string, bool> IsFileInProjectAction { get; set; }

        public Solution2 CurrentActiveSolution { get; set; }

        public bool SimulateIVsHierarchyFailure { get; set; }

        #endregion Test helpers
    }
}