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

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class RoslynAnalysisConfigurationProviderTests
{
    private static readonly ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents> DefaultAnalyzers
        = new Dictionary<RoslynLanguage, AnalyzerAssemblyContents> { { Language.CSharp, new AnalyzerAssemblyContents() } }.ToImmutableDictionary();
    private static readonly List<ActiveRuleDto> DefaultActiveRules = new();
    private static readonly Dictionary<string, string> DefaultAnalysisProperties = new();
    private static readonly AnalyzerInfoDto DefaultAnalyzerInfoDto = new(false, false);

    private ISonarLintXmlProvider sonarLintXmlProvider = null!;
    private IRoslynAnalyzerProvider roslynAnalyzerProvider = null!;
    private IRoslynAnalysisProfilesProvider analyzerProfilesProvider = null!;
    private TestLogger testLogger = null!;
    private RoslynAnalysisConfigurationProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        sonarLintXmlProvider = Substitute.For<ISonarLintXmlProvider>();
        roslynAnalyzerProvider = Substitute.For<IRoslynAnalyzerProvider>();
        roslynAnalyzerProvider.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto).Returns(DefaultAnalyzers);

        analyzerProfilesProvider = Substitute.For<IRoslynAnalysisProfilesProvider>();
        testLogger = Substitute.ForPartsOf<TestLogger>();

        testSubject = new RoslynAnalysisConfigurationProvider(
            sonarLintXmlProvider,
            roslynAnalyzerProvider,
            analyzerProfilesProvider,
            testLogger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalysisConfigurationProvider, IRoslynAnalysisConfigurationProvider>(
            MefTestHelpers.CreateExport<ISonarLintXmlProvider>(),
            MefTestHelpers.CreateExport<IRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IRoslynAnalysisProfilesProvider>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalysisConfigurationProvider>();

    [TestMethod]
    public void Ctor_SetsLogContext() =>
        testLogger.Received(1).ForContext(
            Resources.RoslynAnalysisLogContext,
            Resources.RoslynAnalysisConfigurationLogContext);

    [TestMethod]
    public void GetConfiguration_CreatesConfigurationForEachLanguage()
    {
        var roslynAnalysisProfiles = new Dictionary<RoslynLanguage, RoslynAnalysisProfile>
        {
            {
                Language.CSharp, new RoslynAnalysisProfile(
                    CreateTestAnalyzers(1),
                    CreateTestCodeFixProviders(),
                    [CreateRuleConfiguration(Language.CSharp, "S001"), CreateRuleConfiguration(Language.CSharp, "S002", false)],
                    new() { { "sonar.cs.property", "value" } })
            },
            {
                Language.VBNET, new RoslynAnalysisProfile(
                    CreateTestAnalyzers(2),
                    CreateTestCodeFixProviders(),
                    [CreateRuleConfiguration(Language.VBNET, "S001", false), CreateRuleConfiguration(Language.VBNET, "S002")],
                    new() { { "sonar.vbnet.property", "value" } })
            }
        };

        var xmlConfigurations = SetUpXmlConfigurations(roslynAnalysisProfiles);
        analyzerProfilesProvider.GetAnalysisProfilesByLanguage(DefaultAnalyzers, DefaultActiveRules, DefaultAnalysisProperties)
            .Returns(roslynAnalysisProfiles);

        var result = testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        result.Keys.Should().BeEquivalentTo(roslynAnalysisProfiles.Keys);
        foreach (var language in roslynAnalysisProfiles.Keys)
        {
            result.ContainsKey(language).Should().BeTrue();
            result[language].Analyzers.Should().BeEquivalentTo(roslynAnalysisProfiles[language].Analyzers);
            result[language].CodeFixProvidersByRuleKey.Should().BeSameAs(roslynAnalysisProfiles[language].CodeFixProvidersByRuleKey);
            result[language].DiagnosticOptions.Should().BeEquivalentTo(roslynAnalysisProfiles[language].Rules.ToDictionary(x => x.RuleId.RuleKey, x => x.ReportDiagnostic));
            result[language].SonarLintXml.Should().BeEquivalentTo(xmlConfigurations[language]);
        }
    }

    [TestMethod]
    public void GetConfiguration_NoAnalyzers_LogsAndExcludesLanguage()
    {
        var language = Language.CSharp;
        var roslynAnalysisProfiles = new Dictionary<RoslynLanguage, RoslynAnalysisProfile>
        {
            {
                language, new RoslynAnalysisProfile(
                    ImmutableArray<DiagnosticAnalyzer>.Empty,
                    CreateTestCodeFixProviders(),
                    [CreateRuleConfiguration(language, "S001")],
                    new Dictionary<string, string>())
            }
        };

        analyzerProfilesProvider.GetAnalysisProfilesByLanguage(DefaultAnalyzers, DefaultActiveRules, DefaultAnalysisProperties)
            .Returns(roslynAnalysisProfiles);

        var result = testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        result.Should().BeEmpty();
        testLogger.AssertPartialOutputStringExists(string.Format(Resources.RoslynAnalysisConfigurationNoAnalyzers, language.Name));
    }

    [TestMethod]
    public void GetConfiguration_NoActiveRules_LogsAndExcludesLanguage()
    {
        var language = Language.CSharp;
        var roslynAnalysisProfiles = new Dictionary<RoslynLanguage, RoslynAnalysisProfile>
        {
            {
                language, new RoslynAnalysisProfile(
                    CreateTestAnalyzers(1),
                    CreateTestCodeFixProviders(),
                    [CreateRuleConfiguration(language, "S001", false), CreateRuleConfiguration(language, "S002", false)],
                    new Dictionary<string, string>())
            }
        };

        analyzerProfilesProvider.GetAnalysisProfilesByLanguage(DefaultAnalyzers, DefaultActiveRules, DefaultAnalysisProperties)
            .Returns(roslynAnalysisProfiles);

        var result = testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        result.Should().BeEmpty();
        testLogger.AssertPartialOutputStringExists(string.Format(Resources.RoslynAnalysisConfigurationNoActiveRules, language.Name));
    }

    [TestMethod]
    public void GetConfiguration_NoAnalysisProfiles_ReturnsEmptyDictionary()
    {
        analyzerProfilesProvider.GetAnalysisProfilesByLanguage(DefaultAnalyzers, DefaultActiveRules, DefaultAnalysisProperties)
            .Returns(new Dictionary<RoslynLanguage, RoslynAnalysisProfile>());

        var result = testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        result.Should().BeEmpty();
    }

    private Dictionary<RoslynLanguage, SonarLintXmlConfigurationFile> SetUpXmlConfigurations(Dictionary<RoslynLanguage, RoslynAnalysisProfile> profiles)
    {
        var xmlConfigurations = new Dictionary<RoslynLanguage, SonarLintXmlConfigurationFile>();
        foreach (var profile in profiles)
        {
            var xml = SetUpXmlProvider(profile.Value);
            xmlConfigurations.Add(profile.Key, xml);
        }
        return xmlConfigurations;
    }

    private RoslynRuleConfiguration CreateRuleConfiguration(
        Language language,
        string ruleKey,
        bool isActive = true) =>
        new(new SonarCompositeRuleId(language.RepoInfo.Key, ruleKey),
            isActive,
            []);

    private ImmutableArray<DiagnosticAnalyzer> CreateTestAnalyzers(int count) => Enumerable.Range(0, count).Select(_ => Substitute.For<DiagnosticAnalyzer>()).ToImmutableArray();

    private ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>> CreateTestCodeFixProviders() => ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>>.Empty.Add("any", [Substitute.For<CodeFixProvider>()]);

    private SonarLintXmlConfigurationFile SetUpXmlProvider(RoslynAnalysisProfile profile)
    {
        var slxml = new SonarLintXmlConfigurationFile("any", "any");
        sonarLintXmlProvider.Create(profile).Returns(slxml);
        return slxml;
    }
}
