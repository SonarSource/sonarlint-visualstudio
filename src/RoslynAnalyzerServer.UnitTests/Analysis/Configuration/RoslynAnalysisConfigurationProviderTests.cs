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
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Integration.TestInfrastructure;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class RoslynAnalysisConfigurationProviderTests
{
    private static readonly ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents> DefaultAnalyzers
        = new Dictionary<RoslynLanguage, AnalyzerAssemblyContents> { { Language.CSharp, new AnalyzerAssemblyContents() } }.ToImmutableDictionary();
    private static readonly List<ActiveRuleDto> DefaultActiveRules = [];
    private static readonly Dictionary<string, string> DefaultAnalysisProperties = [];
    private static readonly AnalyzerInfoDto DefaultAnalyzerInfoDto = new(false, false);
    private static readonly Dictionary<RoslynLanguage, RoslynAnalysisProfile> DefaultRoslynAnalysisProfile = [];

    private IThreadHandling threadHandling = null!;
    private IAsyncLock asyncLock = null!;
    private ISonarLintXmlProvider sonarLintXmlProvider = null!;
    private IRoslynAnalyzerProvider roslynAnalyzerProvider = null!;
    private IRoslynAnalysisProfilesProvider analyzerProfilesProvider = null!;
    private TestLogger testLogger = null!;
    private RoslynAnalysisConfigurationProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        var asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        sonarLintXmlProvider = Substitute.For<ISonarLintXmlProvider>();
        roslynAnalyzerProvider = Substitute.For<IRoslynAnalyzerProvider>();
        roslynAnalyzerProvider.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto).Returns(DefaultAnalyzers);
        asyncLock = Substitute.For<IAsyncLock>();
        asyncLockFactory.Create().Returns(asyncLock);

        analyzerProfilesProvider = Substitute.For<IRoslynAnalysisProfilesProvider>();
        analyzerProfilesProvider
            .GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), Arg.Any<List<ActiveRuleDto>>(), Arg.Any<Dictionary<string, string>>())
            .Returns(DefaultRoslynAnalysisProfile);
        testLogger = Substitute.ForPartsOf<TestLogger>();

        testSubject = new RoslynAnalysisConfigurationProvider(
            sonarLintXmlProvider,
            roslynAnalyzerProvider,
            analyzerProfilesProvider,
            asyncLockFactory,
            threadHandling,
            testLogger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalysisConfigurationProvider, IRoslynAnalysisConfigurationProvider>(
            MefTestHelpers.CreateExport<ISonarLintXmlProvider>(),
            MefTestHelpers.CreateExport<IRoslynAnalyzerProvider>(),
            MefTestHelpers.CreateExport<IRoslynAnalysisProfilesProvider>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalysisConfigurationProvider>();

    [TestMethod]
    public void Ctor_SetsLogContext() =>
        testLogger.Received(1).ForContext(
            Resources.RoslynAnalysisLogContext,
            Resources.RoslynAnalysisConfigurationLogContext);

    [TestMethod]
    public async Task GetConfiguration_CreatesConfigurationForEachLanguage()
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

        var result = await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

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
    public async Task GetConfiguration_NoAnalyzers_LogsAndExcludesLanguage()
    {
        var language = Language.CSharp;
        var roslynAnalysisProfiles = new Dictionary<RoslynLanguage, RoslynAnalysisProfile>
        {
            {
                language, new RoslynAnalysisProfile(
                    ImmutableArray<DiagnosticAnalyzer>.Empty,
                    CreateTestCodeFixProviders(),
                    [CreateRuleConfiguration(language, "S001")],
                    [])
            }
        };

        analyzerProfilesProvider.GetAnalysisProfilesByLanguage(DefaultAnalyzers, DefaultActiveRules, DefaultAnalysisProperties)
            .Returns(roslynAnalysisProfiles);

        var result = await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        result.Should().BeEmpty();
        testLogger.AssertPartialOutputStringExists(string.Format(Resources.RoslynAnalysisConfigurationNoAnalyzers, language.Name));
    }

    [TestMethod]
    public async Task GetConfiguration_NoActiveRules_LogsAndExcludesLanguage()
    {
        var language = Language.CSharp;
        var roslynAnalysisProfiles = new Dictionary<RoslynLanguage, RoslynAnalysisProfile>
        {
            {
                language, new RoslynAnalysisProfile(
                    CreateTestAnalyzers(1),
                    CreateTestCodeFixProviders(),
                    [CreateRuleConfiguration(language, "S001", false), CreateRuleConfiguration(language, "S002", false)],
                    [])
            }
        };

        analyzerProfilesProvider.GetAnalysisProfilesByLanguage(DefaultAnalyzers, DefaultActiveRules, DefaultAnalysisProperties)
            .Returns(roslynAnalysisProfiles);

        var result = await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        result.Should().BeEmpty();
        testLogger.AssertPartialOutputStringExists(string.Format(Resources.RoslynAnalysisConfigurationNoActiveRules, language.Name));
    }

    [TestMethod]
    public async Task GetConfiguration_NoAnalysisProfiles_ReturnsEmptyDictionary()
    {
        var result = await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_SameActiveRules_Caches()
    {
        var activeRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "threshold", "3" } }) };
        var sameActiveRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "threshold", "3" } }) };

        await testSubject.GetConfiguration(activeRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(sameActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(sameActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), activeRules, DefaultAnalysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_DifferentActiveRules_InvalidatesCache()
    {
        var activeRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "threshold", "3" } }) };
        var newActiveRules = new List<ActiveRuleDto> { new("S102", new Dictionary<string, string> { { "threshold", "3" } }) };

        await testSubject.GetConfiguration(activeRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), activeRules, DefaultAnalysisProperties);
        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), newActiveRules, DefaultAnalysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_SameRuleWithDifferentParameter_InvalidatesCache()
    {
        var activeRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "threshold", "3" } }) };
        var newActiveRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "timeout", "60" } }) };

        await testSubject.GetConfiguration(activeRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), activeRules, DefaultAnalysisProperties);
        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), newActiveRules, DefaultAnalysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_SameRuleWithDifferentParameters_InvalidatesCache()
    {
        var activeRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "threshold", "3" } }) };
        var newActiveRules = new List<ActiveRuleDto> { new("S101", []) };

        await testSubject.GetConfiguration(activeRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), activeRules, DefaultAnalysisProperties);
        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), newActiveRules, DefaultAnalysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_SameRuleWithDifferentParameterValue_InvalidatesCache()
    {
        var activeRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "threshold", "3" } }) };
        var newActiveRules = new List<ActiveRuleDto> { new("S101", new Dictionary<string, string> { { "threshold", "5" } }) };

        await testSubject.GetConfiguration(activeRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(newActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), activeRules, DefaultAnalysisProperties);
        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), newActiveRules, DefaultAnalysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_SameAnalysisProperties_Caches()
    {
        var analysisProperties = new Dictionary<string, string> { { "sonar.cs.internal.disableRazor", "true" } };
        var sameAnalysisProperties = new Dictionary<string, string> { { "sonar.cs.internal.disableRazor", "true" } };

        await testSubject.GetConfiguration(DefaultActiveRules, analysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, sameAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, sameAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), DefaultActiveRules, analysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_SameAnalysisPropertyWithDifferentValue_InvalidatesCache()
    {
        var analysisProperties = new Dictionary<string, string> { { "sonar.cs.internal.disableRazor", "true" } };
        var newAnalysisProperties = new Dictionary<string, string> { { "sonar.cs.internal.disableRazor", "false" } };

        await testSubject.GetConfiguration(DefaultActiveRules, analysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, newAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, newAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), DefaultActiveRules, analysisProperties);
        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), DefaultActiveRules, newAnalysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_DifferentAnalysisProperties_InvalidatesCache()
    {
        var analysisProperties = new Dictionary<string, string> { { "sonar.cs.internal.disableRazor", "true" } };
        var newAnalysisProperties = new Dictionary<string, string>();

        await testSubject.GetConfiguration(DefaultActiveRules, analysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, newAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, newAnalysisProperties, DefaultAnalyzerInfoDto);

        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), DefaultActiveRules, analysisProperties);
        analyzerProfilesProvider.Received(1).GetAnalysisProfilesByLanguage(Arg.Any<ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>>(), DefaultActiveRules, newAnalysisProperties);
    }

    [TestMethod]
    public async Task GetConfiguration_MultipleCalls_Locks()
    {
        await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);
        await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        asyncLock.Received(3).AcquireAsync().IgnoreAwaitForAssert();
    }

    [TestMethod]
    public async Task GetConfiguration_RunsOnBackgroundThread()
    {
        await testSubject.GetConfiguration(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto);

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<IReadOnlyDictionary<Language, RoslynAnalysisConfiguration>>>>()).IgnoreAwaitForAssert();
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

    private static RoslynRuleConfiguration CreateRuleConfiguration(
        Language language,
        string ruleKey,
        bool isActive = true) =>
        new(new SonarCompositeRuleId(language.RepoInfo.Key, ruleKey),
            isActive,
            []);

    private static ImmutableArray<DiagnosticAnalyzer> CreateTestAnalyzers(int count) => Enumerable.Range(0, count).Select(_ => Substitute.For<DiagnosticAnalyzer>()).ToImmutableArray();

    private static ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>> CreateTestCodeFixProviders() =>
        ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>>.Empty.Add("any", [Substitute.For<CodeFixProvider>()]);

    private SonarLintXmlConfigurationFile SetUpXmlProvider(RoslynAnalysisProfile profile)
    {
        var slxml = new SonarLintXmlConfigurationFile("any", "any");
        sonarLintXmlProvider.Create(profile).Returns(slxml);
        return slxml;
    }
}
