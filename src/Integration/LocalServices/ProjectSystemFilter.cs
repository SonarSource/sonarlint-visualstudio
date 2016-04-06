//-----------------------------------------------------------------------
// <copyright file="ProjectSystemFilter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
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

        private Regex testRegex;

        public ProjectSystemFilter(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.projectSystem = host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
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

            if (IsExcludedViaProjectProperty(propertyStorage))
            {
                return false;
            }
            
            if (IsTestProject(hierarchy, this.testRegex, projectName))
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
        private static bool IsTestProject(IVsHierarchy projectHierarchy, Regex testProjectNameRegex, string projectName)
        {
            IVsBuildPropertyStorage propertyStorage = projectHierarchy as IVsBuildPropertyStorage;
            Debug.Assert(propertyStorage != null);

            // Ignore test projects
            // If specifically marked with test project property, use that to specify if test project or not
            bool? sonarTest = GetPropertyBool(propertyStorage, Constants.SonarQubeTestProjectBuildPropertyKey);
            if (sonarTest.HasValue)
            {
                // Event if the project is a test project by the checks below, if this property was set to false
                // then we treat it as if it's not a test project
                return sonarTest.Value;
            }

            // Otherwise, try to detect test project using known project types and/or regex match
            if (ProjectSystemHelper.IsKnownTestProject(projectHierarchy))
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

        private static bool IsExcludedViaProjectProperty(IVsBuildPropertyStorage propertyStorage)
        {
            Debug.Assert(propertyStorage != null);

            // General exclusions
            // If exclusion property is set to true, this takes precedence
            bool? sonarExclude = GetPropertyBool(propertyStorage, Constants.SonarQubeExcludeBuildPropertyKey);
            return sonarExclude.HasValue && sonarExclude.Value;
        }

        private static bool? GetPropertyBool(IVsBuildPropertyStorage propertyStorage, string propertyName)
        {
            string valueString = null;
            var hr = propertyStorage.GetPropertyValue(propertyName, string.Empty,
                (uint)_PersistStorageType.PST_PROJECT_FILE, out valueString);

            if (ErrorHandler.Succeeded(hr))
            {
                bool value;
                if (bool.TryParse(valueString, out value))
                {
                    return value;
                }
            }

            return null;
        }

        #endregion
    }
}
