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

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_SingleProjectWithNoAnalysisCommands_ReturnsEmptyCollection()
    {
        var (project, commands, compilation) = SetupProjectAndCommands();

        var result = await testSubject.AnalyzeAsync([commands], configurations, cancellationToken);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
        await projectCompilationProvider.Received(1).GetProjectCompilationAsync(project, configurations, cancellationToken);
    }

    public static object[][] TestData =>
    [
        [Language.CSharp],
        [Language.VBNET]
    ];

    [DataTestMethod]
    [DynamicData(nameof(TestData))]
    public async Task AnalyzeAsync_SingleProjectWithSingleAnalysisCommand_ReturnsDiagnostics(Language language)
    {
        var (project, commands, compilation) = SetupProjectAndCommands();
        compilation.Language.Returns(language);
        var (command, diagnostic, sonarDiagnostic) = SetupCommandWithDiagnostic(
            compilation,
            "test-rule",
            "test message");
        AddCommandToProject(command, commands);

        var result = await testSubject.AnalyzeAsync([commands], configurations, cancellationToken);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result.Single().Should().Be(sonarDiagnostic);

        VerifyAnalysisExecution(project, compilation, command, diagnostic, language);
    }

    [TestMethod]
    public async Task AnalyzeAsync_DuplicateDiagnostics_ReturnsSingleDiagnostic()
    {
        var (project, commands, compilation) = SetupProjectAndCommands();
        var (command1, diagnostic1, sonarDiagnostic1) = SetupCommandWithDiagnostic(
            compilation,
            "test-rule",
            "test message");
        AddCommandToProject(command1, commands);
        var (command2, diagnostic2, _) = SetupCommandWithDiagnostic(
            compilation,
            "test-rule-duplicate",
            "test message duplicate",
            sonarDiagnostic1);
        AddCommandToProject(command2, commands);

        var result = await testSubject.AnalyzeAsync([commands], configurations, cancellationToken);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result.Single().Should().Be(sonarDiagnostic1);
        VerifyAnalysisExecution(project, compilation, command1, diagnostic1);
        VerifyAnalysisExecution(project, compilation, command2, diagnostic2);
        logger.AssertPartialOutputStringExists($"Duplicate diagnostic discarded ID: {sonarDiagnostic1.RuleKey}, File: {sonarDiagnostic1.PrimaryLocation.FilePath}, Line: {sonarDiagnostic1.PrimaryLocation.TextRange.StartLine}");
    }

    [TestMethod]
    public async Task AnalyzeAsync_MultipleProjects_ProcessesAllProjects()
    {
        var (project1, commands1, compilation1) = SetupProjectAndCommands();
        var (command1, diagnostic1, sonarDiagnostic1) = SetupCommandWithDiagnostic(
            compilation1,
            "rule1",
            "message1");
        AddCommandToProject(command1, commands1);
        var (project2, commands2, compilation2) = SetupProjectAndCommands();
        var (command2, diagnostic2, sonarDiagnostic2) = SetupCommandWithDiagnostic(
            compilation2,
            "rule2",
            "message2");
        AddCommandToProject(command2, commands2);

        var result = await testSubject.AnalyzeAsync([commands1, commands2], configurations, cancellationToken);

        result.Should().BeEquivalentTo([sonarDiagnostic1, sonarDiagnostic2]);
        VerifyAnalysisExecution(project1, compilation1, command1, diagnostic1);
        VerifyAnalysisExecution(project2, compilation2, command2, diagnostic2);
    }

    [TestMethod]
    public async Task AnalyzeAsync_SingleProjectWithMultipleCommands_ReturnsAllDiagnostics()
    {
        var (project, commands, compilation) = SetupProjectAndCommands();

        var command1 = Substitute.For<IRoslynAnalysisCommand>();
        var diagnostic1A = CreateTestDiagnostic("rule1");
        var diagnostic1B = CreateTestDiagnostic("rule2");
        var sonarDiagnostic1A = CreateSonarDiagnostic("rule1", "message1");
        var sonarDiagnostic1B = CreateSonarDiagnostic("rule2", "message2");
        command1.ExecuteAsync(compilation, cancellationToken).Returns(ImmutableArray.Create(diagnostic1A, diagnostic1B));
        issueConverter.ConvertToSonarDiagnostic(diagnostic1A, Arg.Any<Language>()).Returns(sonarDiagnostic1A);
        issueConverter.ConvertToSonarDiagnostic(diagnostic1B, Arg.Any<Language>()).Returns(sonarDiagnostic1B);
        AddCommandToProject(command1, commands);
        var command2 = Substitute.For<IRoslynAnalysisCommand>();
        var diagnostic2A = CreateTestDiagnostic("rule3");
        var diagnostic2B = CreateTestDiagnostic("rule4");
        var sonarDiagnostic2A = CreateSonarDiagnostic("rule3", "message3");
        var sonarDiagnostic2B = CreateSonarDiagnostic("rule4", "message4");
        command2.ExecuteAsync(compilation, cancellationToken).Returns(ImmutableArray.Create(diagnostic2A, diagnostic2B));
        issueConverter.ConvertToSonarDiagnostic(diagnostic2A, Arg.Any<Language>()).Returns(sonarDiagnostic2A);
        issueConverter.ConvertToSonarDiagnostic(diagnostic2B, Arg.Any<Language>()).Returns(sonarDiagnostic2B);
        AddCommandToProject(command2, commands);

        var result = await testSubject.AnalyzeAsync([commands], configurations, cancellationToken);

        result.Should().BeEquivalentTo(sonarDiagnostic1A, sonarDiagnostic1B, sonarDiagnostic2A, sonarDiagnostic2B);
        await command1.Received(1).ExecuteAsync(compilation, cancellationToken);
        await command2.Received(1).ExecuteAsync(compilation, cancellationToken);
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic1A, Arg.Any<Language>());
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic1B, Arg.Any<Language>());
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic2A, Arg.Any<Language>());
        issueConverter.Received(1).ConvertToSonarDiagnostic(diagnostic2B, Arg.Any<Language>());
    }

    private (IRoslynProjectWrapper project, RoslynProjectAnalysisRequest projectCommand, IRoslynCompilationWithAnalyzersWrapper projectCompilation) SetupProjectAndCommands()
    {
        var project = Substitute.For<IRoslynProjectWrapper>();
        var projectCommands = new RoslynProjectAnalysisRequest(project, new List<IRoslynAnalysisCommand>());
        var compilation = SetupCompilation(project);

        return (project, projectCommands, compilation);
    }

    private static void AddCommandToProject(IRoslynAnalysisCommand command, RoslynProjectAnalysisRequest projectRequest) =>
        ((List<IRoslynAnalysisCommand>)projectRequest.AnalysisCommands).Add(command);

    private (IRoslynAnalysisCommand command, Diagnostic diagnostic, RoslynIssue sonarDiagnostic) SetupCommandWithDiagnostic(
        IRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        string ruleId,
        string message,
        RoslynIssue? existingSonarDiagnostic = null)
    {
        var command = Substitute.For<IRoslynAnalysisCommand>();
        var diagnostic = CreateTestDiagnostic(ruleId);
        command.ExecuteAsync(compilationWithAnalyzers, CancellationToken.None)
            .Returns(ImmutableArray.Create(diagnostic));

        var sonarDiagnostic = existingSonarDiagnostic ?? CreateSonarDiagnostic(ruleId, message);
        issueConverter.ConvertToSonarDiagnostic(diagnostic, Arg.Any<Language>()).Returns(sonarDiagnostic);

        return (command, diagnostic, sonarDiagnostic);
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

    private static RoslynIssue CreateSonarDiagnostic(string ruleId, string message)
    {
        var textRange = new SonarTextRange(1, 1, 0, 1);
        var location = new SonarDiagnosticLocation(message, "test.cs", textRange);
        return new RoslynIssue(ruleId, location);
    }
}
