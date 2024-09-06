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
using NSubstitute.ClearExtensions;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

internal sealed class FileAnalysisTestsRunner : IDisposable
{
    internal static readonly JavaScriptIssuesFile JavaScriptIssues = new();
    internal static readonly OneIssueRuleWithParamFile OneIssueRuleWithParam = new();
    internal static readonly TypeScriptIssuesFile TypeScriptIssues = new();
    internal static readonly CssIssuesFile CssIssues = new();
    internal static readonly VueIssuesFile VueIssues = new();
    internal static readonly SecretsIssuesFile SecretsIssues = new();
    private readonly ActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IListFilesListener listFilesListener;
    private readonly IAnalysisListener analysisListener;
    private readonly SLCoreTestRunner slCoreTestRunner;

    internal FileAnalysisTestsRunner(string testClassName, Dictionary<string, StandaloneRuleConfigDto> initialRuleConfig = null)
    {
        slCoreTestRunner = new SLCoreTestRunner(new TestLogger(), new TestLogger(), testClassName);

        analysisListener = Substitute.For<IAnalysisListener>();

        listFilesListener = Substitute.For<IListFilesListener>();

        slCoreTestRunner.AddListener(new LoggerListener(new TestLogger()));
        slCoreTestRunner.AddListener(new ProgressListener());
        slCoreTestRunner.AddListener(analysisListener);
        slCoreTestRunner.AddListener(listFilesListener);
        slCoreTestRunner.AddListener(new AnalysisConfigurationProviderListener());

        slCoreTestRunner.MockInitialSlCoreRulesSettings(initialRuleConfig ?? []);

        slCoreTestRunner.Start();

        activeConfigScopeTracker = new ActiveConfigScopeTracker(slCoreTestRunner.SLCoreServiceProvider,
            new AsyncLockFactory(),
            new NoOpThreadHandler());
    }

    public void SetRuleConfiguration(Dictionary<string, StandaloneRuleConfigDto> ruleConfig)
    {
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesCoreService)
            .Should().BeTrue();
        rulesCoreService.UpdateStandaloneRulesConfiguration(new UpdateStandaloneRulesConfigurationParams(ruleConfig));
    }

    public async Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunFileAnalysis(ITestingFile testingFile, string configScope,
        bool sendContent = false)
    {
        try
        {
            SetUpListFiles(testingFile.RelativePath, sendContent, configScope, testingFile.GetFullPath());
            var analysisId = Guid.NewGuid();
            var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
            var analysisRaisedIssues = new TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>>();
            SetUpAnalysisListener(
                configScope,
                analysisId,
                analysisReadyCompletionSource,
                analysisRaisedIssues);
            activeConfigScopeTracker.SetCurrentConfigScope(configScope);


            await RunSlCoreFileAnalysis(configScope, testingFile.GetFullPath(), analysisId);
            await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisReadyCompletionSource.Task);
            await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisRaisedIssues.Task);

            return analysisRaisedIssues.Task.Result.issuesByFileUri;
        }
        finally
        {
            activeConfigScopeTracker.RemoveCurrentConfigScope();
        }
    }

    private void SetUpListFiles(string fileToAnalyzeRelativePath, bool sendContent, string configScope,
        string fileToAnalyzeAbsolutePath)
    {
        listFilesListener.ClearSubstitute();
        listFilesListener.ListFilesAsync(Arg.Is<ListFilesParams>(p => p.configScopeId == configScope))
            .Returns(Task.FromResult(new ListFilesResponse([
                CreateFileToAnalyze(fileToAnalyzeRelativePath, fileToAnalyzeAbsolutePath, configScope, sendContent)
            ])));
    }

    private void SetUpAnalysisListener(
        string configScopeId,
        Guid analysisId,
        TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource,
        TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>> analysisRaisedIssues)
    {
        analysisListener.ClearSubstitute();
        analysisListener.When(l =>
                l.DidChangeAnalysisReadiness(Arg.Is<DidChangeAnalysisReadinessParams>(a =>
                    a.areReadyForAnalysis && a.configurationScopeIds.Contains(configScopeId))))
            .Do(info =>
            {
                TraceTest("Readiness was raised");
                analysisReadyCompletionSource.SetResult(info.Arg<DidChangeAnalysisReadinessParams>());
            });

        analysisListener.When(x => x.RaiseIssues(Arg.Any<RaiseFindingParams<RaisedIssueDto>>()))
            .Do(info =>
            {
                TraceTest("RaiseIssue was raised");
                var raiseIssuesParams = info.Arg<RaiseFindingParams<RaisedIssueDto>>();
                TraceTest("raiseIssuesParams.analysisId=" + raiseIssuesParams.analysisId);
                TraceTest("analysisId=" + analysisId);
                if (raiseIssuesParams.analysisId == analysisId && !raiseIssuesParams.isIntermediatePublication)
                {
                    TraceTest($"analysisRaisedIssues was set");
                    if (raiseIssuesParams.issuesByFileUri.Any())
                    {
                        TraceTest($"issues: {raiseIssuesParams.issuesByFileUri.First().Value.Count}");
                    }
                    analysisRaisedIssues.SetResult(raiseIssuesParams);
                }
            });
    }

    private static void TraceTest(string message)
    {
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }

    private async Task RunSlCoreFileAnalysis(string configScopeId, string fileToAnalyzeAbsolutePath, Guid analysisId)
    {
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IAnalysisSLCoreService analysisService).Should().BeTrue();

        TraceTest($"Content of file {fileToAnalyzeAbsolutePath} being analyzed: {File.ReadAllText(fileToAnalyzeAbsolutePath)}");

        var (failedAnalysisFiles, _) = await analysisService.AnalyzeFilesAndTrackAsync(
            new AnalyzeFilesAndTrackParams(configScopeId, analysisId,
                [new FileUri(fileToAnalyzeAbsolutePath)], [], false,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), CancellationToken.None);
        failedAnalysisFiles.Should().BeEmpty();
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

    public void Dispose()
    {
        activeConfigScopeTracker?.Dispose();
        slCoreTestRunner?.Dispose();
    }
}


