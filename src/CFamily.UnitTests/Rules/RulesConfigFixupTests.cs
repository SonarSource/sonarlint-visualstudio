/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.CFamily.Rules;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.CFamily.UnitTests.Rules
{
    [TestClass]
    public class RulesConfigFixupTests
    {
        [TestMethod]
        public void Apply_EmptySettings_NoError()
        {
            var emptySettings = new RulesSettings();
            var testSubject = CreateTestSubject();

            var result = testSubject.Apply(emptySettings);

            result.Rules.Should().BeEmpty();
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

            var emptySettings = new RulesSettings
            {
                Rules =
                {
                    { "any", config1 },         // arbitrary rule key
                    { inputRuleKey, config2},
                    { "cpp:S123", config3 }     // non-legacy
                }
            };

            var testSubject = CreateTestSubject();

            var result = testSubject.Apply(emptySettings);

            result.Rules.Count.Should().Be(3);
            result.Rules["any"].Should().BeSameAs(config1);
            result.Rules["cpp:S123"].Should().BeSameAs(config3);

            result.Rules.TryGetValue(expectedRuleKey, out var outputConfig).Should().BeTrue();
            outputConfig.Should().BeSameAs(config2);
        }

        [TestMethod]
        [DataRow("c:C99CommentUsage", "c:S787")]
        [DataRow("cpp:PPBadIncludeForm", "cpp:S956")]
        public void Apply_BothLegacyAndNewRuleKey_LegacyKeyIsRemoved(string legacyKey, string newKey)
        {
            var legacyKeyConfig = new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Major };
            var newKeyConfig = new RuleConfig { Level = RuleLevel.Off, Severity = IssueSeverity.Minor };

            var emptySettings = new RulesSettings
            {
                Rules =
                {
                    { legacyKey, legacyKeyConfig },
                    { newKey, newKeyConfig }
                }
            };

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(logger);

            var result = testSubject.Apply(emptySettings);

            result.Rules[newKey].Should().BeSameAs(newKeyConfig);
            result.Rules.TryGetValue(legacyKey, out var _).Should().BeFalse();
            result.Rules.Count.Should().Be(1);

            logger.AssertPartialOutputStringExists(legacyKey, newKey);
        }

        [TestMethod]
        public void Apply_NoLegacyKeys_NothingLogged()
        {
            var config1 = new RuleConfig { Level = RuleLevel.On, Severity = IssueSeverity.Major };

            var emptySettings = new RulesSettings
            {
                Rules =
                {
                    { "any non-legacy rule key", config1 }
                }
            };
            var logger = new TestLogger();
            var testSubject = CreateTestSubject(logger);

            var result = testSubject.Apply(emptySettings);

            result.Rules.Count.Should().Be(1);
            logger.AssertNoOutputMessages();
        }

        private static RulesConfigFixup CreateTestSubject(ILogger logger = null)
            => new RulesConfigFixup(logger ?? new TestLogger());
    }
}
