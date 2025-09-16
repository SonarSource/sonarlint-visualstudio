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
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class RoslynProjectCompilationProviderTests
{
    private DiagnosticAnalyzer analyzer1 = null!;
    private DiagnosticAnalyzer analyzer2 = null!;
    private DiagnosticAnalyzer analyzer3 = null!;
    private AnalyzerOptions analyzerOptions = null!;
    private ImmutableArray<DiagnosticAnalyzer> analyzers;
    private ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>> codeFixProviders = null!;
    private IRoslynCompilationWrapper compilation = null!;
    private CompilationOptions compilationOptions = null!;
    private IRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers = null!;
    private ImmutableDictionary<RoslynLanguage, RoslynAnalysisConfiguration> configurations = null!;
    private ImmutableDictionary<string, ReportDiagnostic> diagnosticOptions = null!;
    private AdditionalText existingAdditionalFile = null!;
    private TestLogger logger = null!;
    private IRoslynProjectWrapper project = null!;
    private SonarLintXmlConfigurationFile sonarLintXml = null!;
    private RoslynProjectCompilationProvider testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.ForPartsOf<TestLogger>();

        SetUpCompilation();
        SetUpAdditionalFiles();
        SetUpProject();
        SetUpAnalyzers();
        SetUpCodeFixProviders();
        diagnosticOptions = ImmutableDictionary<string, ReportDiagnostic>.Empty
            .Add("SomeId", ReportDiagnostic.Warn);
        configurations = ImmutableDictionary<RoslynLanguage, RoslynAnalysisConfiguration>.Empty
            .Add(Language.CSharp, new RoslynAnalysisConfiguration(
                sonarLintXml,
                diagnosticOptions,
                analyzers,
                codeFixProviders));
        SetUpCompilationWithAnalyzers();
        testSubject = new RoslynProjectCompilationProvider(logger);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynProjectCompilationProvider, IRoslynProjectCompilationProvider>(
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynProjectCompilationProvider>();

    [TestMethod]
    public void Ctor_SetsLogContext() =>
        logger.Received(1).ForContext(Resources.RoslynLogContext, Resources.RoslynAnalysisLogContext, Resources.RoslynAnalysisAnalyzerExceptionLogContext);

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
                && options.LogAnalyzerExecutionTime == false),
            configurations[Language.CSharp]);
    }

    [TestMethod]
    public async Task GetProjectCompilationAsync_RemovesExistingSonarLintXml()
    {
        var existingSonarLintXml = Substitute.For<AdditionalText>();
        existingSonarLintXml.Path.Returns(@"c:\some\other\path\SonarLint.xml");
        var analyzerOptionsWithSonarLintXml = new AnalyzerOptions(
            ImmutableArray.Create(existingAdditionalFile, existingSonarLintXml));
        project.RoslynAnalyzerOptions.Returns(analyzerOptionsWithSonarLintXml);
        compilation.WithAnalyzers(Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(), Arg.Any<CompilationWithAnalyzersOptions>(), configurations[Language.CSharp])
            .Returns(compilationWithAnalyzers);

        await testSubject.GetProjectCompilationAsync(project, configurations, CancellationToken.None);

        compilation.Received(1).WithAnalyzers(
            Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(),
            Arg.Is<CompilationWithAnalyzersOptions>(options =>
                options.Options != null
                && options.Options.AdditionalFiles.SequenceEqual(ImmutableArray.Create(existingAdditionalFile, sonarLintXml), null as IEqualityComparer<AdditionalText>)
                && options.ConcurrentAnalysis == true
                && options.ReportSuppressedDiagnostics == false
                && options.LogAnalyzerExecutionTime == false),
            configurations[Language.CSharp]);
    }

    [TestMethod]
    public async Task GetProjectCompilationAsync_AnalyzerException_LogsError()
    {
        CompilationWithAnalyzersOptions capturedOptions = null!;
        compilation.WithAnalyzers(
                Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(),
                Arg.Do<CompilationWithAnalyzersOptions>(x => capturedOptions = x), configurations[Language.CSharp])
            .Returns(compilationWithAnalyzers);
        await testSubject.GetProjectCompilationAsync(project, configurations, CancellationToken.None);
        capturedOptions.Should().NotBeNull();
        var exception = new InvalidOperationException("test exception");
        var diagnostic = Diagnostic.Create("TestId", "TestCategory", "TestMessage", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1);

        capturedOptions.OnAnalyzerException!(exception, analyzer1, diagnostic);

        logger.AssertPartialOutputStringExists(
            analyzer1.GetType().Name,
            "TestId",
            "test exception");
    }

    private void SetUpAnalyzers()
    {
        analyzer1 = Substitute.For<DiagnosticAnalyzer>();
        analyzer2 = Substitute.For<DiagnosticAnalyzer>();
        analyzer3 = Substitute.For<DiagnosticAnalyzer>();
        analyzers = ImmutableArray.Create(analyzer1, analyzer2, analyzer3);
    }

    private void SetUpCodeFixProviders()
    {
        codeFixProviders = ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>>.Empty.Add("1", [Substitute.For<CodeFixProvider>()]);
    }

    private void SetUpProject()
    {
        project = Substitute.For<IRoslynProjectWrapper>();
        analyzerOptions = new AnalyzerOptions(ImmutableArray.Create(existingAdditionalFile));
        project.RoslynAnalyzerOptions.Returns(analyzerOptions);
        project.GetCompilationAsync(Arg.Any<CancellationToken>()).Returns(compilation);
    }

    private void SetUpAdditionalFiles()
    {
        sonarLintXml = new SonarLintXmlConfigurationFile(@"C:\B\A", "content");

        existingAdditionalFile = Substitute.For<AdditionalText>();
        existingAdditionalFile.Path.Returns(@"c:\path\to\existing.txt");
    }

    private void SetUpCompilation()
    {
        compilation = Substitute.For<IRoslynCompilationWrapper>();
        compilationOptions = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
        compilation.RoslynCompilationOptions.Returns(compilationOptions);
        compilationWithAnalyzers = Substitute.For<IRoslynCompilationWithAnalyzersWrapper>();
        compilation.Language.Returns(Language.CSharp);
        compilation.WithOptions(Arg.Any<CompilationOptions>()).Returns(compilation);
    }

    private void SetUpCompilationWithAnalyzers() =>
        compilation.WithAnalyzers(Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(), Arg.Any<CompilationWithAnalyzersOptions>(), configurations[Language.CSharp])
            .Returns(compilationWithAnalyzers);
}
