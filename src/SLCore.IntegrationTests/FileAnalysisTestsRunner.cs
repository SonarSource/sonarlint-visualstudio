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

using System.IO;
using System.Text;
using NSubstitute.ClearExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Notifications;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Listener.Analysis;
using SonarLint.VisualStudio.SLCore.Listener.Analysis.Models;
using SonarLint.VisualStudio.SLCore.Listener.Files;
using SonarLint.VisualStudio.SLCore.Listener.Files.Models;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation;
using SonarLint.VisualStudio.SLCore.Listeners.Implementation.Analysis;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.File;
using SonarLint.VisualStudio.SLCore.Service.Rules;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.IntegrationTests;

internal sealed class FileAnalysisTestsRunner : IDisposable
{
    private readonly TimeSpan AnalysisReadinessWaitTimeout = TimeSpan.FromSeconds(60);
    private readonly TimeSpan AnalysisCompletionWaitTimeout = TimeSpan.FromSeconds(60);
    internal static readonly JavaScriptIssuesFile JavaScriptIssues = new();
    internal static readonly OneIssueRuleWithParamFile OneIssueRuleWithParam = new();
    internal static readonly TypeScriptIssuesFile TypeScriptIssues = new();
    internal static readonly TypeScriptWithBomFile TypeScriptWithBom = new();
    internal static readonly CFamilyIssuesFile CFamilyIssues = new();
    internal static readonly CssIssuesFile CssIssues = new();
    internal static readonly VueIssuesFile VueIssues = new();
    internal static readonly SecretsIssuesFile SecretsIssues = new();
    internal static readonly HtmlIssuesFile HtmlIssues = new();
    private readonly ActiveConfigScopeTracker activeConfigScopeTracker;
    private readonly IListFilesListener listFilesListener;
    private readonly IAnalysisListener analysisListener;
    private readonly SLCoreTestRunner slCoreTestRunner;
    private readonly TestLogger infrastructureLogger;
    private readonly TestLogger slCoreStdErrorLogger;
    private readonly TestLogger rpcLogger;
    private readonly IGetFileExclusionsListener getFileExclusionsListener;
    private readonly IClientFileDtoFactory clientFileDtoFactory;

    private FileAnalysisTestsRunner(string testClassName, Dictionary<string, StandaloneRuleConfigDto> initialRuleConfig = null)
    {
        infrastructureLogger = new TestLogger();
        slCoreStdErrorLogger = new TestLogger();
        slCoreTestRunner = new SLCoreTestRunner(infrastructureLogger, slCoreStdErrorLogger, testClassName);

        analysisListener = Substitute.For<IAnalysisListener>();
        getFileExclusionsListener = Substitute.For<IGetFileExclusionsListener>();
        getFileExclusionsListener.GetFileExclusionsAsync(Arg.Any<GetFileExclusionsParams>()).Returns(new GetFileExclusionsResponse([]));
        listFilesListener = Substitute.For<IListFilesListener>();

        rpcLogger = new TestLogger();
        slCoreTestRunner.AddListener(new LoggerListener(rpcLogger));
        slCoreTestRunner.AddListener(new ProgressListener(Substitute.For<IStatusBarNotifier>()));
        slCoreTestRunner.AddListener(analysisListener);
        slCoreTestRunner.AddListener(listFilesListener);
        slCoreTestRunner.AddListener(new AnalysisConfigurationProviderListener(Substitute.For<IActiveConfigScopeTracker>(), Substitute.For<IGitWorkspaceService>()));
        slCoreTestRunner.AddListener(getFileExclusionsListener);

        clientFileDtoFactory = new ClientFileDtoFactory(infrastructureLogger);
        slCoreTestRunner.MockInitialSlCoreRulesSettings(initialRuleConfig ?? []);

        activeConfigScopeTracker = new ActiveConfigScopeTracker(slCoreTestRunner.SLCoreServiceProvider,
            new AsyncLockFactory(),
            new NoOpThreadHandler());
    }

