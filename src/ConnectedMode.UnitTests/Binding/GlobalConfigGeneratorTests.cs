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
using System.Text;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class GlobalConfigGeneratorTests
    {
        [TestMethod]
        public void Generate_RulesAreNull_ThrowsNullException()
        {
            var generator = new GlobalConfigGenerator();

            Action act = () => generator.Generate(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rules");
        }

        [TestMethod]
        public void Generate_MultipleRules_RulesAreSorted()
        {
            var generator = new GlobalConfigGenerator();
            var rules = new List<SonarQubeRule>()
            {
                CreateRule("3", ""),
                CreateRule("5", ""),
                CreateRule("2", ""),
                CreateRule("8", ""),
            };

            var result = generator.Generate(rules);

            var sb = new StringBuilder();
            sb.AppendLine("is_global=true");
            sb.AppendLine("global_level=1999999999");
            sb.AppendLine(GetRuleString("2", "suggestion"));
            sb.AppendLine(GetRuleString("3", "suggestion"));
            sb.AppendLine(GetRuleString("5", "suggestion"));
            sb.AppendLine(GetRuleString("8", "suggestion"));

            result.Should().Be(sb.ToString());
        }

        [TestMethod]
        public void Generate_GlobalConfigSettingsAreCorrect()
        {
            var generator = new GlobalConfigGenerator();
            var rules = new List<SonarQubeRule>() { };

            var result = generator.Generate(rules);

            var sb = new StringBuilder();
            sb.AppendLine("is_global=true");
            sb.AppendLine("global_level=1999999999");

            result.Should().Be(sb.ToString());
        }

        [TestMethod]
        [DataRow(RuleAction.Info, "suggestion")]
        [DataRow(RuleAction.Warning, "warning")]
        [DataRow(RuleAction.None, "none")]
        [DataRow(RuleAction.Error, "error")]
        [DataRow(RuleAction.Hidden, "silent")]
        public void GetActionText_Valid(RuleAction action, string expected)
        {
            GlobalConfigGenerator.GetActionText(action).Should().Be(expected);
        }

        [TestMethod]
        public void GetActionText_Invalid()
        {
            Action act = () => GlobalConfigGenerator.GetActionText((RuleAction)(-1));
            act.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        [DataRow(SonarQubeIssueSeverity.Info, RuleAction.Info)]
        [DataRow(SonarQubeIssueSeverity.Minor, RuleAction.Info)]
        [DataRow(SonarQubeIssueSeverity.Major, RuleAction.Warning)]
        [DataRow(SonarQubeIssueSeverity.Critical, RuleAction.Warning)]
        public void GetVSSeverity_NotBlocker_CorrectlyMapped(SonarQubeIssueSeverity sqSeverity, RuleAction expectedVsSeverity)
        {
            var testSubject = new GlobalConfigGenerator();

            testSubject.GetVsSeverity(sqSeverity).Should().Be(expectedVsSeverity);
        }

        [TestMethod]
        [DataRow(true, RuleAction.Error)]
        [DataRow(false, RuleAction.Warning)]
        public void GetVSSeverity_Blocker_CorrectlyMapped(bool shouldTreatBlockerAsError, RuleAction expectedVsSeverity)
        {
            var envSettingsMock = new Mock<IEnvironmentSettings>();
            envSettingsMock.Setup(x => x.TreatBlockerSeverityAsError()).Returns(shouldTreatBlockerAsError);

            var testSubject = new GlobalConfigGenerator(envSettingsMock.Object);

            testSubject.GetVsSeverity(SonarQubeIssueSeverity.Blocker).Should().Be(expectedVsSeverity);
        }

        [TestMethod]
        [DataRow(SonarQubeIssueSeverity.Unknown)]
        [DataRow((SonarQubeIssueSeverity)(-1))]
        public void GetVSSeverity_Invalid_Throws(SonarQubeIssueSeverity sqSeverity)
        {
            Action act = () => new GlobalConfigGenerator().GetVsSeverity(sqSeverity);
            act.Should().Throw<NotSupportedException>();
        }

        private static SonarQubeRule CreateRule(string ruleKey, string repoKey, bool isActive = true) =>
            new SonarQubeRule(ruleKey, repoKey, isActive, SonarQubeIssueSeverity.Info, null, null, new Dictionary<string, string>(), SonarQubeIssueType.Unknown, null, null, null, null, null, null);

        private string GetRuleString(string expectedKey, string expectedSeverity) =>
            $"dotnet_diagnostic.{expectedKey}.severity = {expectedSeverity}";
    }
}
