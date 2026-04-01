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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;
using Language = SonarLint.VisualStudio.Core.Language;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Pragma;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis;

[TestClass]
public class RoslynAnalysisServiceIntegrationTests
{
    private TestLogger logger = null!;
    private IRoslynWorkspaceWrapper workspaceWrapper = null!;
    private IAdditionalAnalysisIssueStorageWriter additionalAnalysisIssueStorageWriter = null!;
    private ISonarLintSettings sonarLintSettings = null!;
    private RoslynAnalysisService testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        logger = Substitute.ForPartsOf<TestLogger>();
        workspaceWrapper = Substitute.For<IRoslynWorkspaceWrapper>();
        additionalAnalysisIssueStorageWriter = Substitute.For<IAdditionalAnalysisIssueStorageWriter>();
        sonarLintSettings = Substitute.For<ISonarLintSettings>();
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.None);

        var pragmaSuppressionAnalysisConfigurationFactory = new PragmaSuppressionAnalysisConfigurationFactory(sonarLintSettings);
        var analysisCommandProvider = new RoslynSolutionAnalysisCommandProvider(workspaceWrapper, logger);

        var quickFixFactory = Substitute.For<IRoslynQuickFixFactory>();
        quickFixFactory.CreateQuickFixesAsync(
                Arg.Any<Diagnostic>(),
                Arg.Any<IRoslynSolutionWrapper>(),
                Arg.Any<RoslynAnalysisConfiguration>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<RoslynQuickFix>());

        var analysisEngine = new SequentialRoslynAnalysisEngine(
            new DiagnosticToRoslynIssueConverter(),
            new RoslynProjectCompilationProvider(logger),
            quickFixFactory,
            additionalAnalysisIssueStorageWriter,
            pragmaSuppressionAnalysisConfigurationFactory,
            logger);

        testSubject = new RoslynAnalysisService(
            workspaceWrapper,
            analysisEngine,
            Substitute.For<IRoslynQuickFixStorageWriter>(),
            additionalAnalysisIssueStorageWriter,
            CreateMockedConfigurationProvider(),
            analysisCommandProvider);
    }

    [TestMethod]
    public async Task AnalyzeAsync_FileWithIssues_ReturnsRoslynIssuesWithCorrectData()
    {
        var filePath = @"C:\TestProject\BadClassName.cs";
        SetUpSolution(("public class BadClassName { }", filePath));

        var request = CreateAnalysisRequest(filePath);
        var results = (await testSubject.AnalyzeAsync(request, CancellationToken.None)).ToList();

        results.Should().ContainSingle();
        results[0].RuleId.Should().Be("csharpsquid:TEST001");
        results[0].PrimaryLocation.FileUri.LocalPath.Should().Be(filePath);
        results[0].PrimaryLocation.Message.Should().Contain("BadClassName");
        results[0].PrimaryLocation.TextRange.StartLine.Should().Be(1);
    }

    [TestMethod]
    public async Task AnalyzeAsync_CleanFile_ReturnsEmptyResults()
    {
        var filePath = @"C:\TestProject\GoodClassName.cs";
        SetUpSolution(("public class GoodClassName { }", filePath));

        var request = CreateAnalysisRequest(filePath);
        var results = await testSubject.AnalyzeAsync(request, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_MultipleFilesAnalyzed_ReturnsIssuesForAllRequestedFiles()
    {
        var filePath1 = @"C:\TestProject\BadClassName1.cs";
        var filePath2 = @"C:\TestProject\BadClassName2.cs";
        SetUpSolution(
            ("public class BadClassName1 { }", filePath1),
            ("public class BadClassName2 { }", filePath2));

        var request = CreateAnalysisRequest(filePath1, filePath2);
        var results = (await testSubject.AnalyzeAsync(request, CancellationToken.None)).ToList();

        results.Should().HaveCount(2);
        results.Select(r => r.PrimaryLocation.FileUri.LocalPath).Should().BeEquivalentTo(filePath1, filePath2);
    }

    [TestMethod]
    public async Task AnalyzeAsync_MultipleFilesInProject_OnlyAnalyzesRequestedFile()
    {
        var requestedFilePath = @"C:\TestProject\BadClassName.cs";
        var nonTargetFilePath = TestBadClassNameAnalyzer.InvalidFilePath;
        SetUpSolution(
            ("public class BadClassName { }", requestedFilePath),
            ("public class BadNonTarget { }", nonTargetFilePath));

        var requestForInvalidFile = CreateAnalysisRequest(TestBadClassNameAnalyzer.InvalidFilePath);
        var resultsForInvalidFile = (await testSubject.AnalyzeAsync(requestForInvalidFile, CancellationToken.None)).ToList();

        resultsForInvalidFile.Should().BeEmpty();
        logger.AssertPartialOutputStringExists(TestBadClassNameAnalyzer.InvalidFileErrorMessage);
        logger.Reset();

        var request = CreateAnalysisRequest(requestedFilePath);
        var results = (await testSubject.AnalyzeAsync(request, CancellationToken.None)).ToList();

        results.Should().ContainSingle();
        results[0].RuleId.Should().Be("csharpsquid:TEST001");
        results[0].PrimaryLocation.FileUri.LocalPath.Should().Be(requestedFilePath);
        logger.AssertPartialOutputStringDoesNotExist(TestBadClassNameAnalyzer.InvalidFileErrorMessage);
    }

    [TestMethod]
    public async Task AnalyzeAsync_FileNotInWorkspace_ReturnsEmptyResults()
    {
        var workspaceFilePath = @"C:\TestProject\BadClassName.cs";
        var requestedFilePath = @"C:\TestProject\NotInWorkspace.cs";
        SetUpSolution(("public class BadClassName { }", workspaceFilePath));

        var request = CreateAnalysisRequest(requestedFilePath);
        var results = await testSubject.AnalyzeAsync(request, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var filePath = @"C:\TestProject\BadClassName.cs";
        SetUpSolution(("public class BadClassName { }", filePath));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = CreateAnalysisRequest(filePath);
        Func<Task> act = () => testSubject.AnalyzeAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task AnalyzeAsync_FileWithPragmaSuppressedIssue_DoesNotReturnSuppressedIssue()
    {
        var filePath = @"C:\TestProject\SuppressedBadClassName.cs";
        var code = """
                   #pragma warning disable TEST001
                   public class BadClassName { }
                   #pragma warning restore TEST001
                   """;
        EnablePragmaAnalysis();
        SetUpSolution((code, filePath));

        var request = CreateAnalysisRequest(filePath);
        var results = (await testSubject.AnalyzeAsync(request, CancellationToken.None)).ToList();

        results.Should().BeEmpty();
        additionalAnalysisIssueStorageWriter.Received(1).Add(Arg.Is<IEnumerable<RoslynIssue>>(x => !x.Any()));
    }

    [TestMethod]
    public async Task AnalyzeAsync_FileWithUnusedPragma_ReturnsNoMainIssuesButRaisesAdditionalPragmaIssue()
    {
        var filePath = @"C:\TestProject\UnusedPragma.cs";
        var code = """
                   #pragma warning disable TEST001
                   public class GoodClassName { }
                   #pragma warning restore TEST001
                   """;
        EnablePragmaAnalysis();
        SetUpSolution((code, filePath));

        var request = CreateAnalysisRequest(filePath);
        var results = (await testSubject.AnalyzeAsync(request, CancellationToken.None)).ToList();

        results.Should().BeEmpty();
        additionalAnalysisIssueStorageWriter.Received(1).Add(Arg.Is<IEnumerable<RoslynIssue>>(x =>
            x.Any() && x.All(issue => issue.RuleId == $"csharpsquid:{AdditionalRules.UnusedPragmaRuleKey}")));
    }

    [TestMethod]
    public async Task AnalyzeAsync_FileWithIssueAndNoSuppression_ReturnsIssueNormally()
    {
        var filePath = @"C:\TestProject\BadClassNameNoPragma.cs";
        EnablePragmaAnalysis();
        SetUpSolution(("public class BadClassName { }", filePath));

        var request = CreateAnalysisRequest(filePath);
        var results = (await testSubject.AnalyzeAsync(request, CancellationToken.None)).ToList();

        results.Should().ContainSingle();
        results[0].RuleId.Should().Be("csharpsquid:TEST001");
        additionalAnalysisIssueStorageWriter.Received(1).Add(Arg.Is<IEnumerable<RoslynIssue>>(x => !x.Any()));
    }

    private static AdhocWorkspace CreateWorkspaceWithDocuments(params (string code, string filePath)[] documents)
    {
        var workspace = new AdhocWorkspace();

        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Default,
            "TestProject",
            "TestProject",
            LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            metadataReferences: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        workspace.AddProject(projectInfo);

        foreach (var (code, filePath) in documents)
        {
            var documentId = DocumentId.CreateNewId(projectId);
            var documentInfo = DocumentInfo.Create(
                documentId,
                System.IO.Path.GetFileName(filePath),
                loader: Microsoft.CodeAnalysis.TextLoader.From(
                    Microsoft.CodeAnalysis.Text.SourceText.From(code).Container,
                    VersionStamp.Default),
                filePath: filePath);

            workspace.AddDocument(documentInfo);
        }

        return workspace;
    }

    private static RoslynAnalysisConfiguration CreateTestAnalysisConfiguration() =>
        new(
            new SonarLintXmlConfigurationFile(System.IO.Path.GetTempPath(), "<SonarLintConfiguration></SonarLintConfiguration>"),
            ImmutableDictionary<string, ReportDiagnostic>.Empty
                .Add(TestBadClassNameAnalyzer.DiagnosticId, ReportDiagnostic.Warn)
                .Add("AD0001", ReportDiagnostic.Error),
            ImmutableArray.Create<DiagnosticAnalyzer>(new TestBadClassNameAnalyzer()),
            ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>>.Empty);

    private static IRoslynAnalysisConfigurationProvider CreateMockedConfigurationProvider()
    {
        var testConfig = CreateTestAnalysisConfiguration();
        var configurations = new Dictionary<RoslynLanguage, RoslynAnalysisConfiguration>
        {
            { Language.CSharp, testConfig }
        };

        var provider = Substitute.For<IRoslynAnalysisConfigurationProvider>();
        provider.GetConfigurationAsync(
                Arg.Any<List<ActiveRuleDto>>(),
                Arg.Any<Dictionary<string, string>>(),
                Arg.Any<AnalyzerInfoDto>())
            .Returns(configurations);

        return provider;
    }

    private void EnablePragmaAnalysis()
    {
        sonarLintSettings.PragmaRuleSeverity.Returns(PragmaRuleSeverity.Warn);
    }

    private void SetUpSolution(params (string code, string filePath)[] documents)
    {
        var workspace = CreateWorkspaceWithDocuments(documents);
        var solution = new RoslynSolutionWrapper(workspace.CurrentSolution);
        workspaceWrapper.GetCurrentSolution().Returns(solution);
    }

    private static AnalysisRequest CreateAnalysisRequest(params string[] filePaths) =>
        new()
        {
            FileUris = filePaths.Select(fp => new FileUri(fp)).ToList(),
            ActiveRules = [new("csharpsquid:TEST001", new Dictionary<string, string>())],
            AnalysisProperties = [],
            AnalyzerInfo = new(false, false),
            AnalysisId = Guid.NewGuid()
        };
}
