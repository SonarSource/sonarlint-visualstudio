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
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.CFamily.Rules.UnitTests
{
    [TestClass]
    public class CFamilyRuleConfigProviderTests
    {
        [TestMethod]
        public void Get_NullLanguage_ArgumentNullException()
        {
            var testSubject = CreateTestSubject(new RulesSettings(), new DummyCFamilyRulesConfig("cpp"));

            Action act = () => testSubject.GetRulesConfiguration(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("languageKey");
        }

        [TestMethod]
        public void Get_UnknownLanguageKey_ArgumentNullException()
        {
            var testSubject = CreateTestSubject(new RulesSettings(), new DummyCFamilyRulesConfig("cpp"));

            Action act = () => testSubject.GetRulesConfiguration("sfdsfdggretert");

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("language");
        }

        [TestMethod]
        public void Get_EffectiveRulesAreCalculated()
        {
            var standaloneModeSettings = new RulesSettings
            {
                Rules = new Dictionary<string, RuleConfig>
                {
                    {"cpp:rule1", new RuleConfig {Level = RuleLevel.On}},
                    {"cpp:rule2", new RuleConfig {Level = RuleLevel.Off}},
                    {"cpp:rule4", new RuleConfig {Level = RuleLevel.On}},
                    {"XXX:rule3", new RuleConfig {Level = RuleLevel.On}}
                }
            };

            var sonarWayConfig = new DummyCFamilyRulesConfig("cpp")
                .AddRule("rule1", IssueSeverity.Info, isActive: false)
                .AddRule("rule2", IssueSeverity.Major, isActive: false)
                .AddRule("rule3", IssueSeverity.Minor, isActive: true)
                .AddRule("rule4", IssueSeverity.Blocker, isActive: false);

            var testSubject = CreateTestSubject(standaloneModeSettings, sonarWayConfig);

            // Act
            var result = testSubject.GetRulesConfiguration("cpp");

            // Assert
            result.ActivePartialRuleKeys.Should().BeEquivalentTo("rule1", "rule3", "rule4");
            result.AllPartialRuleKeys.Should().BeEquivalentTo("rule1", "rule2", "rule3", "rule4");
        }

        private CFamilyRuleConfigProvider CreateTestSubject(RulesSettings ruleSettings, DummyCFamilyRulesConfig sonarWayConfig)
        {
            var ruleSettingsProvider = new Mock<IRuleSettingsProvider>();
            ruleSettingsProvider.Setup(x => x.Get()).Returns(ruleSettings);

            var ruleSettingsProviderFactory = new Mock<IRuleSettingsProviderFactory>();
            ruleSettingsProviderFactory.Setup(x => x.Get(Language.Cpp)).Returns(ruleSettingsProvider.Object);

            var sonarWayProviderMock = new Mock<ICFamilyRulesConfigProvider>();

            sonarWayProviderMock.Setup(x => x.GetRulesConfiguration(It.IsAny<string>()))
                .Returns(sonarWayConfig);

            var testSubject = new CFamilyRuleConfigProvider(ruleSettingsProviderFactory.Object, sonarWayProviderMock.Object, Mock.Of<ILogger>());

            return testSubject;
        }
    }
}
