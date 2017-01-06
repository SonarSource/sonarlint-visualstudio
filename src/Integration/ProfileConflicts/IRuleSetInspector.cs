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

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.ProfileConflicts
{
    /// <summary>
    /// RuleSet inspection service
    /// </summary>
    public interface IRuleSetInspector : ILocalService
    {
        /// <summary>
        /// Inspects whether the <paramref name="baselineRuleSet"/> rules are missing or less strict than in the <paramref name="targetRuleSet"/>
        /// </summary>
        /// <param name="baselineRuleSet">Required full path to baseline RuleSet</param>
        /// <param name="targetRuleSet">Required full path to target RuleSet</param>
        /// <param name="ruleSetDirectories">Optional rule set directories i.e. when the <paramref name="targetRuleSet"/> is not absolute</param>
        /// <returns><see cref="RuleConflictInfo"/></returns>
        RuleConflictInfo FindConflictingRules(string baselineRuleSet, string targetRuleSet, params string[] ruleSetDirectories);

        /// <summary>
        /// Will analyze the RuleSet in <paramref name="targetRuleSetPath"/> for conflicts with RuleSet in <paramref name="baselineRuleSetPath"/>.
        /// Will fix those conflicts in-memory and will either way return the target RuleSet (i.e. even if there were no conflicts to begin with).
        /// </summary>
        /// <param name="baselineRuleSet">Required full path to baseline RuleSet</param>
        /// <param name="targetRuleSet">Required full path to target RuleSet</param>
        /// <param name="ruleSetDirectories">Optional rule set directories i.e. when the <paramref name="targetRuleSet"/> is not absolute</param>
        FixedRuleSetInfo FixConflictingRules(string baselineRuleSetPath, string targetRuleSetPath, params string[] ruleSetDirectories);
    }
}
