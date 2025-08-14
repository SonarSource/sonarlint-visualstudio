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
        var (project, projectRequest, compilation) = SetupProjectAndAnalysisRequest();

        var result = await testSubject.AnalyzeAsync([projectRequest], configurations, cancellationToken);

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
        var (project, projectRequest, compilation) = SetupProjectAndAnalysisRequest();
        compilation.Language.Returns(language);
        var (diagnostic, roslynIssue) = SetUpDiagnosticAndConvertedModel("test-rule", "test message");
        var command = SetupCommandWithDiagnostic(
            compilation,
            diagnostic);
        AddCommandToProjectRequest(command, projectRequest);

        var result = await testSubject.AnalyzeAsync([projectRequest], configurations, cancellationToken);

        result.Should().BeEquivalentTo(roslynIssue);
        VerifyAnalysisExecution(project, compilation, command, diagnostic, language);
    }

    [TestMethod]
    public async Task AnalyzeAsync_DuplicateDiagnostics_ReturnsSingleDiagnostic()
    {
        var (project, projectRequest, compilation) = SetupProjectAndAnalysisRequest();
        var (diagnostic1, duplicateIssue) = SetUpDiagnosticAndConvertedModel("test-rule", "test message");
        var command1 = SetupCommandWithDiagnostic(compilation, diagnostic1);
        AddCommandToProjectRequest(command1, projectRequest);
        var (diagnostic2, _) = SetUpDiagnosticAndConvertedModel("test-rule-duplicate", "test message duplicate", duplicateIssue);
        var command2 = SetupCommandWithDiagnostic(compilation, diagnostic2);
        AddCommandToProjectRequest(command2, projectRequest);

        var result = await testSubject.AnalyzeAsync([projectRequest], configurations, cancellationToken);

        result.Should().BeEquivalentTo(duplicateIssue);
        VerifyAnalysisExecution(project, compilation, command1, diagnostic1);
        VerifyAnalysisExecution(project, compilation, command2, diagnostic2);
        logger.AssertPartialOutputStringExists(
            $"Duplicate diagnostic discarded ID: {duplicateIssue.RuleKey}, File: {duplicateIssue.PrimaryLocation.FilePath}, Line: {duplicateIssue.PrimaryLocation.TextRange.StartLine}");
    }

    [TestMethod]
    public async Task AnalyzeAsync_MultipleProjects_ProcessesAllProjects()
    {
        var (project1, projectRequest1, compilation1) = SetupProjectAndAnalysisRequest();
        var (diagnostic1, sonarIssue1) = SetUpDiagnosticAndConvertedModel("rule1", "message1");
        var command1 = SetupCommandWithDiagnostic(compilation1, diagnostic1);
        AddCommandToProjectRequest(command1, projectRequest1);
        var (project2, projectRequest2, compilation2) = SetupProjectAndAnalysisRequest();
        var (diagnostic2, sonarIssue2) = SetUpDiagnosticAndConvertedModel("rule2", "message2");
        var command2 = SetupCommandWithDiagnostic(compilation2, diagnostic2);
        AddCommandToProjectRequest(command2, projectRequest2);

        var result = await testSubject.AnalyzeAsync([projectRequest1, projectRequest2], configurations, cancellationToken);

        result.Should().BeEquivalentTo([sonarIssue1, sonarIssue2]);
        VerifyAnalysisExecution(project1, compilation1, command1, diagnostic1);
        VerifyAnalysisExecution(project2, compilation2, command2, diagnostic2);
    }

    [TestMethod]
    public async Task AnalyzeAsync_SingleProjectWithMultipleCommands_ReturnsAllDiagnostics()
    {
        var (project, projectRequest, compilation) = SetupProjectAndAnalysisRequest();
        var command1With2Diagnostics = Substitute.For<IRoslynAnalysisCommand>();
        var (diagnostic1A, sonarIssue1A) = SetUpDiagnosticAndConvertedModel("rule1", "message1");
        var (diagnostic1B, sonarIssue1B) = SetUpDiagnosticAndConvertedModel("rule2", "message2");
        command1With2Diagnostics.ExecuteAsync(compilation, cancellationToken).Returns(ImmutableArray.Create(diagnostic1A, diagnostic1B));
        AddCommandToProjectRequest(command1With2Diagnostics, projectRequest);
        var command2With2Diagnostics = Substitute.For<IRoslynAnalysisCommand>();
        var (diagnostic2A, sonarIssue2A) = SetUpDiagnosticAndConvertedModel("rule3", "message3");
        var (diagnostic2B, sonarIssue2B) = SetUpDiagnosticAndConvertedModel("rule4", "message4");
        command2With2Diagnostics.ExecuteAsync(compilation, cancellationToken).Returns(ImmutableArray.Create(diagnostic2A, diagnostic2B));
        AddCommandToProjectRequest(command2With2Diagnostics, projectRequest);

        var result = await testSubject.AnalyzeAsync([projectRequest], configurations, cancellationToken);

        result.Should().BeEquivalentTo(sonarIssue1A, sonarIssue1B, sonarIssue2A, sonarIssue2B);
        await command1With2Diagnostics.Received(1).ExecuteAsync(compilation, cancellationToken);
        await command2With2Diagnostics.Received(1).ExecuteAsync(compilation, cancellationToken);
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic1A, Arg.Any<Language>());
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic1B, Arg.Any<Language>());
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic2A, Arg.Any<Language>());
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic2B, Arg.Any<Language>());
    }

    private (IRoslynProjectWrapper project, RoslynProjectAnalysisRequest projectRequest, IRoslynCompilationWithAnalyzersWrapper projectCompilation) SetupProjectAndAnalysisRequest()
    {
        var project = Substitute.For<IRoslynProjectWrapper>();
        var projectCommands = new RoslynProjectAnalysisRequest(project, new List<IRoslynAnalysisCommand>());
        var compilation = SetupCompilation(project);

        return (project, projectCommands, compilation);
    }

    private static void AddCommandToProjectRequest(IRoslynAnalysisCommand command, RoslynProjectAnalysisRequest projectRequest) =>
        ((List<IRoslynAnalysisCommand>)projectRequest.AnalysisCommands).Add(command);

    private IRoslynAnalysisCommand SetupCommandWithDiagnostic(
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
        var diagnostic = CreateTestDiagnostic(ruleId);

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
        IRoslynProjectWrapper project,
        IRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        IRoslynAnalysisCommand analysisCommand,
        Diagnostic diagnostic,
        Language? language = null)
    {
        projectCompilationProvider.Received(1)
            .GetProjectCompilationAsync(project, configurations, cancellationToken);
        analysisCommand.Received(1).ExecuteAsync(compilationWithAnalyzers, cancellationToken);
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic, language ?? Arg.Any<Language>());
    }

    private static Diagnostic CreateTestDiagnostic(string id)
    {
        var descriptor = new DiagnosticDescriptor(
            id,
            "title",
            "message",
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
        var textRange = new SonarTextRange(1, 1, 0, 1);
        var location = new SonarDiagnosticLocation(message, "test.cs", textRange);
        return new RoslynIssue(ruleId, location);
    }
}
