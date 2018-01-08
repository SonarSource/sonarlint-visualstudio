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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;

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
            this.fileSystem.RegisterFile(path);
        }

        public void ClearRuleSets()
        {
            this.fileSystem.ClearFiles();
            this.savedRuleSets.Clear();
        }

        public void AssertRuleSetExists(string path)
        {
            this.fileSystem.files.Should().ContainKey(path);
        }

        public void AssertRuleSetNotExists(string path)
        {
            this.fileSystem.files.Should().NotContainKey(path);
        }

        public void AssertRuleSetsAreEqual(string ruleSetPath, RuleSet expectedRuleSet)
        {
            this.AssertRuleSetExists(ruleSetPath);

            RuleSet actualRuleSet = this.savedRuleSets[ruleSetPath];

            actualRuleSet.Should().NotBeNull("Expected rule set to be written");
            RuleSetAssert.AreEqual(expectedRuleSet, actualRuleSet);
        }

        public void AssertRuleSetsAreSame(string ruleSetPath, RuleSet expectedRuleSet)
        {
            this.AssertRuleSetExists(ruleSetPath);

            RuleSet actualRuleSet = this.savedRuleSets[ruleSetPath];
            actualRuleSet.Should().Be(expectedRuleSet);
        }

        public void AssertRuleSetLoaded(string ruleSet, int expectedNumberOfTimes)
        {
            int actual = 0;
            this.ruleSetLoaded.TryGetValue(ruleSet, out actual);
            actual.Should().Be(expectedNumberOfTimes, "RuleSet {0} was loaded unexpected number of times", ruleSet);
        }

        public void AssertAllRegisteredRuleSetsLoadedExactlyOnce()
        {
            this.RegisteredRuleSets.ToList().ForEach(rs => this.AssertRuleSetLoaded(rs, 1));
        }

        #endregion Test Helpers
    }
}