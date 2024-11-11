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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarLint.VisualStudio.SLCore.Core;
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
    private ITaintIssueToIssueVisualizationConverter taintIssueToIssueVisualizationConverter;
    private IToolWindowService toolWindowService;
    private IVsUIServiceOperation vsUiServiceOperation;
    private IVsMonitorSelection vsMonitorSelection;
    private IThreadHandling threadHandling;
    private IAsyncLockFactory asyncLockFactory;
    private IAsyncLock asyncLock;
    private TestLogger logger;
    private TaintIssuesSynchronizer testSubject;
    private IReleaseAsyncLock releaseAsyncLock;
    private static readonly ConfigurationScope None = null;
    private static readonly ConfigurationScope Standalone = new("some id", null, null, "some path");
    private static readonly ConfigurationScope ConnectedWithUninitializedRoot = new("some id", "some connection", "some project");
    private static readonly ConfigurationScope ConnectedReady = new("some id", "some connection", "some project", "some path");

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
        var threadHandlingMock = Substitute.For<IThreadHandling>();
        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls. AsyncLockFactory is free-threaded, calling it is allowed
        testSubject = new TaintIssuesSynchronizer(taintStore,
            slCoreServiceProvider,
            taintIssueToIssueVisualizationConverter,
            toolWindowService,
            vsUiServiceOperation,
            threadHandlingMock,
            asyncLockFactory,
            loggerMock);
        taintStore.ReceivedCalls().Should().BeEmpty();
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        taintIssueToIssueVisualizationConverter.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
        vsUiServiceOperation.ReceivedCalls().Should().BeEmpty();
        threadHandlingMock.ReceivedCalls().Should().BeEmpty();
        asyncLockFactory.Received(1).Create();
        loggerMock.ReceivedCalls().Should().BeEmpty();
    }

    [TestInitialize]
    public void TestInitialize()
    {
        taintStore = Substitute.For<ITaintStore>();
        slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
        taintIssueToIssueVisualizationConverter = Substitute.For<ITaintIssueToIssueVisualizationConverter>();
        toolWindowService = Substitute.For<IToolWindowService>();
        vsMonitorSelection = Substitute.For<IVsMonitorSelection>();
        vsUiServiceOperation = CreateDefaultServiceOperation(vsMonitorSelection);
        threadHandling = new NoOpThreadHandler();
        asyncLock = Substitute.For<IAsyncLock>();
        releaseAsyncLock = Substitute.For<IReleaseAsyncLock>();
        asyncLockFactory = CreateDefaultAsyncLockFactory(asyncLock, releaseAsyncLock);
        logger = new TestLogger();
        // The MEF constructor should be free-threaded, which it will be if
        // it doesn't make any external calls. AsyncLockFactory is free-threaded, calling it is allowed
        testSubject = new TaintIssuesSynchronizer(taintStore,
            slCoreServiceProvider,
            taintIssueToIssueVisualizationConverter,
            toolWindowService,
            vsUiServiceOperation,
            threadHandling,
            asyncLockFactory,
            logger);
    }

    private static IAsyncLockFactory CreateDefaultAsyncLockFactory(IAsyncLock asyncLock, IReleaseAsyncLock release)
    {
        var factory = Substitute.For<IAsyncLockFactory>();
        factory.Create().Returns(asyncLock);
        asyncLock.AcquireAsync().Returns(release);
        return factory;
    }

    [TestMethod]
    public async Task SynchronizeWithServer_NonCriticalException_UIContextAndStoreCleared()
    {
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaintVulnerabilityTrackingSlCoreService>()).Throws(new Exception("this is a test"));

        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        var act = () => testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        await act.Should().NotThrowAsync();
        CheckStoreIsCleared();
        // CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists("this is a test");
    }

    [TestMethod]
    public async Task SynchronizeWithServer_CriticalException_ExceptionNotCaught()
    {
        slCoreServiceProvider.TryGetTransientService(out Arg.Any<ITaintVulnerabilityTrackingSlCoreService>()).Throws(new DivideByZeroException());

        var act = async () => await testSubject.UpdateTaintVulnerabilitiesAsync(ConnectedReady);

        await act.Should().ThrowAsync<Exception>();
    }

    [TestMethod]
    public async Task SynchronizeWithServer_StandaloneMode_StoreAndUIContextCleared()
    {
        const uint cookie = 123;
        SetUpMonitorSelectionMock(cookie);

        await testSubject.UpdateTaintVulnerabilitiesAsync(Standalone);

        CheckStoreIsCleared();
        // CheckUIContextIsCleared(cookie);
        logger.AssertPartialOutputStringExists("not in connected mode");
        slCoreServiceProvider.ReceivedCalls().Should().BeEmpty();
        taintIssueToIssueVisualizationConverter.ReceivedCalls().Should().BeEmpty();
        toolWindowService.ReceivedCalls().Should().BeEmpty();
    }
    //
    // [TestMethod]
    // public async Task SynchronizeWithServer_SonarQubeServerNotYetConnected_StoreAndUIContextCleared()
    // {
    //     var sonarService = CreateSonarService(false);
    //     var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
    //     var taintStore = new Mock<ITaintStore>();
    //     var logger = new TestLogger();
    //
    //     const uint cookie = 999;
    //     var monitor = CreateMonitorSelectionMock(cookie);
    //     var toolWindowService = new Mock<IToolWindowService>();
    //
    //     var testSubject = CreateTestSubject(
    //         BindingConfig_Connected,
    //         taintStore.Object,
    //         converter.Object,
    //         sonarService: sonarService.Object,
    //         vsMonitor: monitor.Object,
    //         toolWindowService: toolWindowService.Object,
    //         logger: logger);
    //
    //     await testSubject.SynchronizeWithServer();
    //
    //     logger.AssertPartialOutputStringExists("not yet established");
    //     CheckConnectedStatusIsChecked(sonarService);
    //     CheckIssuesAreNotFetched(sonarService);
    //
    //     CheckStoreIsCleared(taintStore);
    //     CheckUIContextIsCleared(monitor, cookie);
    //
    //     // Should be nothing to convert or display in the tool window
    //     converter.Invocations.Should().HaveCount(0);
    //     toolWindowService.Invocations.Should().HaveCount(0);
    // }
    //
    // [TestMethod]
    // [DataRow("7.9")]
    // [DataRow("8.5.9.9")]
    // public async Task SynchronizeWithServer_UnsupportedServerVersion_StoreAndUIContextCleared(string versionString)
    // {
    //     var sonarQubeServer = CreateSonarService(true, ServerType.SonarQube, versionString);
    //     var logger = new TestLogger();
    //
    //     const uint cookie = 999;
    //     var monitor = CreateMonitorSelectionMock(cookie);
    //     var taintStore = new Mock<ITaintStore>();
    //
    //     var testSubject = CreateTestSubject(
    //         BindingConfig_Connected,
    //         taintStore.Object,
    //         sonarService: sonarQubeServer.Object,
    //         vsMonitor: monitor.Object,
    //         logger: logger);
    //
    //     await testSubject.SynchronizeWithServer();
    //
    //     logger.AssertPartialOutputStringExists("requires SonarQube v8.6 or later");
    //     logger.AssertPartialOutputStringExists($"Connected SonarQube version: v{versionString}");
    //
    //     CheckIssuesAreNotFetched(sonarQubeServer);
    //     CheckStoreIsCleared(taintStore);
    //     CheckUIContextIsCleared(monitor, cookie);
    // }
    //
    // [TestMethod]
    // [DataRow(ServerType.SonarCloud, "0.1")]
    // [DataRow(ServerType.SonarQube, "8.6.0.0")]
    // [DataRow(ServerType.SonarQube, "9.9")]
    // public async Task SynchronizeWithServer_SupportedServer_IssuesFetched(ServerType serverType, string serverVersion)
    // {
    //     var logger = new TestLogger();
    //     var sonarServer = CreateSonarService(true, serverType, serverVersion);
    //
    //     var bindingConfig = CreateBindingConfig(SonarLintMode.Connected, "keyXXX");
    //     var serverBranchProvider = CreateServerBranchProvider("branchXXX");
    //     SetupTaintIssues(sonarServer, "keyXXX", "branchXXX");
    //
    //     var testSubject = CreateTestSubject(
    //         bindingConfig,
    //         serverBranchProvider: serverBranchProvider.Object,
    //         sonarService: sonarServer.Object,
    //         logger: logger);
    //
    //     await testSubject.SynchronizeWithServer();
    //
    //     logger.AssertPartialOutputStringDoesNotExist("requires SonarQube v8.6 or later");
    //     CheckIssuesAreFetched(sonarServer, "keyXXX", "branchXXX");
    // }
    //
    // [TestMethod]
    // [DataRow(SonarLintMode.Connected)]
    // [DataRow(SonarLintMode.LegacyConnected)]
    // public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_StoreIsSetAndUIContextCleared(SonarLintMode sonarLintMode)
    // {
    //     var sonarService = CreateSonarService();
    //     var bindingConfig = CreateBindingConfig(sonarLintMode, "my-project-key");
    //     var serverBranchProvider = CreateServerBranchProvider("my-branch");
    //     var analysisInformation = new AnalysisInformation("my-branch", DateTimeOffset.Now);
    //
    //     SetupTaintIssues(sonarService, "my-project-key", "my-branch" /* no issues */);
    //     SetupAnalysisInformation(sonarService, "my-project-key", analysisInformation);
    //
    //     var taintStore = new Mock<ITaintStore>();
    //     var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
    //
    //     const uint cookie = 999;
    //     var monitor = CreateMonitorSelectionMock(cookie);
    //     var toolWindowService = new Mock<IToolWindowService>();
    //
    //     var testSubject = CreateTestSubject(
    //         bindingConfig,
    //         taintStore.Object,
    //         converter.Object,
    //         sonarService: sonarService.Object,
    //         serverBranchProvider: serverBranchProvider.Object,
    //         vsMonitor: monitor.Object,
    //         toolWindowService: toolWindowService.Object);
    //
    //     await testSubject.SynchronizeWithServer();
    //
    //     CheckConnectedStatusIsChecked(sonarService);
    //     CheckIssuesAreFetched(sonarService, "my-project-key", "my-branch");
    //     CheckUIContextIsCleared(monitor, cookie);
    //
    //     taintStore.Verify(x => x.Set(Enumerable.Empty<IAnalysisIssueVisualization>(),
    //         It.Is((AnalysisInformation a) =>
    //             a.AnalysisTimestamp == analysisInformation.AnalysisTimestamp &&
    //             a.BranchName == analysisInformation.BranchName)), Times.Once);
    //
    //     // Should be nothing to display in the tool window
    //     toolWindowService.Invocations.Should().HaveCount(0);
    // }
    //
    // [TestMethod]
    // [DataRow(SonarLintMode.Connected)]
    // [DataRow(SonarLintMode.LegacyConnected)]
    // public async Task SynchronizeWithServer_ConnectedMode_UsesExpectedBranch(SonarLintMode sonarLintMode)
    // {
    //     var bindingConfig = CreateBindingConfig(sonarLintMode, "xxx_project-key");
    //
    //     var serverBranchProvider = CreateServerBranchProvider("branch-XYZ");
    //
    //     var sonarQubeService = CreateSonarService();
    //     SetupTaintIssues(sonarQubeService, "xxx_project-key", "branch-XYZ");
    //
    //     var testSubject = CreateTestSubject(
    //         bindingConfig,
    //         sonarService: sonarQubeService.Object,
    //         serverBranchProvider: serverBranchProvider.Object);
    //
    //     await testSubject.SynchronizeWithServer();
    //
    //     serverBranchProvider.VerifyAll();
    //     sonarQubeService.Verify(x => x.GetTaintVulnerabilitiesAsync("xxx_project-key", "branch-XYZ", It.IsAny<CancellationToken>()),
    //         Times.Once());
    // }
    //
    // [TestMethod]
    // [DataRow(SonarLintMode.Connected)]
    // [DataRow(SonarLintMode.LegacyConnected)]
    // public async Task SynchronizeWithServer_ConnectedModeWithIssues_IssuesAddedToStore(SonarLintMode sonarLintMode)
    // {
    //     var serverIssue1 = new TestSonarQubeIssue();
    //     var serverIssue2 = new TestSonarQubeIssue();
    //     var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
    //     var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
    //
    //     var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
    //     converter.Setup(x => x.Convert(serverIssue1)).Returns(issueViz1);
    //     converter.Setup(x => x.Convert(serverIssue2)).Returns(issueViz2);
    //
    //     var taintStore = new Mock<ITaintStore>();
    //
    //     var analysisInformation = new AnalysisInformation("a branch", DateTimeOffset.Now);
    //
    //     var sonarServer = CreateSonarService();
    //
    //     var serverBranchProvider = CreateServerBranchProvider("a branch");
    //     var bindingConfig = CreateBindingConfig(sonarLintMode, "projectKey123");
    //     SetupTaintIssues(sonarServer, "projectKey123", "a branch", serverIssue1, serverIssue2);
    //     SetupAnalysisInformation(sonarServer, "projectKey123", analysisInformation);
    //
    //     var testSubject = CreateTestSubject(
    //         bindingConfig,
    //         taintStore.Object,
    //         converter.Object,
    //         sonarService: sonarServer.Object,
    //         serverBranchProvider: serverBranchProvider.Object);
    //
    //     await testSubject.SynchronizeWithServer();
    //
    //     taintStore.Verify(x => x.Set(new[] { issueViz1, issueViz2 },
    //         It.Is((AnalysisInformation a) =>
    //             a.AnalysisTimestamp == analysisInformation.AnalysisTimestamp &&
    //             a.BranchName == analysisInformation.BranchName)), Times.Once);
    // }
    //
    // [TestMethod]
    // [DataRow(SonarLintMode.Connected)]
    // [DataRow(SonarLintMode.LegacyConnected)]
    // public async Task SynchronizeWithServer_ConnectedModeWithIssues_UIContextIsSetAndToolWindowCalled(SonarLintMode sonarLintMode)
    // {
    //     var sonarService = CreateSonarService();
    //
    //     var bindingConfig = CreateBindingConfig(sonarLintMode, "myProjectKey___");
    //     var serverBranchProvider = CreateServerBranchProvider("branchYYY");
    //     SetupTaintIssues(sonarService, "myProjectKey___", "branchYYY", new TestSonarQubeIssue());
    //     SetupAnalysisInformation(sonarService, "myProjectKey___", new AnalysisInformation("branchYYY", DateTimeOffset.Now));
    //
    //     const uint cookie = 212;
    //     var monitor = CreateMonitorSelectionMock(cookie);
    //     var toolWindowService = new Mock<IToolWindowService>();
    //
    //     var testSubject = CreateTestSubject(
    //         bindingConfig,
    //         serverBranchProvider: serverBranchProvider.Object,
    //         sonarService: sonarService.Object,
    //         vsMonitor: monitor.Object,
    //         toolWindowService: toolWindowService.Object);
    //
    //     await testSubject.SynchronizeWithServer();
    //
    //     CheckConnectedStatusIsChecked(sonarService);
    //     CheckIssuesAreFetched(sonarService, "myProjectKey___", "branchYYY");
    //     CheckUIContextIsSet(monitor, cookie);
    //     CheckToolWindowServiceIsCalled(toolWindowService);
    // }
    //
    // [TestMethod]
    // [DataRow(null)]
    // [DataRow("")]
    // [DataRow("unknown local branch")]
    // public async Task SynchronizeWithServer_NoMatchingServerBranch_UIContextAndStoreCleared(string localBranch)
    // {
    //     var sonarService = CreateSonarService();
    //     var bindingConfig = CreateBindingConfig(SonarLintMode.Connected, "my proj");
    //     var serverBranchProvider = CreateServerBranchProvider(localBranch);
    //     SetupTaintIssues(sonarService, "my proj", localBranch, new TestSonarQubeIssue());
    //
    //     var analysisInformation = new AnalysisInformation("some main branch", DateTimeOffset.Now);
    //     SetupAnalysisInformation(sonarService, "my proj", analysisInformation);
    //
    //     const uint cookie = 212;
    //     var monitor = CreateMonitorSelectionMock(cookie);
    //     var toolWindowService = new Mock<IToolWindowService>();
    //     var taintStore = new Mock<ITaintStore>();
    //
    //     var testSubject = CreateTestSubject(
    //         taintStore: taintStore.Object,
    //         bindingConfig: bindingConfig,
    //         serverBranchProvider: serverBranchProvider.Object,
    //         sonarService: sonarService.Object,
    //         vsMonitor: monitor.Object,
    //         toolWindowService: toolWindowService.Object);
    //
    //     using (new AssertIgnoreScope())
    //     {
    //         await testSubject.SynchronizeWithServer();
    //     }
    //
    //     CheckIssuesAreFetched(sonarService, "my proj", localBranch);
    //     CheckUIContextIsCleared(monitor, cookie);
    //     CheckStoreIsCleared(taintStore);
    // }

    private static BindingConfiguration CreateBindingConfig(SonarLintMode mode = SonarLintMode.Connected, string projectKey = "any") =>
        new(new BoundServerProject("solution", projectKey, new ServerConnection.SonarQube(new Uri("http://bound"))), mode, "any dir");

    private static IVsUIServiceOperation CreateDefaultServiceOperation(IVsMonitorSelection svcToPassToCallback)
    {
        var serviceOp = Substitute.For<IVsUIServiceOperation>();

        // Set up the mock to invoke the operation with the supplied VS service
        serviceOp.When(x => x.Execute<SVsShellMonitorSelection, IVsMonitorSelection>(It.IsAny<Action<IVsMonitorSelection>>()))
            .Do(call => call.Arg<Action<IVsMonitorSelection>>().Invoke(svcToPassToCallback));

        return serviceOp;
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

    private void CheckUIContextIsCleared(uint expectedCookie) => CheckUIContextUpdated(expectedCookie, 0);

    private void CheckUIContextIsSet(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie) => CheckUIContextUpdated(expectedCookie, 1);

    private void CheckUIContextUpdated(uint expectedCookie, int expectedState) =>
        vsMonitorSelection.Received(1).SetCmdUIContext(expectedCookie, expectedState);

    private static void CheckConnectedStatusIsChecked(Mock<ISonarQubeService> serviceMock) => serviceMock.Verify(x => x.GetServerInfo(), Times.Once);

    private static void CheckIssuesAreFetched(Mock<ISonarQubeService> serviceMock, string projectKey, string branch) =>
        serviceMock.Verify(x => x.GetTaintVulnerabilitiesAsync(projectKey, branch, It.IsAny<CancellationToken>()), Times.Once);

    private static void CheckIssuesAreNotFetched(Mock<ISonarQubeService> serviceMock) =>
        serviceMock.Verify(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

    private static void CheckToolWindowServiceIsCalled(Mock<IToolWindowService> toolWindowServiceMock) =>
        toolWindowServiceMock.Verify(x => x.EnsureToolWindowExists(TaintToolWindow.ToolWindowId), Times.Once);

    private void CheckStoreIsCleared() => taintStore.Received(1).Set([], null);
}
