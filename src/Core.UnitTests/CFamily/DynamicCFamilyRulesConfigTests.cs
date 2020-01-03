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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Integration.UnitTests.CFamily;

namespace SonarLint.VisualStudio.Core.UnitTests.CFamily
{
    [TestClass]
    public class DynamicCFamilyRulesConfigTests
    {
        [TestMethod]
        public void Ctor_NullArguments()
        {
            var userSettings = new UserSettings();

            Action act = () => new DynamicCFamilyRulesConfig(null, userSettings);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("defaultRulesConfig");

            // Null settings should be ok
            var testSubject = new DynamicCFamilyRulesConfig(new DummyCFamilyRulesConfig("anyLanguage"), null);
            testSubject.AllPartialRuleKeys.Should().NotBeNull();
            testSubject.ActivePartialRuleKeys.Should().NotBeNull();
            testSubject.RulesMetadata.Should().NotBeNull();
            testSubject.RulesParameters.Should().NotBeNull();
            testSubject.LanguageKey.Should().Be("anyLanguage");
        }

        [TestMethod]
        [DataRow(false, DisplayName = "UserSettings are null")]
        [DataRow(true, DisplayName = "UserSettings are not null but empty")]
        public void NullOrEmptyUserSettings_DefaultsUsed(bool areUserSettingsNull)
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("123")
                .AddRule("rule1", IssueSeverity.Blocker, isActive: true,
                    parameters: new Dictionary<string, string> { { "p1", "v1" } })
                .AddRule("rule2", IssueSeverity.Major, isActive: true,
                    parameters: new Dictionary<string, string> { { "p2", "v2" } })
                .AddRule("rule3", IssueSeverity.Minor, isActive: false,
                    parameters: new Dictionary<string, string> { { "p3", "v3" } });

            var userSettings = areUserSettingsNull ? null : new UserSettings();

            // Act
            var testSubject = new DynamicCFamilyRulesConfig(defaultConfig, userSettings);

            // Assert
            testSubject.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2", "rule3");
            testSubject.ActivePartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2");

            testSubject.LanguageKey.Should().Be("123");

            // Other properties should be pass-throughs
            testSubject.AllPartialRuleKeys.Should().BeEquivalentTo(defaultConfig.AllPartialRuleKeys);
            testSubject.RulesParameters.Should().BeEquivalentTo(defaultConfig.RulesParameters);
            testSubject.RulesMetadata.Should().BeEquivalentTo(defaultConfig.RulesMetadata);
        }

        [TestMethod]
        public void ActiveRules_UserSettingsOverrideDefaults()
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("rule1", isActive: false)
                .AddRule("rule2", isActive: true)
                .AddRule("rule3", isActive: true);

            var userSettings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    // Unknown rules should be ignored
                    { "x:unknown1", new RuleConfig { Level = RuleLevel.On } },

                    // Turn on a rule that was off (case-insensitive comparison on keys)
                    { "c:rule1", new RuleConfig { Level = RuleLevel.On } },

                    // Turn off a rule that was on
                    { "c:rule2", new RuleConfig { Level = RuleLevel.Off} },

                    // Rule key comparison is case-sensitive
                    { "c:RULE3", new RuleConfig { Level = RuleLevel.Off} },