    public static async Task<FileAnalysisTestsRunner> CreateInstance(string testClassName, Dictionary<string, StandaloneRuleConfigDto> initialRuleConfig = null)
    {
        var runner = new FileAnalysisTestsRunner(testClassName, initialRuleConfig);
        await runner.slCoreTestRunner.Start(runner.rpcLogger);
        return runner;
    }

    public void SetRuleConfiguration(Dictionary<string, StandaloneRuleConfigDto> ruleConfig)
    {
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IRulesSLCoreService rulesCoreService)
            .Should().BeTrue();
        rulesCoreService.UpdateStandaloneRulesConfiguration(new UpdateStandaloneRulesConfigurationParams(ruleConfig));
    }

    public IGetFileExclusionsListener SetFileExclusionsInMockedListener(string configScopeId, IEnumerable<string> fileExclusions)
    {
        getFileExclusionsListener.ClearSubstitute();
        getFileExclusionsListener.GetFileExclusionsAsync(Arg.Is<GetFileExclusionsParams>(x => x.configurationScopeId == configScopeId))
            .Returns(new GetFileExclusionsResponse(fileExclusions.ToHashSet()));
        return getFileExclusionsListener;
    }

    public async Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunAnalysisOnOpenFile(
        ITestingFile testingFile,
        string configScope,
        bool sendContent = false,
        string compilationDatabasePath = null)
    {
        try
        {
            var analysisRaisedIssues = await SetUpAnalysis(configScope, sendContent, compilationDatabasePath, testingFile);
            NotifyDidOpenFile(configScope, testingFile.GetFullPath());
            await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisRaisedIssues.Task, "analysis completion", AnalysisCompletionWaitTimeout);
            return analysisRaisedIssues.Task.Result.issuesByFileUri;
        }
        finally
        {
            activeConfigScopeTracker.RemoveCurrentConfigScope();
        }
    }

    public async Task<Dictionary<FileUri, List<RaisedIssueDto>>> RunAnalysisOnUpdateFiles(
        List<ITestingFile> testingFiles,
        string configScope,
        string compilationDatabasePath = null)
    {
        try
        {
            // SlCore triggers analysis when DidUpdateFileSystem is invoked only for the opened files
            testingFiles.ForEach(x => NotifyDidOpenFile(configScope, x.GetFullPath()));
            var analysisRaisedIssues = await SetUpAnalysis(configScope, sendContent: false, compilationDatabasePath, testingFiles.ToArray());
            NotifyDidUpdateFileSystem(configScope, testingFiles);
            await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisRaisedIssues.Task, "analysis completion", AnalysisCompletionWaitTimeout);
            return analysisRaisedIssues.Task.Result.issuesByFileUri;
        }
        finally
        {
            activeConfigScopeTracker.RemoveCurrentConfigScope();
        }
    }

    public async Task VerifyAnalysisSkippedForExclusions(
        ITestingFile testingFile,
        string configScope,
        bool sendContent = false)
    {
        try
        {
            var analysisRaisedIssues = await SetUpAnalysis(configScope, sendContent, compilationDatabasePath: null, testingFile);
            var fileExclusionListenerCompletionSource = new TaskCompletionSource<int>();
            getFileExclusionsListener.When(x => x.GetFileExclusionsAsync(Arg.Any<GetFileExclusionsParams>())).Do(callInfo =>
            {
                var fileExclusionsParams = callInfo.Arg<GetFileExclusionsParams>();
                if (fileExclusionsParams.configurationScopeId == configScope)
                {
                    fileExclusionListenerCompletionSource.SetResult(1);
                }
            });
            NotifyDidOpenFile(configScope, testingFile.GetFullPath());
            await ConcurrencyTestHelper.WaitForTaskWithTimeout(fileExclusionListenerCompletionSource.Task, "file exclusions listener");
            await Task.WhenAny(analysisRaisedIssues.Task, Task.Delay(TimeSpan.FromSeconds(2))); // wait for a short time to see if any issues are raised
            analysisRaisedIssues.Task.IsCompleted.Should().BeFalse();
        }
        finally
        {
            activeConfigScopeTracker.RemoveCurrentConfigScope();
        }
    }

    private async Task<TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>>> SetUpAnalysis(
        string configScope,
        bool sendContent,
        string compilationDatabasePath = null,
        params ITestingFile[] testingFiles)
    {
        SetUpListFiles(sendContent, configScope, testingFiles);
        var analysisReadyCompletionSource = new TaskCompletionSource<DidChangeAnalysisReadinessParams>();
        var analysisRaisedIssues = new TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>>();
        SetUpAnalysisListener(
            configScope,
            analysisReadyCompletionSource,
            analysisRaisedIssues);
        activeConfigScopeTracker.SetCurrentConfigScope(configScope);
        SetupCompilationDatabase(configScope, compilationDatabasePath);

        await ConcurrencyTestHelper.WaitForTaskWithTimeout(analysisReadyCompletionSource.Task, "analysis readiness", AnalysisReadinessWaitTimeout);
        return analysisRaisedIssues;
    }

    private void SetupCompilationDatabase(string configScope, string compilationDatabasePath)
    {
        if (compilationDatabasePath is null)
        {
            return;
        }
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out ICFamilyAnalysisConfigurationSLCoreService slCoreService).Should().BeTrue();
        slCoreService.DidChangePathToCompileCommands(new DidChangePathToCompileCommandsParams(configScope, compilationDatabasePath));
    }

    private void SetUpListFiles(
        bool sendContent,
        string configScope,
        params ITestingFile[] testingFiles)
    {
        listFilesListener.ClearSubstitute();
        var testFilesToAnalyze = testingFiles.Select(x =>
            CreateFileToAnalyze(x.RelativePath, x.GetFullPath(), configScope, sendContent)).ToList();
        listFilesListener.ListFilesAsync(Arg.Is<ListFilesParams>(p => p.configScopeId == configScope))
            .Returns(Task.FromResult(new ListFilesResponse(testFilesToAnalyze)));
    }

    private void SetUpAnalysisListener(
        string configScopeId,
        TaskCompletionSource<DidChangeAnalysisReadinessParams> analysisReadyCompletionSource,
        TaskCompletionSource<RaiseFindingParams<RaisedIssueDto>> analysisRaisedIssues)
    {
        analysisListener.ClearSubstitute();
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
    }

    private void NotifyDidOpenFile(string configScopeId, string fileToAnalyzeAbsolutePath)
    {
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IFileRpcSLCoreService fileRpcService).Should().BeTrue();

        fileRpcService!.DidOpenFile(new DidOpenFileParams(configScopeId, new FileUri(fileToAnalyzeAbsolutePath)));
    }

    private void NotifyDidUpdateFileSystem(string configScopeId, List<ITestingFile> testingFiles)
    {
        slCoreTestRunner.SLCoreServiceProvider.TryGetTransientService(out IFileRpcSLCoreService fileRpcService).Should().BeTrue();

        var root = Path.GetPathRoot(testingFiles[0].GetFullPath());
        var sourceFiles = testingFiles.Select(x => new SourceFile(x.GetFullPath()));
        var addedFiles = sourceFiles.Select(x => clientFileDtoFactory.CreateOrNull(configScopeId, root, x));

        fileRpcService!.DidUpdateFileSystem(new DidUpdateFileSystemParams([], [], addedFiles.ToList()));
    }

    private static ClientFileDto CreateFileToAnalyze(
        string fileToAnalyzeRelativePath,
        string fileToAnalyzeAbsolutePath,
        string configScopeId,
        bool sendContent) =>
        new(new FileUri(fileToAnalyzeAbsolutePath),
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

public interface ITestingFile
{
    string RelativePath { get; }
    List<TestIssue> ExpectedIssues { get; }
}

internal interface ITestingCFamily : ITestingFile
{
    string GetCompilationDatabasePath();
}

public record TestIssue(
    string ruleKey,
    TextRangeDto textRange,
    CleanCodeAttribute? cleanCodeAttribute,
    int expectedFlows);

internal class JavaScriptIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\JavaScriptIssues.js";

    public List<TestIssue> ExpectedIssues =>
    [
        new("javascript:S1135", new TextRangeDto(1, 3, 1, 7), CleanCodeAttribute.COMPLETE, 0),
        new("javascript:S3504", new TextRangeDto(2, 0, 2, 5), CleanCodeAttribute.CLEAR, 0)
    ];
}

