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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.CodeAnalysis.RuleSets;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class RulesHelperTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void ToRuleSet_WhenRoslynExportProfileResponseNull_ReturnsNull()
        {
            // Arrange & Act
            var result = RulesHelper.ToRuleSet(null);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ToRuleSet_DumpsContentToRuleSetFileAndLoadsIt()
        {
            // Arrange
            var ruleset = TestRuleSetHelper.CreateTestRuleSet(numRules: 10);
            var roslynProfileExporter = RoslynExportProfileHelper.CreateExport(ruleset);

            // Act
            var result = RulesHelper.ToRuleSet(roslynProfileExporter);

            // Assert
            TestRuleSetHelper.RuleSetToXml(result).Should().Be(TestRuleSetHelper.RuleSetToXml(ruleset));
        }

        [TestMethod]
        public void ToQualityProfile_WhenRuleSetNull_ReturnsNull()
        {
            // Arrange & Act
            var result = RulesHelper.ToQualityProfile(null, Language.Unknown);

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public void ToQualityProfile_SelectRulesNotMarkedAsNone()
        {
            // Arrange
            var ruleset = TestRuleSetHelper.CreateTestRuleSet(numRules: 5);
            var nonNoneRulesCount = ruleset.Rules.Count(x => x.Action != RuleAction.None);

            // Act
            var result = RulesHelper.ToQualityProfile(ruleset, Language.CSharp);

            // Assert
            result.Language.Should().Be(Language.CSharp);
            result.Rules.Should().HaveCount(nonNoneRulesCount);
        }
    }
}
