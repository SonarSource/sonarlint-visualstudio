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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableSolutionRuleStore: ISolutionRuleStore
    {
        private readonly Dictionary<LanguageGroup, string> registeredPaths = new Dictionary<LanguageGroup, string>();
        private IDictionary<LanguageGroup, RuleSet> availableRuleSets;

        #region ISolutionRuleStore

        string ISolutionRuleStore.GetRuleSetFilePath(LanguageGroup group)
        {
            string path;
            Assert.IsTrue(this.registeredPaths.TryGetValue(group, out path), "No path for group: " + group);
            return path;
        }

        void ISolutionRuleStore.RegisterKnownRuleSets(IDictionary<LanguageGroup, RuleSet> ruleSets)
        {
            Assert.IsNotNull(ruleSets, "Not expecting nulls");

            this.availableRuleSets = ruleSets;
        }

        #endregion

        #region Test helpers

        public void RegisterRuleSetPath(LanguageGroup group, string path)
        {
            this.registeredPaths[group] = path;
        }
        #endregion
    }
}
