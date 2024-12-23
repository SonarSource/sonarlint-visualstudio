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

using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.CFamily.Analysis;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Analysis;

[TestClass]
public class SLCoreAnalyzerTests
{
    private const string ConfigScopeId = "ConfigScopeId";
    private const string FilePath = @"C:\file\path";
    private Guid analysisId;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private IAnalysisSLCoreService analysisService;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
    private ICurrentTimeProvider currentTimeProvider;
    private IAggregatingCompilationDatabaseProvider compilationDatabaseLocator;
    private IAnalysisStatusNotifier notifier;
    private ILogger logger;
    private SLCoreAnalyzer testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        analysisId = Guid.NewGuid();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        analysisService = Substitute.For<IAnalysisSLCoreService>();
        SetUpServiceProvider();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();
        notifier = Substitute.For<IAnalysisStatusNotifier>();
        SetUpDefaultNotifier();
        currentTimeProvider = Substitute.For<ICurrentTimeProvider>();
        compilationDatabaseLocator = Substitute.For<IAggregatingCompilationDatabaseProvider>();
        logger = new TestLogger();
        testSubject = new SLCoreAnalyzer(slCoreServiceProvider,
            activeConfigScopeTracker,
            analysisStatusNotifierFactory,
            currentTimeProvider,
            compilationDatabaseLocator,
            logger);

