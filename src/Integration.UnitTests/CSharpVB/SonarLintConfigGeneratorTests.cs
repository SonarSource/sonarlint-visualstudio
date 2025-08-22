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
using SonarLint.VisualStudio.Integration.CSharpVB;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.CSharpVB;

[TestClass]
public class SonarLintConfigGeneratorTests
{
    private SonarLintConfigGenerator testSubject;
    private static readonly IEnumerable<SonarQubeRule> EmptyRules = Array.Empty<SonarQubeRule>();
    private static readonly IDictionary<string, string> EmptyProperties = new Dictionary<string, string>();
    private static readonly Language ValidLanguage = Language.CSharp;
    private static readonly IReadOnlyDictionary<string, string> ValidParams = new Dictionary<string, string> { { "any", "any value" } };

    [TestInitialize]
    public void TestInitialize() => testSubject = new SonarLintConfigGenerator(LanguageProvider.Instance);

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SonarLintConfigGenerator, ISonarLintConfigGenerator>(
            MefTestHelpers.CreateExport<ILanguageProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<SonarLintConfigGenerator>();

    [TestMethod]
    public void Generate_NullArguments_Throws()
    {
        Action act = () => testSubject.Generate(null, EmptyProperties, new ServerExclusions(), ValidLanguage);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rules");

        act = () => testSubject.Generate(EmptyRules, null, new ServerExclusions(), ValidLanguage);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sonarProperties");

        act = () => testSubject.Generate(EmptyRules, EmptyProperties, null, ValidLanguage);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileExclusions");

        act = () => testSubject.Generate(EmptyRules, EmptyProperties, new ServerExclusions(), null);
        act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("language");
    }

    [TestMethod]
    [DataRow("xxx")]
    [DataRow("CS")] // should be case-sensitive
    [DataRow("vb")] // VB language key is "vbnet"
    public void Generate_UnrecognisedLanguage_Throws(string languageKey)
    {
        Action act = () => testSubject.Generate(EmptyRules, EmptyProperties, new ServerExclusions(),
            new Language(languageKey, "languageX", languageKey, new PluginInfo("pluginKey", null), new RepoInfo("repoKey")));
        act.Should().ThrowExactly<ArgumentOutOfRangeException>().And.ParamName.Should().Be("language");
    }

    [TestMethod]
    [DataRow("cs")]
    [DataRow("vbnet")]
    public void Generate_NoActiveRulesOrSettings_ValidLanguage_ReturnsValidConfig(string languageKey)
    {
        var actual = testSubject.Generate(EmptyRules, EmptyProperties, new ServerExclusions(), ToLanguage(languageKey));

        actual.Should().NotBeNull();
        actual.Rules.Should().BeEmpty();
        actual.Settings.Should().BeEmpty();
    }

    [TestMethod]
    public void Generate_ValidSettings_OnlyLanguageSpecificSettingsReturned()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            { "sonar.cs.property1", "valid setting 1" },
            { "sonar.cs.property2", "valid setting 2" },
            { "sonar.vbnet.property1", "wrong language - not returned" },
            { "sonar.CS.property2", "wrong case - not returned" },
            { "sonar.cs.", "incorrect prefix - not returned" },
            { "xxx.cs.property1", "key does not match - not returned" },
            { ".does.not.match", "not returned" }
        };

        // Act
        var actual = testSubject.Generate(EmptyRules, properties, new ServerExclusions(), Language.CSharp);

