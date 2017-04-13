/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
 * mailto:info AT sonarsource DOT com
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
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

        #endregion IProjectSystemFilter

        #region Test Helpers

        public void AssertTestRegex(string regex, RegexOptions options)
        {
            this.TestRegex.Should().NotBeNull("Expected test regex to be set");
            this.TestRegex.ToString().Should().Be(regex, "Unexpected test regular expression");
            this.TestRegex.Options.Should().Be(options, "Unexpected test regex options");
        }

        #endregion Test Helpers
    }
}