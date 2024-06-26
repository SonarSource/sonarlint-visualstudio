/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.IO;
using System.Text;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.State;
using StreamJsonRpc;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

[TestClass]
public class FileAnalysisTests
{
    public TestContext TestContext { get; set; }

    [TestMethod]
    [ExpectedException(typeof(RemoteInvocationException),"Internal error.")]
    public async Task Sloop_AnalyzesFilesWithSecretsAndRaisesIssues()
    {
        const string configScopeId = "ConfigScope1";
        const string fileToAnalyzeRelativePath = @"Resources\Secrets.yml";
        
        var testLogger = new TestLogger();
        var slCoreLogger = new TestLogger();
        var slCoreErrorLogger = new TestLogger();
        var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
        var analysisRaisedIssues = new TaskCompletionSource<RaiseIssuesParams>();
        var fileToAnalyzeAbsolutePath = Path.GetFullPath(fileToAnalyzeRelativePath);
        var fileToAnalyze = new ClientFileDto(new FileUri(fileToAnalyzeAbsolutePath), fileToAnalyzeRelativePath,
            configScopeId, false, Encoding.UTF8.WebName, fileToAnalyzeAbsolutePath);
        var analysisListener = SetUpAnalysisListener(configScopeId, analysisReadyCompletionSource, analysisRaisedIssues);
        var listFilesListener = Substitute.For<IListFilesListener>();
        listFilesListener.ListFilesAsync(Arg.Any<ListFilesParams>())
            .Returns(Task.FromResult(new ListFilesResponse(new List<ClientFileDto> { fileToAnalyze })));

        using var slCoreTestRunner = new SLCoreTestRunner(testLogger, slCoreErrorLogger, TestContext.TestName);
        slCoreTestRunner.AddListener(new LoggerListener(slCoreLogger));
        slCoreTestRunner.AddListener(new ProgressListener());
        slCoreTestRunner.AddListener(analysisListener);
        slCoreTestRunner.AddListener(listFilesListener);
        slCoreTestRunner.Start();

        var activeConfigScopeTracker = new ActiveConfigScopeTracker(slCoreTestRunner.SLCoreServiceProvider,
            new AsyncLockFactory(),
            new NoOpThreadHandler());
        activeConfigScopeTracker.SetCurrentConfigScope(configScopeId);

        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisReadyCompletionSource.Task);

        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService)
            .Should().BeTrue();
        
        var (failedAnalysisFiles, _) = await analysisService.AnalyzeFilesAndTrackAsync(
            new AnalyzeFilesAndTrackParams(configScopeId, Guid.NewGuid(),
                [new FileUri(fileToAnalyzeAbsolutePath)], [], false,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), CancellationToken.None);
        failedAnalysisFiles.Should().BeEmpty();
        
        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisRaisedIssues.Task);
        analysisRaisedIssues.Task.Result.issuesByFileUri.Should().HaveCount(1);
        analysisRaisedIssues.Task.Result.issuesByFileUri[new FileUri(fileToAnalyzeAbsolutePath)].Should().HaveCount(3);
    }

    private static IAnalysisListener SetUpAnalysisListener(string configScopeId,
        TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource,
        TaskCompletionSource<RaiseIssuesParams> analysisRaisedIssues)
    {
        var analysisListener = Substitute.For<IAnalysisListener>();
        analysisListener.When(l =>
                l.DidChangeAnalysisReadiness(Arg.Is<DidChangeAnalysisReadinessParams>(a =>
                    a.areReadyForAnalysis && a.configurationScopeIds.Contains(configScopeId))))
            .Do(info => analysisReadyCompletionSource.SetResult(info.Arg<DidChangeAnalysisReadinessParams>()));

        analysisListener.When(x => x.RaiseIssues(Arg.Any<RaiseIssuesParams>()))
            .Do(info =>
            {
                var raiseIssuesParams = info.Arg<RaiseIssuesParams>();
                if (!raiseIssuesParams.isIntermediatePublication)
                {
                    analysisRaisedIssues.SetResult(raiseIssuesParams);
                }
            });

        return analysisListener;
    }
}
