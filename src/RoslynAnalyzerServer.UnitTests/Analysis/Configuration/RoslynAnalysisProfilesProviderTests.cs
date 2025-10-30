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
public class RoslynAnalysisProfilesProviderTests
{
    private RoslynAnalysisProfilesProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize() => testSubject = new RoslynAnalysisProfilesProvider();

    [TestMethod]
    public void MefCtor_CheckIsExported() => MefTestHelpers.CheckTypeCanBeImported<RoslynAnalysisProfilesProvider, IRoslynAnalysisProfilesProvider>();

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalysisProfilesProvider>();

    [TestMethod]
    public void GetAnalysisProfilesByLanguage_EmptyInputs_ReturnsEmptyDictionary()
    {
        var supportedDiagnostics = ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents>.Empty;
        var activeRules = new List<ActiveRuleDto>();
        Dictionary<string, string> analysisProperties = [];

        var result = testSubject.GetAnalysisProfilesByLanguage(supportedDiagnostics, activeRules, analysisProperties);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetAnalysisProfilesByLanguage_ReturnsFilteredRulesAndParameters()
    {
        var analyzerAssemblyContents = CreateSupportedDiagnosticsForLanguages(new()
        {
            { Language.CSharp, ([Substitute.For<DiagnosticAnalyzer>(), Substitute.For<DiagnosticAnalyzer>()], ["S001", "S002", "S003"], new(){{"S002", [Substitute.For<CodeFixProvider>()]}}) },
            { Language.VBNET, ([Substitute.For<DiagnosticAnalyzer>(), Substitute.For<DiagnosticAnalyzer>()], ["S001", "S002", "S003"], new (){{"S003", [Substitute.For<CodeFixProvider>()]}}) },
        });
        List<ActiveRuleDto> activeRules =
        [
            new("csharpsquid:S001", new Dictionary<string, string> { { "param1", "value1" } }),
            new("csharpsquid:S003", []),
            new("csharpsquid:SUNSUPPORTED", []),
            new("vbnet:S002", new Dictionary<string, string> { { "param2", "value2" } })
        ];
        var analysisProperties = new Dictionary<string, string> { { "sonar.cs.property1", "value1" }, { "sonar.vbnet.property2", "value2" }, { "someotherkey", "value" } };

        var result = testSubject.GetAnalysisProfilesByLanguage(analyzerAssemblyContents, activeRules, analysisProperties);

        result.Keys.Should().BeEquivalentTo(Language.CSharp, Language.VBNET);
        ValidateProfile(
            result[Language.CSharp],
            analyzerAssemblyContents[Language.CSharp].Analyzers,
            analyzerAssemblyContents[Language.CSharp].CodeFixProvidersByRuleKey,
            [
                CreateRuleConfiguration(Language.CSharp, "S001", new() { { "param1", "value1" } }),
                CreateRuleConfiguration(Language.CSharp, "S002", isActive: false),
                CreateRuleConfiguration(Language.CSharp, "S003", [])
            ],
            new() { { "sonar.cs.property1", "value1" } });
        ValidateProfile(
            result[Language.VBNET],
            analyzerAssemblyContents[Language.VBNET].Analyzers,
            analyzerAssemblyContents[Language.VBNET].CodeFixProvidersByRuleKey,
            [
                CreateRuleConfiguration(Language.VBNET, "S001", isActive: false),
                CreateRuleConfiguration(Language.VBNET, "S002", parameters: new() { { "param2", "value2" } }),
                CreateRuleConfiguration(Language.VBNET, "S003", isActive: false)
            ],
            new Dictionary<string, string> { { "sonar.vbnet.property2", "value2" } });
    }

    private static void ValidateProfile(RoslynAnalysisProfile profile, IEnumerable<DiagnosticAnalyzer> diagnosticAnalyzers, ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>> codeFixProviders, List<RoslynRuleConfiguration> rules, Dictionary<string, string> analysisProperties) =>
        profile.Should().BeEquivalentTo(new RoslynAnalysisProfile(diagnosticAnalyzers.ToImmutableArray(), codeFixProviders, rules, analysisProperties), options => options.ComparingByMembers<RoslynRuleConfiguration>().ComparingByMembers<RoslynAnalysisProfile>());

    private static RoslynRuleConfiguration CreateRuleConfiguration(
        Language language,
        string ruleKey,
        Dictionary<string, string>? parameters = null,
        bool isActive = true) =>
        new(new SonarCompositeRuleId(language.RepoInfo.Key, ruleKey),
            isActive,
            parameters);

    private static ImmutableDictionary<RoslynLanguage, AnalyzerAssemblyContents> CreateSupportedDiagnosticsForLanguages(
        Dictionary<RoslynLanguage, (DiagnosticAnalyzer[] analyzers, string[] RuleKeys, Dictionary<string, IReadOnlyCollection<CodeFixProvider>> CodeFixProviders)> contents) =>
        contents.ToImmutableDictionary(
            x => x.Key,
            y => new AnalyzerAssemblyContents(y.Value.analyzers.ToImmutableArray(), y.Value.RuleKeys.ToImmutableHashSet(), y.Value.CodeFixProviders.ToImmutableDictionary()));
}