internal class OneIssueRuleWithParamFile : ITestingFile
{
    public string RelativePath => @"Resources\RuleParam.js";

    public const string CtorParamRuleId = "javascript:S107";
    public const int ActualCtorParams = 4;
    public const string CtorParamName = "maximumFunctionParameters";
    public List<TestIssue> ExpectedIssues { get; set; }
}

internal class TypeScriptIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\TypeScriptIssues.ts";

    public List<TestIssue> ExpectedIssues =>
    [
        new("typescript:S2737", new TextRangeDto(3, 2, 3, 7), CleanCodeAttribute.CLEAR, 0),
        new("typescript:S1186", new TextRangeDto(7, 16, 7, 19), CleanCodeAttribute.COMPLETE, 0),
        new("typescript:S3776", new TextRangeDto(30, 9, 30, 18), CleanCodeAttribute.FOCUSED, 21)
    ];
}

internal class TypeScriptWithBomFile : ITestingFile
{
    public string RelativePath => @"Resources\TypeScriptWithBom.ts";

    public List<TestIssue> ExpectedIssues =>
    [
        new("typescript:S1135", new TextRangeDto(1, 3, 1, 7), CleanCodeAttribute.COMPLETE, 0),
        new("typescript:S2737", new TextRangeDto(5, 2, 5, 7), CleanCodeAttribute.CLEAR, 0),
    ];
}

