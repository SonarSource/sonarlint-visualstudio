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
using SonarLint.VisualStudio.SLCore.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.UnitTests.Analysis;

[TestClass]
public class SLCoreAnalyzerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreAnalyzer, IAnalyzer>(
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IAnalysisStatusNotifierFactory>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreAnalyzer>();
    }
    
    [TestMethod]
    public void IsAnalysisSupported_ReturnsTrueForNoDetectedLanguage()
    {
        var testSubject = CreateTestSubject();

        testSubject.IsAnalysisSupported([]).Should().BeTrue();
    }
    
    [DataTestMethod]
    [DataRow(AnalysisLanguage.Javascript)]
    [DataRow(AnalysisLanguage.TypeScript)]
    [DataRow(AnalysisLanguage.CFamily)]
    [DataRow(AnalysisLanguage.CascadingStyleSheets)]
    [DataRow(AnalysisLanguage.RoslynFamily)]
    public void IsAnalysisSupported_ReturnsTrueForEveryDetectedLanguage(AnalysisLanguage language)
    {
        var testSubject = CreateTestSubject();

        testSubject.IsAnalysisSupported([language]).Should().BeTrue();
    }

    [TestMethod]
    public void ExecuteAnalysis_CreatesNotifierAndStarts()
    {
        var analysisStatusNotifierFactory = CreateDefaultAnalysisStatusNotifier(out var notifier);
        var testSubject = CreateTestSubject(analysisStatusNotifierFactory: analysisStatusNotifierFactory);
        
        testSubject.ExecuteAnalysis(@"C:\file\path", Guid.NewGuid(), default, default, default, default, default);

        analysisStatusNotifierFactory.Received().Create(nameof(SLCoreAnalyzer), @"C:\file\path");
        notifier.Received().AnalysisStarted();
    }
    
    [TestMethod]
    public void ExecuteAnalysis_ConfigScopeNotInitialized_NotifyNotReady()
    {
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        activeConfigScopeTracker.Current.Returns((ConfigurationScope)null);
        var testSubject = CreateTestSubject(CreatServiceProvider(out var analysisService), activeConfigScopeTracker, CreateDefaultAnalysisStatusNotifier(out var notifier));
        
        testSubject.ExecuteAnalysis(@"C:\file\path", Guid.NewGuid(), default, default, default, default, default);

        _ = activeConfigScopeTracker.Received().Current;
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisNotReady(SLCoreStrings.ConfigScopeNotInitialized);
    }
    
    [TestMethod]
    public void ExecuteAnalysis_ServiceProviderUnavailable_NotifyFailed()
    {
        var slCoreServiceProvider = CreatServiceProvider(out var analysisService, false);
        var testSubject = CreateTestSubject(slCoreServiceProvider, CreateInitializedConfigScope("someconfigscopeid"), CreateDefaultAnalysisStatusNotifier(out var notifier));
        
        testSubject.ExecuteAnalysis(@"C:\file\path", Guid.NewGuid(), default, default, default, default, default);

        slCoreServiceProvider.Received().TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>());
        analysisService.ReceivedCalls().Should().BeEmpty();
        notifier.Received().AnalysisFailed(SLCoreStrings.ServiceProviderNotInitialized);
    }
    
    [TestMethod]
    public void ExecuteAnalysis_PassesCorrectArgumentsToAnalysisService()
    {
        var timestampTestStart = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var analysisId = Guid.NewGuid();
        var testSubject = CreateTestSubject(CreatServiceProvider(out var analysisService), CreateInitializedConfigScope("someconfigscopeid"));

        testSubject.ExecuteAnalysis(@"C:\file\path", analysisId, default, default, default, default, default);

        var timestampTestAssert = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Is<AnalyzeFilesAndTrackParams>(a => 
                a.analysisId == analysisId 
                && a.configurationScopeId == "someconfigscopeid"
                && a.filesToAnalyze.Single() == new FileUri(@"C:\file\path")
                && a.extraProperties != null
                && a.shouldFetchServerIssues
                && a.startTime >= timestampTestStart
                && a.startTime <= timestampTestAssert),
            Arg.Any<CancellationToken>());
    }
    
    [TestMethod]
    public void ExecuteAnalysis_PassesCorrectCancellationTokenToAnalysisService()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var analysisId = Guid.NewGuid();
        var testSubject = CreateTestSubject(CreatServiceProvider(out var analysisService), CreateInitializedConfigScope("someconfigscopeid"));

        testSubject.ExecuteAnalysis(@"C:\file\path", analysisId, default, default, default, default, cancellationTokenSource.Token);
        
        analysisService.Received().AnalyzeFilesAndTrackAsync(Arg.Any<AnalyzeFilesAndTrackParams>(),
            cancellationTokenSource.Token);
    }
    
    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceSucceeds_ExitsWithoutFinishingAnalysis()
    {
        var testSubject = CreateTestSubject(CreatServiceProvider(out var analysisService), CreateInitializedConfigScope("someconfigscopeid"), CreateDefaultAnalysisStatusNotifier(out var notifier));
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri>(), []));
        
        testSubject.ExecuteAnalysis(@"C:\file\path", Guid.NewGuid(), default, default, default, default, default);
        
        notifier.DidNotReceiveWithAnyArgs().AnalysisNotReady(default);
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(Exception));
        notifier.DidNotReceiveWithAnyArgs().AnalysisFailed(default(string));
        notifier.DidNotReceiveWithAnyArgs().AnalysisFinished(default, default);
    }
    
    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceFailsForFile_NotifyFailed()
    {
        var testSubject = CreateTestSubject(CreatServiceProvider(out var analysisService), CreateInitializedConfigScope("someconfigscopeid"), CreateDefaultAnalysisStatusNotifier(out var notifier));
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ReturnsForAnyArgs(new AnalyzeFilesResponse(new HashSet<FileUri>{new(@"C:\file\path")}, []));
        
        testSubject.ExecuteAnalysis(@"C:\file\path", Guid.NewGuid(), default, default, default, default, default);
        
        notifier.Received().AnalysisFailed(SLCoreStrings.AnalysisFailedReason);
    }
    
    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceCancelled_NotifyCancel()
    {
        var testSubject = CreateTestSubject(CreatServiceProvider(out var analysisService), CreateInitializedConfigScope("someconfigscopeid"), CreateDefaultAnalysisStatusNotifier(out var notifier));
        var operationCanceledException = new OperationCanceledException();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs(operationCanceledException);
        
        testSubject.ExecuteAnalysis(@"C:\file\path", Guid.NewGuid(), default, default, default, default, default);
        
        notifier.Received().AnalysisCancelled();
    }
    
    [TestMethod]
    public void ExecuteAnalysis_AnalysisServiceThrows_NotifyFailed()
    {
        var testSubject = CreateTestSubject(CreatServiceProvider(out var analysisService), CreateInitializedConfigScope("someconfigscopeid"), CreateDefaultAnalysisStatusNotifier(out var notifier));
        var exception = new Exception();
        analysisService.AnalyzeFilesAndTrackAsync(default, default).ThrowsAsyncForAnyArgs(exception);
        
        testSubject.ExecuteAnalysis(@"C:\file\path", Guid.NewGuid(), default, default, default, default, default);
        
        notifier.Received().AnalysisFailed(exception);
    }

    private static ISLCoreServiceProvider CreatServiceProvider(out IAnalysisSLCoreService analysisService, bool result = true)
    {
        var service = Substitute.For<IAnalysisSLCoreService>();
        analysisService = service;
        var slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<IAnalysisSLCoreService>())
            .Returns(info =>
            {
                info[0] = service;
                return result;
            });
        return slCoreServiceProvider;
    }

    private static IActiveConfigScopeTracker CreateInitializedConfigScope(string id)
    {
        var activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        activeConfigScopeTracker.Current.Returns(new ConfigurationScope(id));
        return activeConfigScopeTracker;
    }

    private static IAnalysisStatusNotifierFactory CreateDefaultAnalysisStatusNotifier(out IAnalysisStatusNotifier notifier)
    {
        var analysisStatusNotifierFactory = Substitute.For<IAnalysisStatusNotifierFactory>();
        notifier = Substitute.For<IAnalysisStatusNotifier>();
        analysisStatusNotifierFactory.Create(nameof(SLCoreAnalyzer), Arg.Any<string>()).Returns(notifier);
        return analysisStatusNotifierFactory;
    }

    private static SLCoreAnalyzer CreateTestSubject(ISLCoreServiceProvider slCoreServiceProvider = null,
        IActiveConfigScopeTracker activeConfigScopeTracker = null,
        IAnalysisStatusNotifierFactory analysisStatusNotifierFactory = null)
    {
        slCoreServiceProvider ??= Substitute.For<ISLCoreServiceProvider>();
        activeConfigScopeTracker ??= Substitute.For<IActiveConfigScopeTracker>();
        analysisStatusNotifierFactory ??= Substitute.For<IAnalysisStatusNotifierFactory>();
        return new SLCoreAnalyzer(slCoreServiceProvider,
            activeConfigScopeTracker,
            analysisStatusNotifierFactory);
    }
}
