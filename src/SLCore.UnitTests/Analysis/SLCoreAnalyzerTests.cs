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
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IAnalysisStatusNotifierFactory analysisStatusNotifierFactory;
    private ICurrentTimeProvider currentTimeProvider;
    private IAggregatingCompilationDatabaseProvider compilationDatabaseLocator;
    private SLCoreAnalyzer testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        analysisId = Guid.NewGuid();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();
        currentTimeProvider = Substitute.For<ICurrentTimeProvider>();
        compilationDatabaseLocator = Substitute.For<IAggregatingCompilationDatabaseProvider>();
        testSubject = new SLCoreAnalyzer(slCoreServiceProvider,
            activeConfigScopeTracker,
            analysisStatusNotifierFactory,
            currentTimeProvider,
            compilationDatabaseLocator);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<SLCoreAnalyzer, IAnalyzer>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>(),
            MefTestHelpers.CreateExport<ICurrentTimeProvider>(),
            MefTestHelpers.CreateExport<IAggregatingCompilationDatabaseProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<SLCoreAnalyzer>();

    [TestMethod]
    public void IsAnalysisSupported_ReturnsTrueForNoDetectedLanguage() =>
        testSubject.IsAnalysisSupported([]).Should().BeTrue();

    [DataTestMethod]
    [DataRow(AnalysisLanguage.Javascript)]
    [DataRow(AnalysisLanguage.TypeScript)]
    [DataRow(AnalysisLanguage.CFamily)]
    [DataRow(AnalysisLanguage.CascadingStyleSheets)]
    [DataRow(AnalysisLanguage.RoslynFamily)]
    public void IsAnalysisSupported_ReturnsTrueForEveryDetectedLanguage(AnalysisLanguage language) =>
        testSubject.IsAnalysisSupported([language]).Should().BeTrue();

    [TestMethod]
    public void ExecuteAnalysis_CreatesNotifierAndStarts()
    {
        SetUpDefaultAnalysisStatusNotifier(out var notifier);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        analysisStatusNotifierFactory.Received().Create(nameof(SLCoreAnalyzer), FilePath, analysisId);
        notifier.Received().AnalysisStarted();
    }

    [TestMethod]
    public void ExecuteAnalysis_ConfigScopeNotInitialized_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);
        SetUpDefaultAnalysisStatusNotifier(out var notifier);
        SetUpServiceProvider(out var analysisService);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void ExecuteAnalysis_ConfigScopeNotReadyForAnalysis_NotifyNotReady()
    {
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: false));
        SetUpDefaultAnalysisStatusNotifier(out var notifier);
        SetUpServiceProvider(out var analysisService);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }

    [TestMethod]
    public void ExecuteAnalysis_ServiceProviderUnavailable_NotifyFailed()
    {
        SetUpServiceProvider(out var analysisService, false);
        SetUpDefaultAnalysisStatusNotifier(out var notifier);
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>());
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisFailed(SLCoreStrings.ServiceProviderNotInitialized);
    }

    [TestMethod]
    public void ExecuteAnalysis_PassesCorrectArgumentsToAnalysisService()
    {
        var expectedTimeStamp = DateTimeOffset.Now;
        SetUpCurrentTimeProvider(expectedTimeStamp);
        SetUpServiceProvider(out var analysisService);
        SetUpInitializedConfigScope();
        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

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
        SetUpServiceProvider(out var analysisService);
        SetUpInitializedConfigScope();

        testSubject.ExecuteAnalysis(FilePath, default, default, default, options, default);

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
        SetUpServiceProvider(out var analysisService);

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], default, default, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.extraProperties != null
                && a.extraProperties["sonar.cfamily.compile-commands"] == compilationDatabasePath),
            Arg.Any<CancellationToken>());
        compilationDatabaseHandle.Received().Dispose();
    }

    [TestMethod]
    public void ExecuteAnalysis_ForCFamily_AnalysisThrows_CompilationDatabaaseDisposed()
    {
        const string filePath = @"C:\file\path\myclass.cpp";
        const string compilationDatabasePath = @"C:\file\path\compilation_database.json";
        var compilationDatabaseHandle = CreateCompilationDatabaseHandle(compilationDatabasePath);
        SetUpCompilationDatabaseLocator(filePath, compilationDatabaseHandle);
        SetUpInitializedConfigScope();
        SetUpServiceProvider(out var analysisService);
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs<Exception>();

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], default, default, default);

        compilationDatabaseHandle.Received().Dispose();
    }

    [TestMethod]
    public void ExecuteAnalysis_ForCFamily_WithoutCompilationDatabase_DoesNotPassExtraProperty()
    {
        const string filePath = @"C:\file\path\myclass.cpp";
        SetUpCompilationDatabaseLocator(filePath, null);
        SetUpInitializedConfigScope();
        SetUpServiceProvider(out var analysisService);

        testSubject.ExecuteAnalysis(filePath, analysisId, [AnalysisLanguage.CFamily], default, default, default);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a =>
                a.extraProperties != null
                && !a.extraProperties.ContainsKey("sonar.cfamily.compile-commands")),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public void ExecuteAnalysis_PassesCorrectCancellationTokenToAnalysisService()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        SetUpInitializedConfigScope();
        SetUpServiceProvider(out var analysisService);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, cancellationTokenSource.Token);

        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Any<AnalyzeFilesAndTrackParams>(),
            cancellationTokenSource.Token);
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceSucceeds_ExitsWithoutFinishingAnalysis()
    {
        SetUpDefaultAnalysisStatusNotifier(out var notifier);
        SetUpInitializedConfigScope();
        SetUpServiceProvider(out var analysisService);
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri>(), []));

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        notifier.DidNotReceiveWithAnyArgs().AnalysisNotReady(default);
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(Exception));
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(string));
        notifier.DidNotReceiveWithAnyArgs().AnalysisFinished(default, default);
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceFailsForFile_NotifyFailed()
    {
        SetUpDefaultAnalysisStatusNotifier(out var notifier);
        SetUpInitializedConfigScope();
        SetUpServiceProvider(out var analysisService);
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri> { new(@"C:\file\path") }, []));

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        notifier.Received().AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceCancelled_NotifyCancel()
    {
        SetUpDefaultAnalysisStatusNotifier(out var notifier);
        SetUpInitializedConfigScope();
        SetUpServiceProvider(out var analysisService);
        var operationCanceledException = new OperationCanceledException();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs(operationCanceledException);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        notifier.Received().AnalysisCancelled();
    }

    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceThrows_NotifyFailed()
    {
        SetUpDefaultAnalysisStatusNotifier(out var notifier);
        SetUpInitializedConfigScope();
        SetUpServiceProvider(out var analysisService);
        var exception = new Exception();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs(exception);

        testSubject.ExecuteAnalysis(FilePath, analysisId, default, default, default, default);

        notifier.Received().AnalysisFailed(exception);
    }

    private void SetUpServiceProvider(out IAnalysisSLCoreService analysisService, bool result = true)
    {
        var service = Substitute.For<IAnalysisSLCoreService>();
        analysisService = service;
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>())
            .Returns(info =>
            {
                info[0] = service;
                return result;
            });
    }

    private void SetUpInitializedConfigScope() =>
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(ConfigScopeId, IsReadyForAnalysis: true));

    private void SetUpDefaultAnalysisStatusNotifier(out IAnalysisStatusNotifier notifier)
    {
        notifier = Substitute.For<IAnalysisStatusNotifier>();
        analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), FilePath, analysisId).Returns(notifier);
    }

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
}
