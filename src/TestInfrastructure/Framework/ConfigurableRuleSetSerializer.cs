//-----------------------------------------------------------------------
// <copyright file="ConfigurableRuleSetSerializer.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableRuleSetSerializer : IRuleSetSerializer
    {
        private readonly Dictionary<string, RuleSet> savedRuleSets = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);
        private readonly ConfigurableFileSystem fileSystem;
        private readonly Dictionary<string, int> ruleSetLoaded = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public ConfigurableRuleSetSerializer()
            : this(new ConfigurableFileSystem())
        {

        }

        public ConfigurableRuleSetSerializer(ConfigurableFileSystem fs)
        {
            this.fileSystem = fs;
        }

        #region IRuleSetFileSystem
        RuleSet IRuleSetSerializer.LoadRuleSet(string path)
        {
            RuleSet rs = null;
            this.savedRuleSets.TryGetValue(path, out rs);
            int counter = 0;
            this.ruleSetLoaded.TryGetValue(path, out counter);
            this.ruleSetLoaded[path] = ++counter;
            rs?.Validate();
            return rs;
        }

        void IRuleSetSerializer.WriteRuleSetFile(RuleSet ruleSet, string path)
        {
            RuleSet rs;
            if (!this.savedRuleSets.TryGetValue(path, out rs))
            {
                this.savedRuleSets[path] = ruleSet;
            }
            this.fileSystem.UpdateTimestamp(path);
        }
        #endregion

        #region Test Helpers
        public IEnumerable<string> RegisteredRuleSets
        {
            get
            {
                return this.savedRuleSets.Keys;
            }
        }

        public void RegisterRuleSet(RuleSet ruleSet)
        {
            this.RegisterRuleSet(ruleSet, ruleSet.FilePath);
        }

        public void RegisterRuleSet(RuleSet ruleSet, string path)
        {
            this.savedRuleSets[path] = ruleSet;
            this.fileSystem.RegisterFile(path);
        }

        public void ClearRuleSets()
        {
            this.fileSystem.ClearFiles();
            this.savedRuleSets.Clear();
        }

        public void AssertRuleSetExists(string path)
        {
            this.fileSystem.AssertFileExists(path);
        }

        public void AssertRuleSetNotExists(string path)
        {
            this.fileSystem.AssertFileNotExists(path);
        }

        public void AssertRuleSetsAreEqual(string ruleSetPath, RuleSet expectedRuleSet)
        {
            this.AssertRuleSetExists(ruleSetPath);

            RuleSet actualRuleSet = this.savedRuleSets[ruleSetPath];

            Assert.IsNotNull(actualRuleSet, "Expected rule set to be written");
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet);
        }

        public void AssertRuleSetsAreSame(string ruleSetPath, RuleSet expectedRuleSet)
        {
            this.AssertRuleSetExists(ruleSetPath);

            RuleSet actualRuleSet = this.savedRuleSets[ruleSetPath];
            Assert.AreSame(expectedRuleSet, actualRuleSet);
        }

        public void AssertRuleSetLoaded(string ruleSet, int expectedNumberOfTimes)
        {
            int actual = 0;
            this.ruleSetLoaded.TryGetValue(ruleSet, out actual);
            Assert.AreEqual(expectedNumberOfTimes, actual, "RuleSet {0} was loaded unexpected number of times", ruleSet);
        }

        public void AssertAllRegisteredRuleSetsLoadedExactlyOnce()
        {
            this.RegisteredRuleSets.ToList().ForEach(rs => this.AssertRuleSetLoaded(rs, 1));
        }
        #endregion
    }
}
