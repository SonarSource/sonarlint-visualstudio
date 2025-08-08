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
public class SequentialSonarRoslynAnalysisEngineTests
{
    private IRoslynDiagnosticsConverter diagnosticsConverter = null!;
    private ISonarRoslynProjectCompilationProvider projectCompilationProvider = null!;
    private ILogger logger = null!;
    private ImmutableDictionary<Language, SonarRoslynAnalysisConfiguration> configurations = null!;
    private CancellationToken cancellationToken;
    private SequentialSonarRoslynAnalysisEngine testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        diagnosticsConverter = Substitute.For<IRoslynDiagnosticsConverter>();
        projectCompilationProvider = Substitute.For<ISonarRoslynProjectCompilationProvider>();
        logger = Substitute.ForPartsOf<TestLogger>();

        testSubject = new SequentialSonarRoslynAnalysisEngine(diagnosticsConverter, projectCompilationProvider, logger);

        configurations = ImmutableDictionary.Create<Language, SonarRoslynAnalysisConfiguration>();
        cancellationToken = new CancellationToken();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SequentialSonarRoslynAnalysisEngine, ISonarRoslynAnalysisEngine>(
            MefTestHelpers.CreateExport<IRoslynDiagnosticsConverter>(),
            MefTestHelpers.CreateExport<ISonarRoslynProjectCompilationProvider>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SequentialSonarRoslynAnalysisEngine>();

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

    [TestMethod]
    public async Task AnalyzeAsync_SingleProjectWithSingleAnalysisCommand_ReturnsDiagnostics()
    {
        var (project, commands, compilation) = SetupProjectAndCommands();
        var (command, diagnostic, sonarDiagnostic) = SetupCommandWithDiagnostic(
            compilation,
            "test-rule",
            "test message");
        AddCommandToProject(command, commands);

        var result = await testSubject.AnalyzeAsync([commands], configurations, cancellationToken);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result.Single().Should().Be(sonarDiagnostic);

        VerifyAnalysisExecution(project, compilation, command, diagnostic);
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

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(sonarDiagnostic1);
        result.Should().Contain(sonarDiagnostic2);
        VerifyAnalysisExecution(project1, compilation1, command1, diagnostic1);
        VerifyAnalysisExecution(project2, compilation2, command2, diagnostic2);
    }

    [TestMethod]
    public async Task AnalyzeAsync_SingleProjectWithMultipleCommands_ReturnsAllDiagnostics()
    {
        var (project, commands, compilation) = SetupProjectAndCommands();

        // First command with two diagnostics
        var command1 = Substitute.For<ISonarRoslynAnalysisCommand>();
        var diagnostic1A = CreateTestDiagnostic("rule1");
        var diagnostic1B = CreateTestDiagnostic("rule2");
        var sonarDiagnostic1A = CreateSonarDiagnostic("rule1", "message1");
        var sonarDiagnostic1B = CreateSonarDiagnostic("rule2", "message2");
        command1.ExecuteAsync(compilation, cancellationToken).Returns([diagnostic1A, diagnostic1B]);
        diagnosticsConverter.ConvertToSonarDiagnostic(diagnostic1A).Returns(sonarDiagnostic1A);
        diagnosticsConverter.ConvertToSonarDiagnostic(diagnostic1B).Returns(sonarDiagnostic1B);
        AddCommandToProject(command1, commands);

        // Second command with two different diagnostics
        var command2 = Substitute.For<ISonarRoslynAnalysisCommand>();
        var diagnostic2A = CreateTestDiagnostic("rule3");
        var diagnostic2B = CreateTestDiagnostic("rule4");
        var sonarDiagnostic2A = CreateSonarDiagnostic("rule3", "message3");
        var sonarDiagnostic2B = CreateSonarDiagnostic("rule4", "message4");
        command2.ExecuteAsync(compilation, cancellationToken).Returns([diagnostic2A, diagnostic2B]);
        diagnosticsConverter.ConvertToSonarDiagnostic(diagnostic2A).Returns(sonarDiagnostic2A);
        diagnosticsConverter.ConvertToSonarDiagnostic(diagnostic2B).Returns(sonarDiagnostic2B);
        AddCommandToProject(command2, commands);

        var result = await testSubject.AnalyzeAsync([commands], configurations, cancellationToken);

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result.Should().Contain(sonarDiagnostic1A);
        result.Should().Contain(sonarDiagnostic1B);
        result.Should().Contain(sonarDiagnostic2A);
        result.Should().Contain(sonarDiagnostic2B);

        await command1.Received(1).ExecuteAsync(compilation, cancellationToken);
        await command2.Received(1).ExecuteAsync(compilation, cancellationToken);
        diagnosticsConverter.Received(1).ConvertToSonarDiagnostic(diagnostic1A);
        diagnosticsConverter.Received(1).ConvertToSonarDiagnostic(diagnostic1B);
        diagnosticsConverter.Received(1).ConvertToSonarDiagnostic(diagnostic2A);
        diagnosticsConverter.Received(1).ConvertToSonarDiagnostic(diagnostic2B);
    }

    private (ISonarRoslynProjectWrapper project, SonarRoslynProjectAnalysisCommands projectCommand, ISonarRoslynCompilationWithAnalyzersWrapper projectCompilation) SetupProjectAndCommands()
    {
        var project = Substitute.For<ISonarRoslynProjectWrapper>();
        var commands = new List<ISonarRoslynAnalysisCommand>();
        var projectCommands = new SonarRoslynProjectAnalysisCommands(project, commands);
        var compilation = SetupCompilation(project);

        return (project, projectCommands, compilation);
    }

    private void AddCommandToProject(ISonarRoslynAnalysisCommand command, SonarRoslynProjectAnalysisCommands projectCommands) =>
    (
        (List<ISonarRoslynAnalysisCommand>)projectCommands.AnalysisCommands).Add(command);

    private (ISonarRoslynAnalysisCommand command, Diagnostic diagnostic, SonarDiagnostic sonarDiagnostic) SetupCommandWithDiagnostic(
        ISonarRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        string ruleId,
        string message,
        SonarDiagnostic? existingSonarDiagnostic = null)
    {
        var command = Substitute.For<ISonarRoslynAnalysisCommand>();
        var diagnostic = CreateTestDiagnostic(ruleId);
        command.ExecuteAsync(compilationWithAnalyzers, CancellationToken.None)
            .Returns([diagnostic]);

        var sonarDiagnostic = existingSonarDiagnostic ?? CreateSonarDiagnostic(ruleId, message);
        diagnosticsConverter.ConvertToSonarDiagnostic(diagnostic).Returns(sonarDiagnostic);

        return (command, diagnostic, sonarDiagnostic);
    }

    private ISonarRoslynCompilationWithAnalyzersWrapper SetupCompilation(ISonarRoslynProjectWrapper project)
    {
        var compilationWithAnalyzers = Substitute.For<ISonarRoslynCompilationWithAnalyzersWrapper>();
        projectCompilationProvider.GetProjectCompilationAsync(project, configurations, cancellationToken)
            .Returns(compilationWithAnalyzers);
        return compilationWithAnalyzers;
    }

    private void VerifyAnalysisExecution(
        ISonarRoslynProjectWrapper project,
        ISonarRoslynCompilationWithAnalyzersWrapper compilationWithAnalyzers,
        ISonarRoslynAnalysisCommand analysisCommand,
        Diagnostic diagnostic)
    {
        projectCompilationProvider.Received(1)
            .GetProjectCompilationAsync(project, configurations, cancellationToken);
        analysisCommand.Received(1).ExecuteAsync(compilationWithAnalyzers, cancellationToken);
        diagnosticsConverter.Received(1).ConvertToSonarDiagnostic(diagnostic);
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

    private static SonarDiagnostic CreateSonarDiagnostic(string ruleId, string message)
    {
        var textRange = new SonarTextRange(1, 1, 0, 1, null);
        var location = new SonarDiagnosticLocation(message, "test.cs", textRange);
        return new SonarDiagnostic(ruleId, true, location);
    }
}