internal class CFamilyIssuesFile : ITestingCFamily
{
    public string RelativePath => @"Resources\CFamilyIssues.cpp";

    public List<TestIssue> ExpectedIssues =>
    [
        new("cpp:S1135", new TextRangeDto(7, 4, 7, 29), CleanCodeAttribute.COMPLETE, 0),
        new("cpp:S1481", new TextRangeDto(10, 9, 10, 10), CleanCodeAttribute.CLEAR, 0),
        new("cpp:S5350", new TextRangeDto(10, 4, 10, 17), CleanCodeAttribute.CLEAR, 0),
        new("cpp:S4962", new TextRangeDto(10, 13, 10, 17), CleanCodeAttribute.CONVENTIONAL, 0),
    ];

    public string GetCompilationDatabasePath() => GenerateTestCompilationDatabase();

    private string GenerateTestCompilationDatabase()
    {
        /* The CFamily analysis apart from the source code file requires also the compilation database file.
           The compilation database file must contain the absolute path to the source code file the compilation database json file and the compiler path.
           For the compiler we use the MSVC which is set as an environment variable. Make sure the environment variable is set to point to the compiler path
           (the absolute path to cl.exe). */
        var compilerPath = NormalizePath(Environment.GetEnvironmentVariable("MSVC"));
        File.Exists(compilerPath).Should().BeTrue(compilerPath);
        var cFamilyIssuesFileAbsolutePath = NormalizePath(this.GetFullPath());
        var analysisDirectory = NormalizePath(Path.GetDirectoryName(cFamilyIssuesFileAbsolutePath));
        var jsonContent = $$"""
                            [
                            {
                              "directory": "{{analysisDirectory}}",
                              "command": "\"{{compilerPath}}\" /nologo /TP /DWIN32 /D_WINDOWS /W3 /GR /EHsc /MDd /Ob0 /Od /RTC1 -std:c++20 -ZI /FoCFamilyIssues.cpp.obj /FS -c {{cFamilyIssuesFileAbsolutePath}}",
                              "file": "{{cFamilyIssuesFileAbsolutePath}}"
                            }
                            ]
                            """;
        var tempCompilationDatabase = Path.ChangeExtension(Path.GetTempFileName(), ".json");
        File.WriteAllText(tempCompilationDatabase, jsonContent);

        return tempCompilationDatabase;
    }

