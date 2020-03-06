/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Text.RegularExpressions;
using EnvDTE;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration
{
    internal class ProjectNameTestProjectIndicator : ITestProjectIndicator, ITestProjectRegexSetter
    {
        private const string DefaultRegex = @"[^\\]*test[^\\]*$";
        private readonly ILogger logger;
        private Regex testRegex;

        public ProjectNameTestProjectIndicator(ILogger logger)
        {
            this.logger = logger;

            SetTestRegex(DefaultRegex);
        }

        public bool? IsTestProject(Project project)
        {
            var isMatch = testRegex.IsMatch(project.Name);

            return isMatch ? true : (bool?)null;
        }

        public void SetTestRegex(string pattern)
        {
            if (pattern == null)
            {
                return;
            }

            try
            {
                Regex.IsMatch("", pattern);

                // Should never realistically take more than 1 second to match against a project name
                var timeout = TimeSpan.FromSeconds(1);
                testRegex = new Regex(pattern, RegexOptions.IgnoreCase, timeout);
            }
            catch (ArgumentException)
            {
                logger.WriteLine(Strings.InvalidTestProjectRegexPattern, pattern);
            }
        }
    }
}
