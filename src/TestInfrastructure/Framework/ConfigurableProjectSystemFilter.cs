/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
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
            this.TestRegex.Should().NotBeNull("Expected test regex to be set");
            this.TestRegex.ToString().Should().Be(regex, "Unexpected test regular expression");
            this.TestRegex.Options.Should().Be(options, "Unexpected test regex options");
        }

        #endregion
    }
}
