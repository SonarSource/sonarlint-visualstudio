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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.TestInfrastructure;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Configuration;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis.Wrappers;
using SonarLint.VisualStudio.RoslynAnalyzerServer.Http.Models;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.TestInfrastructure;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.UnitTests;

[TestClass]
public class RoslynAnalysisServiceTests
{
    private static readonly List<ActiveRuleDto> DefaultActiveRules = new() { new ActiveRuleDto("sample-rule-id", new Dictionary<string, string> { { "paramKey", "paramValue" } }) };
    private static readonly Dictionary<string, string> DefaultAnalysisProperties = new() { { "sonar.cs.any", "any" } };
    private static readonly Dictionary<RoslynLanguage, RoslynAnalysisConfiguration> DefaultAnalysisConfigurations = new() { { Language.CSharp, new RoslynAnalysisConfiguration() } };
    private static readonly List<RoslynProjectAnalysisRequest> DefaultProjectAnalysisRequests = new() { new RoslynProjectAnalysisRequest(Substitute.For<IRoslynProjectWrapper>(), []) };
    private static readonly List<RoslynIssue> DefaultIssues = new() { new RoslynIssue("sample-rule-id", new RoslynIssueLocation("any", new FileUri("file:///C:/any.cs"), new RoslynIssueTextRange(1, 1, 1, 1))) };
    private static readonly AnalyzerInfoDto DefaultAnalyzerInfoDto = new(false, false);
    private IRoslynSolutionAnalysisCommandProvider analysisCommandProvider = null!;
    private IRoslynAnalysisConfigurationProvider analysisConfigurationProvider = null!;

    private IRoslynAnalysisEngine analysisEngine = null!;
    private IRoslynWorkspaceWrapper workspace = null!;
    private IRoslynQuickFixStorageWriter quickFixStorageWriter = null!;
    private RoslynAnalysisService testSubject = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        workspace = Substitute.For<IRoslynWorkspaceWrapper>();
        analysisEngine = Substitute.For<IRoslynAnalysisEngine>();
        analysisConfigurationProvider = Substitute.For<IRoslynAnalysisConfigurationProvider>();
        analysisCommandProvider = Substitute.For<IRoslynSolutionAnalysisCommandProvider>();
        quickFixStorageWriter = Substitute.For<IRoslynQuickFixStorageWriter>();

