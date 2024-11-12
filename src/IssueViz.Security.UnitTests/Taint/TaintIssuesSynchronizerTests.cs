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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Moq;
using NSubstitute.ClearExtensions;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Protocol;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.Service.Taint;
using SonarLint.VisualStudio.SLCore.State;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintIssuesSynchronizerTests
{
    private ITaintStore taintStore;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private ITaintVulnerabilityTrackingSlCoreService taintService;
    private ITaintIssueToIssueVisualizationConverter converter;
    private IToolWindowService toolWindowService;
    private IVsUIServiceOperation vsUiServiceOperation;
    private IVsMonitorSelection vsMonitorSelection;
    private IThreadHandling threadHandling;
    private IAsyncLockFactory asyncLockFactory;
    private IAsyncLock asyncLock;
    private TestLogger logger;
    private TaintIssuesSynchronizer testSubject;
    private IReleaseAsyncLock asyncLockReleaser;
    private static readonly ConfigurationScope None = null;
    private static readonly ConfigurationScope Standalone = new("some id 1", null, null, "some path");
    private static readonly ConfigurationScope ConnectedWithUninitializedRoot = new("some id 2", "some connection", "some project");
    private static readonly ConfigurationScope ConnectedReady = new("some id 3", "some connection", "some project", "some path");

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<TaintIssuesSynchronizer, ITaintIssuesSynchronizer>(
            MefTestHelpers.CreateExport<ITaintStore>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(),
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void Ctor_DoesNotCallAnyServices()
    {
        asyncLockFactory.ClearSubstitute();
        var loggerMock = Substitute.For<ILogger>();
        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls. AsyncLockFactory is free-threaded, calling it is allowed
        testSubject = new TaintIssuesSynchronizer(taintStore,
            slCoreServiceProvider,
            converter,
            toolWindowService,
            vsUiServiceOperation,
            threadHandling,
            asyncLockFactory,
            loggerMock);
        taintStore.ReceivedCalls().Should().BeEmpty();
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        converter.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
        vsUiServiceOperation.ReceivedCalls().Should().BeEmpty();
        threadHandling.ReceivedCalls().Should().BeEmpty();
        asyncLockFactory.Received(1).Create();
        loggerMock.ReceivedCalls().Should().BeEmpty();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        taintStore = Substitute.For<ITaintStore>();
        taintService = Substitute.For<ITaintVulnerabilityTrackingSlCoreService>();
        slCoreServiceProvider = CreateDefaultServiceProvider(taintService);
        converter = Substitute.For<ITaintIssueToIssueVisualizationConverter>();
        toolWindowService = Substitute.For<IToolWindowService>();
        vsMonitorSelection = Substitute.For<IVsMonitorSelection>();
        vsUiServiceOperation = CreateDefaultServiceOperation(vsMonitorSelection);
        threadHandling = CreateDefaultThreadHandling();
        asyncLock = Substitute.For<IAsyncLock>();
        asyncLockReleaser = Substitute.For<IReleaseAsyncLock>();
        asyncLockFactory = CreateDefaultAsyncLockFactory(asyncLock, asyncLockReleaser);
        logger = new TestLogger();
        testSubject = new TaintIssuesSynchronizer(taintStore,
            slCoreServiceProvider,
            converter,
            toolWindowService,
            vsUiServiceOperation,
            threadHandling,
            asyncLockFactory,
            logger);
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_NonCriticalException_UIContextAndStoreCleared()
    {
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaintVulnerabilityTrackingSlCoreService>()).Throws(new Exception("this is a test"));

        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        var act = () => testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        await act.Should().NotThrowAsync();
        CheckStoreIsCleared();
        // CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists("this is a test");
        asyncLockReleaser.Received().Dispose();
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_CriticalException_ExceptionNotCaught()
    {
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaintVulnerabilityTrackingSlCoreService>()).Throws(new DivideByZeroException());

        var act = async () => await testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        await act.Should().ThrowAsync<Exception>();
        asyncLockReleaser.Received().Dispose();
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_NoConfigurationScope_StoreAndUIContextCleared()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateTaintVulnerabilitiesAsync(None);

        CheckStoreIsCleared();
        // CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists("not in connected mode");
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        converter.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_StandaloneMode_StoreAndUIContextCleared()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateTaintVulnerabilitiesAsync(Standalone);

        CheckStoreIsCleared();
        // CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists("not in connected mode");
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_ConnectedModeConfigScope_NotReady_StoreAndUIContextCleared()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedWithUninitializedRoot);

        CheckStoreIsCleared();
        // CheckUIContextIsCleared(cookie);
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_SLCoreNotInitialized_StoreAndUIContextCleared()
    {
        slCoreServiceProvider.TryGetTransientService(out ITaintVulnerabilityTrackingSlCoreService _).Returns(false);
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        CheckStoreIsCleared();
        // CheckUIContextIsCleared(cookie);
        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            asyncLock.AcquireAsync();
            slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaintVulnerabilityTrackingSlCoreService>());
            asyncLockReleaser.Dispose();
        });
        taintService.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_TaintStoreAlreadyInitialized_Ignored()
    {
        taintStore.ConfigurationScope.Returns(ConnectedReady.Id);

        await testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        // todo log
        taintStore.DidNotReceiveWithAnyArgs().Set(default, default);
        taintService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_NoIssuesForConfigScope_SetsStoreAndClearsUIContext()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);
        taintService.ListAllAsync(Arg.Is<ListAllTaintsParams>(x => x.shouldRefresh && x.configurationScopeId == ConnectedReady.Id)).Returns(new ListAllTaintsResponse([]));

        await testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        // CheckUIContextIsCleared(cookie);
        converter.DidNotReceiveWithAnyArgs().Convert(default, default);
        taintStore.Received(1).Set(Arg.Is<IReadOnlyCollection<IAnalysisIssueVisualization>>(x => x.SequenceEqual(Array.Empty<IAnalysisIssueVisualization>())), ConnectedReady.Id);
        toolWindowService.ReceivedCalls().Should().BeEmpty();
        logger.AssertPartialOutputStringExists(string.Format(TaintResources.Synchronizer_NumberOfServerIssues, 0));
    }

    [TestMethod]
    public async Task UpdateTaintVulnerabilitiesAsync_MultipleIssues_SetsStoreAndUIContextAndCallsToolWindow()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);
        List<TaintVulnerabilityDto> taints = [CreateDefaultTaintDto(), CreateDefaultTaintDto(), CreateDefaultTaintDto()];
        List<IAnalysisIssueVisualization> taintVisualizations = [Substitute.For<IAnalysisIssueVisualization>(), Substitute.For<IAnalysisIssueVisualization>(), Substitute.For<IAnalysisIssueVisualization>()];
        taintService.ListAllAsync(Arg.Is<ListAllTaintsParams>(x => x.shouldRefresh && x.configurationScopeId == ConnectedReady.Id)).Returns(new ListAllTaintsResponse(taints));
        for (var i = 0; i < taints.Count; i++)
        {
            converter.Convert(taints[i], ConnectedReady.RootPath).Returns(taintVisualizations[i]);
        }

        await testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        // CheckUIContextIsSet(cookie);
        converter.ReceivedWithAnyArgs(3).Convert(default, default);
        taintStore.Received(1).Set(Arg.Is<IReadOnlyCollection<IAnalysisIssueVisualization>>(x => x.SequenceEqual(taintVisualizations)), ConnectedReady.Id);
        CheckToolWindowServiceIsCalled();
        logger.AssertPartialOutputStringExists(string.Format(TaintResources.Synchronizer_NumberOfServerIssues, 3));
    }

    private void SetUpMonitorSelectionMock(uint cookie)
    {
        var localGuid = TaintIssuesExistUIContext.Guid;
        vsMonitorSelection.GetCmdUIContextCookie(ref localGuid, out Arg.Any<uint>()).Returns(call =>
        {
            call[1] = cookie;
            return VSConstants.S_OK;
        });
    }

    private static ISLCoreServiceProvider CreateDefaultServiceProvider(ITaintVulnerabilityTrackingSlCoreService taintService)
    {
        var slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        slCoreServiceProvider.TryGetTransientService(out ITaintVulnerabilityTrackingSlCoreService _).Returns(call =>
        {
            call[0] = taintService;
            return true;
        });

        return slCoreServiceProvider;
    }

    private static IAsyncLockFactory CreateDefaultAsyncLockFactory(IAsyncLock asyncLock, IReleaseAsyncLock release)
    {
        var factory = Substitute.For<IAsyncLockFactory>();
        factory.Create().Returns(asyncLock);
        asyncLock.AcquireAsync().Returns(release);
        return factory;
    }

    private static IVsUIServiceOperation CreateDefaultServiceOperation(IVsMonitorSelection svcToPassToCallback)
    {
        var serviceOp = Substitute.For<IVsUIServiceOperation>();

        // Set up the mock to invoke the operation with the supplied VS service
        serviceOp.When(x => x.Execute<SVsShellMonitorSelection, IVsMonitorSelection>(It.IsAny<Action<IVsMonitorSelection>>()))
            .Do(call => call.Arg<Action<IVsMonitorSelection>>().Invoke(svcToPassToCallback));

        return serviceOp;
    }

    private static IThreadHandling CreateDefaultThreadHandling()
    {
        var mock = Substitute.For<IThreadHandling>();
        mock.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(call => call.Arg<Func<Task<int>>>()());
        return mock;
    }

    private static TaintVulnerabilityDto CreateDefaultTaintDto() =>
        new(
            Guid.Parse("efa697a2-9cfd-4faf-ba21-71b378667a81"),
            "serverkey",
            true,
            "rulekey:S123",
            "message1",
            "file\\path\\1",
            DateTimeOffset.Now,
            new StandardModeDetails(IssueSeverity.MINOR, RuleType.VULNERABILITY),
            [],
            new TextRangeWithHashDto(1, 2, 3, 4, "hash1"),
            "rulecontext",
            false);

    private void CheckUIContextIsCleared(uint expectedCookie) => CheckUIContextUpdated(expectedCookie, 0);

    private void CheckUIContextIsSet(uint expectedCookie) => CheckUIContextUpdated(expectedCookie, 1);

    private void CheckUIContextUpdated(uint expectedCookie, int expectedState) =>
        vsMonitorSelection.Received(1).SetCmdUIContext(expectedCookie, expectedState);

    private void CheckToolWindowServiceIsCalled() =>
        toolWindowService.Received().EnsureToolWindowExists(TaintToolWindow.ToolWindowId);

    private void CheckStoreIsCleared() => taintStore.Received(1).Set([], null);
}
