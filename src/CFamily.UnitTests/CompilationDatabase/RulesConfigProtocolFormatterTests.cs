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
using SonarLint.VisualStudio.CFamily.Helpers.UnitTests;

namespace SonarLint.VisualStudio.CFamily.CompilationDatabase.UnitTests
{
    [TestClass]
    public class RulesConfigProtocolFormatterTests
    {
        [TestMethod]
        public void Format_NullRulesConfig_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Format(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("rulesConfig");
        }

        [TestMethod]
        public void Format_NoRules_EmptyQualityProfile()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp");

            var testSubject = CreateTestSubject();
            var result = testSubject.Format(rulesConfig);

            result.QualityProfile.Should().BeEmpty();
        }

        [TestMethod]
        public void Format_NoActiveRules_EmptyQualityProfile()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp");
            rulesConfig.AddRule("123", false);

            var testSubject = CreateTestSubject();
            var result = testSubject.Format(rulesConfig);

            result.QualityProfile.Should().BeEmpty();
        }

        [TestMethod]
        public void Format_OneActiveRule_OneRuleInQualityProfile()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp");
            rulesConfig.AddRule("123", true);

            var testSubject = CreateTestSubject();
            var result = testSubject.Format(rulesConfig);

            result.QualityProfile.Should().Be("123");
        }

        [TestMethod]
        public void Format_MultipleActiveRules_CommaSeparatedQualityProfile()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp");
            rulesConfig.AddRule("12", true);
            rulesConfig.AddRule("34", false);
            rulesConfig.AddRule("56", true);
            rulesConfig.AddRule("78", false);

            var testSubject = CreateTestSubject();
            var result = testSubject.Format(rulesConfig);

            result.QualityProfile.Should().Be("12,56");
        }

        [TestMethod]
        public void Format_NoRules_EmptyRuleParameters()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp");

            var testSubject = CreateTestSubject();
            var result = testSubject.Format(rulesConfig);

            result.RuleParameters.Should().BeEmpty();
        }

        [TestMethod]
        public void Format_MultipleRules_DotSeparatedParametersForActiveRules()
        {
            var rulesConfig = new DummyCFamilyRulesConfig("cpp");

            rulesConfig.AddRule("rule1", true, new Dictionary<string, string>
            {
                {"param1", "some value"},
                {"param2", "some other value"}
            });

            // inactive rules should be ignored
            rulesConfig.AddRule("inactive", false, new Dictionary<string, string>
            {
                {"param3", "value3"},
                {"param4", "value4"}
            });

            rulesConfig.AddRule("rule2", true, new Dictionary<string, string>
            {
                {"some param", "value1"},
                {"some other param", "value2"}
            });

            var testSubject = CreateTestSubject();
            var result = testSubject.Format(rulesConfig);

            result.RuleParameters.Should().BeEquivalentTo(new Dictionary<string, string>
            {
                {"rule1.param1", "some value"},
                {"rule1.param2", "some other value"},
                {"rule2.some param", "value1"},
                {"rule2.some other param", "value2"}
            });
        }

        private RulesConfigProtocolFormatter CreateTestSubject() => new RulesConfigProtocolFormatter();
    }
}
