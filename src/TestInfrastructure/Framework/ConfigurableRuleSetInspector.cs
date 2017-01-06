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

using SonarLint.VisualStudio.Integration.ProfileConflicts;
using System;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableRuleSetInspector : IRuleSetInspector
    {
        #region IRuleSetInspector
        RuleConflictInfo IRuleSetInspector.FindConflictingRules(string baselineRuleSet, string targetRuleSet, params string[] ruleSetDirectories)
        {
            return this.FindConflictingRulesAction?.Invoke(baselineRuleSet, targetRuleSet, ruleSetDirectories);
        }

        FixedRuleSetInfo IRuleSetInspector.FixConflictingRules(string baselineRuleSetPath, string targetRuleSetPath, params string[] ruleSetDirectories)
        {
            return this.FixConflictingRulesAction?.Invoke(baselineRuleSetPath, targetRuleSetPath, ruleSetDirectories);
        }
        #endregion

        #region Test helpers
        public Func<string, string, string[], RuleConflictInfo> FindConflictingRulesAction
        {
            get; set;
        }

        public Func<string, string, string[], FixedRuleSetInfo> FixConflictingRulesAction
        {
            get; set;
        }
        #endregion
    }
}
