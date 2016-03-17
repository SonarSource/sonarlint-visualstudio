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
        private readonly Dictionary<RuleSetGroup, string> registeredPaths = new Dictionary<RuleSetGroup, string>();
        private IDictionary<RuleSetGroup, RuleSet> availableRuleSets;

        #region ISolutionRuleStore

        string ISolutionRuleStore.GetRuleSetFilePath(RuleSetGroup group)
        {
            string path;
            Assert.IsTrue(this.registeredPaths.TryGetValue(group, out path), "No path for group: " + group);
            return path;
        }

        void ISolutionRuleStore.RegisterKnownRuleSets(IDictionary<RuleSetGroup, RuleSet> ruleSets)
        {
            Assert.IsNotNull(ruleSets, "Not expecting nulls");

            this.availableRuleSets = ruleSets;
        }

        #endregion

        #region Test helpers

        public void RegisterRuleSetPath(RuleSetGroup group, string path)
        {
            this.registeredPaths[group] = path;
        }
        #endregion
    }
}
