//-----------------------------------------------------------------------
// <copyright file="ConfigurableSolutionRuleStore.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Assert.IsTrue(this.availableRuleSets.TryGetValue(language, out ruleSet), "No RuleSet for group: " + language);

            return ruleSet;
        }

        void ISolutionRuleStore.RegisterKnownRuleSets(IDictionary<Language, RuleSet> ruleSets)
        {
            Assert.IsNotNull(ruleSets, "Not expecting nulls");

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