        void SetUpDefaultNotifier() => analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), FilePath, analysisId).Returns(notifier);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SLCoreAnalyzer, IAnalyzer>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
            MefTestHelpers.CreateExport<ICurrentTimeProvider>(),
            MefTestHelpers.CreateExport<IAggregatingCompilationDatabaseProvider>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SLCoreAnalyzer>();

    [TestMethod]
    public void ExecuteAnalysis_CreatesNotifierAndStarts()
    {
        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        analysisStatusNotifierFactory.Received().Create(nameof(SLCoreAnalyzer), FilePath, analysisId);
        notifier.Received().AnalysisStarted();
    }

    [TestMethod]
    public void ExecuteAnalysis_ConfigScopeNotInitialized_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void ExecuteAnalysis_ConfigScopeNotReadyForAnalysis_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: false));

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void ExecuteAnalysis_ServiceProviderUnavailable_NotifyFailed()
    {
        SetUpServiceProvider(false);
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>());
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisFailed(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void ExecuteAnalysis_PassesCorrectArgumentsToAnalysisService()
    {
        var expectedTimeStamp = DateTimeOffset.Now;
        SetUpCurrentTimeProvider(expectedTimeStamp);
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.analysisId == analysisId
                && a.configurationScopeId == ConfigScopeId
                && a.filesToAnalyze.Single() == new FileUri(FilePath)
                && a.extraProperties.Count == 0
                && a.startTime == expectedTimeStamp.ToUnixTimeMilliseconds()),
            Arg.Any<CancellationToken>());
    }

    [DataTestMethod]
    [DataRow(null, false)]
    [DataRow(false, false)]
    [DataRow(true, true)]
    public void ExecuteAnalysis_ShouldFetchServerIssues_PassesCorrectValueToAnalysisService(bool? value, bool expected)
    {
        IAnalyzerOptions options = value.HasValue ? new AnalyzerOptions { IsOnOpen = value.Value } : null;
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(FilePath, default, default, options, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.shouldFetchServerIssues == expected),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteAnalysis_ForCFamily_PassesCompilationDatabaseAsExtraProperties()
    {
        const string filePath = @"C:\file\path\myclass.cpp";
        const string compilationDatabasePath = @"C:\file\path\compilation_database.json";
        var compilationDatabaseHandle = CreateCompilationDatabaseHandle(compilationDatabasePath);
        SetUpCompilationDatabaseLocator(filePath, compilationDatabaseHandle);
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], default, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.extraProperties != null
                && a.extraProperties["sonar.cfamily.compile-commands"] == compilationDatabasePath),
            Arg.Any<CancellationToken>());
        compilationDatabaseHandle.Received().Dispose();
    }

    [TestMethod]
    public void ExecuteAnalysis_CFamilyReproducerEnabled_SetsExtraProperty()
    {
        const string filePath = @"C:\file\path\myclass.cpp";
        SetUpCompilationDatabaseLocator(filePath, CreateCompilationDatabaseHandle("somepath"));
        SetUpInitializedConfigScope();
        var cFamilyAnalyzerOptions = CreateCFamilyAnalyzerOptions(true);

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], cFamilyAnalyzerOptions, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.extraProperties != null
                && a.extraProperties["sonar.cfamily.reproducer"] == filePath),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteAnalysis_CFamilyReproducerDisabled_DoesNotSetExtraProperty()
    {
        const string filePath = @"C:\file\path\myclass.cpp";
        SetUpCompilationDatabaseLocator(filePath, CreateCompilationDatabaseHandle("somepath"));
        SetUpInitializedConfigScope();
        var cFamilyAnalyzerOptions = CreateCFamilyAnalyzerOptions(false);

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], cFamilyAnalyzerOptions, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.extraProperties == null || !a.extraProperties.ContainsKey("sonar.cfamily.reproducer")),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteAnalysis_ForCFamily_AnalysisThrows_CompilationDatabaaseDisposed()
    {
        const string filePath = @"C:\file\path\myclass.cpp";
        const string compilationDatabasePath = @"C:\file\path\compilation_database.json";
        var compilationDatabaseHandle = CreateCompilationDatabaseHandle(compilationDatabasePath);
        SetUpCompilationDatabaseLocator(filePath, compilationDatabaseHandle);
        SetUpInitializedConfigScope();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs<Exception>();

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], default, default);

        compilationDatabaseHandle.Received().Dispose();
    }

    [TestMethod]
    public void ExecuteAnalysis_ForCFamily_WithoutCompilationDatabase_PassesEmptyStringAsExtraProperty()
    {
        const string filePath = @"C:\file\path\myclass.cpp";
        SetUpCompilationDatabaseLocator(filePath, null);
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], default, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.extraProperties != null
                && a.extraProperties.ContainsKey("sonar.cfamily.compile-commands")
                && a.extraProperties["sonar.cfamily.compile-commands"] == ""),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteAnalysis_PassesCorrectCancellationTokenToAnalysisService()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, cancellationTokenSource.Token);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Any<AnalyzeFilesAndTrackParams>(),
            cancellationTokenSource.Token);
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceSucceeds_ExitsByFinishingAnalysis()
    {
        SetUpInitializedConfigScope();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri>(), []));

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        notifier.DidNotReceiveWithAnyArgs().AnalysisNotReady(default);
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(Exception));
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(string));
        notifier.Received().AnalysisFinished(Arg.Is<TimeSpan>(x => x > TimeSpan.Zero));
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceFailsForFile_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri> { new(@"C:\file\path") }, []));

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        notifier.Received().AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceCancelled_NotifyCancel()
    {
        SetUpInitializedConfigScope();
        var operationCanceledException = new OperationCanceledException();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs(operationCanceledException);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        notifier.Received().AnalysisCancelled();
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceThrows_NotifyFailed()
    {
        SetUpInitializedConfigScope();
        var exception = new Exception();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs(exception);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default);

        notifier.Received().AnalysisFailed(exception);
    }

    private void SetUpServiceProvider(bool result = true) =>
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>())
            .Returns(info =>
            {
                info[0] = analysisService;
                return result;
            });

    private void SetUpInitializedConfigScope() =>
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: true));

    private void SetUpCurrentTimeProvider(DateTimeOffset nowTime) =>
        currentTimeProvider.Now.Returns(nowTime);

    private static ICompilationDatabaseHandle CreateCompilationDatabaseHandle(string compilationDatabasePath)
    {
        var handle = Substitute.For<ICompilationDatabaseHandle>();
        handle.FilePath.Returns(compilationDatabasePath);
        return handle;
    }

    private void SetUpCompilationDatabaseLocator(string filePath, ICompilationDatabaseHandle handle) =>
        compilationDatabaseLocator.GetOrNull(filePath).Returns(handle);


    private static ICFamilyAnalyzerOptions CreateCFamilyAnalyzerOptions(bool createReproducer)
    {
        var cFamilyAnalyzerOptions = Substitute.For<ICFamilyAnalyzerOptions>();
        cFamilyAnalyzerOptions.IsOnOpen.Returns(false);
        cFamilyAnalyzerOptions.CreateReproducer.Returns(createReproducer);
        return cFamilyAnalyzerOptions;
    }
}
