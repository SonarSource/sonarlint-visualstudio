//-----------------------------------------------------------------------
// <copyright file="ProjectSystemFilter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

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
