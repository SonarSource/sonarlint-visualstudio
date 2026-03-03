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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using Language = SonarLint.VisualStudio.Core.Language;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.IntegrationTests.Analysis;

[TestClass]
public class RoslynAnalysisServiceIntegrationTests
{
    [TestMethod]
    public async Task AnalyzeAsync_FileWithIssues_ReturnsRoslynIssuesWithCorrectData()
    {
        var filePath = @"C:\TestProject\BadClassName.cs";
        using var workspace = CreateWorkspaceWithDocuments(("public class BadClassName { }", filePath));
        using var testSubject = CreateTestSubject(workspace);

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
        using var workspace = CreateWorkspaceWithDocuments(("public class GoodClassName { }", filePath));
        using var testSubject = CreateTestSubject(workspace);

        var request = CreateAnalysisRequest(filePath);
        var results = await testSubject.AnalyzeAsync(request, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_MultipleFilesAnalyzed_ReturnsIssuesForAllRequestedFiles()
    {
        var filePath1 = @"C:\TestProject\BadClassName1.cs";
        var filePath2 = @"C:\TestProject\BadClassName2.cs";
        using var workspace = CreateWorkspaceWithDocuments(
            ("public class BadClassName1 { }", filePath1),
            ("public class BadClassName2 { }", filePath2));
        using var testSubject = CreateTestSubject(workspace);

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
        using var workspace = CreateWorkspaceWithDocuments(
            ("public class BadClassName { }", requestedFilePath),
            ("public class BadNonTarget { }", nonTargetFilePath));
        using var testSubject = CreateTestSubject(workspace);

        var request = CreateAnalysisRequest(requestedFilePath);
        var results = (await testSubject.AnalyzeAsync(request, CancellationToken.None)).ToList();

        results.Should().ContainSingle();
        results[0].RuleId.Should().Be("csharpsquid:TEST001");
        results[0].PrimaryLocation.FileUri.LocalPath.Should().Be(requestedFilePath);
    }

    [TestMethod]
    public async Task AnalyzeAsync_FileNotInWorkspace_ReturnsEmptyResults()
    {
        var workspaceFilePath = @"C:\TestProject\BadClassName.cs";
        var requestedFilePath = @"C:\TestProject\NotInWorkspace.cs";
        using var workspace = CreateWorkspaceWithDocuments(("public class BadClassName { }", workspaceFilePath));
        using var testSubject = CreateTestSubject(workspace);

        var request = CreateAnalysisRequest(requestedFilePath);
        var results = await testSubject.AnalyzeAsync(request, CancellationToken.None);

        results.Should().BeEmpty();
    }

    [TestMethod]
    public async Task AnalyzeAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var filePath = @"C:\TestProject\BadClassName.cs";
        using var workspace = CreateWorkspaceWithDocuments(("public class BadClassName { }", filePath));
        using var testSubject = CreateTestSubject(workspace);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var request = CreateAnalysisRequest(filePath);
        Func<Task> act = () => testSubject.AnalyzeAsync(request, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
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

    private static RoslynAnalysisConfiguration CreateTestAnalysisConfiguration()
    {
        return new RoslynAnalysisConfiguration(
            new SonarLintXmlConfigurationFile(System.IO.Path.GetTempPath(), "<SonarLintConfiguration></SonarLintConfiguration>"),
            ImmutableDictionary<string, ReportDiagnostic>.Empty
                .Add(TestBadClassNameAnalyzer.DiagnosticId, ReportDiagnostic.Warn),
            ImmutableArray.Create<DiagnosticAnalyzer>(new TestBadClassNameAnalyzer()),
            ImmutableDictionary<string, IReadOnlyCollection<CodeFixProvider>>.Empty);
    }

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

    private static RoslynAnalysisService CreateTestSubject(AdhocWorkspace workspace)
    {
        var logger = Substitute.ForPartsOf<TestLogger>();
        var workspaceChangeIndicator = Substitute.For<IWorkspaceChangeIndicator>();
        var treatWarningsAsErrorsCacheUpdater = Substitute.For<ITreatWarningsAsErrorsCacheUpdater>();
        var treatWarningsAsErrorsChangeIndicator = Substitute.For<ITreatWarningsAsErrorsChangeIndicator>();
        var analysisRequester = Substitute.For<IAnalysisRequester>();
        var threadHandling = Substitute.For<IThreadHandling>();

        var workspaceWrapper = new RoslynWorkspaceWrapper(
            workspace,
            workspaceChangeIndicator,
            treatWarningsAsErrorsCacheUpdater,
            treatWarningsAsErrorsChangeIndicator,
            analysisRequester,
            logger,
            threadHandling);

        var analysisCommandProvider = new RoslynSolutionAnalysisCommandProvider(workspaceWrapper, logger);
        var issueConverter = new DiagnosticToRoslynIssueConverter();
        var compilationProvider = new RoslynProjectCompilationProvider(logger);

        var quickFixFactory = Substitute.For<IRoslynQuickFixFactory>();
        quickFixFactory.CreateQuickFixesAsync(
                Arg.Any<Diagnostic>(),
                Arg.Any<IRoslynSolutionWrapper>(),
                Arg.Any<RoslynAnalysisConfiguration>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<RoslynQuickFix>());

        var analysisEngine = new SequentialRoslynAnalysisEngine(
            issueConverter,
            compilationProvider,
            quickFixFactory,
            logger);

        var quickFixStorageWriter = Substitute.For<IRoslynQuickFixStorageWriter>();
        var configurationProvider = CreateMockedConfigurationProvider();

        return new RoslynAnalysisService(
            workspaceWrapper,
            analysisEngine,
            quickFixStorageWriter,
            configurationProvider,
            analysisCommandProvider);
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
