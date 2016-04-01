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

namespace SonarLint.VisualStudio.Integration.Binding
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

        public bool IsAccepted(DteProject dteProject)
        {
            if (dteProject == null)
            {
                throw new ArgumentNullException(nameof(dteProject));
            }

            var projectName = dteProject.Name;
            var hierarchy = this.projectSystem.GetIVsHierarchy(dteProject);
            var propertyStorage = hierarchy as IVsBuildPropertyStorage;

            if (hierarchy == null || propertyStorage == null)
            {
                throw new ArgumentException(Strings.ProjectFilterDteProjectFailedToGetIVs, nameof(dteProject));
            }

            // Accept only supported languages
            var language = Language.ForProject(dteProject);
            if (language == null || !language.IsSupported)
            {
                return false;
            }

            // General exclusions
            // If exclusion property is set, this takes precedence
            bool? sonarExclude = GetPropertyBool(propertyStorage, Constants.SonarQubeExcludeBuildPropertyKey);
            if (sonarExclude.HasValue)
            {
                return !sonarExclude.Value;
            }

            // Ignore test projects
            // If specifically marked with test project property, use that to specifiy if test project or not
            bool? sonarTest = GetPropertyBool(propertyStorage, Constants.SonarQubeTestProjectBuildPropertyKey);
            if (sonarTest.HasValue)
            {
                return !sonarTest.Value;
            }
            
            // Otherwise, try to detect test project using known project types and/or regex match
            if (ProjectSystemHelper.IsKnownTestProject(hierarchy)
            || (this.testRegex != null && this.testRegex.IsMatch(projectName)))
            {
                return false;
            }

            return true;
        }

        public void SetTestRegex(Regex regex)
        {
            this.testRegex = regex;
            Debug.Assert(this.testRegex == null || (this.testRegex.MatchTimeout != Regex.InfiniteMatchTimeout), "Should have set non-infinite timeout");
        }

        #endregion

        #region Helpers

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
