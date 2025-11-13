/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class RoslynAnalyzerProviderTests
{
    private const string CsharpAnalyzerPath = "c:\\analyzers\\csharp.dll";
    private const string CsharpEnterpriseAnalyzerPath = "c:\\analyzers\\csharp.enterprise.dll";
    private const string VbAnalyzerPath = "c:\\analyzers\\vb.dll";
    private const string VbEnterpriseAnalyzerPath = "c:\\analyzers\\vb.enterprise.dll";
    private readonly DiagnosticAnalyzer CsharpAnalyzer = CreateAnalyzerWithDiagnostic();
    private readonly DiagnosticAnalyzer CsharpEnterpriseAnalyzer = CreateAnalyzerWithDiagnostic();
    private readonly DiagnosticAnalyzer VbAnalyzer = CreateAnalyzerWithDiagnostic();
    private readonly DiagnosticAnalyzer VbEnterpriseAnalyzer = CreateAnalyzerWithDiagnostic();
    private static readonly AnalyzerInfoDto DefaultAnalyzerInfoDto = new(false, false);

    private IEmbeddedDotnetAnalyzersLocator analyzersLocator = null!;
    private IRoslynAnalyzerLoader roslynAnalyzerLoader = null!;
    private RoslynAnalyzerProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        analyzersLocator = Substitute.For<IEmbeddedDotnetAnalyzersLocator>();
        roslynAnalyzerLoader = Substitute.For<IRoslynAnalyzerLoader>();
        testSubject = new RoslynAnalyzerProvider(analyzersLocator, roslynAnalyzerLoader);
    }

    [TestMethod]
    public void MefCtor_IRoslynAnalyzerProvider_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalyzerProvider, IRoslynAnalyzerProvider>(
            MefTestHelpers.CreateExport<IEmbeddedDotnetAnalyzersLocator>(),
            MefTestHelpers.CreateExport<IRoslynAnalyzerLoader>());

    [TestMethod]
    public void MefCtor_IRoslynAnalyzerAssemblyContentsLoader_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalyzerProvider, IRoslynAnalyzerAssemblyContentsLoader>(
            MefTestHelpers.CreateExport<IEmbeddedDotnetAnalyzersLocator>(),
            MefTestHelpers.CreateExport<IRoslynAnalyzerLoader>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalyzerProvider>();

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_NoAnalyzers_ReturnsEmptyDictionary()
    {
        analyzersLocator.GetAnalyzerFullPathsByLicensedLanguage().Returns(new Dictionary<LicensedRoslynLanguage, List<string>>());

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_WithAnalyzers_LoadsAnalyzersAndReturnsCorrectDictionary()
    {
        analyzersLocator.GetAnalyzerFullPathsByLicensedLanguage()
            .Returns(new Dictionary<LicensedRoslynLanguage, List<string>> { { new(Language.CSharp, false), [CsharpAnalyzerPath] }, { new(Language.VBNET, false), [VbAnalyzerPath] } });
        var csharpAnalyzer = CreateAnalyzerWithDiagnostic("CS0001");
        var vbAnalyzer = CreateAnalyzerWithDiagnostic("VB0001");
        var csharpCodeFixer = CreateCodeFixProviderWithDiagnostics("CS0001");
        var vbCodeFixer = CreateCodeFixProviderWithDiagnostics("VB0001");
        roslynAnalyzerLoader.LoadAnalyzerAssembly(CsharpAnalyzerPath).Returns(new LoadedAnalyzerClasses([csharpAnalyzer], [csharpCodeFixer]));
        roslynAnalyzerLoader.LoadAnalyzerAssembly(VbAnalyzerPath).Returns(new LoadedAnalyzerClasses([vbAnalyzer], [vbCodeFixer]));

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        result.Keys.Should().BeEquivalentTo(Language.CSharp, Language.VBNET);
        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(csharpAnalyzer);
        result[Language.CSharp].SupportedRuleKeys.Should().BeEquivalentTo("CS0001");
        result[Language.CSharp].CodeFixProvidersByRuleKey.Should().BeEquivalentTo(
            new Dictionary<string, IReadOnlyCollection<CodeFixProvider>> { { "CS0001", [csharpCodeFixer] } });

        result[Language.VBNET].Analyzers.Should().BeEquivalentTo(vbAnalyzer);
        result[Language.VBNET].SupportedRuleKeys.Should().BeEquivalentTo("VB0001");
        result[Language.VBNET].CodeFixProvidersByRuleKey.Should().BeEquivalentTo(
            new Dictionary<string, IReadOnlyCollection<CodeFixProvider>> { { "VB0001", [vbCodeFixer] } });
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_IgnoresDuplicateIdsForTheSameLanguage()
    {
        analyzersLocator.GetAnalyzerFullPathsByLicensedLanguage()
            .Returns(new Dictionary<LicensedRoslynLanguage, List<string>> { { new(Language.CSharp, false), [CsharpAnalyzerPath] }, { new(Language.VBNET, false), [VbAnalyzerPath] } });
        var csharpAnalyzer1 = CreateAnalyzerWithDiagnostic("S001", "SDUPLICATE");
        var csharpAnalyzer2 = CreateAnalyzerWithDiagnostic("S002", "SDUPLICATE");
        var vbAnalyzer = CreateAnalyzerWithDiagnostic("S001", "S002");
        roslynAnalyzerLoader.LoadAnalyzerAssembly(CsharpAnalyzerPath).Returns(new LoadedAnalyzerClasses([csharpAnalyzer1, csharpAnalyzer2], []));
        roslynAnalyzerLoader.LoadAnalyzerAssembly(VbAnalyzerPath).Returns(new LoadedAnalyzerClasses([vbAnalyzer], []));

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        result.Keys.Should().BeEquivalentTo(Language.CSharp, Language.VBNET);
        result[Language.CSharp].SupportedRuleKeys.Should().BeEquivalentTo("S001", "SDUPLICATE", "S002");
        result[Language.VBNET].SupportedRuleKeys.Should().BeEquivalentTo("S001", "S002");
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_MultipleAnalyzersPerLanguage_CombinesAllRules()
    {
        const string csharpAnalyzerPath2 = "c:\\analyzers\\csharp2.dll";
        analyzersLocator.GetAnalyzerFullPathsByLicensedLanguage()
            .Returns(new Dictionary<LicensedRoslynLanguage, List<string>> { { new(Language.CSharp, false), [CsharpAnalyzerPath, csharpAnalyzerPath2] } });
        var csharpAnalyzer1 = CreateAnalyzerWithDiagnostic("S001");
        var csharpAnalyzer2 = CreateAnalyzerWithDiagnostic("S002", "S003");
        var csharpAnalyzer3 = CreateAnalyzerWithDiagnostic("S004");
        roslynAnalyzerLoader.LoadAnalyzerAssembly(CsharpAnalyzerPath).Returns(new LoadedAnalyzerClasses([csharpAnalyzer1], []));
        roslynAnalyzerLoader.LoadAnalyzerAssembly(csharpAnalyzerPath2).Returns(new LoadedAnalyzerClasses([csharpAnalyzer2, csharpAnalyzer3], []));

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        result.Keys.Should().BeEquivalentTo(Language.CSharp);
        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(csharpAnalyzer1, csharpAnalyzer2, csharpAnalyzer3);
        result[Language.CSharp].SupportedRuleKeys.Should().BeEquivalentTo("S001", "S002", "S003", "S004");
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_NoCodeFixProviders_ReturnsEmptyMap()
    {
        var csharpAnalyzer = CreateAnalyzerWithDiagnostic("S001");
        MockCodeProvidersForCsharp(csharpAnalyzer, []);

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        result[Language.CSharp].CodeFixProvidersByRuleKey.Should().BeEmpty();
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_CodeFixProviderWithMultipleDiagnostics_AddedToAllMappings()
    {
        var csharpAnalyzer = CreateAnalyzerWithDiagnostic("S001", "S002", "S003");
        var codeFixProvider = CreateCodeFixProviderWithDiagnostics("S001", "S002");
        MockCodeProvidersForCsharp(csharpAnalyzer, codeFixProvider);

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        result[Language.CSharp].CodeFixProvidersByRuleKey.Should().BeEquivalentTo(
            new Dictionary<string, IReadOnlyCollection<CodeFixProvider>> { { "S001", [codeFixProvider] }, { "S002", [codeFixProvider] } });
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_MultipleCodeFixProvidersForSameId_AllAddedToSameCollection()
    {
        var csharpAnalyzer = CreateAnalyzerWithDiagnostic("S001");
        var codeFixProvider1 = CreateCodeFixProviderWithDiagnostics("S001");
        var codeFixProvider2 = CreateCodeFixProviderWithDiagnostics("S001");
        MockCodeProvidersForCsharp(csharpAnalyzer, codeFixProvider1, codeFixProvider2);

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        result[Language.CSharp].CodeFixProvidersByRuleKey.Should().BeEquivalentTo(
            new Dictionary<string, IReadOnlyCollection<CodeFixProvider>> { { "S001", [codeFixProvider1, codeFixProvider2] } });
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_BothEnterprise_ReturnsEnterpriseDlls()
    {
        MockAnalyzerFullPathsByLicensedLanguage();
        var analyzerInfo = new AnalyzerInfoDto(ShouldUseCsharpEnterprise: true, ShouldUseVbEnterprise: true);

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(analyzerInfo);

        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { CsharpEnterpriseAnalyzer });
        result[Language.VBNET].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { VbEnterpriseAnalyzer });
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_BothBasic_ReturnsBasicDlls()
    {
        MockAnalyzerFullPathsByLicensedLanguage();
        var analyzerInfo = new AnalyzerInfoDto(ShouldUseCsharpEnterprise: false, ShouldUseVbEnterprise: false);

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(analyzerInfo);

        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { CsharpAnalyzer });
        result[Language.VBNET].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { VbAnalyzer });
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_OnlyCsharpEnterprise_ReturnsCsharpEnterpriseAndVbBasic()
    {
        MockAnalyzerFullPathsByLicensedLanguage();
        var analyzerInfo = new AnalyzerInfoDto(ShouldUseCsharpEnterprise: true, ShouldUseVbEnterprise: false);

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(analyzerInfo);

        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { CsharpEnterpriseAnalyzer });
        result[Language.VBNET].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { VbAnalyzer });
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_OnlyVbEnterprise_ReturnsVbEnterpriseAndCsharpBasic()
    {
        MockAnalyzerFullPathsByLicensedLanguage();
        var analyzerInfo = new AnalyzerInfoDto(ShouldUseCsharpEnterprise: false, ShouldUseVbEnterprise: true);

        var result = testSubject.LoadAndProcessAnalyzerAssemblies(analyzerInfo);

        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { CsharpAnalyzer });
        result[Language.VBNET].Analyzers.Should().BeEquivalentTo(new List<DiagnosticAnalyzer> { VbEnterpriseAnalyzer });
    }

    [TestMethod]
    public void LoadAndProcessAnalyzerAssemblies_MultipleCalls_AnalyzerAssemblyContentsAreCached()
    {
        MockCodeProvidersForCsharp(CsharpAnalyzer);

        testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);
        testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);
        testSubject.LoadAndProcessAnalyzerAssemblies(DefaultAnalyzerInfoDto);

        analyzersLocator.Received(1).GetAnalyzerFullPathsByLicensedLanguage();
        roslynAnalyzerLoader.Received(1).LoadAnalyzerAssembly(Arg.Any<string>());
    }

    [TestMethod]
    public void LoadRoslynAnalyzerAssemblyContentsIfNeeded_MultipleCalls_AnalyzerAssemblyContentsAreCached()
    {
        MockCodeProvidersForCsharp(CsharpAnalyzer);

        testSubject.LoadRoslynAnalyzerAssemblyContentsIfNeeded();
        testSubject.LoadRoslynAnalyzerAssemblyContentsIfNeeded();
        testSubject.LoadRoslynAnalyzerAssemblyContentsIfNeeded();

        analyzersLocator.Received(1).GetAnalyzerFullPathsByLicensedLanguage();
        roslynAnalyzerLoader.Received(1).LoadAnalyzerAssembly(Arg.Any<string>());
    }

    private static DiagnosticAnalyzer CreateAnalyzerWithDiagnostic(params string[] diagnosticIds)
    {
        var analyzer = Substitute.For<DiagnosticAnalyzer>();
        analyzer.SupportedDiagnostics.Returns(diagnosticIds.Select(CreateDiagnosticDescriptor).ToImmutableArray());
        return analyzer;
    }

    private static CodeFixProvider CreateCodeFixProviderWithDiagnostics(params string[] diagnosticIds)
    {
        var codeFixProvider = Substitute.For<CodeFixProvider>();
        codeFixProvider.FixableDiagnosticIds.Returns(diagnosticIds.ToImmutableArray());
        return codeFixProvider;
    }

    private static DiagnosticDescriptor CreateDiagnosticDescriptor(string id) =>
        new(
            id,
            "any title",
            "any message",
            "any category",
            DiagnosticSeverity.Warning,
            true);

    private void MockCodeProvidersForCsharp(DiagnosticAnalyzer csharpAnalyzer, params CodeFixProvider[] codeFixProviders)
    {
        analyzersLocator.GetAnalyzerFullPathsByLicensedLanguage()
            .Returns(new Dictionary<LicensedRoslynLanguage, List<string>> { { new(Language.CSharp, false), [CsharpAnalyzerPath] } });
        roslynAnalyzerLoader.LoadAnalyzerAssembly(CsharpAnalyzerPath).Returns(new LoadedAnalyzerClasses([csharpAnalyzer], codeFixProviders));
    }

    private void MockAnalyzerFullPathsByLicensedLanguage()
    {
        roslynAnalyzerLoader.LoadAnalyzerAssembly(CsharpAnalyzerPath).Returns(new LoadedAnalyzerClasses([CsharpAnalyzer], []));
        roslynAnalyzerLoader.LoadAnalyzerAssembly(CsharpEnterpriseAnalyzerPath).Returns(new LoadedAnalyzerClasses([CsharpEnterpriseAnalyzer], []));
        roslynAnalyzerLoader.LoadAnalyzerAssembly(VbAnalyzerPath).Returns(new LoadedAnalyzerClasses([VbAnalyzer], []));
        roslynAnalyzerLoader.LoadAnalyzerAssembly(VbEnterpriseAnalyzerPath).Returns(new LoadedAnalyzerClasses([VbEnterpriseAnalyzer], []));
        analyzersLocator.GetAnalyzerFullPathsByLicensedLanguage()
            .Returns(new Dictionary<LicensedRoslynLanguage, List<string>>
            {
                { new(Language.CSharp, false), [CsharpAnalyzerPath] },
                { new(Language.CSharp, true), [CsharpEnterpriseAnalyzerPath] },
                { new(Language.VBNET, false), [VbAnalyzerPath] },
                { new(Language.VBNET, true), [VbEnterpriseAnalyzerPath] },
            });
    }
}