        // Assert
        actual.Settings.Should().BeEquivalentTo(new Dictionary<string, string> { { "sonar.cs.property1", "valid setting 1" }, { "sonar.cs.property2", "valid setting 2" } });
    }

    [TestMethod]
    public void Generate_ValidSettings_AreSorted()
    {
        // Arrange
        var properties = new Dictionary<string, string> { { "sonar.cs.property3", "aaa" }, { "sonar.cs.property1", "bbb" }, { "sonar.cs.property2", "ccc" }, };

        // Act
        var actual = testSubject.Generate(EmptyRules, properties, new ServerExclusions(), Language.CSharp);

        // Assert
        actual.Settings[0].Key.Should().Be("sonar.cs.property1");
        actual.Settings[0].Value.Should().Be("bbb");

        actual.Settings[1].Key.Should().Be("sonar.cs.property2");
        actual.Settings[1].Value.Should().Be("ccc");

        actual.Settings[2].Key.Should().Be("sonar.cs.property3");
        actual.Settings[2].Value.Should().Be("aaa");
    }

    [TestMethod]
    public void Generate_ValidSettings_SecuredSettingsAreNotReturned()
    {
        // Arrange
        var properties = new Dictionary<string, string>
        {
            { "sonar.cs.property1.secured", "secure - should not be returned" },
            { "sonar.cs.property2", "valid setting" },
            { "sonar.cs.property3.SECURED", "secure - should not be returned2" },
        };

        // Act
        var actual = testSubject.Generate(EmptyRules, properties, new ServerExclusions(), Language.CSharp);

        // Assert
        actual.Settings.Should().BeEquivalentTo(new Dictionary<string, string> { { "sonar.cs.property2", "valid setting" } });
    }

    [TestMethod]
    [DataRow("cs", "csharpsquid")]
    [DataRow("vbnet", "vbnet")]
    public void Generate_ValidRules_OnlyRulesFromKnownRepositoryReturned(string knownLanguageKey, string knownRepoKey)
    {
        // Arrange
        var rules = new List<IRuleParameters>()
        {
            CreateRuleWithValidParams("valid1", knownRepoKey),
            CreateRuleWithValidParams("unknown1", "unknown.repo.key"),
            CreateRuleWithValidParams("valid2", knownRepoKey),
            CreateRuleWithValidParams("invalid2", "another.unknown.repo.key"),
            CreateRuleWithValidParams("valid3", knownRepoKey)
        };

        // Act
        var actual = testSubject.Generate(rules, EmptyProperties, new ServerExclusions(), ToLanguage(knownLanguageKey));

        // Assert
        actual.Rules.Select(r => r.Key).Should().BeEquivalentTo(new string[] { "valid1", "valid2", "valid3" });
    }

    [TestMethod]
    [DataRow("cs")]
    [DataRow("vbnet")]
    public void Generate_SonarSecurityRules_AreNotReturned(string languageKey)
    {
        // Arrange
        var rules = new List<IRuleParameters>()
        {
            CreateRuleWithValidParams("valid1", $"roslyn.sonaranalyzer.security.{languageKey}"), CreateRuleWithValidParams("valid2", $"roslyn.sonaranalyzer.security.{languageKey}")
        };

        // Act
        var actual = testSubject.Generate(rules, EmptyProperties, new ServerExclusions(), ToLanguage(languageKey));

        // Assert
        actual.Rules.Should().BeEmpty();
    }

    [TestMethod]
    public void Generate_ValidRules_OnlyRulesWithParametersReturned()
    {
        // Arrange
        var rule1Params = new Dictionary<string, string> { { "param1", "value1" }, { "param2", "value2" } };
        var rule3Params = new Dictionary<string, string> { { "param3", "value4" } };

        var rules = new List<IRuleParameters>() { CreateRule("s111", "csharpsquid", rule1Params), CreateRule("s222", "csharpsquid" /* no params */), CreateRule("s333", "csharpsquid", rule3Params) };

        // Act
        var actual = testSubject.Generate(rules, EmptyProperties, new ServerExclusions(), Language.CSharp);

        // Assert
        actual.Rules.Count.Should().Be(2);

        actual.Rules[0].Key.Should().Be("s111");
        actual.Rules[0].Parameters.Should().BeEquivalentTo(rule1Params);
        actual.Rules[1].Key.Should().Be("s333");
        actual.Rules[1].Parameters.Should().BeEquivalentTo(rule3Params);
    }

    [TestMethod]
    public void Generate_ValidRules_AreSorted()
    {
        // Arrange
        var rules = new List<IRuleParameters>()
        {
            CreateRule("s222", "csharpsquid",
                new Dictionary<string, string> { { "any", "any" } }),
            CreateRule("s111", "csharpsquid",
                new Dictionary<string, string> { { "CCC", "value 1" }, { "BBB", "value 2" }, { "AAA", "value 3" } }),
            CreateRule("s333", "csharpsquid",
                new Dictionary<string, string> { { "any", "any" } })
        };

        // Act
        var actual = testSubject.Generate(rules, EmptyProperties, new ServerExclusions(), Language.CSharp);

        // Assert
        actual.Rules.Count.Should().Be(3);

        actual.Rules[0].Key.Should().Be("s111");
        actual.Rules[1].Key.Should().Be("s222");
        actual.Rules[2].Key.Should().Be("s333");

        actual.Rules[0].Parameters[0].Key.Should().Be("AAA");
        actual.Rules[0].Parameters[0].Value.Should().Be("value 3");

        actual.Rules[0].Parameters[1].Key.Should().Be("BBB");
        actual.Rules[0].Parameters[1].Value.Should().Be("value 2");

        actual.Rules[0].Parameters[2].Key.Should().Be("CCC");
        actual.Rules[0].Parameters[2].Value.Should().Be("value 1");
    }

    [TestMethod]
    public void Generate_HasExclusions_ExclusionsIncludedInConfig()
    {
        var exclusions = new ServerExclusions(
            exclusions: new[] { "**/path1", "**/*/path2" },
            globalExclusions: new[] { "**/path3" },
            inclusions: new[] { "**/path4" });

        var actual = testSubject.Generate(EmptyRules, EmptyProperties, exclusions, Language.CSharp);

        actual.Settings.Count.Should().Be(3);

        actual.Settings[0].Should().BeEquivalentTo(new SonarLintKeyValuePair { Key = "sonar.exclusions", Value = "**/path1,**/*/path2" });
        actual.Settings[1].Should().BeEquivalentTo(new SonarLintKeyValuePair { Key = "sonar.global.exclusions", Value = "**/path3" });
        actual.Settings[2].Should().BeEquivalentTo(new SonarLintKeyValuePair { Key = "sonar.inclusions", Value = "**/path4" });
    }

    private static IRuleParameters CreateRuleWithValidParams(string ruleKey, string repoKey) => CreateRule(ruleKey, repoKey, ValidParams);

    private static IRuleParameters CreateRule(string ruleKey, string repoKey, IReadOnlyDictionary<string, string> parameters = null)
    {
        var ruleParameters = Substitute.For<IRuleParameters>();
        ruleParameters.Key.Returns(ruleKey);
        ruleParameters.RepositoryKey.Returns(repoKey);
        ruleParameters.Parameters.Returns(parameters);

        return ruleParameters;
    }

    private static Language ToLanguage(string sqLanguageKey) => LanguageProvider.Instance.GetLanguageFromLanguageKey(sqLanguageKey);
}
