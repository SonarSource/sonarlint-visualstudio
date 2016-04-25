//-----------------------------------------------------------------------
// <copyright file="ProjectPropertyManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IProjectPropertyManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectPropertyManager : IProjectPropertyManager
    {
        private const string TestProperty = Constants.SonarQubeTestProjectBuildPropertyKey;
        private const string ExcludeProperty = Constants.SonarQubeExcludeBuildPropertyKey;

        private readonly IProjectSystemHelper projectSystem;

        [ImportingConstructor]
        public ProjectPropertyManager(IHost host)
        {
            this.projectSystem = host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
        }

        #region IProjectPropertyManager

        public IEnumerable<Project> GetSupportedSelectedProjects()
        {
            return this.projectSystem?
                .GetSelectedProjects()
                .Where(x => Language.ForProject(x).IsSupported)
                ?? Enumerable.Empty<Project>();
        }

        public bool GetExcludedProperty(Project project)
        {
            string propertyString = this.projectSystem.GetProjectProperty(project, ExcludeProperty);

            bool propertyValue;
            if (bool.TryParse(propertyString, out propertyValue))
            {
                return propertyValue;
            }

            return false;
        }

        public void SetExcludedProperty(Project project, bool value)
        {
            if (value)
            {
                this.projectSystem.SetProjectProperty(project, ExcludeProperty, true.ToString());
            }
            else
            {
                this.projectSystem.ClearProjectProperty(project, ExcludeProperty);
            }
        }

        public bool? GetTestProjectProperty(Project project)
        {
            string propertyString = this.projectSystem.GetProjectProperty(project, TestProperty);

            bool propertyValue;
            if (bool.TryParse(propertyString, out propertyValue))
            {
                return propertyValue;
            }

            return null;
        }

        public void SetTestProjectProperty(Project project, bool? value)
        {
            if (value.HasValue)
            {
                this.projectSystem.SetProjectProperty(project, TestProperty, value.Value.ToString());
            }
            else
            {
                this.projectSystem.ClearProjectProperty(project, TestProperty);
            }
        }

        #endregion
    }
}
