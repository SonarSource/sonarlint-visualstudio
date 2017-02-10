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

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.ProfileConflicts;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableConflictsManager : IConflictsManager
    {
        private readonly List<ProjectRuleSetConflict> currentConflicts = new List<ProjectRuleSetConflict>();

        #region IConflictsManager

        IReadOnlyList<ProjectRuleSetConflict> IConflictsManager.GetCurrentConflicts()
        {
            return this.currentConflicts;
        }

        #endregion IConflictsManager

        #region Test helpers

        public static ProjectRuleSetConflict CreateConflict(string projectFilePath = "project.csproj", string baselineRuleSet = "baseline.ruleset", string projectRuleSet = "project.csproj", int numberOfConflictingRules = 1)
        {
            IEnumerable<string> ids = Enumerable.Range(0, numberOfConflictingRules).Select(i => "id" + i);
            var ruleSet = TestRuleSetHelper.CreateTestRuleSetWithRuleIds(ids);

            var conflict = new ProjectRuleSetConflict(
                    new RuleConflictInfo(ruleSet.Rules, new Dictionary<RuleReference, RuleAction>()),
                    new RuleSetInformation(projectFilePath, baselineRuleSet, projectRuleSet, null));

            return conflict;
        }

        public ProjectRuleSetConflict AddConflict()
        {
            ProjectRuleSetConflict conflict = CreateConflict();
            this.AddConflict(conflict);
            return conflict;
        }

        public void AddConflict(ProjectRuleSetConflict conflict)
        {
            this.currentConflicts.Add(conflict);
        }

        public void ClearConflicts()
        {
            this.currentConflicts.Clear();
        }

        #endregion Test helpers
    }
}