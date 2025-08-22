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
using Microsoft.CodeAnalysis.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.TestInfrastructure;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests.Analysis;

[TestClass]
public class SequentialRoslynAnalysisEngineTests
{
    private IDiagnosticToRoslynIssueConverter issueConverter = null!;
    private IRoslynProjectCompilationProvider projectCompilationProvider = null!;
    private TestLogger logger = null!;
    private ImmutableDictionary<Language, RoslynAnalysisConfiguration> configurations = null!;
    private CancellationToken cancellationToken;
    private SequentialRoslynAnalysisEngine testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        issueConverter = Substitute.For<IDiagnosticToRoslynIssueConverter>();
        projectCompilationProvider = Substitute.For<IRoslynProjectCompilationProvider>();
        logger = Substitute.ForPartsOf<TestLogger>();

        testSubject = new SequentialRoslynAnalysisEngine(issueConverter, projectCompilationProvider, logger);

        configurations = ImmutableDictionary.Create<Language, RoslynAnalysisConfiguration>();
        cancellationToken = new CancellationToken();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SequentialRoslynAnalysisEngine, IRoslynAnalysisEngine>(
            MefTestHelpers.CreateExport<IDiagnosticToRoslynIssueConverter>(),
            MefTestHelpers.CreateExport<IRoslynProjectCompilationProvider>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SequentialRoslynAnalysisEngine>();

    [TestMethod]
    public async Task AnalyzeAsync_EmptyAnalysisCommands_ReturnsEmptyCollection()
    {
        var result = await testSubject.AnalyzeAsync([], configurations, cancellationToken);

        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_SingleProjectWithNoAnalysisCommands_ReturnsEmptyCollection()
    {
        var (project, _) = SetupProjectAnalysisRequestAndCompilation();

        var result = await testSubject.AnalyzeAsync([CreateProjectRequest(project)], configurations, cancellationToken);

        result.Should().BeEmpty();
        await projectCompilationProvider.Received(1).GetProjectCompilationAsync(project, configurations, cancellationToken);
    }

    public static object[][] RoslynLanguages =>
    [
        [Language.CSharp],
        [Language.VBNET]
    ];

    [DataTestMethod]
    [DynamicData(nameof(RoslynLanguages))]
    public async Task AnalyzeAsync_SingleProjectWithSingleAnalysisCommand_ReturnsDiagnostics(Language language)
    {
        var (diagnostic, roslynIssue) = SetUpDiagnosticAndConvertedModel("test-rule", "test message");
        var (requestForProject, compilationForProject) = SetupProjectAnalysisRequestAndCompilation([[diagnostic]]);
        compilationForProject.Language.Returns(language);

        var result = await testSubject.AnalyzeAsync([requestForProject], configurations, cancellationToken);

        result.Should().BeEquivalentTo(roslynIssue);
        VerifyAnalysisExecution(requestForProject, compilationForProject, [diagnostic], language);
    }

    [TestMethod]
    public async Task AnalyzeAsync_DuplicateDiagnostics_ReturnsSingleDiagnostic()
    {
        var (duplicateDiagnostic1, duplicateIssue1) = SetUpDiagnosticAndConvertedModel("test-rule", "test message");
        var (duplicateDiagnostic2, duplicateIssue2) = SetUpDiagnosticAndConvertedModel("test-rule", "test message duplicate");
        var (requestForProject, compilationForProject) = SetupProjectAnalysisRequestAndCompilation([[duplicateDiagnostic1], [duplicateDiagnostic2]]);

        var result = await testSubject.AnalyzeAsync([requestForProject], configurations, cancellationToken);

        result.Should().BeEquivalentTo(duplicateIssue1);
        VerifyAnalysisExecution(requestForProject, compilationForProject, [duplicateDiagnostic1, duplicateDiagnostic2]);
        logger.AssertPartialOutputStringExists(
            $"Duplicate diagnostic discarded ID: {duplicateIssue2.RuleId}, File: {duplicateIssue2.PrimaryLocation.FilePath}, Line: {duplicateIssue2.PrimaryLocation.TextRange.StartLine}");
    }

    [TestMethod]
    public async Task AnalyzeAsync_DuplicateDiagnosticsInDifferentProjects_ReturnsSingleDiagnostic()
    {
        var (diagnostic1, duplicateIssue) = SetUpDiagnosticAndConvertedModel("test-rule", "test message");
        var (requestForProject1, compilationForProject1) = SetupProjectAnalysisRequestAndCompilation([[diagnostic1]]);
        var (diagnostic2, _) = SetUpDiagnosticAndConvertedModel("test-rule-duplicate", "test message duplicate", duplicateIssue);
        var (requestForProject2, compilationForProject2) = SetupProjectAnalysisRequestAndCompilation([[diagnostic2]]);

        var result = await testSubject.AnalyzeAsync([requestForProject1, requestForProject2], configurations, cancellationToken);

        result.Should().BeEquivalentTo(duplicateIssue);
        VerifyAnalysisExecution(requestForProject1, compilationForProject1, [diagnostic1]);
        VerifyAnalysisExecution(requestForProject2, compilationForProject2, [diagnostic2]);
        logger.AssertPartialOutputStringExists(
            $"Duplicate diagnostic discarded ID: {duplicateIssue.RuleId}, File: {duplicateIssue.PrimaryLocation.FilePath}, Line: {duplicateIssue.PrimaryLocation.TextRange.StartLine}");
    }

    [TestMethod]
    public async Task AnalyzeAsync_MultipleProjects_ProcessesAllProjects()
    {
        var (diagnostic1, sonarIssue1) = SetUpDiagnosticAndConvertedModel("rule1", "message1");
        var (requestForProject1, compilationForProject1) = SetupProjectAnalysisRequestAndCompilation([[diagnostic1]]);
        var (diagnostic2, sonarIssue2) = SetUpDiagnosticAndConvertedModel("rule2", "message2");
        var (requestForProject2, compilationForProject2) = SetupProjectAnalysisRequestAndCompilation([[diagnostic2]]);

        var result = await testSubject.AnalyzeAsync([requestForProject1, requestForProject2], configurations, cancellationToken);

        result.Should().BeEquivalentTo([sonarIssue1, sonarIssue2]);
        VerifyAnalysisExecution(requestForProject1, compilationForProject1, [diagnostic1]);
        VerifyAnalysisExecution(requestForProject2, compilationForProject2, [diagnostic2]);
    }

    [TestMethod]
    public async Task AnalyzeAsync_SingleProjectWithMultipleCommands_ReturnsAllDiagnostics()
    {
        var (diagnostic1A, sonarIssue1A) = SetUpDiagnosticAndConvertedModel("rule1", "message1");
        var (diagnostic1B, sonarIssue1B) = SetUpDiagnosticAndConvertedModel("rule2", "message2");
        var (diagnostic2A, sonarIssue2A) = SetUpDiagnosticAndConvertedModel("rule3", "message3");
        var (diagnostic2B, sonarIssue2B) = SetUpDiagnosticAndConvertedModel("rule4", "message4");
        var (requestForProject, compilationForProject) = SetupProjectAnalysisRequestAndCompilation([[diagnostic1A, diagnostic1B], [diagnostic2A, diagnostic2B]]);

        var result = await testSubject.AnalyzeAsync([requestForProject], configurations, cancellationToken);

        result.Should().BeEquivalentTo(sonarIssue1A, sonarIssue1B, sonarIssue2A, sonarIssue2B);
        VerifyAnalysisExecution(requestForProject, compilationForProject, [diagnostic1A, diagnostic1B, diagnostic2A, diagnostic2B]);
    }

    private (RoslynProjectAnalysisRequest request, IRoslynCompilationWithAnalyzersWrapper compilation) SetupProjectAnalysisRequestAndCompilation(
        Diagnostic[][] diagnosticsPerCommand)
    {
        var (project, projectCompilation) = SetupProjectAnalysisRequestAndCompilation();
        var analysisCommands = diagnosticsPerCommand.Select(x => SetupCommandWithDiagnostics(projectCompilation, x)).ToArray();

        return (new RoslynProjectAnalysisRequest(project, analysisCommands), projectCompilation);
    }

    private RoslynProjectAnalysisRequest CreateProjectRequest(IRoslynProjectWrapper project, params IRoslynAnalysisCommand[] commands) =>
        new(project, commands);

    private (IRoslynProjectWrapper project, IRoslynCompilationWithAnalyzersWrapper projectCompilation) SetupProjectAnalysisRequestAndCompilation()
    {
        var project = Substitute.For<IRoslynProjectWrapper>();
        var compilation = SetupCompilation(project);

        return (project, compilation);
    }

    private IRoslynAnalysisCommand SetupCommandWithDiagnostics(
        IRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        params Diagnostic[] diagnostics)
    {
        var command = Substitute.For<IRoslynAnalysisCommand>();
        command.ExecuteAsync(compilationWithAnalyzers, CancellationToken.None)
            .Returns(ImmutableArray.Create(diagnostics));
        return command;
    }

    private (Diagnostic, RoslynIssue) SetUpDiagnosticAndConvertedModel(
        string ruleId,
        string message,
        RoslynIssue? existingSonarIssue = null)
    {
        var diagnostic = CreateTestDiagnostic(ruleId, message);

        var sonarIssue = existingSonarIssue ?? CreateSonarIssue(ruleId, message);
        issueConverter.ConvertToSonarDiagnostic(diagnostic, Arg.Any<Language>()).Returns(sonarIssue);

        return (diagnostic, sonarIssue);
    }

    private IRoslynCompilationWithAnalyzersWrapper SetupCompilation(IRoslynProjectWrapper project)
    {
        var compilationWithAnalyzers = Substitute.For<IRoslynCompilationWithAnalyzersWrapper>();
        projectCompilationProvider.GetProjectCompilationAsync(project, configurations, cancellationToken)
            .Returns(compilationWithAnalyzers);
        return compilationWithAnalyzers;
    }

    private void VerifyAnalysisExecution(
        RoslynProjectAnalysisRequest projectRequest,
        IRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        Diagnostic[] diagnostics,
        Language? language = null)

    {
        projectCompilationProvider.Received(1)
            .GetProjectCompilationAsync(projectRequest.Project, configurations, cancellationToken).IgnoreAwaitForAssert();
        foreach (var analysisCommand in projectRequest.AnalysisCommands)
        {
            analysisCommand.Received(1).ExecuteAsync(compilationWithAnalyzers, cancellationToken).IgnoreAwaitForAssert();
        }
        foreach (var diagnostic in diagnostics)
        {
            issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic, language ?? Arg.Any<Language>());
        }
    }

    private static Diagnostic CreateTestDiagnostic(string id, string message)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "title",
            message,
            "category",
            DiagnosticSeverity.Warning,
            true);

        var location = Location.Create(
            "test.cs",
            new TextSpan(0, 1),
            new LinePositionSpan(new LinePosition(0, 0), new LinePosition(0, 1)));

        return Diagnostic.Create(descriptor, location);
    }

    private static RoslynIssue CreateSonarIssue(string ruleId, string message)
    {
        var textRange = new RoslynIssueTextRange(1, 1, 0, 1);
        var location = new RoslynIssueLocation(message, "test.cs", textRange);
        return new RoslynIssue(ruleId, location);
    }
}
