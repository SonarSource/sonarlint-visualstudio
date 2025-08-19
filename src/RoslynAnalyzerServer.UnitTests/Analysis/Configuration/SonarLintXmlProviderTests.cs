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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class SonarLintXmlProviderTests
{
    private const string SerializedXml = "serialized xml content";
    private ISonarLintConfigurationXmlSerializer sonarLintConfigurationXmlSerializer = null!;
    private SonarLintXmlProvider testSubject = null!;
    private static readonly RoslynRuleConfiguration RuleWithoutParameters = CreateRuleConfig("rule1");
    private static readonly RoslynRuleConfiguration RuleWithParameters = CreateRuleConfig("rule2", parameters: new Dictionary<string, string> { { "param1", "paramValue1" }, { "param2", "paramValue2" } });

    [TestInitialize]
    public void TestInitialize()
    {
        sonarLintConfigurationXmlSerializer = Substitute.For<ISonarLintConfigurationXmlSerializer>();
        sonarLintConfigurationXmlSerializer.Serialize(Arg.Any<SonarLintConfiguration>()).Returns(SerializedXml);
        testSubject = new SonarLintXmlProvider(sonarLintConfigurationXmlSerializer);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<SonarLintXmlProvider, ISonarLintXmlProvider>(MefTestHelpers.CreateExport<ISonarLintConfigurationXmlSerializer>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SonarLintXmlProvider>();

    [TestMethod]
    public void Create_ReturnsExpectedXml()
    {
        var profile = new RoslynAnalysisProfile(default, [], []);

        var result = testSubject.Create(profile);

        result.Should().NotBeNull();
        result.Path.Should().Be(Path.Combine(Path.GetTempPath(), "SonarLint.xml"));
        result.GetText().ToString().Should().Be(SerializedXml);
        sonarLintConfigurationXmlSerializer.Received(1).Serialize(Arg.Any<SonarLintConfiguration>());
    }

    [TestMethod]
    public void Create_WithMultipleRulesAndProperties_ExpectedConfigurationSerialized()
    {
        var analysisProperties = new Dictionary<string, string> { { "prop1", "value1" }, { "prop2", "value2" } };
        var result = testSubject.Create(new RoslynAnalysisProfile(default, [RuleWithoutParameters, RuleWithParameters], analysisProperties));

        result.Should().NotBeNull();

        var sonarLintConfiguration = sonarLintConfigurationXmlSerializer.ReceivedCalls().Single().GetArguments()[0] as SonarLintConfiguration;
        sonarLintConfiguration.Rules.Count.Should().Be(2);
        sonarLintConfiguration.Settings.Should().BeEquivalentTo([
            new SonarLintKeyValuePair { Key = "prop1", Value = "value1" },
            new SonarLintKeyValuePair { Key = "prop2", Value = "value2" }
        ]);
    }

    [TestMethod]
    public void Create_WithRuleNoParametersNoProperties_SerializesCorrectRules()
    {
        var profile = new RoslynAnalysisProfile(default, [RuleWithoutParameters], []);

        var result = testSubject.Create(profile);

        result.Should().NotBeNull();
        var sonarLintConfiguration = sonarLintConfigurationXmlSerializer.ReceivedCalls().Single().GetArguments()[0] as SonarLintConfiguration;
        sonarLintConfiguration.Should().BeEquivalentTo(new SonarLintConfiguration { Rules = [new SonarLintRule { Key = "rule1", Parameters = [] }], Settings = [] });
    }

    [TestMethod]
    public void Create_WithRuleWithParameters_SerializesCorrectRules()
    {
        var profile = new RoslynAnalysisProfile(default, [RuleWithParameters], []);

        var result = testSubject.Create(profile);

        result.Should().NotBeNull();
        var sonarLintConfiguration = sonarLintConfigurationXmlSerializer.ReceivedCalls().Single().GetArguments()[0] as SonarLintConfiguration;
        sonarLintConfiguration.Should().BeEquivalentTo(new SonarLintConfiguration
        {
            Rules =
            [
                new SonarLintRule
                {
                    Key = "rule2", Parameters = [new SonarLintKeyValuePair { Key = "param1", Value = "paramValue1" }, new SonarLintKeyValuePair { Key = "param2", Value = "paramValue2" }]
                }
            ],
            Settings = []
        });
    }

    [TestMethod]
    public void Create_WithInactiveRules_OnlyIncludesActiveRules()
    {
        const string inactiveRuleKey = "inactiveRule";
        var rules = new List<RoslynRuleConfiguration> { RuleWithoutParameters, CreateRuleConfig(inactiveRuleKey, false), RuleWithParameters };
        var profile = new RoslynAnalysisProfile(default, rules, []);

        var result = testSubject.Create(profile);

        result.Should().NotBeNull();
        var sonarLintConfiguration = (SonarLintConfiguration)sonarLintConfigurationXmlSerializer.ReceivedCalls().Single().GetArguments()[0]!;
        sonarLintConfiguration.Rules.Count.Should().Be(2);
        sonarLintConfiguration.Rules.Select(x => x.Key).Should().NotContain(inactiveRuleKey);
    }

    private static RoslynRuleConfiguration CreateRuleConfig(
        string ruleKey,
        bool isActive = true,
        Dictionary<string, string>? parameters = null) =>
        new(
            new SonarCompositeRuleId(Language.CSharp.RepoInfo.Key, ruleKey),
            isActive,
            parameters);
}
