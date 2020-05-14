/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableRuleSetSerializer : IRuleSetSerializer
    {
        private readonly Dictionary<string, RuleSet> savedRuleSets = new Dictionary<string, RuleSet>(StringComparer.OrdinalIgnoreCase);
        private readonly MockFileSystem fileSystem;
        private readonly Dictionary<string, int> ruleSetLoaded = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public ConfigurableRuleSetSerializer(MockFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        #region IRuleSetFileSystem

        RuleSet IRuleSetSerializer.LoadRuleSet(string path)
        {
            this.savedRuleSets.TryGetValue(path, out RuleSet rs);
            this.ruleSetLoaded.TryGetValue(path, out int counter);
            this.ruleSetLoaded[path] = ++counter;
            rs?.Validate();
            return rs;
        }

        void IRuleSetSerializer.WriteRuleSetFile(RuleSet ruleSet, string path)
        {
            if (!this.savedRuleSets.TryGetValue(path, out _))
            {
                this.savedRuleSets[path] = ruleSet;
            }
            this.fileSystem.AddFile(path, new MockFileData(""));
        }

        #endregion IRuleSetFileSystem

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
            this.fileSystem.AddFile(path, new MockFileData(""));
        }

        public void ClearRuleSets()
        {
            foreach (var filePath in fileSystem.AllFiles)
            {
                fileSystem.RemoveFile(filePath);
            }
            savedRuleSets.Clear();
        }

        public void AssertRuleSetExists(string path)
        {
            fileSystem.GetFile(path).Should().NotBe(null);
        }

        public void AssertRuleSetNotExists(string path)
        {
            fileSystem.GetFile(path).Should().Be(null);
        }

        public void AssertRuleSetsAreEqual(string ruleSetPath, Core.CSharpVB.RuleSet expectedCoreRuleSet)
        {
            this.AssertRuleSetExists(ruleSetPath);
            RuleSet actualRuleSet = this.savedRuleSets[ruleSetPath];

            actualRuleSet.Should().NotBeNull("Expected rule set to be written");

            var expectedVsRuleSet = ToVsRuleSet(expectedCoreRuleSet);
            RuleSetAssert.AreEqual(expectedVsRuleSet, actualRuleSet);
        }

        private static RuleSet ToVsRuleSet(Core.CSharpVB.RuleSet coreRuleset)
        {
            var tempRuleSetFilePath = Path.GetTempFileName();
            File.WriteAllText(tempRuleSetFilePath, coreRuleset.ToXml());
            var ruleSet = RuleSet.LoadFromFile(tempRuleSetFilePath);

            return ruleSet;
        }

        public void AssertRuleSetsAreSame(string ruleSetPath, RuleSet expectedRuleSet)
        {
            this.AssertRuleSetExists(ruleSetPath);

            RuleSet actualRuleSet = this.savedRuleSets[ruleSetPath];
            actualRuleSet.Should().Be(expectedRuleSet);
        }

        public void AssertRuleSetLoaded(string ruleSet, int expectedNumberOfTimes)
        {
            this.ruleSetLoaded.TryGetValue(ruleSet, out int actual);
            actual.Should().Be(expectedNumberOfTimes, "RuleSet {0} was loaded unexpected number of times", ruleSet);
        }

        #endregion Test Helpers
    }
}
