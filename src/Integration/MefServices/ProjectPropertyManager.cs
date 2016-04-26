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

        #region IProjectPropertyManager

        public IEnumerable<Project> GetSelectedProjects()
        {
            return this.projectSystem
                .GetSelectedProjects()
                ?? Enumerable.Empty<Project>();
        }

        public bool? GetBooleanProperty(Project project, string propertyName)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            string propertyString = this.projectSystem.GetProjectProperty(project, propertyName);

            bool propertyValue;
            if (bool.TryParse(propertyString, out propertyValue))
            {
                return propertyValue;
            }

            return null;
        }

        public void SetBooleanProperty(Project project, string propertyName, bool? value)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (value.HasValue)
            {
                this.projectSystem.SetProjectProperty(project, propertyName, value.Value.ToString());
            }
            else
            {
                this.projectSystem.ClearProjectProperty(project, propertyName);
            }
        }

        #endregion
    }
}
