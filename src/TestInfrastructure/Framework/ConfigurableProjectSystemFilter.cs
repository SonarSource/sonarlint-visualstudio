//-----------------------------------------------------------------------
// <copyright file="ConfigurableProjectSystemFilter.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DteProject = EnvDTE.Project;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableProjectSystemFilter : IProjectSystemFilter
    {
        public Regex TestRegex { get; set; }

        public bool? AllProjectsMatchReturn { get; set; }

        public List<DteProject> MatchingProjects { get; } = new List<DteProject>();

        #region IProjectSystemFilter

        bool IProjectSystemFilter.IsAccepted(DteProject dteProject)
        {
            if (this.AllProjectsMatchReturn.HasValue)
            {
                return this.AllProjectsMatchReturn.Value;
            }

            return this.MatchingProjects.Any(x => StringComparer.OrdinalIgnoreCase.Equals(x.UniqueName, dteProject.UniqueName));
        }

        void IProjectSystemFilter.SetTestRegex(Regex regex)
        {
            this.TestRegex = regex;
        }

        #endregion

        #region Test Helpers

        public void AssertTestRegex(string regex, RegexOptions options)
        {
            Assert.IsNotNull(this.TestRegex, "Expected test regex to be set");
            Assert.AreEqual(regex, this.TestRegex.ToString(), "Unexpected test regular expression");
            Assert.AreEqual(options, this.TestRegex.Options, "Unexpected test regex options");
        }

        #endregion
    }
}