        testSubject = new RoslynAnalysisService(workspace, analysisEngine, quickFixStorageWriter, analysisConfigurationProvider, analysisCommandProvider);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynAnalysisService, IRoslynAnalysisService>(
            MefTestHelpers.CreateExport<IRoslynWorkspaceWrapper>(),
            MefTestHelpers.CreateExport<IRoslynAnalysisEngine>(),
            MefTestHelpers.CreateExport<IRoslynQuickFixStorageWriter>(),
            MefTestHelpers.CreateExport<IRoslynAnalysisConfigurationProvider>(),
            MefTestHelpers.CreateExport<IRoslynSolutionAnalysisCommandProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<RoslynAnalysisService>();

    [TestMethod]
    public async Task AnalyzeAsync_PassesCorrectArgumentsToEngine()
    {
        string[] filePaths = [@"C:\file1.cs", @"C:\folder\file2.cs"];
        SetUpBasicAnalysisServices(filePaths);
        analysisEngine.AnalyzeAsync(DefaultProjectAnalysisRequests, DefaultAnalysisConfigurations, Arg.Any<CancellationToken>()).Returns(DefaultIssues);

        var analysisRequest = CreateAnalysisRequest(filePaths.Select(x => new FileUri(x)).ToList());

        var issues = await testSubject.AnalyzeAsync(analysisRequest, CancellationToken.None);

        quickFixStorageWriter.Received().Clear(filePaths[0]);
        quickFixStorageWriter.Received().Clear(filePaths[1]);
        issues.Should().BeSameAs(DefaultIssues);
    }

    [TestMethod]
    public void Cancel_NonExistingId_ReturnsFalse()
    {
        var nonExistingId = Guid.NewGuid();
        var cancellationRequest = CreateCancellationRequest(nonExistingId);

        var result = testSubject.Cancel(cancellationRequest);

        result.Should().BeFalse();
    }

    [TestMethod]
    public void Cancel_ExistingId_ReturnsTrueAndCancelsToken()
    {
        var analysisId = Guid.NewGuid();
        var analysisRequest = CreateAnalysisRequest(analysisId: analysisId);

        SetUpBasicAnalysisServices();

        var taskCompletionSource = new TaskCompletionSource<IEnumerable<RoslynIssue>>();
        var internalAnalysisToken = CancellationToken.None;

        analysisEngine.AnalyzeAsync(
                Arg.Any<List<RoslynProjectAnalysisRequest>>(),
                Arg.Any<IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration>>(),
                Arg.Do<CancellationToken>(t => internalAnalysisToken = t))
            .Returns(taskCompletionSource.Task);

        testSubject.AnalyzeAsync(analysisRequest, CancellationToken.None).IgnoreAwaitForAssert();
        var cancellationRequest = CreateCancellationRequest(analysisId);

        var result = testSubject.Cancel(cancellationRequest);

        result.Should().BeTrue();
        taskCompletionSource.SetResult(DefaultIssues);
        internalAnalysisToken.IsCancellationRequested.Should().BeTrue();
    }

    [TestMethod]
    public async Task AnalyzeAsync_TokenRemovedAfterAnalysis_EvenIfAnalysisSucceeds()
    {
        var analysisId = Guid.NewGuid();
        var analysisRequest = CreateAnalysisRequest(analysisId: analysisId);
        SetUpBasicAnalysisServices();
        analysisEngine
            .AnalyzeAsync(Arg.Any<List<RoslynProjectAnalysisRequest>>(), Arg.Any<IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration>>(), Arg.Any<CancellationToken>())
            .Returns(DefaultIssues);

        await testSubject.AnalyzeAsync(analysisRequest, CancellationToken.None);

        var cancellationRequest = CreateCancellationRequest(analysisId);
        var result = testSubject.Cancel(cancellationRequest);
        result.Should().BeFalse();
    }

    [TestMethod]
    public void AnalyzeAsync_TokenRemovedAfterAnalysis_EvenIfAnalysisThrows()
    {
        var analysisId = Guid.NewGuid();
        var analysisRequest = CreateAnalysisRequest(analysisId: analysisId);
        SetUpBasicAnalysisServices();
        analysisEngine
            .AnalyzeAsync(Arg.Any<List<RoslynProjectAnalysisRequest>>(), Arg.Any<IReadOnlyDictionary<RoslynLanguage, RoslynAnalysisConfiguration>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var act = () => testSubject.AnalyzeAsync(analysisRequest, CancellationToken.None);
        act.Should().ThrowAsync<InvalidOperationException>().IgnoreAwaitForAssert();

        var cancellationRequest = CreateCancellationRequest(analysisId);
        var result = testSubject.Cancel(cancellationRequest);
        result.Should().BeFalse();
    }

    [TestMethod]
    public void Dispose_DisposesWorkspace()
    {
        testSubject.Dispose();

        workspace.Received().Dispose();
    }

    private void SetUpConfigurationProvider() =>
        analysisConfigurationProvider
            .GetConfigurationAsync(DefaultActiveRules, DefaultAnalysisProperties, DefaultAnalyzerInfoDto)
            .Returns(DefaultAnalysisConfigurations);

    private void SetUpBasicAnalysisServices(string[]? filePaths = null)
    {
        SetUpConfigurationProvider();
        analysisCommandProvider
            .GetAnalysisCommandsForCurrentSolution(filePaths is not null ? Arg.Is<string[]>(x => x.SequenceEqual(filePaths)) : Arg.Any<string[]>())
            .Returns(DefaultProjectAnalysisRequests);
    }

    private static AnalysisRequest CreateAnalysisRequest(
        List<FileUri>? fileNames = null,
        Guid? analysisId = null)
    {
        fileNames ??= [new FileUri(@"C:\file1.cs")];

        return new AnalysisRequest
        {
            FileUris = fileNames,
            ActiveRules = DefaultActiveRules,
            AnalysisProperties = DefaultAnalysisProperties,
            AnalyzerInfo = DefaultAnalyzerInfoDto,
            AnalysisId = analysisId ?? Guid.NewGuid()
        };
    }

    private static AnalysisCancellationRequest CreateCancellationRequest(Guid analysisId) => new() { AnalysisId = analysisId };
}
