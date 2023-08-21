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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.Rules.UnitTests
{
    [TestClass]
    public class DynamicCFamilyRulesConfigTests
    {
        [TestMethod]
        public void Ctor_NullArguments_Throws()
        {
            var settings = new RulesSettings();

            // 1. Default rules config
            Action act = () => new DynamicCFamilyRulesConfig(null,
                settings,
                Mock.Of<IConnectedModeFeaturesConfiguration>(),
                new TestLogger());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("defaultRulesConfig");

            // 2. Custom settings
            act = () => new DynamicCFamilyRulesConfig(new DummyCFamilyRulesConfig("anyLanguage"),
                null,
                Mock.Of<IConnectedModeFeaturesConfiguration>(),
                new TestLogger());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("customRulesSettings");

            // 3. Logger
            act = () => new DynamicCFamilyRulesConfig(new DummyCFamilyRulesConfig("anyLanguage"),
                settings,
                Mock.Of<IConnectedModeFeaturesConfiguration>(),
                null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
            
            // 4. Hotspot config
            act = () => new DynamicCFamilyRulesConfig(new DummyCFamilyRulesConfig("anyLanguage"),
                settings,
                null,
                new TestLogger());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("connectedModeFeaturesConfiguration");
        }

        [TestMethod]
        public void NullOrEmptyRulesSettings_DefaultsUsed()
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("123")
                .AddRule("rule1", IssueSeverity.Blocker, isActive: true,
                    parameters: new Dictionary<string, string> { { "p1", "v1" } })
                .AddRule("rule2", IssueSeverity.Major, isActive: true,
                    parameters: new Dictionary<string, string> { { "p2", "v2" } })
                .AddRule("rule3", IssueSeverity.Minor, isActive: false,
                    parameters: new Dictionary<string, string> { { "p3", "v3" } });

            var settings = new RulesSettings();

            // Act
            using (new AssertIgnoreScope())
            {
                var testSubject = CreateTestSubject(defaultConfig, settings);

                // Assert
                testSubject.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2", "rule3");
                testSubject.ActivePartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2");

                testSubject.LanguageKey.Should().Be("123");

                // Other properties should be pass-throughs
                testSubject.AllPartialRuleKeys.Should().BeEquivalentTo(defaultConfig.AllPartialRuleKeys);
                testSubject.RulesParameters.Should().BeEquivalentTo(defaultConfig.RulesParameters);
                testSubject.RulesMetadata.Should().BeEquivalentTo(defaultConfig.RulesMetadata);
            }
        }

        [TestMethod]
        public void RuleConfigFixup_IsCalled()
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("123")
                .AddRule("rule1", isActive: true, null)
                .AddRule("rule2", isActive: true, null);

            var inputSettings = new RulesSettings();

            var hotspotAnalysisConfigurationMock = Mock.Of<IConnectedModeFeaturesConfiguration>();

            // Fixup that should disable rule1
            var fixedUpSettings = new RulesSettings
            {
                Rules = { { "123:rule1", new RuleConfig { Level = RuleLevel.Off } } }
            };
            var fixup = new Mock<IRulesConfigFixup>();
            fixup
                .Setup(x => x.Apply(inputSettings, hotspotAnalysisConfigurationMock))
                .Returns(fixedUpSettings);

            // Act
            var testSubject = CreateTestSubject(defaultConfig,
                inputSettings,
                fixup.Object,
                hotspotAnalysisConfigurationMock);

            // Assert
            fixup.VerifyAll();

            testSubject.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2");
            testSubject.ActivePartialRuleKeys.Should().BeEquivalentTo("rule2");
        }

        [TestMethod]
        public void ActiveRules_CustomSettingsOverrideDefaults()
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("rule1", isActive: false)
                .AddRule("rule2", isActive: true)
                .AddRule("rule3", isActive: true);

            var settings = new RulesSettings
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
            var testSubject = CreateTestSubject(defaultConfig, settings);

            // Assert
            testSubject.ActivePartialRuleKeys.Should().BeEquivalentTo("rule1", "rule3");
        }

        [TestMethod]
        public void EffectiveSeverity_CustomSettingsOverrideDefaults()
        {
            // Arrange
            var defaultConfig = new DummyCFamilyRulesConfig("c")
                .AddRule("rule1", IssueSeverity.Major, isActive: false)
                .AddRule("rule2", IssueSeverity.Minor, isActive: true)
                .AddRule("rule3", IssueSeverity.Info, isActive: true);

            var settings = new RulesSettings();

            // Rule 1 - severity not specified -> should use default
            settings.Rules["c:rule1"] = new RuleConfig();

            // Rule 2 - should override default severity
            // Rule key comparison should be case-insensitive
            settings.Rules["c:RULE2"] = new RuleConfig { Severity = IssueSeverity.Blocker };

            // Rule 3 for a different language -> should be ignored and the default config used
            settings.Rules["cpp:rule3"] = new RuleConfig { Severity = IssueSeverity.Critical };

            // rule in user settings that isn't in the default config should be ignored
            settings.Rules["c:missingRule"] = new RuleConfig { Severity = IssueSeverity.Critical };

            // Act
            var dynamicConfig = CreateTestSubject(defaultConfig, settings);

            // Assert
            dynamicConfig.RulesMetadata.Count.Should().Be(3);
            dynamicConfig.RulesMetadata["rule1"].DefaultSeverity.Should().Be(IssueSeverity.Major);
            dynamicConfig.RulesMetadata["rule2"].DefaultSeverity.Should().Be(IssueSeverity.Blocker);
            dynamicConfig.RulesMetadata["rule3"].DefaultSeverity.Should().Be(IssueSeverity.Info);
        }

        [TestMethod]
        public void Parameters_CustomSettingsOverrideDefaults()
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

            var settings = new RulesSettings
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
            var dynamicConfig = CreateTestSubject(defaultConfig, settings);
            

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
        public void EffectiveParameters_CustomSettingsOverrideDefaults()
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

        private static DynamicCFamilyRulesConfig CreateTestSubject(ICFamilyRulesConfig defaultConfig,
            RulesSettings customSettings,
            IRulesConfigFixup fixup = null,
            IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration = null)
        {
            fixup ??= new NoOpRulesConfigFixup();
            return new DynamicCFamilyRulesConfig(defaultConfig, customSettings,
                connectedModeFeaturesConfiguration ?? Mock.Of<IConnectedModeFeaturesConfiguration>(), new TestLogger(), fixup);
        }

        private class NoOpRulesConfigFixup : IRulesConfigFixup
        {
            public RulesSettings Apply(RulesSettings input,
                IConnectedModeFeaturesConfiguration connectedModeFeaturesConfiguration) => input;
        }

    }

    internal static class RulesSettingsExtensions
    {
        public static RulesSettings AddRule(this RulesSettings settings, string ruleKey, RuleLevel level)
        {
            settings.Rules.Add(ruleKey, new RuleConfig { Level = level });
            return settings;
        }
    }
}
