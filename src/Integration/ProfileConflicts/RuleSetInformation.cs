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

using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// Data-only class that represents aggregated rule set information.
    /// Same rule sets should be represented by the same instance of <see cref="RuleSetInformation"/> and have their <see cref="RuleSetDeclaration.ConfigurationContext"/>
    /// associated with the shared instance by adding them into <see cref="ConfigurationContexts"/>.
    /// </summary>
    /// <seealso cref="RuleSetDeclaration"/>
    public class RuleSetInformation
    {
        public RuleSetInformation(string projectFullName, string baselineRuleSet, string projectRuleSet, IEnumerable<string> ruleSetDirectories)
        {
            if (string.IsNullOrWhiteSpace(projectFullName))
            {
                throw new ArgumentNullException(nameof(projectFullName));
            }

            if (string.IsNullOrWhiteSpace(baselineRuleSet))
            {
                throw new ArgumentNullException(nameof(baselineRuleSet));
            }

            if (string.IsNullOrWhiteSpace(projectRuleSet))
            {
                throw new ArgumentNullException(nameof(projectRuleSet));
            }

            this.RuleSetProjectFullName = projectFullName;
            this.BaselineFilePath = baselineRuleSet;
            this.RuleSetFilePath = projectRuleSet;
            this.RuleSetDirectories = ruleSetDirectories?.ToArray() ?? new string[0];
        }

        public string RuleSetProjectFullName { get; }

        public string BaselineFilePath { get; }

        public string RuleSetFilePath { get; }

        public string[] RuleSetDirectories { get; }

        public HashSet<string> ConfigurationContexts { get; } = new HashSet<string>();
    }
}
