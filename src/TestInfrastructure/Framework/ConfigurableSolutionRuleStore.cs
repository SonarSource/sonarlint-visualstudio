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
using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
using SonarLint.VisualStudio.Integration.Binding;
using System.Collections.Generic;
using static SonarLint.VisualStudio.Integration.Binding.SolutionBindingOperation;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionRuleStore : ISolutionRuleStore
    {
        private readonly Dictionary<Language, RuleSetInformation> availableRuleSets = new Dictionary<Language, RuleSetInformation>();

        #region ISolutionRuleStore

        RuleSetInformation ISolutionRuleStore.GetRuleSetInformation(Language language)
        {
            RuleSetInformation ruleSet;
            this.availableRuleSets.TryGetValue(language, out ruleSet).Should().BeTrue("No RuleSet for group: " + language);

            return ruleSet;
        }

        void ISolutionRuleStore.RegisterKnownRuleSets(IDictionary<Language, RuleSet> ruleSets)
        {
            ruleSets.Should().NotBeNull("Not expecting nulls");

            foreach (var rule in ruleSets)
            {
                availableRuleSets.Add(rule.Key, new RuleSetInformation(rule.Key, rule.Value));
            }
        }

        #endregion

        #region Test helpers

        public void RegisterRuleSetPath(Language language, string path)
        {
            if (!this.availableRuleSets.ContainsKey(language))
            {
                this.availableRuleSets[language] = new RuleSetInformation(language, new RuleSet("SonarQube"));
            }

            this.availableRuleSets[language].NewRuleSetFilePath = path;
        }

        #endregion
    }
}
