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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.Core.UserRuleSettings;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.Rules.UnitTests
{
    [TestClass]
    public class RulesConfigFixupTests
    {
        [TestMethod]
        public void Sanity_NoOverlapBetweenExludedAndLegacyRuleKeys()
        {
            // The logic in the fixup class assumes that there legacy rules keys and 
            // excluded rules are disjoint sets. Check this is actually the case.
            RulesConfigFixup.fullLegacyToNewKeyMap.Keys.Intersect(RulesConfigFixup.ExcludedRulesKeys)
                .Should().BeEmpty();

            RulesConfigFixup.fullLegacyToNewKeyMap.Values.Intersect(RulesConfigFixup.ExcludedRulesKeys)
                .Should().BeEmpty();
        }

        [TestMethod]
        [DataRow("c:C99CommentUsage", "c:S787")]
        [DataRow("cpp:C99CommentUsage", "cpp:S787")]
        [DataRow("c:PPBadIncludeForm", "c:S956")]
        [DataRow("cpp:PPBadIncludeForm", "cpp:S956")]
        [DataRow("CPP:PPBADINCLUDEFORM", "CPP:PPBADINCLUDEFORM")] // replacement is case-sensitive
        public void Apply_LegacyRuleKeys_KeysAreTranslated(string inputRuleKey, string expectedRuleKey)
        {
            var config1 = new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Major };
            var config2 = new RuleConfig { Level = RuleLevel.Off, Severity = IssueSeverity.Minor };
            var config3 = new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Critical };

            var originalSettings = new RulesSettings
            {
                Rules =
                {
                    { "any", config1 },         // arbitrary rule key
                    { inputRuleKey, config2},
                    { "cpp:S123", config3 }     // non-legacy
                }
            };

            var logger = new TestLogger();
            var testSubject = CreateTestSubject(logger);

            var result = testSubject.Apply(originalSettings, CreateHotspotAnalysisConfig());

            result.Rules["any"].Should().BeSameAs(config1);
            result.Rules["cpp:S123"].Should().BeSameAs(config3);

            result.Rules.TryGetValue(expectedRuleKey, out var outputConfig).Should().BeTrue();
            outputConfig.Should().BeSameAs(config2);

            // Not expecting any messages for the non-legacy rule keys
            logger.AssertPartialOutputStringDoesNotExist("any");
            logger.AssertPartialOutputStringDoesNotExist("cpp:S123");

            CheckInstanceIsDifferent(originalSettings, result);
        }

        [TestMethod]
        [DataRow("c:C99CommentUsage", "c:S787")]
        [DataRow("cpp:PPBadIncludeForm", "cpp:S956")]
        public void Apply_BothLegacyAndNewRuleKey_LegacyKeyIsRemoved(string legacyKey, string newKey)
        {
            var legacyKeyConfig = new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Major };
            var newKeyConfig = new RuleConfig { Level = RuleLevel.Off, Severity = IssueSeverity.Minor };

            var originalSettings = new RulesSettings
            {
                Rules =
                {
                    { legacyKey, legacyKeyConfig },
                    { newKey, newKeyConfig }
                }
            };

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(logger);

            var result = testSubject.Apply(originalSettings, CreateHotspotAnalysisConfig());

            result.Rules[newKey].Should().BeSameAs(newKeyConfig);
            result.Rules.TryGetValue(legacyKey, out var _).Should().BeFalse();

            logger.AssertPartialOutputStringExists(legacyKey, newKey);
            CheckInstanceIsDifferent(originalSettings, result);
        }
        
        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void Apply_NoCustomRules_ExcludedRulesAreDisabled(bool hotspotsEnabled)
        {
            var logger = new TestLogger();
            var emptySettings = new RulesSettings();
            var testSubject = CreateTestSubject(logger);

            var result = testSubject.Apply(emptySettings, CreateHotspotAnalysisConfig(hotspotsEnabled));

            CheckInstanceIsDifferent(emptySettings, result);
            emptySettings.Rules.Count.Should().Be(0); // original settings should not have changed

            var excludedKeys = RulesConfigFixup.ExcludedRulesKeys;
            
            if (!hotspotsEnabled)
            {
                excludedKeys = excludedKeys.Concat(RulesConfigFixup.HotspotRulesKeys).ToArray();
            }
            
            result.Rules.Keys.Should().BeEquivalentTo(excludedKeys);
            result.Rules.Values.Select(x => x.Level)
                .All(x => x == RuleLevel.Off)
                .Should().BeTrue();

            foreach(string excludedKey in excludedKeys)
            {
                logger.AssertPartialOutputStringExists(excludedKey);
            }
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void Apply_CustomRulesAndExcludedRulesExist_CustomRulesAreUnchangedExcludedRulesAreDisabled(bool hotspotsEnabled)
        {
            // Arrange
            var logger = new TestLogger();

            string excludedKey1 = RulesConfigFixup.ExcludedRulesKeys[0];
            string excludedKey2 = RulesConfigFixup.ExcludedRulesKeys[1];
            string excludedKey3 = RulesConfigFixup.ExcludedRulesKeys[2];

            var custom = new RulesSettings()
                .AddRule("xxx", RuleLevel.On)
                .AddRule(excludedKey1, RuleLevel.On)
                .AddRule("yyy", RuleLevel.Off)
                .AddRule(excludedKey2, RuleLevel.Off)
                .AddRule(excludedKey3, RuleLevel.On);

            var testSubject = CreateTestSubject(logger);

            // Act
            var result = testSubject.Apply(custom, CreateHotspotAnalysisConfig(hotspotsEnabled));

            // Assert
            CheckInstanceIsDifferent(custom, result);
            custom.Rules.Count.Should().Be(5);

            IEnumerable<string> expectedKeys = RulesConfigFixup.ExcludedRulesKeys;
            
            if (!hotspotsEnabled)
            {
                expectedKeys = expectedKeys.Concat(RulesConfigFixup.HotspotRulesKeys);   
            }
            
            expectedKeys = expectedKeys.Union(new string[]  { "xxx", "yyy"});

            result.Rules.Keys.Should().BeEquivalentTo(expectedKeys);

            // Non-excluded rules should be unchanged
            result.Rules["xxx"].Level.Should().Be(RuleLevel.On);
            result.Rules["yyy"].Level.Should().Be(RuleLevel.Off);

            // All excluded rules that were in the custom settings should be "Off"
            result.Rules[excludedKey1].Level.Should().Be(RuleLevel.Off);
            result.Rules[excludedKey2].Level.Should().Be(RuleLevel.Off);
            result.Rules[excludedKey3].Level.Should().Be(RuleLevel.Off);
        }

        private static IConnectedModeFeaturesConfiguration CreateHotspotAnalysisConfig(bool isEnabled = false)
        {
            var mock = new Mock<IConnectedModeFeaturesConfiguration>();
            mock.Setup(x => x.IsHotspotsAnalysisEnabled()).Returns(isEnabled);
            return mock.Object;
        }

        private static RulesConfigFixup CreateTestSubject(ILogger logger = null)
            => new RulesConfigFixup(logger ?? new TestLogger());

        // Checks that the changes have been made to a copy of the settings,
        // not the original settings.
        private static void CheckInstanceIsDifferent(RulesSettings original, RulesSettings modified) =>
            modified.Should().NotBeSameAs(original);
    }
}
