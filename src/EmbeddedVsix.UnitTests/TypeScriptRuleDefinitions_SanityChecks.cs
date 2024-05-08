/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Linq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Configuration;
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

            var settingsProvider = new Mock<IRuleSettingsProviderFactory>();
            settingsProvider.Setup(x => x.Get(Language.Unknown)).Returns(Mock.Of<IRuleSettingsProvider>());

            var hotspotsAnalysisConfiguration = new Mock<IConnectedModeFeaturesConfiguration>();
            hotspotsAnalysisConfiguration.Setup(x => x.IsHotspotsAnalysisEnabled()).Returns(true);

            // Sanity check that the json file is loadable and has rules
            var factory = new RulesProviderFactory(filePath, settingsProvider.Object, hotspotsAnalysisConfiguration.Object);

            var jsRules = factory.Create("javascript", Language.Unknown).GetDefinitions();
            CheckRules("JavaScript", jsRules);

            var tsRules = factory.Create("typescript", Language.Unknown).GetDefinitions();
            CheckRules("TypeScript", tsRules);
        }

        private static void CheckRules(string language, IEnumerable<RuleDefinition> rules)
        {
            var ruleCount = rules.Count();
            var parameterisedRulesCount = rules.Count(RuleHasParameters);

            var hasNullDefaultParams = rules.Any(HasNullDefaultParameters);
            hasNullDefaultParams.Should().BeFalse();

            var rulesWithDefaultParamsCount = rules.Count(HasDefaultParameters);

            Console.WriteLine($"{language}: rules: {ruleCount}, parameterised rules: {parameterisedRulesCount}, non-null default params: {rulesWithDefaultParamsCount}");

            ruleCount.Should().BeGreaterThan(200);
            parameterisedRulesCount.Should().BeGreaterThan(10);
            rulesWithDefaultParamsCount.Should().BeGreaterThan(5);

            rules.All(RuleKeyIsValid).Should().BeTrue();
            rules.All(EslintKeyIsValid).Should().BeTrue();

            // All Sonar and ESLint rule keys should be distinct
            rules.Select(r => r.RuleKey).Distinct().Count().Should().Be(ruleCount);
            rules.Select(r => r.EslintKey).Distinct().Count().Should().Be(ruleCount);
        }

        private static bool RuleHasParameters(RuleDefinition ruleDefinition) =>
            ruleDefinition.Params.Length > 0;

        private static bool RuleKeyIsValid(RuleDefinition ruleDefinition) =>
            ruleDefinition.RuleKey != null &&
            (HasRepoPrefix(ruleDefinition, "javascript:") || HasRepoPrefix(ruleDefinition, "typescript:"));

        private static bool HasRepoPrefix(RuleDefinition ruleDefinition, string prefix) =>
            ruleDefinition.RuleKey.StartsWith(prefix) && ruleDefinition.RuleKey.Length > prefix.Length;

        private static bool EslintKeyIsValid(RuleDefinition ruleDefinition) =>
            !string.IsNullOrEmpty(ruleDefinition.EslintKey) ||
            // Special case - "JavaScript parser failure"
            ruleDefinition.RuleKey.EndsWith(":S2260");

        private static bool HasDefaultParameters(RuleDefinition ruleDefinition) =>
            ruleDefinition.DefaultParams.Length > 0;

        private static bool HasNullDefaultParameters(RuleDefinition ruleDefinition) =>
            ruleDefinition.DefaultParams == null;
    }
}
