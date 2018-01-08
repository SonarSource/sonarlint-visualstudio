/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using SonarLint.VisualStudio.Integration.Binding;

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

        #endregion ISolutionRuleStore

        #region Test helpers

        public void RegisterRuleSetPath(Language language, string path)
        {
            if (!this.availableRuleSets.ContainsKey(language))
            {
                this.availableRuleSets[language] = new RuleSetInformation(language, new RuleSet("SonarQube"));
            }

            this.availableRuleSets[language].NewRuleSetFilePath = path;
        }

        #endregion Test helpers
    }
}