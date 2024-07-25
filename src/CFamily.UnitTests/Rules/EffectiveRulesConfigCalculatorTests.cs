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

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.Rules.UnitTests
{
    [TestClass]
    public class EffectiveRulesConfigCalculatorTests
    {
        private TestLogger testLogger;
        private EffectiveRulesConfigCalculator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testLogger = new TestLogger();
            testSubject = new EffectiveRulesConfigCalculator(Mock.Of<IConnectedModeFeaturesConfiguration>(), testLogger);
        }

        [TestMethod]
        public void GetConfig_NullArguments_Throws()
        {
            var defaultRulesConfig = CreateWellKnownRulesConfig("language1");
            var customSettings = new RulesSettings();

            // 1. Language
            Action act = () => testSubject.GetEffectiveRulesConfig(null, defaultRulesConfig, customSettings);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("languageKey");

            // 2. Default rules config
            act = () => testSubject.GetEffectiveRulesConfig("x", null, customSettings);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("defaultRulesConfig");

            // 3. Custom settings
            act = () => testSubject.GetEffectiveRulesConfig("x", defaultRulesConfig, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("customSettings");
        }

        [TestMethod]
        public void GetConfig_NoCustomSettings_DefaultsReturned()
        {
            // Arrange
            var defaultRulesConfig = CreateWellKnownRulesConfig("language1");
            var sourcesSettings = new RulesSettings();

            // Act
            var result = testSubject.GetEffectiveRulesConfig("language1", defaultRulesConfig, sourcesSettings);

            // Assert
            result.LanguageKey.Should().Be("language1");
            result.AllPartialRuleKeys.Should().BeEquivalentTo(defaultRulesConfig.AllPartialRuleKeys);

            testLogger.AssertOutputStringExists(Resources.NoCustomRulesSettings);
        }

        [DataRow(true, 2)]
        [DataRow(false, 1)]
        [DataTestMethod]
        public void GetConfig_NoCustomSettings_RespectsHotspotConfig(bool analyzeHotspots, int expectedActiveCount)
        {
            // Arrange
            var defaultRulesConfig = new DummyCFamilyRulesConfig("cpp")
                .AddRule(RulesConfigFixup.HotspotRulesKeys[0].Split(':')[1], true)
                .AddRule("12345678", true);
            var sourcesSettings = new RulesSettings();
            var hotspotAnalysisConfigurationMock = new Mock<IConnectedModeFeaturesConfiguration>();
            hotspotAnalysisConfigurationMock.Setup(x => x.IsHotspotsAnalysisEnabled()).Returns(analyzeHotspots);

            var testSubject1 =
                new EffectiveRulesConfigCalculator(hotspotAnalysisConfigurationMock.Object, testLogger);

            // Act
            var result = testSubject1.GetEffectiveRulesConfig("cpp", defaultRulesConfig, sourcesSettings);

            // Assert
            result.LanguageKey.Should().Be("cpp");
            result.ActivePartialRuleKeys.Should().HaveCount(expectedActiveCount);

            testLogger.AssertOutputStringExists(Resources.NoCustomRulesSettings);
        }

        [TestMethod]
        public void GetConfig_RulesInCustomSettings_MergedConfigReturned()
        {
            // Arrange
            var defaultRulesConfig = CreateWellKnownRulesConfig("key");
            var sourcesSettings = new RulesSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    // Turn an active rule off...
                    { "key:" + WellKnownPartialRuleKey1_Active, new RuleConfig { Level = RuleLevel.Off } },
                    // ... and an inactive rule on
                    { "key:" + WellKnownPartialRuleKey3_Inactive, new RuleConfig { Level = RuleLevel.On } }
                }
            };

            var result = testSubject.GetEffectiveRulesConfig("key", defaultRulesConfig, sourcesSettings);

            result.LanguageKey.Should().Be("key");
            result.AllPartialRuleKeys.Should().BeEquivalentTo(defaultRulesConfig.AllPartialRuleKeys);
            result.ActivePartialRuleKeys.Should().BeEquivalentTo(WellKnownPartialRuleKey2_Active, WellKnownPartialRuleKey3_Inactive);
        }

        [TestMethod]
        public void GetConfig_CachedResultsReturnedIfAvailable()
        {
            // Arrange
            var defaultRulesConfig = CreateWellKnownRulesConfig("key");
            var sourcesSettings = new RulesSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    { "rule1", new RuleConfig() }
                }
            };

            // 1. First call -> new config returned
            var result1 = testSubject.GetEffectiveRulesConfig("language1", defaultRulesConfig, sourcesSettings);

            result1.Should().NotBeNull();
            result1.Should().NotBeSameAs(defaultRulesConfig);
            testLogger.AssertOutputStringExists(Resources.EffectiveRules_CacheMiss);

            // 2. Second call with same settings -> cache hit
            testLogger.Reset();
            var result2 = testSubject.GetEffectiveRulesConfig("language1", defaultRulesConfig, sourcesSettings);

            result2.Should().BeSameAs(result1);
            testLogger.AssertOutputStringExists(Resources.EffectiveRules_CacheHit);

            // 3. Call with different key -> cache miss
            testLogger.Reset();
            var result3 = testSubject.GetEffectiveRulesConfig("another language", defaultRulesConfig, sourcesSettings);

            result3.Should().NotBeSameAs(result2);
            testLogger.AssertOutputStringExists(Resources.EffectiveRules_CacheMiss);
        }

        internal const string WellKnownPartialRuleKey1_Active = "rule1";
        internal const string WellKnownPartialRuleKey2_Active = "rule2";
        internal const string WellKnownPartialRuleKey3_Inactive = "rule3";

        private static ICFamilyRulesConfig CreateWellKnownRulesConfig(string languageKey)
        {
            var defaultRulesConfig = new DummyCFamilyRulesConfig(languageKey)
                .AddRule(WellKnownPartialRuleKey1_Active, IssueSeverity.Blocker, isActive: true, parameters: null, code: null)
                .AddRule(WellKnownPartialRuleKey2_Active, IssueSeverity.Major, isActive: true, parameters: null, code: null)
                .AddRule(WellKnownPartialRuleKey3_Inactive, IssueSeverity.Minor, isActive: false, parameters: null, code: null);

            return defaultRulesConfig;
        }
    }
}
