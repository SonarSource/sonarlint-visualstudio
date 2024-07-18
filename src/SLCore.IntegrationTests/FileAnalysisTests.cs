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

[TestClass]
public class FileAnalysisTests
{
    private const string TwoJsIssuesPath = @"Resources\TwoIssues.js";
    private const string ThreeSecretsIssuesPath = @"Resources\Secrets.yml";
    private const string OneIssueRuleWithParamPath = @"Resources\RuleParam.js";
    private const string LetRuleId = "javascript:S3504";
    private const string CloudSecretsRuleId = "secrets:S6336";
    private const string CtorParamRuleId = "javascript:S107";
    private const int ActualCtorParams = 4;
    private const string CtorParamName = "maximumFunctionParameters";
    public TestContext TestContext { get; set; }

    [TestMethod]
    public async Task DefaultRuleConfig_JavaScriptAnalysisProducesExpectedNumberOfIssues()
    {
        var issuesByFileUri = await RunFileAnalysis(ThreeSecretsIssuesPath);
        
        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(ThreeSecretsIssuesPath))].Should().HaveCount(3);
    }
    
    [TestMethod]
    public async Task DefaultRuleConfig_SecretsAnalysisProducesExpectedNumberOfIssues()
    {
        var issuesByFileUri = await RunFileAnalysis(TwoJsIssuesPath);
        
        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(TwoJsIssuesPath))].Should().HaveCount(2);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_JavaScriptAnalysisShouldIgnoreOneIssueOfInactiveRule()
    {
        var letRuleConfig = CreateInactiveRuleConfig(LetRuleId);

        var issuesByFileUri = await RunFileAnalysis(TwoJsIssuesPath, letRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(TwoJsIssuesPath))].Should().HaveCount(1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_SecretsAnalysisShouldIgnoreTwoIssuesOfInactiveRule()
    {
        var secretsRuleConfig = CreateInactiveRuleConfig(CloudSecretsRuleId);

        var issuesByFileUri = await RunFileAnalysis(ThreeSecretsIssuesPath, secretsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(ThreeSecretsIssuesPath))].Should().HaveCount(1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CtorParamsUnderThreshold_JavaScriptActiveRuleShouldHaveNoIssue()
    {
        var ctorParamsRuleConfig = CreateActiveCtorParamRuleConfig(threshold:ActualCtorParams + 1);

        var issuesByFileUri = await RunFileAnalysis(OneIssueRuleWithParamPath, ctorParamsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(OneIssueRuleWithParamPath))].Should().HaveCount(0);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CtorParamsAboveThreshold_JavaScriptActiveRuleShouldHaveOneIssue()
    {
        var ctorParamsRuleConfig = CreateActiveCtorParamRuleConfig(threshold:ActualCtorParams - 1);

        var issuesByFileUri = await RunFileAnalysis(OneIssueRuleWithParamPath, ctorParamsRuleConfig);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(OneIssueRuleWithParamPath))].Should().HaveCount(1);
    }

    private async Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysis(string fileToAnalyzeRelativePath, Dictionary<string, StandaloneRuleConfigDto> ruleConfigByKey = null)
    {
        const string configScopeId = "ConfigScope1";

        var testLogger = new TestLogger();
        var slCoreLogger = new TestLogger();
        var slCoreErrorLogger = new TestLogger();
        var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
        var analysisRaisedIssues = new TaskCompletionSource<RaiseIssuesParams>();
        var fileToAnalyzeAbsolutePath = GetFullPath(fileToAnalyzeRelativePath);
        var fileToAnalyze = new ClientFileDto(new FileUri(fileToAnalyzeAbsolutePath), fileToAnalyzeRelativePath,
            configScopeId, false, Encoding.UTF8.WebName, fileToAnalyzeAbsolutePath);
        var analysisListener = SetUpAnalysisListener(configScopeId, analysisReadyCompletionSource, analysisRaisedIssues);
        var listFilesListener = Substitute.For<IListFilesListener>();
        listFilesListener.ListFilesAsync(Arg.Any<ListFilesParams>())
            .Returns(Task.FromResult(new ListFilesResponse([ fileToAnalyze ])));

        using var slCoreTestRunner = new SLCoreTestRunner(testLogger, slCoreErrorLogger, TestContext.TestName);
        slCoreTestRunner.AddListener(new LoggerListener(slCoreLogger));
        slCoreTestRunner.AddListener(new ProgressListener());
        slCoreTestRunner.AddListener(analysisListener);
        slCoreTestRunner.AddListener(listFilesListener);
        slCoreTestRunner.AddListener(new GetBaseDirListener());
        slCoreTestRunner.Start();

        var activeConfigScopeTracker = new ActiveConfigScopeTracker(slCoreTestRunner.SLCoreServiceProvider,
            new AsyncLockFactory(),
            new NoOpThreadHandler());
        activeConfigScopeTracker.SetCurrentConfigScope(configScopeId);
        
        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisReadyCompletionSource.Task);

        UpdateStandaloneRulesConfiguration(slCoreTestRunner, ruleConfigByKey);

        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService)
            .Should().BeTrue();

        var (failedAnalysisFiles, _) = await analysisService.AnalyzeFilesAndTrackAsync(
            new AnalyzeFilesAndTrackParams(configScopeId, Guid.NewGuid(),
                [new FileUri(fileToAnalyzeAbsolutePath)], [], false,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), CancellationToken.None);
        failedAnalysisFiles.Should().BeEmpty();

        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisRaisedIssues.Task);
        return analysisRaisedIssues.Task.Result.issuesByFileUri;
    }

    private static string GetFullPath(string fileToAnalyzeRelativePath)
    {
        return Path.GetFullPath(fileToAnalyzeRelativePath);
    }

    private static void UpdateStandaloneRulesConfiguration(SLCoreTestRunner slCoreTestRunner, Dictionary<string, StandaloneRuleConfigDto> ruleConfigByKey = null)
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

    private static Dictionary<string, StandaloneRuleConfigDto> CreateActiveCtorParamRuleConfig(int threshold)
    {
        return new () 
        { 
            {CtorParamRuleId, new StandaloneRuleConfigDto(isActive: true, new (){ {CtorParamName, threshold.ToString()}}) }
        };
    }

    private static Dictionary<string, StandaloneRuleConfigDto> CreateInactiveRuleConfig(string ruleId)
    {
        return new()
        {
            {ruleId, new StandaloneRuleConfigDto(isActive: false, []) }
        };
    }
}
