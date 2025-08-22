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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis.Configuration;

[TestClass]
public class RoslynAnalyzerProviderTests
{
    private const string CsharpAnalyzerPath = "c:\\analyzers\\csharp.dll";
    private const string VbAnalyzerPath = "c:\\analyzers\\vb.dll";

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
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalyzerProvider, IRoslynAnalyzerProvider>(
            MefTestHelpers.CreateExport<IEmbeddedDotnetAnalyzersLocator>(),
            MefTestHelpers.CreateExport<IRoslynAnalyzerLoader>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalyzerProvider>();

    [TestMethod]
    public void GetAnalyzersByLanguage_NoAnalyzers_ReturnsEmptyDictionary()
    {
        analyzersLocator.GetBasicAnalyzerFullPathsByLanguage().Returns(new Dictionary<RoslynLanguage, List<string>>());

        var result = testSubject.LoadAnalyzerAssemblies();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetAnalyzersByLanguage_WithAnalyzers_LoadsAnalyzersAndReturnsCorrectDictionary()
    {
        analyzersLocator.GetBasicAnalyzerFullPathsByLanguage().Returns(new Dictionary<RoslynLanguage, List<string>> { { Language.CSharp, [CsharpAnalyzerPath] }, { Language.VBNET, [VbAnalyzerPath] } });
        var csharpAnalyzer = CreateAnalyzerWithDiagnostic("CS0001");
        var vbAnalyzer = CreateAnalyzerWithDiagnostic("VB0001");
        roslynAnalyzerLoader.LoadAnalyzers(CsharpAnalyzerPath).Returns([csharpAnalyzer]);
        roslynAnalyzerLoader.LoadAnalyzers(VbAnalyzerPath).Returns([vbAnalyzer]);

        var result = testSubject.LoadAnalyzerAssemblies();

        result.Keys.Should().BeEquivalentTo(Language.CSharp, Language.VBNET);
        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(csharpAnalyzer);
        result[Language.CSharp].SupportedRuleKeys.Should().BeEquivalentTo("CS0001");
        result[Language.VBNET].Analyzers.Should().BeEquivalentTo(vbAnalyzer);
        result[Language.VBNET].SupportedRuleKeys.Should().BeEquivalentTo("VB0001");
    }

    [TestMethod]
    public void GetAnalyzersByLanguage_IgnoresDuplicateIdsForTheSameLanguage()
    {
        analyzersLocator.GetBasicAnalyzerFullPathsByLanguage().Returns(new Dictionary<RoslynLanguage, List<string>> { { Language.CSharp, [CsharpAnalyzerPath] }, { Language.VBNET, [VbAnalyzerPath] } });
        var csharpAnalyzer1 = CreateAnalyzerWithDiagnostic("S001", "SDUPLICATE");
        var csharpAnalyzer2 = CreateAnalyzerWithDiagnostic("S002", "SDUPLICATE");
        var vbAnalyzer = CreateAnalyzerWithDiagnostic("S001", "S002");
        roslynAnalyzerLoader.LoadAnalyzers(CsharpAnalyzerPath).Returns([csharpAnalyzer1, csharpAnalyzer2]);
        roslynAnalyzerLoader.LoadAnalyzers(VbAnalyzerPath).Returns([vbAnalyzer]);

        var result = testSubject.LoadAnalyzerAssemblies();

        result.Keys.Should().BeEquivalentTo(Language.CSharp, Language.VBNET);
        result[Language.CSharp].SupportedRuleKeys.Should().BeEquivalentTo("S001", "SDUPLICATE", "S002");
        result[Language.VBNET].SupportedRuleKeys.Should().BeEquivalentTo("S001", "S002");
    }

    [TestMethod]
    public void GetAnalyzersByLanguage_MultipleAnalyzersPerLanguage_CombinesAllRules()
    {
        const string csharpAnalyzerPath2 = "c:\\analyzers\\csharp2.dll";
        analyzersLocator.GetBasicAnalyzerFullPathsByLanguage().Returns(new Dictionary<RoslynLanguage, List<string>> { { Language.CSharp, [CsharpAnalyzerPath, csharpAnalyzerPath2] } });
        var csharpAnalyzer1 = CreateAnalyzerWithDiagnostic("S001");
        var csharpAnalyzer2 = CreateAnalyzerWithDiagnostic("S002", "S003");
        var csharpAnalyzer3 = CreateAnalyzerWithDiagnostic("S004");
        roslynAnalyzerLoader.LoadAnalyzers(CsharpAnalyzerPath).Returns([csharpAnalyzer1]);
        roslynAnalyzerLoader.LoadAnalyzers(csharpAnalyzerPath2).Returns([csharpAnalyzer2, csharpAnalyzer3]);

        var result = testSubject.LoadAnalyzerAssemblies();

        result.Keys.Should().BeEquivalentTo(Language.CSharp);
        result[Language.CSharp].Analyzers.Should().BeEquivalentTo(csharpAnalyzer1, csharpAnalyzer2, csharpAnalyzer3);
        result[Language.CSharp].SupportedRuleKeys.Should().BeEquivalentTo("S001", "S002", "S003", "S004");
    }

    private static DiagnosticAnalyzer CreateAnalyzerWithDiagnostic(params string[] diagnosticIds)
    {
        var analyzer = Substitute.For<DiagnosticAnalyzer>();
        analyzer.SupportedDiagnostics.Returns(diagnosticIds.Select(CreateDiagnosticDescriptor).ToImmutableArray());
        return analyzer;
    }

    private static DiagnosticDescriptor CreateDiagnosticDescriptor(string id) =>
        new(
            id,
            "any title",
            "any message",
            "any category",
            DiagnosticSeverity.Warning,
            true);
}
