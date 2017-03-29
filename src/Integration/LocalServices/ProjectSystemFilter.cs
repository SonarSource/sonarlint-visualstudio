/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto: contact AT sonarsource DOT com
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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DteProject = EnvDTE.Project;

namespace SonarLint.VisualStudio.Integration
{
    internal class ProjectSystemFilter : IProjectSystemFilter
    {
        private readonly IProjectSystemHelper projectSystem;
        private readonly IProjectPropertyManager propertyManager;

        private Regex testRegex;

        public ProjectSystemFilter(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.projectSystem = host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();

            this.propertyManager = host.GetMefService<IProjectPropertyManager>();
            Debug.Assert(this.propertyManager != null, $"Failed to get {nameof(IProjectPropertyManager)}");
        }

        #region IProjectSystemFilter

        public bool IsAccepted(DteProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var projectName = project.Name;
            var hierarchy = this.projectSystem.GetIVsHierarchy(project);
            var propertyStorage = hierarchy as IVsBuildPropertyStorage;

            if (hierarchy == null || propertyStorage == null)
            {
                throw new ArgumentException(Strings.ProjectFilterDteProjectFailedToGetIVs, nameof(project));
            }

            if (IsNotSupportedProject(project))
            {
                return false;
            }

            if (IsExcludedViaProjectProperty(project))
            {
                return false;
            }

            if (IsTestProject(project, hierarchy, this.testRegex, projectName))
            {
                return false;
            }

            return true;
        }

        public void SetTestRegex(Regex regex)
        {
            if (regex == null)
            {
                throw new ArgumentNullException(nameof(regex));
            }

            this.testRegex = regex;
            Debug.Assert(this.testRegex.MatchTimeout != Regex.InfiniteMatchTimeout, "Should have set non-infinite timeout");
        }

        #endregion

        #region Helpers
        private bool IsTestProject(DteProject dteProject, IVsHierarchy projectHierarchy, Regex testProjectNameRegex, string projectName)
        {
            Debug.Assert(dteProject != null);
            Debug.Assert(projectHierarchy != null);

            // Ignore test projects
            // If specifically marked with test project property, use that to specify if test project or not
            bool? sonarTest = this.propertyManager.GetBooleanProperty(dteProject, Constants.SonarQubeTestProjectBuildPropertyKey);
            if (sonarTest.HasValue)
            {
                // Even if the project is a test project by the checks below, if this property was set to false
                // then we treat it as if it's not a test project
                return sonarTest.Value;
            }

            // Otherwise, try to detect test project using known project types and/or regex match
            if (this.projectSystem.IsKnownTestProject(projectHierarchy))
            {
                return true;
            }

            // Heuristically exclude by project name
            return (testProjectNameRegex != null && testProjectNameRegex.IsMatch(projectName));
        }

        private static bool IsNotSupportedProject(DteProject project)
        {
            var language = Language.ForProject(project);
            return (language == null || !language.IsSupported);
        }

        private bool IsExcludedViaProjectProperty(DteProject dteProject)
        {
            Debug.Assert(dteProject != null);

            // General exclusions
            // If exclusion property is set to true, this takes precedence
            bool? sonarExclude = this.propertyManager.GetBooleanProperty(dteProject, Constants.SonarQubeExcludeBuildPropertyKey);
            return sonarExclude.HasValue && sonarExclude.Value;
        }

        #endregion
    }
}
