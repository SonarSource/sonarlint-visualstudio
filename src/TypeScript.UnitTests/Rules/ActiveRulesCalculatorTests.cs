/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Rules
{
    [TestClass]
    public class ActiveRulesCalculatorTests
    {
        private static readonly IRuleSettingsProvider EmptyRuleSettingsProvider = new RuleSettingsBuilder();

        [TestMethod]
        public void Get_ReturnsRulesWithCorrectFileTargetType()
        {
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule("active 1", activeByDefault: true)
                .AddRule("inactive AAA", activeByDefault: false)
                .AddRule("active 2", activeByDefault: true);

            var testSubject = CreateTestSubject(ruleDefns, EmptyRuleSettingsProvider);

            var result = testSubject.Calculate().ToArray();

            result.Length.Should().Be(2);
            result[0].FileTypeTarget.Should().BeEquivalentTo("MAIN");
            result[1].FileTypeTarget.Should().BeEquivalentTo("MAIN");
        }

        [TestMethod]
        public void Get_NoRuleOverrides_InactiveRulesAreNotReturned()
        {
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule("active 1", activeByDefault: true)
                .AddRule("inactive AAA", activeByDefault: false)
                .AddRule("active 2", activeByDefault: true)
                .AddRule("inactive BBB", activeByDefault: false);

            var testSubject = CreateTestSubject(ruleDefns, EmptyRuleSettingsProvider);

            var result = testSubject.Calculate().ToArray();

            CheckExpectedRuleKeys(result, "active 1", "active 2");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_NoRuleOverrides_HotspotsAreNotReturned()
        {
            // NOTE: there are currently no taint vulnerabilities in the SonarJS jar
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule("bug", ruleType: RuleType.BUG)
                .AddRule("codesmell", ruleType: RuleType.CODE_SMELL)
                .AddRule("hotspot", ruleType: RuleType.SECURITY_HOTSPOT)
                .AddRule("vuln", ruleType: RuleType.VULNERABILITY);

            var testSubject = CreateTestSubject(ruleDefns, EmptyRuleSettingsProvider);

            var result = testSubject.Calculate().ToArray();

            CheckExpectedRuleKeys(result, "bug", "codesmell", "vuln");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_NoRuleOverrides_RulesWithNullEslintKeysAreNotReturned()
        {
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule("aaa")
                .AddRule(null)
                .AddRule("bbb");

            var testSubject = CreateTestSubject(ruleDefns, EmptyRuleSettingsProvider);

            var result = testSubject.Calculate().ToArray();

            CheckExpectedRuleKeys(result, "aaa", "bbb");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_NoRuleOverrides_RulesWithConfigurations_ExpectedConfigsReturned()
        {
            var config1 = new object();
            var config2 = "a default rule value";

            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule("aaa", configurations: new object[] { config1, config2 });

            var testSubject = CreateTestSubject(ruleDefns, EmptyRuleSettingsProvider);

            var result = testSubject.Calculate().ToArray();

            CheckExpectedRuleKeys(result, "aaa");
            result[0].Configurations.Should().ContainInOrder(config1, config2);
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        public void Get_HasRuleOverrides_OverridesDefaultActivationLevel(
            bool onByDefault, bool onInRuleSettings)
        {
            // The value in the settings should always override the default
            var expectedRuleCount = onInRuleSettings ? 1 : 0;

            // Create definition and rule settings with a single rule
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule(ruleKey: "javascript:rule1", activeByDefault: onByDefault, eslintKey: "esKey");

            var settingsLevel = onInRuleSettings ? RuleLevel.On : RuleLevel.Off;
            var ruleSettings = new RuleSettingsBuilder()
                .Add("javascript:rule1", settingsLevel);

            var testSubject = CreateTestSubject(ruleDefns, ruleSettings);

            var result = testSubject.Calculate().ToArray();

            result.Length.Should().Be(expectedRuleCount);
        }

        [TestMethod]
        public void Get_HasRuleOverrides_NoRulesMatchTheRuleOverrides_RuleOverridesAreIgnored()
        {
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule(ruleKey: "javascript:activeRule1", activeByDefault: true, eslintKey: "active1");

            var ruleSettings = new RuleSettingsBuilder()
                .Add("wrongLanguage:activeRule1", RuleLevel.Off) // Correct rule key but wrong repo -> ignored
                .Add("javascript:unknownRule", RuleLevel.On); // Right repo, but rule key doesn't match a defined rule -> ignored

            var testSubject = CreateTestSubject(ruleDefns, ruleSettings);

            var result = testSubject.Calculate().ToArray();

            result.Length.Should().Be(1);
            result[0].Key.Should().Be("active1");
        }

        [TestMethod]
        public void Get_HasRuleOverrides_IrregularRules_RuleOverridesAreIgnored()
        {
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule(ruleKey: "javascript:hotspot1", activeByDefault: false, eslintKey: "hotspot1", ruleType: RuleType.SECURITY_HOTSPOT)
                .AddRule(ruleKey: "javascript:S2260", activeByDefault: false, eslintKey: null);

            var ruleSettings = new RuleSettingsBuilder()
                .Add("javascript:hotspot1", RuleLevel.On)
                .Add("javascript:S2260", RuleLevel.On);

            var testSubject = CreateTestSubject(ruleDefns, ruleSettings);

            var result = testSubject.Calculate().ToArray();

            result.Length.Should().Be(0);
        }

        [TestMethod]
        public void Get_HasStylelintKey_EslintKeyNull_ReturnsRule()
        {
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule(ruleKey: "javascript:S001", eslintKey: "eslintKey", stylelintKey: null)
                .AddRule(ruleKey: "javascript:S002", eslintKey: null, stylelintKey: null)
                .AddRule(ruleKey: "css:S001", eslintKey: null, stylelintKey: "stylelintKey");

            var testSubject = CreateTestSubject(ruleDefns, EmptyRuleSettingsProvider);

            var result = testSubject.Calculate().ToList();

            result.Should().HaveCount(2);
            result[0].Key.Should().Be("eslintKey");
            result[1].Key.Should().Be("stylelintKey");
        }

        [DataRow(true, 2)]
        [DataRow(false, 0)]
        [DataTestMethod]
        public void Get_RespectsHotspotAnalysisConfiguration(bool hotspotsEnabled, int expectedCount)
        {
            var hotspotConfigurationMock = new Mock<IConnectedModeFeaturesConfiguration>();
            hotspotConfigurationMock.Setup(x => x.IsHotspotsAnalysisEnabled()).Returns(hotspotsEnabled);
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule(ruleKey: "javascript:hotspot1", eslintKey: "hotspot1", ruleType: RuleType.SECURITY_HOTSPOT)
                .AddRule(ruleKey: "javascript:hotspot2", eslintKey: "hotspot2", ruleType: RuleType.SECURITY_HOTSPOT);

            var testSubject = CreateTestSubject(ruleDefns, EmptyRuleSettingsProvider, hotspotConfigurationMock.Object);

            var result = testSubject.Calculate().ToList();

            result.Should().HaveCount(expectedCount);
        }
        
        [TestMethod]
        public void Get_HotspotsEnabled_OverridesApplied()
        {
            var hotspotConfigurationMock = new Mock<IConnectedModeFeaturesConfiguration>();
            hotspotConfigurationMock.Setup(x => x.IsHotspotsAnalysisEnabled()).Returns(true);
            var ruleDefns = new RuleDefinitionsBuilder()
                .AddRule(ruleKey: "javascript:hotspot1", activeByDefault: false, eslintKey: "hotspot1",
                    ruleType: RuleType.SECURITY_HOTSPOT);

            var ruleSettings = new RuleSettingsBuilder()
                .Add("javascript:hotspot1", RuleLevel.On);

            var testSubject = CreateTestSubject(ruleDefns, ruleSettings, hotspotConfigurationMock.Object);

            var result = testSubject.Calculate().ToList();

            result.Should().ContainSingle();
        }

        private static void CheckExpectedRuleKeys(IEnumerable<Rule> result, params string[] expected) =>
            result.Select(x => x.Key).Should().BeEquivalentTo(expected);

        private static void CheckConfigurationsAreEmpty(IEnumerable<Rule> result) =>
            result.All(x => x.Configurations.Length == 0).Should().BeTrue();

        private static ActiveRulesCalculator CreateTestSubject(RuleDefinitionsBuilder ruleDefinitionsBuilder, IRuleSettingsProvider ruleSettingsProvider, IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration = null) =>
            new(ruleDefinitionsBuilder.GetDefinitions(), ruleSettingsProvider, connectedModeFeaturesConfiguration ?? new Mock<IConnectedModeFeaturesConfiguration>().Object);

        private class RuleDefinitionsBuilder
        {
            private readonly IList<RuleDefinition> definitions = new List<RuleDefinition>();

            public IEnumerable<RuleDefinition> GetDefinitions() => definitions;

            public RuleDefinitionsBuilder AddRule(string eslintKey = "any", bool activeByDefault = true, RuleType ruleType = RuleType.BUG,
                object[] configurations = null, string ruleKey = "any", string stylelintKey = null)
            {
                configurations ??= Array.Empty<object>();
                var newDefn = new RuleDefinition
                {
                    RuleKey = ruleKey,
                    EslintKey = eslintKey,
                    ActivatedByDefault = activeByDefault,
                    Type = ruleType,
                    DefaultParams = configurations,
                    StylelintKey = stylelintKey
                };

                definitions.Add(newDefn);
                return this;
            }
        }

        private class RuleSettingsBuilder : IRuleSettingsProvider
        {
            private readonly RulesSettings ruleSettings;

            public RuleSettingsBuilder()
            {
                ruleSettings = new RulesSettings();
            }

            public RuleSettingsBuilder Add(string ruleKey, RuleLevel ruleLevel)
            {
                ruleSettings.Rules.Add(ruleKey, new RuleConfig { Level = ruleLevel });
                return this;
            }

            public RulesSettings Get()
            {
                return ruleSettings;
            }
        }
    }
}