                    // Settings for other languages should be ignored
                    { "cpp:rule3", new RuleConfig { Level = RuleLevel.Off } }
                }
            };

            // Act
            var testSubject = new DynamicCFamilyRulesConfig(defaultConfig, userSettings);

            // Assert
            testSubject.ActivePartialRuleKeys.Should().BeEquivalentTo("rule1", "rule3");
        }

        [TestMethod]
        public void EffectiveSeverity_UserSettingsOverrideDefaults()
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("rule1", IssueSeverity.Major, isActive: false)
                .AddRule("rule2", IssueSeverity.Minor, isActive: true)
                .AddRule("rule3", IssueSeverity.Info, isActive: true);

            var userSettings = new UserSettings();

            // Rule 1 - severity not specified -> should use default
            userSettings.Rules["c:rule1"] = new RuleConfig();

            // Rule 2 - should override default severity
            // Rule key comparison should be case-sensitive
            userSettings.Rules["c:RULE2"] = new RuleConfig { Severity = IssueSeverity.Blocker };

            // Rule 3 for a different language -> should be ignored and the default config used
            userSettings.Rules["cpp:rule3"] = new RuleConfig { Severity = IssueSeverity.Critical };

            // rule in user settings that isn't in the default config should be ignored
            userSettings.Rules["c:missingRule"] = new RuleConfig { Severity = IssueSeverity.Critical };

            // Act
            var dynamicConfig = new DynamicCFamilyRulesConfig(defaultConfig, userSettings);

            // Assert
            dynamicConfig.RulesMetadata.Count.Should().Be(3);
            dynamicConfig.RulesMetadata["rule1"].DefaultSeverity.Should().Be(IssueSeverity.Major);
            dynamicConfig.RulesMetadata["rule2"].DefaultSeverity.Should().Be(IssueSeverity.Blocker);
            dynamicConfig.RulesMetadata["rule3"].DefaultSeverity.Should().Be(IssueSeverity.Info);
        }

        [TestMethod]
        public void Parameters_UserSettingsOverrideDefaults()
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("rule1", isActive: false,
                    parameters: new Dictionary<string, string>
                    {
                        { "r1 param1", "r1p1 default" },
                        { "r1 param2", "r1p2 default" }
                    })

                .AddRule("rule2", isActive: true,
                    parameters: new Dictionary<string, string>
                    {
                        { "r2 param1", "r2p1 default" },
                        { "r2 param2", "r2p2 default" }
                    })
                .AddRule("rule3", isActive: true,
                    parameters: new Dictionary<string, string>
                    {
                        { "r3 param1", "r3p1 default" },
                        { "r3 param2", "r3p2 default" }
                    }
                );

            var userSettings = new UserSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    // Rule 1 - no user params -> same as default

                    // Rule 2 - all default params overridden
                    { "c:rule2", new RuleConfig
                        {
                            Parameters = new Dictionary<string, string>
                            {
                                { "r2 param1", "r2p1 user"},
                                { "r2 param2", "r2p2 user"}
                            }
                        }
                    },

                    // Rule 3 - params merged, with user taking priority
                    { "c:rule3", new RuleConfig
                        {
                            Parameters = new Dictionary<string, string>
                            {
                                { "r3 param1", "r3p1 user"},
                                { "r3 param3", "r3p3 user"}
                            }
                        }
                    }
                }
            };

            // Act
            var dynamicConfig = new DynamicCFamilyRulesConfig(defaultConfig, userSettings);

            // Assert
            dynamicConfig.RulesParameters.Count.Should().Be(3);
            dynamicConfig.RulesParameters["rule1"]["r1 param1"].Should().Be("r1p1 default");
            dynamicConfig.RulesParameters["rule1"]["r1 param2"].Should().Be("r1p2 default");
            dynamicConfig.RulesParameters["rule1"].Count.Should().Be(2);

            dynamicConfig.RulesParameters["rule2"]["r2 param1"].Should().Be("r2p1 user");
            dynamicConfig.RulesParameters["rule2"]["r2 param2"].Should().Be("r2p2 user");
            dynamicConfig.RulesParameters["rule2"].Count.Should().Be(2);

            dynamicConfig.RulesParameters["rule3"]["r3 param1"].Should().Be("r3p1 user");
            dynamicConfig.RulesParameters["rule3"]["r3 param2"].Should().Be("r3p2 default");
            dynamicConfig.RulesParameters["rule3"]["r3 param3"].Should().Be("r3p3 user");
            dynamicConfig.RulesParameters["rule3"].Count.Should().Be(3);
        }

        #region Static method tests

        [TestMethod]
        public void EffectiveParameters_NullHandling()
        {
            var nonEmptyParams = new Dictionary<string, string> { { "p1", "v1" } };

            // 1. Both null -> null returned
            var actual = DynamicCFamilyRulesConfig.GetEffectiveParameters(null, null);
            actual.Should().BeNull();

            // 2. Null default params -> user params returned (not expected in practice, but we don't want to fail if it does)
            actual = DynamicCFamilyRulesConfig.GetEffectiveParameters(nonEmptyParams, null);
            actual.Should().BeEquivalentTo(nonEmptyParams);

            // 3. Null user params -> default params returned
            actual = DynamicCFamilyRulesConfig.GetEffectiveParameters(null, nonEmptyParams);
            actual.Should().BeEquivalentTo(nonEmptyParams);
        }

        [TestMethod]
        public void EffectiveParameters_UserSettingsOverrideDefaults()
        {
            // Arrange
            var defaultParams = new Dictionary<string, string>
            {
                { "param1", "param 1 default" },
                { "param2", "param 2 default" },
                { "param3", "param 3 default" }, // expected
                { "param3a", "param 3a default" },
                { "param4", "param 4 default" }, // expected
            };

            var userParams = new Dictionary<string, string>
            {
                { "param1", "param 1 user" },     // expected
                { "PARAM2", "param 2 user" },     // expected
                { "param3a", "param 3a user" },   // expected - not an exact match for param3, should override param3a
                //  NOTE: params not in the set of default parameters will be included i.e. any arbitrary params set by the user will be included
                { "NonDefaultParam", "non-default param value" }, // expected
            };

            // Act
            var effectiveParams = DynamicCFamilyRulesConfig.GetEffectiveParameters(defaultParams, userParams);

            // Assert
            effectiveParams.Keys.Should().BeEquivalentTo("param1", "param2", "param3", "param3a", "param4", "NonDefaultParam");

            effectiveParams["param1"].Should().Be("param 1 user");
            effectiveParams["param2"].Should().Be("param 2 user");
            effectiveParams["param3"].Should().Be("param 3 default");
            effectiveParams["param3a"].Should().Be("param 3a user");
            effectiveParams["param4"].Should().Be("param 4 default");
            effectiveParams["NonDefaultParam"].Should().Be("non-default param value");
        }

        #endregion Static method tests
    }
}
