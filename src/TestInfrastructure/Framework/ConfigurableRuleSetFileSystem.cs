//-----------------------------------------------------------------------
// <copyright file="ConfigurableRuleSetFileSystem.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableRuleSetFileSystem : IRuleSetFileSystem
    {
        private readonly Dictionary<string, RuleSet> savedRuleSets = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);
        private readonly ConfigurableFileSystem fileSystem;

        public ConfigurableRuleSetFileSystem()
            :this(new ConfigurableFileSystem())
        {

        }

        public ConfigurableRuleSetFileSystem(ConfigurableFileSystem fs)
        {
            this.fileSystem = fs;
        }

        #region IRuleSetFileSystem
        RuleSet IRuleSetFileSystem.LoadRuleSet(string path)
        {
            RuleSet rs;
            if (this.savedRuleSets.TryGetValue(path, out rs))
            {
                if (rs == null)
                {
                    throw new XmlException("File is empty in test file system"); // Simulate RuleSet.LoadFromFile()
                }
                rs.Validate(); // Simulate RuleSet.LoadFromFile() (throws InvalidRuleSetException)
                return rs;
            }
            throw new IOException("File does not exist in test file system"); // Simulate RuleSet.LoadFromFile()
        }

        void IRuleSetFileSystem.WriteRuleSetFile(RuleSet ruleSet, string path)
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
        #endregion
    }
}
