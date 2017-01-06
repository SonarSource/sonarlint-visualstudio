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

using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Data-only class that represents rule set information for specific configuration
    /// </summary>
    public class RuleSetDeclaration
    {
        public RuleSetDeclaration(Project project, Property ruleSetProperty, string ruleSetPath, string activationContext, params string[] ruleSetDirectories)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (ruleSetProperty == null)
            {
                throw new ArgumentNullException(nameof(ruleSetProperty));
            }

            this.RuleSetProjectFullName = project.FullName;
            this.DeclaringProperty = ruleSetProperty;
            this.ConfigurationContext = activationContext;
            this.RuleSetPath = ruleSetPath;
            this.RuleSetDirectories = ruleSetDirectories.ToList(); // avoid aliasing bugs
        }

        /// <summary>
        /// <see cref="Project.FullName"/>
        /// </summary>
        public string RuleSetProjectFullName { get; }

        /// <summary>
        /// The property declaring the rule set
        /// </summary>
        public Property DeclaringProperty { get; }

        /// <summary>
        /// Path to the rule set file. File name, relative path, absolute path and whitespace are all valid.
        /// </summary>
        public string RuleSetPath { get; }

        /// <summary>
        /// Additional rule set directories to search the <see cref="RuleSetPath"/> i.e. in case the rule set is not an absolute path.
        /// </summary>
        public IEnumerable<string> RuleSetDirectories { get; }

        /// <summary>
        /// In which context the ruleset is active i.e. the configuration name
        /// </summary>
        public string ConfigurationContext { get; }
    }
}
