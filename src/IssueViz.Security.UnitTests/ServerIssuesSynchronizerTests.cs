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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.DependencyRisks;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using SonarLint.VisualStudio.SLCore.Service.Taint;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests;

[TestClass]
public class ServerIssuesSynchronizerTests
{
    private ITaintStore taintStore;
    private IDependencyRisksStore dependencyRisksStore;
    private ISLCoreServiceProvider slCoreServiceProvider;
    private ITaintVulnerabilityTrackingSlCoreService taintService;
    private IDependencyRiskSlCoreService dependencyRiskSlCoreService;
    private ITaintIssueToIssueVisualizationConverter taintConverter;
    private IScaIssueDtoToDependencyRiskConverter scaConverter;
    private IToolWindowService toolWindowService;
    private IVsUIServiceOperation vsUiServiceOperation;
    private IVsMonitorSelection vsMonitorSelection;
    private IThreadHandling threadHandling;
    private IAsyncLockFactory asyncLockFactory;
    private IAsyncLock asyncLock;
    private TestLogger logger;
    private ServerIssuesSynchronizer testSubject;
    private IReleaseAsyncLock asyncLockReleaser;
    private static readonly ConfigurationScope None = null;
    private static readonly ConfigurationScope Standalone = new("some id 1", null, null, "some path");
    private static readonly ConfigurationScope ConnectedWithUninitializedRoot = new("some id 2", "some connection", "some project");
    private static readonly ConfigurationScope ConnectedReady = new("some id 3", "some connection", "some project", "some path");

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ServerIssuesSynchronizer, IServerIssuesSynchronizer>(
            MefTestHelpers.CreateExport<ITaintStore>(),
            MefTestHelpers.CreateExport<IDependencyRisksStore>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
            MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(),
            MefTestHelpers.CreateExport<IToolWindowService>(),
            MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IScaIssueDtoToDependencyRiskConverter>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ServerIssuesSynchronizer>();

    [TestMethod]
    public void Ctor_DoesNotCallAnyServices()
    {
        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls. AsyncLockFactory is free-threaded, calling it is allowed
        taintStore.ReceivedCalls().Should().BeEmpty();
        dependencyRisksStore.ReceivedCalls().Should().BeEmpty();
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        taintConverter.ReceivedCalls().Should().BeEmpty();
        scaConverter.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
        vsUiServiceOperation.ReceivedCalls().Should().BeEmpty();
        threadHandling.ReceivedCalls().Should().BeEmpty();
        asyncLockFactory.Received(1).Create();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        taintStore = Substitute.For<ITaintStore>();
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        taintService = Substitute.For<ITaintVulnerabilityTrackingSlCoreService>();
        dependencyRiskSlCoreService = Substitute.For<IDependencyRiskSlCoreService>();
        slCoreServiceProvider = CreateDefaultServiceProvider(taintService, dependencyRiskSlCoreService);
        taintConverter = Substitute.For<ITaintIssueToIssueVisualizationConverter>();
        scaConverter = Substitute.For<IScaIssueDtoToDependencyRiskConverter>();
        toolWindowService = Substitute.For<IToolWindowService>();
        vsMonitorSelection = Substitute.For<IVsMonitorSelection>();
        vsUiServiceOperation = CreateDefaultServiceOperation(vsMonitorSelection);
        threadHandling = CreateDefaultThreadHandling();
        asyncLock = Substitute.For<IAsyncLock>();
        asyncLockReleaser = Substitute.For<IReleaseAsyncLock>();
        asyncLockFactory = CreateDefaultAsyncLockFactory(asyncLock, asyncLockReleaser);
        logger = Substitute.ForPartsOf<TestLogger>();
        testSubject = new ServerIssuesSynchronizer(taintStore,
            dependencyRisksStore,
            slCoreServiceProvider,
            taintConverter,
            toolWindowService,
            vsUiServiceOperation,
            threadHandling,
            asyncLockFactory,
            logger,
            scaConverter);
    }

    [TestMethod]
    public void Ctor_SetsBaseLogContext() => logger.Received(1).ForContext(Resources.Synchronizer_LogContext_General);

    [TestMethod]
    public async Task UpdateServerIssuesAsyncNoConfigurationScope_StoreAndUIContextCleared()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateServerIssuesAsync(None);

        CheckTaintStoreIsCleared();
        CheckDependencyRisksStoreIsCleared();
        CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists(Resources.Synchronizer_NotInConnectedMode);
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        taintConverter.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_StandaloneMode_StoreAndUIContextCleared()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateServerIssuesAsync(Standalone);

        CheckTaintStoreIsCleared();
        CheckDependencyRisksStoreIsCleared();
        CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists(Resources.Synchronizer_NotInConnectedMode);
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_ConnectedModeNotReady_StoreAndUIContextCleared()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateServerIssuesAsync(ConnectedWithUninitializedRoot);

        CheckTaintStoreIsCleared();
        CheckDependencyRisksStoreIsCleared();
        CheckUIContextIsCleared(cookie);
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Taint_ServiceNotInitialized_StoreAndUIContextCleared()
    {
        slCoreServiceProvider.TryGetTransientService(out ITaintVulnerabilityTrackingSlCoreService _).Returns(false);
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        CheckTaintStoreIsCleared();
        CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists(Resources.Synchronizer_SLCoreNotReady);
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
    public async Task UpdateServerIssuesAsync_Taint_StoreAlreadyInitialized_Ignored()
    {
        taintStore.ConfigurationScope.Returns(ConnectedReady.Id);

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        taintStore.DidNotReceiveWithAnyArgs().Set(default, default);
        taintService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Taint_NoIssuesForConfigScope_SetsEmptyStoreAndClearsUIContext()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);
        ConfigureTaintService(ConnectedReady.Id, []);

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        CheckUIContextIsCleared(cookie);
        taintConverter.DidNotReceiveWithAnyArgs().Convert(default, default);
        taintStore.Received(1).Set(Arg.Is<IReadOnlyCollection<IAnalysisIssueVisualization>>(x => x.SequenceEqual(Array.Empty<IAnalysisIssueVisualization>())), ConnectedReady.Id);
        toolWindowService.ReceivedCalls().Should().BeEmpty();
        logger.AssertPartialOutputStringExists(string.Format(Resources.Synchronizer_NumberOfTaintIssues, 0));
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Taint_MultipleIssues_SetsStoreAndUIContextAndCallsToolWindow()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);
        List<TaintVulnerabilityDto> taints = [CreateDefaultTaintDto(), CreateDefaultTaintDto(), CreateDefaultTaintDto()];
        List<IAnalysisIssueVisualization> taintVisualizations =
            [Substitute.For<IAnalysisIssueVisualization>(), Substitute.For<IAnalysisIssueVisualization>(), Substitute.For<IAnalysisIssueVisualization>()];
        ConfigureTaintService(ConnectedReady.Id, taints);

        for (var i = 0; i < taints.Count; i++)
        {
            taintConverter.Convert(taints[i], ConnectedReady.RootPath).Returns(taintVisualizations[i]);
        }

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        CheckUIContextIsSet(cookie);
        taintConverter.ReceivedWithAnyArgs(3).Convert(default, default);
        taintStore.Received(1).Set(Arg.Is<IReadOnlyCollection<IAnalysisIssueVisualization>>(x => x.SequenceEqual(taintVisualizations)), ConnectedReady.Id);
        CheckToolWindowServiceIsCalled();
        logger.AssertPartialOutputStringExists(string.Format(Resources.Synchronizer_NumberOfTaintIssues, 3));
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Taint_NonCriticalException_UIContextAndStoreCleared()
    {
        ConfigureScaService(ConnectedReady.Id, []);
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaintVulnerabilityTrackingSlCoreService>())
            .Throws(new Exception("this is a test"));

        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        var act = () => testSubject.UpdateServerIssuesAsync(ConnectedReady);

        await act.Should().NotThrowAsync();
        CheckTaintStoreIsCleared();
        CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists("this is a test");
        asyncLockReleaser.Received().Dispose();
        dependencyRiskSlCoreService.ReceivedWithAnyArgs(1).ListAllAsync(default);
        dependencyRisksStore.DidNotReceiveWithAnyArgs().Reset();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Taint_CriticalException_ExceptionNotCaught()
    {
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaintVulnerabilityTrackingSlCoreService>())
            .Throws(new DivideByZeroException());

        var act = async () => await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        await act.Should().ThrowAsync<DivideByZeroException>();
        asyncLockReleaser.Received().Dispose();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Sca_ServiceNotInitialized_DependencyRisksStoreCleared()
    {
        slCoreServiceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _).Returns(false);

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        CheckDependencyRisksStoreIsCleared();
        logger.AssertPartialOutputStringExists(Resources.Synchronizer_SLCoreNotReady);
        Received.InOrder(() =>
        {
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            asyncLock.AcquireAsync();
            slCoreServiceProvider.TryGetTransientService(out Arg.Any<IDependencyRiskSlCoreService>());
            asyncLockReleaser.Dispose();
        });
        dependencyRiskSlCoreService.ReceivedCalls().Should().BeEmpty();
        scaConverter.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Sca_StoreAlreadyInitialized_Ignored()
    {
        dependencyRisksStore.CurrentConfigurationScope.Returns(ConnectedReady.Id);

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        dependencyRisksStore.DidNotReceiveWithAnyArgs().Set(default, default);
        dependencyRiskSlCoreService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Sca_NoIssuesForConfigScope_SetsEmptyStore()
    {
        ConfigureScaService(ConnectedReady.Id, []);

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        scaConverter.ReceivedCalls().Should().BeEmpty();
        dependencyRisksStore.Received(1).Set(Arg.Is<IEnumerable<IDependencyRisk>>(x =>
            !x.Any()), ConnectedReady.Id);
        logger.AssertPartialOutputStringExists(string.Format(Resources.Synchronizer_NumberOfDependencyRisks, 0));
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Sca_MultipleIssues_SetsStoreDependencyRisks()
    {
        List<DependencyRiskDto> scaIssues = [CreateDefaultDependencyRiskDto(), CreateDefaultDependencyRiskDto(), CreateDefaultDependencyRiskDto()];
        List<IDependencyRisk> dependencyRisks =
        [
            Substitute.For<IDependencyRisk>(),
            Substitute.For<IDependencyRisk>(),
            Substitute.For<IDependencyRisk>()
        ];
        ConfigureScaService(ConnectedReady.Id, scaIssues);
        for (var i = 0; i < scaIssues.Count; i++)
        {
            scaConverter.Convert(scaIssues[i]).Returns(dependencyRisks[i]);
        }

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        scaConverter.ReceivedWithAnyArgs(3).Convert(default);
        dependencyRisksStore.Received(1).Set(Arg.Is<IEnumerable<IDependencyRisk>>(x =>
            x.SequenceEqual(dependencyRisks)), ConnectedReady.Id);
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Sca_NonCriticalException_DependencyRisksStoreCleared()
    {
        ConfigureTaintService(ConnectedReady.Id, []);
        slCoreServiceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _)
            .Throws(new Exception("sca service error"));

        await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        CheckDependencyRisksStoreIsCleared();
        logger.AssertPartialOutputStringExists("sca service error");
        taintService.ReceivedWithAnyArgs(1).ListAllAsync(default);
        taintStore.DidNotReceiveWithAnyArgs().Reset();
    }

    [TestMethod]
    public async Task UpdateServerIssuesAsync_Sca_CriticalException_ExceptionNotCaught()
    {
        slCoreServiceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _)
            .Throws(new DivideByZeroException());

        var act = async () => await testSubject.UpdateServerIssuesAsync(ConnectedReady);

        await act.Should().ThrowAsync<DivideByZeroException>();
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

    private static ISLCoreServiceProvider CreateDefaultServiceProvider(ITaintVulnerabilityTrackingSlCoreService taintService, IDependencyRiskSlCoreService scaService)
    {
        var slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        slCoreServiceProvider.TryGetTransientService(out ITaintVulnerabilityTrackingSlCoreService _).Returns(call =>
        {
            call[0] = taintService;
            return true;
        });
        slCoreServiceProvider.TryGetTransientService(out IDependencyRiskSlCoreService _).Returns(call =>
        {
            call[0] = scaService;
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
        serviceOp.When(x => x.Execute<SVsShellMonitorSelection, IVsMonitorSelection>(Arg.Any<Action<IVsMonitorSelection>>()))
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

    private static DependencyRiskDto CreateDefaultDependencyRiskDto() => new(Guid.NewGuid(), default, default, default, default, default, default, default, default);

    private void CheckUIContextIsCleared(uint expectedCookie) => CheckUIContextUpdated(expectedCookie, 0);

    private void CheckUIContextIsSet(uint expectedCookie) => CheckUIContextUpdated(expectedCookie, 1);

    private void CheckUIContextUpdated(uint expectedCookie, int expectedState) => vsMonitorSelection.Received(1).SetCmdUIContext(expectedCookie, expectedState);

    private void CheckToolWindowServiceIsCalled() => toolWindowService.Received().EnsureToolWindowExists(TaintToolWindow.ToolWindowId);

    private void CheckTaintStoreIsCleared() => taintStore.Received(1).Reset();

    private void CheckDependencyRisksStoreIsCleared() => dependencyRisksStore.Received(1).Reset();

    private void ConfigureTaintService(string configScopeId, List<TaintVulnerabilityDto> vulnerabilities)
    {
        taintService.ListAllAsync(Arg.Is<ListAllTaintsParams>(x => x.shouldRefresh && x.configurationScopeId == configScopeId))
            .Returns(new ListAllTaintsResponse(vulnerabilities ?? []));
    }

    private void ConfigureScaService(string configScopeId, List<DependencyRiskDto> scaIssues)
    {
        dependencyRiskSlCoreService.ListAllAsync(Arg.Is<ListAllDependencyRisksParams>(x => x.configurationScopeId == configScopeId))
            .Returns(new ListAllDependencyRisksResponse(scaIssues ?? []));
    }
}