    private static string NormalizePath(string path)
    {
        var singleDirectorySeparator = Path.DirectorySeparatorChar.ToString();
        var doubleDirectorySeparator = singleDirectorySeparator + singleDirectorySeparator;
        return path?.Replace(singleDirectorySeparator, doubleDirectorySeparator);
    }
}

internal class CssIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\CssIssues.css";

    public List<TestIssue> ExpectedIssues =>
    [
        new("css:S4666", new TextRangeDto(20, 0, 20, 77), CleanCodeAttribute.LOGICAL, 0),
        new("css:S4655", new TextRangeDto(12, 0, 12, 38), CleanCodeAttribute.LOGICAL, 0),
    ];
}

internal class VueIssuesFile : ITestingFile
{
    public string RelativePath => @"Resources\VueIssues.vue";

    public List<TestIssue> ExpectedIssues =>
    [
        new("css:S4661", new TextRangeDto(12, 0, 12, 43), CleanCodeAttribute.LOGICAL, 0),
        new("css:S4658", new TextRangeDto(12, 0, 12, 43), CleanCodeAttribute.CLEAR, 0),
    ];
}

internal class SecretsIssuesFile : ITestingFile
{
    private const string AmazonSecretsRuleKey = "secrets:S6290";
    private const string AzureSecretsRuleKey = "secrets:S6684";
    public string RelativePath => @"Resources\Secrets.yml";
    public (string ruleKey, int issuesCount) RuleWithMultipleIssues => (AmazonSecretsRuleKey, 2);

    public List<TestIssue> ExpectedIssues =>
    [
        new(AmazonSecretsRuleKey, new TextRangeDto(9, 38, 9, 78), CleanCodeAttribute.TRUSTWORTHY, 0),
        new(AmazonSecretsRuleKey, new TextRangeDto(14, 38, 14, 78), CleanCodeAttribute.TRUSTWORTHY, 0),
        new(AzureSecretsRuleKey, new TextRangeDto(20, 33, 20, 65), CleanCodeAttribute.TRUSTWORTHY, 0),
    ];
}

internal class HtmlIssuesFile : ITestingFile
{
    public const string WebS6844RuleKey = "Web:S6844";

    public string RelativePath => @"Resources\HtmlIssues.html";
    public (string ruleKey, int issuesCount) RuleWithMultipleIssues => (WebS6844RuleKey, 3);

    public List<TestIssue> ExpectedIssues
    {
        get
        {
            return
            [
                new("Web:S5254", new TextRangeDto(2, 0, 2, 6), CleanCodeAttribute.COMPLETE, 0),
                new(WebS6844RuleKey, new TextRangeDto(5, 4, 5, 47), CleanCodeAttribute.CONVENTIONAL, 0),
                new(WebS6844RuleKey, new TextRangeDto(6, 4, 6, 30), CleanCodeAttribute.CONVENTIONAL, 0),
                new(WebS6844RuleKey, new TextRangeDto(7, 4, 7, 21), CleanCodeAttribute.CONVENTIONAL, 0),
                new("Web:S6811", new TextRangeDto(9, 0, 9, 38), CleanCodeAttribute.CONVENTIONAL, 0),
                new("Web:S6819", new TextRangeDto(9, 0, 9, 38), CleanCodeAttribute.CONVENTIONAL, 0),
                new("Web:PageWithoutTitleCheck", new TextRangeDto(2, 0, 2, 7), CleanCodeAttribute.CONVENTIONAL, 0),
            ];
        }
    }
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