internal interface ITestingFile
{
    string RelativePath { get; }
    List<ExpectedTestIssue> ExpectedIssues { get; }
}

internal record ExpectedTestIssue(string ruleKey, TextRangeDto textRange, RuleType type, int expectedFlows);

internal class JavaScriptIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\JavaScriptIssues.js";

    public List<ExpectedTestIssue> ExpectedIssues =>
    [
        new ExpectedTestIssue("javascript:S1135", new TextRangeDto(1, 3, 1, 7), RuleType.CODE_SMELL, 0),
        new ExpectedTestIssue("javascript:S3504", new TextRangeDto(2, 0, 2, 5), RuleType.CODE_SMELL, 0)
    ];
}

internal class OneIssueRuleWithParamFile : ITestingFile
{
    public string RelativePath => @"Resources\RuleParam.js";
    
    public readonly string CtorParamRuleId = "javascript:S107";
    public readonly  int ActualCtorParams = 4;
    public readonly  string CtorParamName = "maximumFunctionParameters";
    public List<ExpectedTestIssue> ExpectedIssues { get; set; }
}

internal class TypeScriptIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\TypeScriptIssues.ts";

    public List<ExpectedTestIssue> ExpectedIssues =>
    [
        new ExpectedTestIssue("typescript:S2737", new TextRangeDto(3, 2, 3, 7), RuleType.CODE_SMELL, 0),
        new ExpectedTestIssue("typescript:S1186", new TextRangeDto(7, 16, 7, 19), RuleType.CODE_SMELL, 0),
        new ExpectedTestIssue("typescript:S3776", new TextRangeDto(30, 9, 30, 18), RuleType.CODE_SMELL, 21)
    ];
}

internal class CssIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\CssIssues.css";

    public List<ExpectedTestIssue> ExpectedIssues =>
    [
        new ExpectedTestIssue("css:S4666", new TextRangeDto(20, 0, 20, 77), RuleType.CODE_SMELL, 0),
        new ExpectedTestIssue("css:S4655", new TextRangeDto(12, 0, 12, 38), RuleType.BUG, 0),
    ];
}

internal class VueIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\VueIssues.vue";

    public List<ExpectedTestIssue> ExpectedIssues =>
    [
        new ExpectedTestIssue("css:S4661", new TextRangeDto(12, 0, 12, 43), RuleType.BUG, 0),
        new ExpectedTestIssue("css:S4658", new TextRangeDto(12, 0, 12, 43), RuleType.CODE_SMELL, 0),
    ];
}

internal class SecretsIssuesFile : ITestingFile
{
    private const string CloudSecretsRuleKey = "secrets:S6336";
    public string RelativePath => @"Resources\Secrets.yml";
    public (string ruleKey, int issuesCount) RuleWithMultipleIssues => (CloudSecretsRuleKey, 2);

    public List<ExpectedTestIssue> ExpectedIssues =>
    [
        new ExpectedTestIssue(CloudSecretsRuleKey, new TextRangeDto(9, 1, 9, 25), RuleType.VULNERABILITY, 0),
        new ExpectedTestIssue(CloudSecretsRuleKey, new TextRangeDto(14, 24, 14, 54), RuleType.VULNERABILITY, 0),
        new ExpectedTestIssue("secrets:S6337", new TextRangeDto(20, 12, 20, 56), RuleType.VULNERABILITY, 0),
    ];
}

internal static class TestingFileExtensions
{
    public static string GetFullPath(this ITestingFile testingFile)
    {
        var currentDomainBaseDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        var integrationTestBaseDirectory = currentDomainBaseDirectory.Parent.Parent.Parent.FullName;
        return Path.Combine(integrationTestBaseDirectory, testingFile.RelativePath);
    }
}
