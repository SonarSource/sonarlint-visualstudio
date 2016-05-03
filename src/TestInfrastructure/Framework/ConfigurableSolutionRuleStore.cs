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
        private readonly Dictionary<Language, string> registeredPaths = new Dictionary<Language, string>();
        private IDictionary<Language, RuleSet> availableRuleSets;

        #region ISolutionRuleStore

        string ISolutionRuleStore.GetRuleSetFilePath(Language language)
        {
            string path;
            Assert.IsTrue(this.registeredPaths.TryGetValue(language, out path), "No path for group: " + language);
            return path;
        }

        void ISolutionRuleStore.RegisterKnownRuleSets(IDictionary<Language, RuleSet> ruleSets)
        {
            Assert.IsNotNull(ruleSets, "Not expecting nulls");

            this.availableRuleSets = ruleSets;
        }

        #endregion

        #region Test helpers

        public void RegisterRuleSetPath(Language language, string path)
        {
            this.registeredPaths[language] = path;
        }
        #endregion
    }
}
