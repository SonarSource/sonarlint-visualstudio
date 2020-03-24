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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class EffectiveRulesConfigCalculatorTests
    {
        [TestMethod]
        [DataRow(true /* custom settings are null */)]
        [DataRow(false /* custom settings are not null, but emtpy */)]
        public void GetConfig_NullCustomSettings_DefaultConfigReturned(bool sourceCustomSettingsAreNull)
        {
            // Arrange
            var testLogger = new TestLogger();
            var testSubject = new EffectiveRulesConfigCalculator(testLogger);

            var defaultRulesConfig = CreateMockConfig("language1");
            var sourcesSettings = sourceCustomSettingsAreNull ? null : new RulesSettings();

            // Act
            var result = testSubject.GetEffectiveRulesConfig("language1", defaultRulesConfig, sourcesSettings);

            // Assert - optimisation - expecting the same object instance
            result.Should().BeSameAs(defaultRulesConfig);
            testLogger.AssertOutputStringExists(CoreStrings.EffectiveRules_NoCustomRulesSettings);
        }

        [TestMethod]
        public void GetConfig_RulesInCustomSettings_MergedConfigReturned()
        {
            // Arrange
            var testLogger = new TestLogger();
            var testSubject = new EffectiveRulesConfigCalculator(testLogger);

            var defaultRulesConfig = CreateMockConfig("key");
            var sourcesSettings = new RulesSettings
            {
                Rules = new System.Collections.Generic.Dictionary<string, RuleConfig>
                {
                    { "rule1", new RuleConfig() }
                }
            };

            // 1. First call -> new config returned
            var result1 = testSubject.GetEffectiveRulesConfig("language1", defaultRulesConfig, sourcesSettings);

            result1.Should().NotBeSameAs(defaultRulesConfig);
            testLogger.AssertOutputStringExists(CoreStrings.EffectiveRules_CacheMiss);

            // 2. Second call with same settings -> cache hit
            testLogger.Reset();
            var result2 = testSubject.GetEffectiveRulesConfig("language1", defaultRulesConfig, sourcesSettings);

            result2.Should().BeSameAs(result1);
            testLogger.AssertOutputStringExists(CoreStrings.EffectiveRules_CacheHit);

            // 3. Call with different key -> cache miss
            testLogger.Reset();
            var result3 = testSubject.GetEffectiveRulesConfig("another language", defaultRulesConfig, sourcesSettings);

            result3.Should().NotBeSameAs(result2);
            testLogger.AssertOutputStringExists(CoreStrings.EffectiveRules_CacheMiss);
        }

        #region Cache tests

        [TestMethod]
        public void Cache_DifferentSourceConfig_NotFound_AndEntryCleared()
        {
            var sourceConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var sourceSettings1 = new RulesSettings();
            var effectiveConfig1 = new Mock<ICFamilyRulesConfig>().Object;

            var testSubject = new EffectiveRulesConfigCalculator.RulesConfigCache();

            testSubject.Add("key1", sourceConfig1, sourceSettings1, effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 1. Search for added item -> found
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeSameAs(effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 2. Different source config -> not found
            testSubject.FindConfig("key1", new Mock<ICFamilyRulesConfig>().Object, sourceSettings1).Should().BeNull();
            testSubject.CacheCount.Should().Be(0);
        }

        [TestMethod]
        public void Cache_DifferentSourceSettings_NotFound_AndEntryCleared()
        {
            var sourceConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var sourceSettings1 = new RulesSettings();
            var effectiveConfig1 = new Mock<ICFamilyRulesConfig>().Object;

            var testSubject = new EffectiveRulesConfigCalculator.RulesConfigCache();

            testSubject.Add("key1", sourceConfig1, sourceSettings1, effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 1. Search for added item -> found
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeSameAs(effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 2. Different source settings -> not found
            testSubject.FindConfig("key1", sourceConfig1, new RulesSettings()).Should().BeNull();
            testSubject.CacheCount.Should().Be(0);
        }

        [TestMethod]
        public void Cache_MultipleEntries()
        {
            var sourceConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var sourceConfig2 = new Mock<ICFamilyRulesConfig>().Object;

            var sourceSettings1 = new RulesSettings();
            var sourceSettings2 = new RulesSettings();

            var effectiveConfig1 = new Mock<ICFamilyRulesConfig>().Object;
            var effectiveConfig2 = new Mock<ICFamilyRulesConfig>().Object;

            var testSubject = new EffectiveRulesConfigCalculator.RulesConfigCache();

            // 1. Empty cache -> cache miss
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeNull();

            // 2. Add first entry to cache
            testSubject.Add("key1", sourceConfig1, sourceSettings1, effectiveConfig1);
            testSubject.CacheCount.Should().Be(1);

            // 3. Find second language - not found
            testSubject.FindConfig("key2", sourceConfig2, sourceSettings2).Should().BeNull();

            // 4. Add second entry to cache
            testSubject.Add("key2", sourceConfig2, sourceSettings2, effectiveConfig2);
            testSubject.CacheCount.Should().Be(2);

            // 5. Check can find both entries
            testSubject.FindConfig("key1", sourceConfig1, sourceSettings1).Should().BeSameAs(effectiveConfig1);
            testSubject.FindConfig("key2", sourceConfig2, sourceSettings2).Should().BeSameAs(effectiveConfig2);
        }

        #endregion Cache tests

        private static ICFamilyRulesConfig CreateMockConfig(string languageKey)
        {
            var defaultRulesConfigMock = new Mock<ICFamilyRulesConfig>();
            defaultRulesConfigMock.Setup(x => x.LanguageKey).Returns(languageKey);
            return defaultRulesConfigMock.Object;
        }
    }
}
