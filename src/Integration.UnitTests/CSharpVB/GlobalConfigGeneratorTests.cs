/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using System.Text;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.Integration.CSharpVB;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB;

[TestClass]
public class GlobalConfigGeneratorTests
{
    private GlobalConfigGenerator testSubject;

    [TestInitialize]
    public void TestInitialize() => testSubject = new GlobalConfigGenerator();

    [TestMethod]
    public void Generate_RulesAreNull_ThrowsNullException()
    {
        Action act = () => testSubject.Generate(null);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rules");
    }

    [TestMethod]
    public void Generate_MultipleRules_RulesAreSorted()
    {
        List<IRoslynRuleStatus> rules =
        [
            CreateRule("3"), CreateRule("5"), CreateRule("2"), CreateRule("8")
        ];

        var result = testSubject.Generate(rules);

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
        var result = testSubject.Generate([]);

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
        var sb = new StringBuilder();
        sb.AppendLine("is_global=true");
        sb.AppendLine("global_level=1999999999");
        sb.AppendLine(GetRuleString("rulekey", expected));

        var result = testSubject.Generate([CreateRule("rulekey", action)]);

        result.Should().Be(sb.ToString());
    }

    [TestMethod]
    public void GetActionText_Invalid()
    {
        Action act = () => testSubject.Generate([CreateRule("rulekey", (RuleAction)(-1))]);;
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static IRoslynRuleStatus CreateRule(string key, RuleAction action = RuleAction.Info)
    {
        var roslynRuleStatus = Substitute.For<IRoslynRuleStatus>();
        roslynRuleStatus.Key.Returns(key);
        roslynRuleStatus.GetSeverity().Returns(action);
        return roslynRuleStatus;
    }

    private static string GetRuleString(string expectedKey, string expectedSeverity) =>
        $"dotnet_diagnostic.{expectedKey}.severity = {expectedSeverity}";
}
