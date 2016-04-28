//-----------------------------------------------------------------------
// <copyright file="ConfigurableSolutionBindingInformationProvider.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using EnvDTE;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionBindingInformationProvider : ISolutionBindingInformationProvider
    {
        public IEnumerable<Project> BoundProjects { get; set; } = Enumerable.Empty<Project>();

        public IEnumerable<Project> UnboundProjects { get; set; } = Enumerable.Empty<Project>();

        public bool SolutionBound { get; set; }

        public IEnumerable<Project> GetBoundProjects()
        {
            return this.BoundProjects;
        }

        public IEnumerable<Project> GetUnboundProjects()
        {
            return this.UnboundProjects;
        }

        public bool IsSolutionBound()
        {
            return this.SolutionBound;
        }
    }
}
