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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class SonarRoslynProjectCompilationProviderTests
{
    private DiagnosticAnalyzer analyzer1 = null!;
    private DiagnosticAnalyzer analyzer2 = null!;
    private DiagnosticAnalyzer analyzer3 = null!;
    private AnalyzerOptions analyzerOptions = null!;
    private ImmutableArray<DiagnosticAnalyzer> analyzers;
    private ISonarRoslynCompilationWrapper compilation = null!;
    private CompilationOptions compilationOptions = null!;
    private ISonarRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers = null!;
    private ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> configurations = null!;
    private ImmutableDictionary<string, ReportDiagnostic> diagnosticOptions = null!;
    private AdditionalText existingAdditionalFile = null!;
    private TestLogger logger = null!;
    private ISonarRoslynProjectWrapper project = null!;
    private AdditionalText sonarLintXml = null!;
    private SonarRoslynProjectCompilationProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.ForPartsOf<TestLogger>();

        project = Substitute.For<ISonarRoslynProjectWrapper>();

        compilation = Substitute.For<ISonarRoslynCompilationWrapper>();
        compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
        compilation.RoslynCompilationOptions.Returns(compilationOptions);

        compilationWithAnalyzers = Substitute.For<ISonarRoslynCompilationWithAnalyzersWrapper>();

        sonarLintXml = Substitute.For<AdditionalText>();
        sonarLintXml.Path.Returns(@"c:\path\to\SonarLint.xml");

        existingAdditionalFile = Substitute.For<AdditionalText>();
        existingAdditionalFile.Path.Returns(@"c:\path\to\existing.txt");

        analyzerOptions = new AnalyzerOptions(ImmutableArray.Create(existingAdditionalFile));
        project.RoslynAnalyzerOptions.Returns(analyzerOptions);

        analyzer1 = Substitute.For<DiagnosticAnalyzer>();
        analyzer2 = Substitute.For<DiagnosticAnalyzer>();
        analyzer3 = Substitute.For<DiagnosticAnalyzer>();
        analyzers = ImmutableArray.Create(analyzer1, analyzer2, analyzer3);

        diagnosticOptions = ImmutableDictionary<string, ReportDiagnostic>.Empty
            .Add("SomeId", ReportDiagnostic.Warn);

        var configuration = new SonarRoslynAnalysisConfiguration(
            sonarLintXml,
            diagnosticOptions,
            analyzers);

        configurations = ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration>.Empty
            .Add(Language.CSharp, configuration);

        compilation.Language.Returns(Language.CSharp);
        compilation.WithOptions(Arg.Any<CompilationOptions>()).Returns(compilation);
        compilation.WithAnalyzers(Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(), Arg.Any<CompilationWithAnalyzersOptions>())
            .Returns(compilationWithAnalyzers);
        project.GetCompilationAsync(Arg.Any<CancellationToken>()).Returns(compilation);

        testSubject = new SonarRoslynProjectCompilationProvider(logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SonarRoslynProjectCompilationProvider, ISonarRoslynProjectCompilationProvider>(
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SonarRoslynProjectCompilationProvider>();

    [TestMethod]
    public async Task GetProjectCompilationAsync_ConfiguresCompilationWithCorrectOptions()
    {
        var result = await testSubject.GetProjectCompilationAsync(project, configurations, CancellationToken.None);

        result.Should().Be(compilationWithAnalyzers);
        compilation.Received(1).WithOptions(Arg.Is<CompilationOptions>(options =>
            options.SpecificDiagnosticOptions == diagnosticOptions));
        compilation.Received(1).WithAnalyzers(
            Arg.Is<ImmutableArray<DiagnosticAnalyzer>>(analyzersArg =>
                analyzersArg.SequenceEqual(analyzers, null as IEqualityComparer<DiagnosticAnalyzer>)),
            Arg.Is<CompilationWithAnalyzersOptions>(options =>
                options.Options != null
                && options.Options.AdditionalFiles.SequenceEqual(ImmutableArray.Create(existingAdditionalFile, sonarLintXml), null as IEqualityComparer<AdditionalText>)
                && options.ConcurrentAnalysis == true
                && options.ReportSuppressedDiagnostics == false
                && options.LogAnalyzerExecutionTime == false));
    }

    [TestMethod]
    public async Task GetProjectCompilationAsync_RemovesExistingSonarLintXml()
    {
        var existingSonarLintXml = Substitute.For<AdditionalText>();
        existingSonarLintXml.Path.Returns(@"c:\some\other\path\SonarLint.xml");
        var analyzerOptionsWithSonarLintXml = new AnalyzerOptions(
            ImmutableArray.Create(existingAdditionalFile, existingSonarLintXml));
        project.RoslynAnalyzerOptions.Returns(analyzerOptionsWithSonarLintXml);
        compilation.WithAnalyzers(Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(), Arg.Any<CompilationWithAnalyzersOptions>())
            .Returns(compilationWithAnalyzers);

        await testSubject.GetProjectCompilationAsync(project, configurations, CancellationToken.None);

        compilation.Received(1).WithAnalyzers(
            Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(),
            Arg.Is<CompilationWithAnalyzersOptions>(options =>
                options.Options != null
                && options.Options.AdditionalFiles.SequenceEqual(ImmutableArray.Create(existingAdditionalFile, sonarLintXml), null as IEqualityComparer<AdditionalText>)
                && options.ConcurrentAnalysis == true
                && options.ReportSuppressedDiagnostics == false
                && options.LogAnalyzerExecutionTime == false));
    }

    [TestMethod]
    public async Task GetProjectCompilationAsync_AnalyzerException_LogsError()
    {
        CompilationWithAnalyzersOptions capturedOptions = null!;
        compilation.WithAnalyzers(
                Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(),
                Arg.Do<CompilationWithAnalyzersOptions>(x => capturedOptions = x))
            .Returns(compilationWithAnalyzers);
        await testSubject.GetProjectCompilationAsync(project, configurations, CancellationToken.None);
        capturedOptions.Should().NotBeNull();
        var exception = new InvalidOperationException("test exception");
        var diagnostic = Diagnostic.Create("TestId", "TestCategory", "TestMessage", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1);

        capturedOptions.OnAnalyzerException!(exception, analyzer1, diagnostic);

        logger.AssertPartialOutputStringExists(
            "Roslyn Analyzer Exception",
            analyzer1.GetType().Name,
            "TestId",
            "test exception");
    }
}
