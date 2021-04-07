/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.AdditionalFiles.UnitTests
{
    // Sanity checks that the json file extracted from the SonarJS jar is in the format we expect

    [TestClass]
    public class TypeScriptRuleDefinitions_SanityChecks
    {
        [TestMethod]
        public void LoadRuleMetadataFromFile()
        {
            // There should be a copy of the rules metadata file in the test bin directory
            var filePath = Path.Combine(
                Path.GetDirectoryName(this.GetType().Assembly.Location),
                "ts\\sonarlint-metadata.json"
                );

            File.Exists(filePath).Should().BeTrue("Test setup error: could not find rule metadata file. Expected path: " + filePath);

            // Sanity check that the json file is loadable and has rules
            var rulesProvider = new RuleDefinitionsProvider(filePath);

            var jsRules = ((IJavaScriptRuleDefinitionsProvider)rulesProvider).GetDefinitions();
            // Note: there's currently a bug on the SonarJS side - see https://github.com/SonarSource/sonarlint-visualstudio/issues/2282
            jsRules.Count().Should().Be(0);
            jsRules.Any(RuleHasParameters).Should().BeFalse();
            jsRules.All(RuleKeyIsValid).Should().BeTrue();

            var tsRules = ((ITypeScriptRuleDefinitionsProvider)rulesProvider).GetDefinitions();
            tsRules.Count().Should().BeGreaterThan(50);
            tsRules.Any(RuleHasParameters).Should().BeTrue();
            tsRules.All(RuleKeyIsValid).Should().BeTrue();
        }

        private static bool RuleHasParameters(RuleDefinition ruleDefinition) =>
            ruleDefinition.Params.Length > 0;

        private static bool RuleKeyIsValid(RuleDefinition ruleDefinition) =>
            ruleDefinition.RuleKey != null &&
            (HasRepoPrefix(ruleDefinition, "javascript:") || HasRepoPrefix(ruleDefinition, "typescript:"));

        private static bool HasRepoPrefix(RuleDefinition ruleDefinition, string prefix) =>
            ruleDefinition.RuleKey.StartsWith(prefix) && ruleDefinition.RuleKey.Length > prefix.Length;
    }
}
