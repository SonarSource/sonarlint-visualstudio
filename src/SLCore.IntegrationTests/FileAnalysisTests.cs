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
    private const string ConfigScopeId = "ConfigScope1";
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

    [TestMethod]
    public async Task StandaloneRuleConfig_JsLetRuleIsDisableInSettingsFile_JavaScriptAnalysisShouldIgnoreIssueOnInitialization()
    {
        var letRuleConfig = CreateInactiveRuleConfig(LetRuleId);

        var issuesByFileUri = await RunFileAnalysis(TwoJsIssuesPath, initialRulesConfig: letRuleConfig, updatedRulesConfig:null);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(TwoJsIssuesPath))].Should().HaveCount(1);
    }

    [TestMethod]
    public async Task StandaloneRuleConfig_CloudSecretsRuleIsDisabledInSettingsFile_SecretsAnalysisShouldIgnoreIssueOnInitialization()
    {
        var secretsRuleConfig = CreateInactiveRuleConfig(CloudSecretsRuleId);

        var issuesByFileUri = await RunFileAnalysis(ThreeSecretsIssuesPath, initialRulesConfig:secretsRuleConfig, updatedRulesConfig:null);

        issuesByFileUri.Should().HaveCount(1);
        issuesByFileUri[new FileUri(GetFullPath(ThreeSecretsIssuesPath))].Should().HaveCount(1);
    }

    private async Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysis(string fileToAnalyzeRelativePath, Dictionary<string, StandaloneRuleConfigDto> ruleConfigByKey = null)
    {
        return await RunFileAnalysis(fileToAnalyzeRelativePath, null, ruleConfigByKey);
    }

    private async Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysis(
        string fileToAnalyzeRelativePath, 
        Dictionary<string, StandaloneRuleConfigDto> initialRulesConfig,
        Dictionary<string, StandaloneRuleConfigDto> updatedRulesConfig)
    {
        using var slCoreTestRunner = new SLCoreTestRunner(new TestLogger(), new TestLogger(), TestContext.TestName);
        var fileToAnalyzeAbsolutePath = GetFullPath(fileToAnalyzeRelativePath);
        var analysisRaisedIssues = new TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>>();
        slCoreTestRunner.MockInitialSlCoreRulesSettings(initialRulesConfig);

        await SetupSlCoreAnalysis(slCoreTestRunner, fileToAnalyzeRelativePath, fileToAnalyzeAbsolutePath, analysisRaisedIssues);
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
        TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>> analysisRaisedIssues)
    {
        var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
       
        var fileToAnalyze = new ClientFileDto(new FileUri(fileToAnalyzeAbsolutePath), fileToAnalyzeRelativePath,
            ConfigScopeId, false, Encoding.UTF8.WebName, fileToAnalyzeAbsolutePath);
        var analysisListener = SetUpAnalysisListener(ConfigScopeId, analysisReadyCompletionSource, analysisRaisedIssues);
        var listFilesListener = Substitute.For<IListFilesListener>();
        listFilesListener.ListFilesAsync(Arg.Any<ListFilesParams>())
            .Returns(Task.FromResult(new ListFilesResponse([ fileToAnalyze ])));

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
