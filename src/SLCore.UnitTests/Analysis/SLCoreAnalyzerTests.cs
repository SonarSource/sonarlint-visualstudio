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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.Service.TaskProgress;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Analysis;

[TestClass]
public class SLCoreAnalyzerTests
{
    private const string ConfigScopeId = "ConfigScopeId";
    private const string FilePath = @"C:\file\path";
    private ISLCoreServiceProvider slCoreServiceProvider;
    private IAnalysisSLCoreService analysisService;
    private ITaskProgressSLCoreService taskProgressSlCoreService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
    private IAnalysisStatusNotifier notifier;
    private ILogger logger;
    private SLCoreAnalyzer testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        analysisService = Substitute.For<IAnalysisSLCoreService>();
        taskProgressSlCoreService = Substitute.For<ITaskProgressSLCoreService>();
        SetUpAnalysisServiceProvider();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();
        notifier = Substitute.For<IAnalysisStatusNotifier>();
        SetUpDefaultNotifier();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        testSubject = new SLCoreAnalyzer(slCoreServiceProvider, activeConfigScopeTracker, analysisStatusNotifierFactory, logger);

        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri>(), []));

        void SetUpDefaultNotifier() => analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), Arg.Any<string[]>()).Returns(notifier);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SLCoreAnalyzer, IAnalyzer>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SLCoreAnalyzer>();

    [TestMethod]
    public void Ctor_LoggerSetsContext() => logger.Received(1).ForContext(nameof(SLCoreAnalyzer));

    [TestMethod]
    public async Task ExecuteAnalysis_CreatesNotifierAndStarts()
    {
        await testSubject.ExecuteAnalysis([FilePath]);

        analysisStatusNotifierFactory.Received().Create(nameof(SLCoreAnalyzer), [FilePath]);
        notifier.Received().AnalysisStarted();
    }

    [TestMethod]
    public async Task ExecuteAnalysis_ConfigScopeNotInitialized_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        await testSubject.ExecuteAnalysis([FilePath]);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(null, SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysis_ConfigScopeNotReadyForAnalysis_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: false));

        await testSubject.ExecuteAnalysis([FilePath]);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(null, SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysis_ServiceProviderUnavailable_NotifyFailed()
    {
        SetUpAnalysisServiceProvider(false);
        SetUpInitializedConfigScope();

        await testSubject.ExecuteAnalysis([FilePath]);

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>());
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisFailed(null, SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysis_AnalysisServiceSucceeds_ReturnsAnalysisIdFromSlCore()
    {
        SetUpInitializedConfigScope();
        var expectedAnalysisId = Guid.NewGuid();
        MockAnalyzeFileList(expectedAnalysisId);

        var analysisId = await testSubject.ExecuteAnalysis([FilePath]);

        analysisId.Should().Be(expectedAnalysisId);
        await analysisService.Received(1)
            .AnalyzeFileListAsync(Arg.Is<AnalyzeFileListParams>(x => x.configScopeId == ConfigScopeId && x.filesToAnalyze.SequenceEqual(new List<FileUri> { new FileUri(FilePath) })));
        AssertAnalysisNotFailed();
    }

    [TestMethod]
    public async Task ExecuteAnalysis_AnalysisServiceReturnsNull_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        MockAnalyzeFileList(analysisId: null);

        await testSubject.ExecuteAnalysis([FilePath]);

        notifier.Received().AnalysisFailed(null, SLCoreStrings.AnalysisFailedReason);
    }

    [TestMethod]
    public async Task ExecuteAnalysis_AnalysisServiceThrows_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        var exception = new Exception();
        analysisService.AnalyzeFileListAsync(Arg.Any<AnalyzeFileListParams>()).ThrowsAsyncForAnyArgs(exception);

        await testSubject.ExecuteAnalysis([FilePath]);

        notifier.Received().AnalysisFailed(null, exception);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenFiles_CreatesNotifierAndStarts()
    {
        await testSubject.ExecuteAnalysisForOpenFiles();

        analysisStatusNotifierFactory.Received().Create(nameof(SLCoreAnalyzer));
        notifier.Received().AnalysisStarted();
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenFiles_ConfigScopeNotInitialized_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        await testSubject.ExecuteAnalysisForOpenFiles();

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(null, SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenFiles_ConfigScopeNotReadyForAnalysis_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: false));

        await testSubject.ExecuteAnalysisForOpenFiles();

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(null, SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenFiles_ServiceProviderUnavailable_NotifyFailed()
    {
        SetUpAnalysisServiceProvider(false);
        SetUpInitializedConfigScope();

        await testSubject.ExecuteAnalysisForOpenFiles();

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>());
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisFailed(null, SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenFiles_AnalysisServiceSucceeds_ReturnsAnalysisIdFromSlCore()
    {
        SetUpInitializedConfigScope();
        var expectedAnalysisId = Guid.NewGuid();
        MockAnalyzeOpenFiles(expectedAnalysisId);

        var analysisId = await testSubject.ExecuteAnalysisForOpenFiles();

        analysisId.Should().Be(expectedAnalysisId);
        await analysisService.Received(1)
            .AnalyzeOpenFilesAsync(Arg.Is<AnalyzeOpenFilesParams>(x => x.configScopeId == ConfigScopeId));
        AssertAnalysisNotFailed();
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenFiles_AnalysisServiceReturnsNull_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        MockAnalyzeOpenFiles(analysisId: null);

        await testSubject.ExecuteAnalysisForOpenFiles();

        notifier.Received().AnalysisFailed(null, SLCoreStrings.AnalysisFailedReason);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenFiles_AnalysisServiceThrows_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        var exception = new Exception();
        analysisService.AnalyzeOpenFilesAsync(Arg.Any<AnalyzeOpenFilesParams>()).ThrowsAsyncForAnyArgs(exception);

        await testSubject.ExecuteAnalysisForOpenFiles();

        notifier.Received().AnalysisFailed(null, exception);
    }

    [TestMethod]
    public void CancelAnalysis_ServiceProviderUnavailable_NotifyFailed()
    {
        var expectedAnalysisId = Guid.NewGuid();
        SetUpTaskProgressSLCoreService(result: false);

        testSubject.CancelAnalysis(expectedAnalysisId);

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<ITaskProgressSLCoreService>());
        logger.Received(1).WriteLine(SLCoreStrings.ServiceProviderNotInitialized);
        taskProgressSlCoreService.DidNotReceiveWithAnyArgs().CancelTask(Arg.Any<CancelTaskParams>());
    }

    [TestMethod]
    public void CancelAnalysis_AnalysisCancellationSucceeds_Logs()
    {
        var expectedAnalysisId = Guid.NewGuid();
        SetUpTaskProgressSLCoreService(result: true);

        testSubject.CancelAnalysis(expectedAnalysisId);

        taskProgressSlCoreService.Received(1).CancelTask(Arg.Is<CancelTaskParams>(x => x.taskId == expectedAnalysisId.ToString()));
        logger.Received(1).WriteLine(SLCoreStrings.AnalysisCancelled, expectedAnalysisId);
    }

    private void AssertAnalysisNotFailed()
    {
        notifier.DidNotReceiveWithAnyArgs().AnalysisCancelled(default);
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default, default(Exception));
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default, default(string));
        notifier.ReceivedWithAnyArgs().AnalysisFinished(default, default);
    }

    private void SetUpAnalysisServiceProvider(bool result = true) =>
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>())
            .Returns(info =>
            {
                info[0] = analysisService;
                return result;
            });

    private void SetUpTaskProgressSLCoreService(bool result = true) =>
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaskProgressSLCoreService>())
            .Returns(info =>
            {
                info[0] = taskProgressSlCoreService;
                return result;
            });

    private void SetUpInitializedConfigScope() => activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: true));

    private void MockAnalyzeFileList(Guid? analysisId) => analysisService.AnalyzeFileListAsync(Arg.Any<AnalyzeFileListParams>()).Returns(new ForceAnalyzeResponse(analysisId));

    private void MockAnalyzeOpenFiles(Guid? analysisId) => analysisService.AnalyzeOpenFilesAsync(Arg.Any<AnalyzeOpenFilesParams>()).Returns(new ForceAnalyzeResponse(analysisId));
}
