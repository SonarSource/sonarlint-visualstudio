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
        testSubject = new SLCoreAnalyzer(slCoreServiceProvider, activeConfigScopeTracker, analysisStatusNotifierFactory, logger);

        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri>(), []));

        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2049 Pass the analysis ID and the paths to the correct method
        void SetUpDefaultNotifier() => analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), Arg.Any<string>(), Arg.Any<Guid?>()).Returns(notifier);
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
    public void Ctor_LoggerSetsContext() => logger.Received(1).ForVerboseContext(nameof(SLCoreAnalyzer));

    [TestMethod]
    public async Task ExecuteAnalysis_CreatesNotifierAndStarts()
    {
        await testSubject.ExecuteAnalysis([FilePath]);

        analysisStatusNotifierFactory.Received().Create(nameof(SLCoreAnalyzer), FilePath, null);
        notifier.Received().AnalysisStarted();
    }

    [TestMethod]
    public async Task ExecuteAnalysis_ConfigScopeNotInitialized_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        await testSubject.ExecuteAnalysis([FilePath]);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysis_ConfigScopeNotReadyForAnalysis_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: false));

        await testSubject.ExecuteAnalysis([FilePath]);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysis_ServiceProviderUnavailable_NotifyFailed()
    {
        SetUpAnalysisServiceProvider(false);
        SetUpInitializedConfigScope();

        await testSubject.ExecuteAnalysis([FilePath]);

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>());
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisFailed(SLCoreStrings.ServiceProviderNotInitialized);
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

        notifier.Received().AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
    }

    [TestMethod]
    public async Task ExecuteAnalysis_AnalysisServiceThrows_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        var exception = new Exception();
        analysisService.AnalyzeFileListAsync(Arg.Any<AnalyzeFileListParams>()).ThrowsAsyncForAnyArgs(exception);

        await testSubject.ExecuteAnalysis([FilePath]);

        notifier.Received().AnalysisFailed(exception);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenedFiles_CreatesNotifierAndStarts()
    {
        await testSubject.ExecuteAnalysisForOpenedFiles();

        analysisStatusNotifierFactory.Received().Create(nameof(SLCoreAnalyzer), null, null);
        notifier.Received().AnalysisStarted();
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenedFiles_ConfigScopeNotInitialized_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        await testSubject.ExecuteAnalysisForOpenedFiles();

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenedFiles_ConfigScopeNotReadyForAnalysis_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: false));

        await testSubject.ExecuteAnalysisForOpenedFiles();

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenedFiles_ServiceProviderUnavailable_NotifyFailed()
    {
        SetUpAnalysisServiceProvider(false);
        SetUpInitializedConfigScope();

        await testSubject.ExecuteAnalysisForOpenedFiles();

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>());
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisFailed(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenedFiles_AnalysisServiceSucceeds_ReturnsAnalysisIdFromSlCore()
    {
        SetUpInitializedConfigScope();
        var expectedAnalysisId = Guid.NewGuid();
        MockAnalyzeOpenFiles(expectedAnalysisId);

        var analysisId = await testSubject.ExecuteAnalysisForOpenedFiles();

        analysisId.Should().Be(expectedAnalysisId);
        await analysisService.Received(1)
            .AnalyzeOpenFilesAsync(Arg.Is<AnalyzeOpenFilesParams>(x => x.configScopeId == ConfigScopeId));
        AssertAnalysisNotFailed();
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenedFiles_AnalysisServiceReturnsNull_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        MockAnalyzeOpenFiles(analysisId: null);

        await testSubject.ExecuteAnalysisForOpenedFiles();

        notifier.Received().AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
    }

    [TestMethod]
    public async Task ExecuteAnalysisForOpenedFiles_AnalysisServiceThrows_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        var exception = new Exception();
        analysisService.AnalyzeOpenFilesAsync(Arg.Any<AnalyzeOpenFilesParams>()).ThrowsAsyncForAnyArgs(exception);

        await testSubject.ExecuteAnalysisForOpenedFiles();

        notifier.Received().AnalysisFailed(exception);
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
        notifier.DidNotReceiveWithAnyArgs().AnalysisCancelled();
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(Exception));
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(string));
        notifier.ReceivedWithAnyArgs().AnalysisFinished(default);
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
