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

using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using Task = System.Threading.Tasks.Task;

using VSShellInterop = Microsoft.VisualStudio.Shell.Interop;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintIssuesSynchronizerTests
    {
        private static readonly BindingConfiguration BindingConfig_Standalone = BindingConfiguration.Standalone;
        private static readonly BindingConfiguration BindingConfig_Connected = CreateBindingConfig(SonarLintMode.Connected, "any project key");

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintIssuesSynchronizer, ITaintIssuesSynchronizer>(
                MefTestHelpers.CreateExport<ITaintStore>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IStatefulServerBranchProvider>(),
                // The constructor calls the service provider so we need to pass a correctly-configured one
                MefTestHelpers.CreateExport<IVsUIServiceOperation>(),
                MefTestHelpers.CreateExport<IToolWindowService>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_DoesNotCallAnyServices()
        {
            var taintStore = new Mock<ITaintStore>();
            var sonarQubeService = new Mock<ISonarQubeService>();
            var taintIssueToIssueVisualizationConverter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var configurationProvider = new Mock<IConfigurationProvider>();
            var statefulServerBranchProvider = new Mock<IStatefulServerBranchProvider>();
            var vsUIServiceOperation = new Mock<IVsUIServiceOperation>();
            var toolWindowService = new Mock<IToolWindowService>();
            var logger = new Mock<ILogger>();

            _ = new TaintIssuesSynchronizer(taintStore.Object, sonarQubeService.Object, taintIssueToIssueVisualizationConverter.Object, configurationProvider.Object,
                toolWindowService.Object, statefulServerBranchProvider.Object, vsUIServiceOperation.Object, logger.Object);

            // The MEF constructor should be free-threaded, which it will be if
            // it doesn't make any external calls.
            taintStore.Invocations.Should().BeEmpty();
            sonarQubeService.Invocations.Should().BeEmpty();
            taintIssueToIssueVisualizationConverter.Invocations.Should().BeEmpty();
            configurationProvider.Invocations.Should().BeEmpty();
            toolWindowService.Invocations.Should().BeEmpty();
            statefulServerBranchProvider.Invocations.Should().BeEmpty();
            vsUIServiceOperation.Invocations.Should().BeEmpty();
            logger.Invocations.Should().BeEmpty();
        }

        [TestMethod]
        public async Task SynchronizeWithServer_NonCriticalException_UIContextAndStoreCleared()
        {
            var logger = new TestLogger();

            var sonarServer = CreateSonarService();
            sonarServer.Setup(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("this is a test"));

            var taintStore = new Mock<ITaintStore>();
            const uint cookie = 123;
            var monitor = CreateMonitorSelectionMock(cookie);

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                sonarService: sonarServer.Object,
                taintStore: taintStore.Object,
                vsMonitor: monitor.Object,
                logger: logger);

            Func<Task> act = testSubject.SynchronizeWithServer;
            await act.Should().NotThrowAsync();

            CheckStoreIsCleared(taintStore);
            CheckUIContextIsCleared(monitor, cookie);
            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public async Task SynchronizeWithServer_CriticalException_ExceptionNotCaught()
        {
            var sonarServer = CreateSonarService();
            sonarServer.Setup(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(sonarService: sonarServer.Object);

            Func<Task> act = testSubject.SynchronizeWithServer;
            await act.Should().ThrowAsync<StackOverflowException>();
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/3152")]
        public void SynchronizeWithServer_DisconnectedInTheMiddle_ServerInfoIsReusedAndNoExceptions()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            sonarQubeServer
                .SetupSequence(x => x.GetServerInfo())
                .Returns(new ServerInfo(new Version(1, 1), ServerType.SonarQube))
                .Returns((ServerInfo)null);

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                sonarService: sonarQubeServer.Object,
                logger: logger);

            Func<Task> act = testSubject.SynchronizeWithServer;

            act.Should().NotThrow();

            logger.AssertPartialOutputStringDoesNotExist("NullReferenceException");
        }

        [TestMethod]
        public async Task SynchronizeWithServer_StandaloneMode_StoreAndUIContextCleared()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();
            var serverBranchProvider = new Mock<IStatefulServerBranchProvider>();
            var logger = new TestLogger();

            const uint cookie = 123;
            var monitor = CreateMonitorSelectionMock(cookie);
            var toolWindowService = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Standalone,
                taintStore: taintStore.Object,
                sonarService: sonarQubeServer.Object,
                converter: converter.Object,
                serverBranchProvider: serverBranchProvider.Object,
                vsMonitor: monitor.Object,
                toolWindowService: toolWindowService.Object,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            CheckStoreIsCleared(taintStore);
            CheckUIContextIsCleared(monitor, cookie);
            logger.AssertPartialOutputStringExists("not in connected mode");

            // Server components should not be called
            sonarQubeServer.Invocations.Should().HaveCount(0);
            converter.Invocations.Should().HaveCount(0);
            serverBranchProvider.Invocations.Should().HaveCount(0);
            toolWindowService.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_SonarQubeServerNotYetConnected_StoreAndUIContextCleared()
        {
            var sonarService = CreateSonarService(isConnected: false);
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();
            var logger = new TestLogger();

            const uint cookie = 999;
            var monitor = CreateMonitorSelectionMock(cookie);
            var toolWindowService = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                taintStore: taintStore.Object,
                converter: converter.Object,
                sonarService: sonarService.Object,
                vsMonitor: monitor.Object,
                toolWindowService: toolWindowService.Object,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            logger.AssertPartialOutputStringExists("not yet established");
            CheckConnectedStatusIsChecked(sonarService);
            CheckIssuesAreNotFetched(sonarService);

            CheckStoreIsCleared(taintStore);
            CheckUIContextIsCleared(monitor, cookie);

            // Should be nothing to convert or display in the tool window
            converter.Invocations.Should().HaveCount(0);
            toolWindowService.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        [DataRow("7.9")]
        [DataRow("8.5.9.9")]
        public async Task SynchronizeWithServer_UnsupportedServerVersion_StoreAndUIContextCleared(string versionString)
        {
            var sonarQubeServer = CreateSonarService(isConnected: true, serverType: ServerType.SonarQube, versionString);
            var logger = new TestLogger();

            const uint cookie = 999;
            var monitor = CreateMonitorSelectionMock(cookie);
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                taintStore: taintStore.Object,
                sonarService: sonarQubeServer.Object,
                vsMonitor: monitor.Object,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            logger.AssertPartialOutputStringExists("requires SonarQube v8.6 or later");
            logger.AssertPartialOutputStringExists($"Connected SonarQube version: v{versionString}");

            CheckIssuesAreNotFetched(sonarQubeServer);
            CheckStoreIsCleared(taintStore);
            CheckUIContextIsCleared(monitor, cookie);
        }

        [TestMethod]
        [DataRow(ServerType.SonarCloud, "0.1")]
        [DataRow(ServerType.SonarQube, "8.6.0.0")]
        [DataRow(ServerType.SonarQube, "9.9")]
        public async Task SynchronizeWithServer_SupportedServer_IssuesFetched(ServerType serverType, string serverVersion)
        {
            var logger = new TestLogger();
            var sonarServer = CreateSonarService(isConnected: true, serverType, serverVersion);

            var bindingConfig = CreateBindingConfig(SonarLintMode.Connected, "keyXXX");
            var serverBranchProvider = CreateServerBranchProvider("branchXXX");
            SetupTaintIssues(sonarServer, "keyXXX", "branchXXX");

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                serverBranchProvider: serverBranchProvider.Object,
                sonarService: sonarServer.Object,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            logger.AssertPartialOutputStringDoesNotExist("requires SonarQube v8.6 or later");
            CheckIssuesAreFetched(sonarServer, "keyXXX", "branchXXX");
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_StoreIsSetAndUIContextCleared(SonarLintMode sonarLintMode)
        {
            var sonarService = CreateSonarService(isConnected: true);
            var bindingConfig = CreateBindingConfig(sonarLintMode, "my-project-key");
            var serverBranchProvider = CreateServerBranchProvider("my-branch");
            var analysisInformation = new AnalysisInformation("my-branch", DateTimeOffset.Now);

            SetupTaintIssues(sonarService, "my-project-key", "my-branch" /* no issues */);
            SetupAnalysisInformation(sonarService, "my-project-key", analysisInformation);

            var taintStore = new Mock<ITaintStore>();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();

            const uint cookie = 999;
            var monitor = CreateMonitorSelectionMock(cookie);
            var toolWindowService = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                taintStore: taintStore.Object,
                converter: converter.Object,
                sonarService: sonarService.Object,
                serverBranchProvider: serverBranchProvider.Object,
                vsMonitor: monitor.Object,
                toolWindowService: toolWindowService.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarService);
            CheckIssuesAreFetched(sonarService, "my-project-key", "my-branch");
            CheckUIContextIsCleared(monitor, cookie);

            taintStore.Verify(x => x.Set(Enumerable.Empty<IAnalysisIssueVisualization>(),
                It.Is((AnalysisInformation a) =>
                    a.AnalysisTimestamp == analysisInformation.AnalysisTimestamp &&
                    a.BranchName == analysisInformation.BranchName)), Times.Once);

            // Should be nothing to display in the tool window
            toolWindowService.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedMode_UsesExpectedBranch(SonarLintMode sonarLintMode)
        {
            var bindingConfig = CreateBindingConfig(sonarLintMode, "xxx_project-key");

            var serverBranchProvider = CreateServerBranchProvider("branch-XYZ");

            var sonarQubeService = CreateSonarService(isConnected: true);
            SetupTaintIssues(sonarQubeService, "xxx_project-key", "branch-XYZ");

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                sonarService: sonarQubeService.Object,
                serverBranchProvider: serverBranchProvider.Object);

            await testSubject.SynchronizeWithServer();

            serverBranchProvider.VerifyAll();
            sonarQubeService.Verify(x => x.GetTaintVulnerabilitiesAsync("xxx_project-key", "branch-XYZ", It.IsAny<CancellationToken>()),
                Times.Once());
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithIssues_IssuesAddedToStore(SonarLintMode sonarLintMode)
        {
            var serverIssue1 = new TestSonarQubeIssue();
            var serverIssue2 = new TestSonarQubeIssue();
            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();

            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            converter.Setup(x => x.Convert(serverIssue1)).Returns(issueViz1);
            converter.Setup(x => x.Convert(serverIssue2)).Returns(issueViz2);

            var taintStore = new Mock<ITaintStore>();

            var analysisInformation = new AnalysisInformation("a branch", DateTimeOffset.Now);

            var sonarServer = CreateSonarService();

            var serverBranchProvider = CreateServerBranchProvider("a branch");
            var bindingConfig = CreateBindingConfig(sonarLintMode, "projectKey123");
            SetupTaintIssues(sonarServer, "projectKey123", "a branch", serverIssue1, serverIssue2);
            SetupAnalysisInformation(sonarServer, "projectKey123", analysisInformation);

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                taintStore: taintStore.Object,
                converter: converter.Object,
                sonarService: sonarServer.Object,
                serverBranchProvider: serverBranchProvider.Object);

            await testSubject.SynchronizeWithServer();

            taintStore.Verify(x => x.Set(new[] { issueViz1, issueViz2 },
                It.Is((AnalysisInformation a) =>
                    a.AnalysisTimestamp == analysisInformation.AnalysisTimestamp &&
                    a.BranchName == analysisInformation.BranchName)), Times.Once);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithIssues_UIContextIsSetAndToolWindowCalled(SonarLintMode sonarLintMode)
        {
            var sonarService = CreateSonarService();

            var bindingConfig = CreateBindingConfig(sonarLintMode, "myProjectKey___");
            var serverBranchProvider = CreateServerBranchProvider("branchYYY");
            SetupTaintIssues(sonarService, "myProjectKey___", "branchYYY", new TestSonarQubeIssue());
            SetupAnalysisInformation(sonarService, "myProjectKey___", new AnalysisInformation("branchYYY", DateTimeOffset.Now));

            const uint cookie = 212;
            var monitor = CreateMonitorSelectionMock(cookie);
            var toolWindowService = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                serverBranchProvider: serverBranchProvider.Object,
                sonarService: sonarService.Object,
                vsMonitor: monitor.Object,
                toolWindowService: toolWindowService.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarService);
            CheckIssuesAreFetched(sonarService, "myProjectKey___", "branchYYY");
            CheckUIContextIsSet(monitor, cookie);
            CheckToolWindowServiceIsCalled(toolWindowService);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("unknown local branch")]
        public async Task SynchronizeWithServer_NoMatchingServerBranch_UIContextAndStoreCleared(string localBranch)
        {
            var sonarService = CreateSonarService();
            var bindingConfig = CreateBindingConfig(SonarLintMode.Connected, "my proj");
            var serverBranchProvider = CreateServerBranchProvider(localBranch);
            SetupTaintIssues(sonarService, "my proj", localBranch, new TestSonarQubeIssue());

            var analysisInformation = new AnalysisInformation("some main branch", DateTimeOffset.Now);
            SetupAnalysisInformation(sonarService, "my proj", analysisInformation);

            const uint cookie = 212;
            var monitor = CreateMonitorSelectionMock(cookie);
            var toolWindowService = new Mock<IToolWindowService>();
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(
                taintStore: taintStore.Object,
                bindingConfig: bindingConfig,
                serverBranchProvider: serverBranchProvider.Object,
                sonarService: sonarService.Object,
                vsMonitor: monitor.Object,
                toolWindowService: toolWindowService.Object);

            using (new AssertIgnoreScope())
            {
                await testSubject.SynchronizeWithServer();
            }

            CheckIssuesAreFetched(sonarService, "my proj", localBranch);
            CheckUIContextIsCleared(monitor, cookie);
            CheckStoreIsCleared(taintStore);
        }

        private static BindingConfiguration CreateBindingConfig(SonarLintMode mode = SonarLintMode.Connected, string projectKey = "any")
            => new(new BoundServerProject("solution", projectKey, new ServerConnection.SonarQube(new Uri("http://bound"))), mode, "any dir");

        private static TaintIssuesSynchronizer CreateTestSubject(
            BindingConfiguration bindingConfig = null,
            ITaintStore taintStore = null,
            ITaintIssueToIssueVisualizationConverter converter = null,
            ILogger logger = null,
            ISonarQubeService sonarService = null,
            IStatefulServerBranchProvider serverBranchProvider = null,
            IVsMonitorSelection vsMonitor = null,
            IToolWindowService toolWindowService = null)
        {
            taintStore ??= Mock.Of<ITaintStore>();
            converter ??= Mock.Of<ITaintIssueToIssueVisualizationConverter>();

            var serviceOperation = CreateServiceOperation(vsMonitor);

            bindingConfig ??= CreateBindingConfig(SonarLintMode.Connected, "any branch");

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider
                .Setup(x => x.GetConfiguration())
                .Returns(bindingConfig);

            sonarService ??= CreateSonarService().Object;
            serverBranchProvider ??= Mock.Of<IStatefulServerBranchProvider>();
            toolWindowService ??= Mock.Of<IToolWindowService>();

            logger ??= Mock.Of<ILogger>();

            return new TaintIssuesSynchronizer(taintStore, sonarService, converter, configurationProvider.Object,
                toolWindowService, serverBranchProvider, serviceOperation, logger);
        }

        private static IVsUIServiceOperation CreateServiceOperation(IVsMonitorSelection svcToPassToCallback)
        {
            svcToPassToCallback ??= Mock.Of<IVsMonitorSelection>();

            var serviceOp = new Mock<IVsUIServiceOperation>();

            // Set up the mock to invoke the operation with the supplied VS service
            serviceOp.Setup(x => x.Execute<VSShellInterop.SVsShellMonitorSelection, VSShellInterop.IVsMonitorSelection>(It.IsAny<Action<IVsMonitorSelection>>()))
                .Callback<Action<IVsMonitorSelection>>(op => op(svcToPassToCallback));

            return serviceOp.Object;
        }

        private static Mock<IStatefulServerBranchProvider> CreateServerBranchProvider(string branchName)
        {
            var serverBranchProvider = new Mock<IStatefulServerBranchProvider>();
            serverBranchProvider.Setup(x => x.GetServerBranchNameAsync(It.IsAny<CancellationToken>())).ReturnsAsync(branchName);
            return serverBranchProvider;
        }

        private static Mock<ISonarQubeService> CreateSonarService(bool isConnected = true,
            ServerType serverType = ServerType.SonarQube,
            string versionString = "9.9")
        {
            var serverInfo = isConnected ? new ServerInfo(new Version(versionString), serverType) : null;

            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.GetServerInfo()).Returns(serverInfo);

            return sonarQubeService;
        }

        private static Mock<IVsMonitorSelection> CreateMonitorSelectionMock(uint cookie)
        {
            var monitor = new Mock<IVsMonitorSelection>();
            var localGuid = TaintIssuesExistUIContext.Guid;
            monitor.Setup(x => x.GetCmdUIContextCookie(ref localGuid, out cookie));

            return monitor;
        }

        private static void CheckUIContextIsCleared(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie) =>
            CheckUIContextUpdated(monitorMock, expectedCookie, 0);

        private static void CheckUIContextIsSet(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie) =>
            CheckUIContextUpdated(monitorMock, expectedCookie, 1);

        private static void CheckUIContextUpdated(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie, int expectedState) =>
            monitorMock.Verify(x => x.SetCmdUIContext(expectedCookie, expectedState), Times.Once);

        private static void CheckConnectedStatusIsChecked(Mock<ISonarQubeService> serviceMock) =>
            serviceMock.Verify(x => x.GetServerInfo(), Times.Once);

        private static void CheckIssuesAreFetched(Mock<ISonarQubeService> serviceMock, string projectKey, string branch) =>
            serviceMock.Verify(x => x.GetTaintVulnerabilitiesAsync(projectKey, branch, It.IsAny<CancellationToken>()), Times.Once);

        private static void CheckIssuesAreNotFetched(Mock<ISonarQubeService> serviceMock) =>
            serviceMock.Verify(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        private static void CheckToolWindowServiceIsCalled(Mock<IToolWindowService> toolWindowServiceMock) =>
            toolWindowServiceMock.Verify(x => x.EnsureToolWindowExists(TaintToolWindow.ToolWindowId), Times.Once);

        private void CheckStoreIsCleared(Mock<ITaintStore> taintStore) =>
            taintStore.Verify(x => x.Set(Enumerable.Empty<IAnalysisIssueVisualization>(), null), Times.Once());

        private class TestSonarQubeIssue : SonarQubeIssue
        {
            public TestSonarQubeIssue()
                : base("test", "test", "test", "test", "test", "test", true, SonarQubeIssueSeverity.Info,
                      DateTimeOffset.MinValue, DateTimeOffset.MinValue, null, null)
            {
            }
        }

        private void SetupAnalysisInformation(Mock<ISonarQubeService> sonarQubeService, string projectKey, AnalysisInformation mainBranchInformation)
        {
            var projectBranches = new[]
                {
                    new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, DateTimeOffset.MaxValue, "BRANCH"),
                    new SonarQubeProjectBranch(mainBranchInformation.BranchName, true, mainBranchInformation.AnalysisTimestamp, "BRANCH"),
                    new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, DateTimeOffset.MinValue, "BRANCH")
                };

            sonarQubeService.Setup(x => x.GetProjectBranchesAsync(projectKey, CancellationToken.None))
                .ReturnsAsync(projectBranches);
        }

        private void SetupTaintIssues(Mock<ISonarQubeService> sonarQubeService, string projectKey, string branch, params SonarQubeIssue[] issues)
        {
            sonarQubeService
                .Setup(x => x.GetTaintVulnerabilitiesAsync(projectKey, branch, CancellationToken.None))
                .ReturnsAsync(issues);
        }
    }
}
