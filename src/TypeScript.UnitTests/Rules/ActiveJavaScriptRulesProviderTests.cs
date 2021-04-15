﻿/*
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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.EslintBridgeClient.Contract;
using SonarLint.VisualStudio.TypeScript.Rules;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.Rules
{
    [TestClass]
    public class ActiveJavaScriptRulesProviderTests
    {
        private static readonly IUserSettingsProvider EmptyUserSettingsProvider = new UserSettingsBuilder();

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ActiveJavaScriptRulesProvider, IActiveJavaScriptRulesProvider>(null, new[]
            {
                MefTestHelpers.CreateExport<IJavaScriptRuleDefinitionsProvider>(Mock.Of<IJavaScriptRuleDefinitionsProvider>()),
                MefTestHelpers.CreateExport<IUserSettingsProvider>(Mock.Of<IUserSettingsProvider>())
            });
        }

        [TestMethod]
        public void Get_NoUserOverrides_InactiveRulesAreNotReturned()
        {
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule("active 1", activeByDefault: true);
            ruleDefns.AddRule("inactive AAA", activeByDefault: false);
            ruleDefns.AddRule("active 2", activeByDefault: true);
            ruleDefns.AddRule("inactive BBB", activeByDefault: false);

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns, EmptyUserSettingsProvider);

            var result = testSubject.Get().ToArray();

            CheckExpectedRuleKeys(result, "active 1", "active 2");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_NoUserOverrides_HotspotsAreNotReturned()
        {
            // NOTE: there are currently no taint vulnerabilities in the SonarJS jar
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule("bug", ruleType: RuleType.BUG);
            ruleDefns.AddRule("codesmell", ruleType: RuleType.CODE_SMELL);
            ruleDefns.AddRule("hotspot", ruleType: RuleType.SECURITY_HOTSPOT);
            ruleDefns.AddRule("vuln", ruleType: RuleType.VULNERABILITY);

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns, EmptyUserSettingsProvider);

            var result = testSubject.Get().ToArray();

            CheckExpectedRuleKeys(result, "bug", "codesmell", "vuln");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_NoUserOverrides_RulesWithNullEslintKeysAreNotReturned()
        {
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule("aaa");
            ruleDefns.AddRule(null);
            ruleDefns.AddRule("bbb");

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns, EmptyUserSettingsProvider);

            var result = testSubject.Get().ToArray();

            CheckExpectedRuleKeys(result, "aaa", "bbb");
            CheckConfigurationsAreEmpty(result);
        }

        [TestMethod]
        public void Get_NoUserOverrides_RulesWithConfigurations_ExpectedConfigsReturned()
        {
            var config1 = new object();
            var config2 = "a default rule value";

            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule("aaa", configurations: new object[] { config1, config2 });

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns, EmptyUserSettingsProvider);

            var result = testSubject.Get().ToArray();

            CheckExpectedRuleKeys(result, "aaa");
            result[0].Configurations.Should().ContainInOrder(config1, config2);
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(false, false)]
        [DataRow(false, true)]
        [DataRow(true, false)]
        public void Get_HasUserOverrides_OverridesDefaultActivationLevel(
            bool onByDefault, bool onInUserSettings)
        {
            // The value in the user settings should always override the default
            var expectedRuleCount = onInUserSettings ? 1 : 0;

            // Create definition and rule settings with a single rule
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule(ruleKey: "javascript:rule1", activeByDefault: onByDefault, eslintKey: "esKey");

            var userSettingsLevel = onInUserSettings ? RuleLevel.On : RuleLevel.Off;
            var userSettings = new UserSettingsBuilder()
                .Add("javascript:rule1", userSettingsLevel);

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns, userSettings);

            var result = testSubject.Get().ToArray();

            result.Length.Should().Be(expectedRuleCount);
        }

        [TestMethod]
        public void Get_HasUserOverrides_NoRulesMatchTheUserOverrides_UserOverridesAreIgnored()
        {
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule(ruleKey: "javascript:activeRule1", activeByDefault: true, eslintKey: "active1");

            var userSettings = new UserSettingsBuilder()
                .Add("wrongLanguage:activeRule1", RuleLevel.Off) // Correct rule key but wrong repo -> ignored
                .Add("javascript:unknownRule", RuleLevel.On); // Right repo, but rule key doesn't match a defined rule -> ignored

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns, userSettings);

            var result = testSubject.Get().ToArray();

            result.Length.Should().Be(1);
            result[0].Key.Should().Be("active1");
        }

        [TestMethod]
        public void Get_HasUserOverrides_IrregularRules_UserOverridesAreIgnored()
        {
            var ruleDefns = new RuleDefinitionsBuilder();
            ruleDefns.AddRule(ruleKey: "javascript:hotspot1", activeByDefault: false, eslintKey: "hotspot1", ruleType: RuleType.SECURITY_HOTSPOT);
            ruleDefns.AddRule(ruleKey: "javascript:S2260", activeByDefault: false, eslintKey: null);

            var userSettings = new UserSettingsBuilder()
                .Add("javascript:hotspot1", RuleLevel.On)
                .Add("javascript:S2260", RuleLevel.On);

            var testSubject = new ActiveJavaScriptRulesProvider(ruleDefns, userSettings);

            var result = testSubject.Get().ToArray();

            result.Length.Should().Be(0);
        }

        private static void CheckExpectedRuleKeys(IEnumerable<Rule> result, params string[] expected) =>
            result.Select(x => x.Key).Should().BeEquivalentTo(expected);

        private static void CheckConfigurationsAreEmpty(IEnumerable<Rule> result) =>
            result.All(x => x.Configurations.Length == 0).Should().BeTrue();

        private class RuleDefinitionsBuilder : IJavaScriptRuleDefinitionsProvider
        {
            private readonly IList<RuleDefinition> definitions = new List<RuleDefinition>();

            IEnumerable<RuleDefinition> IJavaScriptRuleDefinitionsProvider.GetDefinitions() => definitions;

            public void AddRule(string eslintKey = "any", bool activeByDefault = true, RuleType ruleType = RuleType.BUG,
                object[] configurations = null, string ruleKey = "any")
            {
                configurations ??= Array.Empty<object>();
                var newDefn = new RuleDefinition
                {
                    RuleKey = ruleKey,
                    EslintKey = eslintKey,
                    ActivatedByDefault = activeByDefault,
                    Type = ruleType,
                    DefaultParams = configurations
                };

                definitions.Add(newDefn);
            }
        }

        private class UserSettingsBuilder : IUserSettingsProvider
        {
            private readonly RulesSettings ruleSettings;

            public UserSettingsBuilder()
            {
                ruleSettings = new RulesSettings();
                UserSettings = new UserSettings(ruleSettings);
            }

            public UserSettingsBuilder Add(string ruleKey, RuleLevel ruleLevel)
            {
                ruleSettings.Rules.Add(ruleKey, new RuleConfig { Level = ruleLevel });
                return this;
            }

            #region IUserSettingsProvider members

            public UserSettings UserSettings { get; }

            public string SettingsFilePath => throw new NotImplementedException();

            public event EventHandler SettingsChanged;

            public void DisableRule(string ruleId) => throw new NotImplementedException();

            public void EnsureFileExists() => throw new NotImplementedException();

            #endregion
        }
    }
}
