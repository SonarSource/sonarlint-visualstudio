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
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

public class FileAnalysisTestsBase
{
    private const string ConfigScopeId = "ConfigScope1";
    protected const string TwoJsIssuesPath = @"Resources\TwoIssues.js";
    protected const string ThreeSecretsIssuesPath = @"Resources\Secrets.yml";
    protected const string OneIssueRuleWithParamPath = @"Resources\RuleParam.js";

    public TestContext TestContext { get; set; }
    
    protected Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysis(string fileToAnalyzeRelativePath, bool sendContent = false)
    {
        return RunFileAnalysisInternal(fileToAnalyzeRelativePath, null, null, sendContent);
    }

    protected  Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysisWithUpdatedRulesConfiguration(string fileToAnalyzeRelativePath, 
        Dictionary<string, StandaloneRuleConfigDto> updatedRulesConfig = null)
    {
        return RunFileAnalysisInternal(fileToAnalyzeRelativePath, null, updatedRulesConfig, false);
    }

    protected Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysisWithInitialRulesConfiguration(string fileToAnalyzeRelativePath,
        Dictionary<string, StandaloneRuleConfigDto> initialRulesConfig)
    {
        return RunFileAnalysisInternal(fileToAnalyzeRelativePath, initialRulesConfig, null, false);
    }

    private async Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysisInternal(
        string fileToAnalyzeRelativePath, 
        Dictionary<string, StandaloneRuleConfigDto> initialRulesConfig,
        Dictionary<string, StandaloneRuleConfigDto> updatedRulesConfig,
        bool sendContent)
    {
        using var slCoreTestRunner = new SLCoreTestRunner(new TestLogger(), new TestLogger(), TestContext.TestName);
        var fileToAnalyzeAbsolutePath = GetFullPath(fileToAnalyzeRelativePath);
        var analysisRaisedIssues = new TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>>();
        slCoreTestRunner.MockInitialSlCoreRulesSettings(initialRulesConfig);

        await SetupSlCoreAnalysis(slCoreTestRunner, fileToAnalyzeRelativePath, fileToAnalyzeAbsolutePath, analysisRaisedIssues, sendContent);
        UpdateStandaloneRulesConfiguration(slCoreTestRunner, updatedRulesConfig);
        await RunSlCoreFileAnalysis(slCoreTestRunner, fileToAnalyzeAbsolutePath);
        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisRaisedIssues.Task);

        return analysisRaisedIssues.Task.Result.issuesByFileUri;
    }

    private static async Task RunSlCoreFileAnalysis(SLCoreTestRunner slCoreTestRunner, string fileToAnalyzeAbsolutePath)
    {
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService).Should().BeTrue();

        var (failedAnalysisFiles, _) = await analysisService.AnalyzeFilesAndTrackAsync(
            new AnalyzeFilesAndTrackParams(ConfigScopeId, Guid.NewGuid(),
                [new FileUri(fileToAnalyzeAbsolutePath)], [], false,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), CancellationToken.None);
        failedAnalysisFiles.Should().BeEmpty();
    }

    private async Task SetupSlCoreAnalysis(SLCoreTestRunner slCoreTestRunner, 
        string fileToAnalyzeRelativePath,
        string fileToAnalyzeAbsolutePath, 
        TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>> analysisRaisedIssues, 
        bool sendContent)
    {
        var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
        var fileToAnalyze = CreateFileToAnalyze(fileToAnalyzeRelativePath, fileToAnalyzeAbsolutePath, ConfigScopeId, sendContent);
        var analysisListener = SetUpAnalysisListener(ConfigScopeId, analysisReadyCompletionSource, analysisRaisedIssues);
        var listFilesListener = Substitute.For<IListFilesListener>();
        listFilesListener.ListFilesAsync(Arg.Any<ListFilesParams>())
            .Returns(Task.FromResult(new ListFilesResponse([fileToAnalyze])));

        slCoreTestRunner.AddListener(new LoggerListener(new TestLogger()));
        slCoreTestRunner.AddListener(new ProgressListener());
        slCoreTestRunner.AddListener(analysisListener);
        slCoreTestRunner.AddListener(listFilesListener);
        slCoreTestRunner.AddListener(new GetBaseDirListener());
        slCoreTestRunner.Start();

        var activeConfigScopeTracker = new ActiveConfigScopeTracker(slCoreTestRunner.SLCoreServiceProvider,
            new AsyncLockFactory(),
            new NoOpThreadHandler());
        activeConfigScopeTracker.SetCurrentConfigScope(ConfigScopeId);
        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisReadyCompletionSource.Task);
    }

    private static ClientFileDto CreateFileToAnalyze(string fileToAnalyzeRelativePath,
        string fileToAnalyzeAbsolutePath,
        string configScopeId,
        bool sendContent)
        => new(new FileUri(fileToAnalyzeAbsolutePath),
            fileToAnalyzeRelativePath,
            configScopeId,
            false,
            Encoding.UTF8.WebName,
            fileToAnalyzeAbsolutePath,
            sendContent
                ? File.ReadAllText(fileToAnalyzeAbsolutePath)
                : null);

    protected static string GetFullPath(string fileToAnalyzeRelativePath)
    {
        return Path.GetFullPath(fileToAnalyzeRelativePath);
    }

    private static void UpdateStandaloneRulesConfiguration(SLCoreTestRunner slCoreTestRunner,
        Dictionary<string, StandaloneRuleConfigDto> ruleConfigByKey = null)
    {
        if (ruleConfigByKey == null)
        {
            return;
        }

        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesCoreService)
            .Should().BeTrue();
        rulesCoreService.UpdateStandaloneRulesConfiguration(new UpdateStandaloneRulesConfigurationParams(ruleConfigByKey));
    }

    private static IAnalysisListener SetUpAnalysisListener(string configScopeId,
        TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource,
        TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>> analysisRaisedIssues)
    {
        var analysisListener = Substitute.For<IAnalysisListener>();
        analysisListener.When(l =>
                l.DidChangeAnalysisReadiness(Arg.Is<DidChangeAnalysisReadinessParams>(a =>
                    a.areReadyForAnalysis && a.configurationScopeIds.Contains(configScopeId))))
            .Do(info => analysisReadyCompletionSource.SetResult(info.Arg<DidChangeAnalysisReadinessParams>()));

        analysisListener.When(x => x.RaiseIssues(Arg.Any<RaiseFindingParams<RaisedIssueDto>>()))
            .Do(info =>
            {
                var raiseIssuesParams = info.Arg<RaiseFindingParams<RaisedIssueDto>>();
                if (!raiseIssuesParams.isIntermediatePublication)
                {
                    analysisRaisedIssues.SetResult(raiseIssuesParams);
                }
            });

        return analysisListener;
    }
}
