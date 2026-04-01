/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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
    private IRoslynCompilationWithAnalyzersWrapper additionalCompilationWithAnalyzers = null!;
    private ImmutableDictionary<RoslynLanguage, RoslynAnalysisConfiguration> configurations = null!;
    private RoslynAnalysisConfiguration additionalConfiguration;
    private ImmutableDictionary<RoslynLanguage, RoslynAnalysisConfiguration> additionalConfigurations = null!;
    private ImmutableDictionary<string, ReportDiagnostic> diagnosticOptions = null!;
    private AdditionalText existingAdditionalFile = null!;
    private TestLogger logger = null!;
    private IRoslynProjectWrapper project = null!;
    private SonarLintXmlConfigurationFile sonarLintXml = null!;
    private ImmutableHashSet<string> targetFilePaths = null!;
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
            .Add("SomeId", ReportDiagnostic.Warn)
            .Add("SomeOtherId", ReportDiagnostic.Error);
        configurations = ImmutableDictionary<RoslynLanguage, RoslynAnalysisConfiguration>.Empty
            .Add(Language.CSharp, new RoslynAnalysisConfiguration(
                sonarLintXml,
                diagnosticOptions,
                analyzers,
                codeFixProviders));
        SetUpCompilationWithAnalyzers();
        SetUpAdditionalConfiguration();
        targetFilePaths = ImmutableHashSet.Create<string>("file1.cs", "file2.cs");
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
        var result = await testSubject.GetProjectCompilationAsync(CreateScope(), configurations, CancellationToken.None);

        result.Should().Be(compilationWithAnalyzers);
        compilation.Received(1).WithOptions(Arg.Is<CompilationOptions>(options =>
            options.SpecificDiagnosticOptions == diagnosticOptions
            && options.SyntaxTreeOptionsProvider is TreeOptionsProvider));
        compilation.Received(1).WithAnalyzers(
            Arg.Is<ImmutableArray<DiagnosticAnalyzer>>(analyzersArg =>
                analyzersArg.SequenceEqual(analyzers, null as IEqualityComparer<DiagnosticAnalyzer>)),
            Arg.Is<CompilationWithAnalyzersOptions>(options =>
                options.Options != null
                && options.Options.AdditionalFiles.SequenceEqual(ImmutableArray.Create(existingAdditionalFile, sonarLintXml), null as IEqualityComparer<AdditionalText>)
                && options.ConcurrentAnalysis == true
                && options.ReportSuppressedDiagnostics == true
                && options.LogAnalyzerExecutionTime == false),
            configurations[Language.CSharp]);
    }

    [TestMethod]
    public async Task GetProjectCompilationAsync_ProjectHasOverridesForDiagnosticOptions_OverridesQualityProfileWithProjectSettings()
    {
        project.SpecificDiagnosticOptions.Returns(ImmutableDictionary.Create<string, ReportDiagnostic>().Add("SomeId", ReportDiagnostic.Suppress));

        var result = await testSubject.GetProjectCompilationAsync(CreateScope(), configurations, CancellationToken.None);

        result.Should().Be(compilationWithAnalyzers);
        compilation.Received(1).WithOptions(Arg.Is<CompilationOptions>(options =>
            options.SpecificDiagnosticOptions["SomeId"] == ReportDiagnostic.Suppress && options.SpecificDiagnosticOptions["SomeOtherId"] == ReportDiagnostic.Error));
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

        await testSubject.GetProjectCompilationAsync(CreateScope(), configurations, CancellationToken.None);

        compilation.Received(1).WithAnalyzers(
            Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(),
            Arg.Is<CompilationWithAnalyzersOptions>(options =>
                options.Options != null
                && options.Options.AdditionalFiles.SequenceEqual(ImmutableArray.Create(existingAdditionalFile, sonarLintXml), null as IEqualityComparer<AdditionalText>)
                && options.ConcurrentAnalysis == true
                && options.ReportSuppressedDiagnostics == true
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
        await testSubject.GetProjectCompilationAsync(CreateScope(), configurations, CancellationToken.None);
        capturedOptions.Should().NotBeNull();
        var exception = new InvalidOperationException("test exception");
        var diagnostic = Diagnostic.Create("TestId", "TestCategory", "TestMessage", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1);

        capturedOptions.OnAnalyzerException!(exception, analyzer1, diagnostic);

        logger.AssertPartialOutputStringExists(
            analyzer1.GetType().Name,
            "TestId",
            "test exception");
    }

    [TestMethod]
    public async Task GetProjectCompilationsAsync_AdditionalConfigExists_ReturnsBothCompilations()
    {
        var result = await testSubject.GetProjectCompilationsAsync(CreateScope(), configurations, additionalConfigurations, CancellationToken.None);

        result.mainCompilation.Should().Be(compilationWithAnalyzers);
        result.additionalCompilation.Should().Be(additionalCompilationWithAnalyzers);
    }

    [TestMethod]
    public async Task GetProjectCompilationsAsync_NoAdditionalConfig_ReturnsNullAdditionalCompilation()
    {
        var emptyAdditionalConfigs = ImmutableDictionary<RoslynLanguage, RoslynAnalysisConfiguration>.Empty;

        var result = await testSubject.GetProjectCompilationsAsync(CreateScope(), configurations, emptyAdditionalConfigs, CancellationToken.None);

        result.mainCompilation.Should().Be(compilationWithAnalyzers);
        result.additionalCompilation.Should().BeNull();
    }

    [TestMethod]
    public async Task GetProjectCompilationsAsync_MainCompilation_UsesMainConfiguration()
    {
        await testSubject.GetProjectCompilationsAsync(CreateScope(), configurations, additionalConfigurations, CancellationToken.None);

        compilation.Received().WithAnalyzers(
            Arg.Is<ImmutableArray<DiagnosticAnalyzer>>(a =>
                a.SequenceEqual(analyzers, null as IEqualityComparer<DiagnosticAnalyzer>)),
            Arg.Any<CompilationWithAnalyzersOptions>(),
            configurations[Language.CSharp]);
    }

    [TestMethod]
    public async Task GetProjectCompilationsAsync_AdditionalCompilation_UsesAdditionalConfiguration()
    {
        await testSubject.GetProjectCompilationsAsync(CreateScope(), configurations, additionalConfigurations, CancellationToken.None);

        compilation.Received().WithAnalyzers(
            Arg.Is<ImmutableArray<DiagnosticAnalyzer>>(a =>
                a.SequenceEqual(additionalConfiguration.Analyzers, null as IEqualityComparer<DiagnosticAnalyzer>)),
            Arg.Any<CompilationWithAnalyzersOptions>(),
            additionalConfiguration);
    }

    private void SetUpAdditionalConfiguration()
    {
        var additionalAnalyzer = Substitute.For<DiagnosticAnalyzer>();
        var additionalAnalyzersSet = ImmutableArray.Create(additionalAnalyzer);
        var additionalSonarLintXml = new SonarLintXmlConfigurationFile(@"C:\additional", "additional content");
        additionalConfiguration = new RoslynAnalysisConfiguration(
            additionalSonarLintXml,
            ImmutableDictionary<string, ReportDiagnostic>.Empty,
            additionalAnalyzersSet,
            ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>>.Empty);
        additionalConfigurations = ImmutableDictionary<RoslynLanguage, RoslynAnalysisConfiguration>.Empty
            .Add(Language.CSharp, additionalConfiguration);
        additionalCompilationWithAnalyzers = Substitute.For<IRoslynCompilationWithAnalyzersWrapper>();
        compilation.WithAnalyzers(Arg.Any<ImmutableArray<DiagnosticAnalyzer>>(), Arg.Any<CompilationWithAnalyzersOptions>(), additionalConfiguration)
            .Returns(additionalCompilationWithAnalyzers);
    }

    private ProjectAnalysisRequestScope CreateScope() => new(project, targetFilePaths);

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
