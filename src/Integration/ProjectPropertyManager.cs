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
using System;

namespace SonarLint.VisualStudio.Integration
{
    [Export(typeof(IProjectPropertyManager))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ProjectPropertyManager : IProjectPropertyManager
    {
        public const string TestProperty = Constants.SonarQubeTestProjectBuildPropertyKey;
        public const string ExcludeProperty = Constants.SonarQubeExcludeBuildPropertyKey;

        private readonly IProjectSystemHelper projectSystem;

        [ImportingConstructor]
        public ProjectPropertyManager(IHost host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.projectSystem = host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
        }

        internal /*for testing purposes*/ ProjectPropertyManager(IProjectSystemHelper projectSystem)
        {
            this.projectSystem = projectSystem;
        }

        #region IProjectPropertyManager

        public IEnumerable<Project> GetSelectedProjects()
        {
            return this.projectSystem?
                .GetSelectedProjects()
                ?? Enumerable.Empty<Project>();
        }

        public bool GetExcludedProperty(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

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
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

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
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

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
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

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
