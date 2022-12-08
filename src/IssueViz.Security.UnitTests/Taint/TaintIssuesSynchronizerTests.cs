/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList;
using SonarQube.Client;
using SonarQube.Client.Models;
using Task = System.Threading.Tasks.Task;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintIssuesSynchronizerTests
    {
        private static readonly BindingConfiguration BindingConfig_Standalone = CreateBindingConfig(SonarLintMode.Standalone, null);
        private static readonly BindingConfiguration BindingConfig_Connected = CreateBindingConfig(SonarLintMode.Connected, "any project key");

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintIssuesSynchronizer, ITaintIssuesSynchronizer>(
                MefTestHelpers.CreateExport<ITaintStore>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IServerBranchProvider>(),
                // The constructor calls the service provider so we need to pass a correctly-configured one
                MefTestHelpers.CreateExport<SVsServiceProvider>(CreateServiceProvider()),
                MefTestHelpers.CreateExport<IToolWindowService>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void SynchronizeWithServer_FailureToCheckServerConnection_CriticalException_ExceptionThrown()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            sonarQubeServer.Setup(x => x.GetServerInfo()).Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                sonarService: sonarQubeServer.Object);

            Func<Task> act = testSubject.SynchronizeWithServer;

            act.Should().Throw<StackOverflowException>();
        }

        [TestMethod]
        public void SynchronizeWithServer_FailureToCheckServerConnection_NonCriticalException_ExceptionIsCaught()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            sonarQubeServer.Setup(x => x.GetServerInfo()).Throws(new NotImplementedException("this is a test"));

            var logger = new TestLogger();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                sonarService: sonarQubeServer.Object,
                logger: logger);

            Func<Task> act = testSubject.SynchronizeWithServer;

            act.Should().NotThrow();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/3152")]
        public void SynchronizeWithServer_DisconnectedInTheMiddle_ServerInfoIsReusedAndNoExceptions()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            sonarQubeServer
                .SetupSequence(x => x.GetServerInfo())
                .Returns(new ServerInfo(new Version(1, 1), ServerType.SonarQube))
                .Returns((ServerInfo) null);

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
        public async Task SynchronizeWithServer_NotInConnectedMode_ServerComponentsNotCalledAndStoreCleared()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();
            var serverBranchProvider = new Mock<IServerBranchProvider>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Standalone,
                taintStore: taintStore.Object,
                sonarService: sonarQubeServer.Object,
                converter: converter.Object,
                serverBranchProvider: serverBranchProvider.Object,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            CheckStoreIsCleared(taintStore);

            // Server components should not be called
            logger.AssertPartialOutputStringExists("not in connected mode");
            sonarQubeServer.Invocations.Should().HaveCount(0);
            converter.Invocations.Should().HaveCount(0);
            serverBranchProvider.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        [DataRow("7.9")]
        [DataRow("8.5.9.9")]
        public async Task SynchronizeWithServer_UnsupportedServerVersion_StoreCleared(string versionString)
        {
            const uint cookie = 999;
            var sonarQubeServer = CreateSonarService(isConnected: true, serverType: ServerType.SonarQube, versionString);
            var taintStore = new Mock<ITaintStore>();
            var monitorMock = CreateMonitorSelectionMock(cookie);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                taintStore: taintStore.Object,
                sonarService: sonarQubeServer.Object,
                vsMonitor: monitorMock.Object,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            logger.AssertPartialOutputStringExists("requires SonarQube v8.6 or later");
            logger.AssertPartialOutputStringExists($"Connected SonarQube version: v{versionString}");

            CheckIssuesAreNotFetched(sonarQubeServer);
            CheckStoreIsCleared(taintStore);
            CheckUIContextUpdated(monitorMock, cookie, 0);
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
        public async Task SynchronizeWithServer_SonarQubeServerNotYetConnected_StoreCleared()
        {
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();
            var sonarService = CreateSonarService(isConnected: false);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                taintStore: taintStore.Object,
                converter: converter.Object,
                sonarService: sonarService.Object,
                logger: logger);
            
            await testSubject.SynchronizeWithServer();

            logger.AssertPartialOutputStringExists("not yet established");
            CheckIssuesAreNotFetched(sonarService);
            CheckStoreIsCleared(taintStore);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_StoreCleared()
        {
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();
            var sonarService = CreateSonarService(isConnected: true);
            SetupTaintIssues(sonarService /* no issues */);

            var testSubject = CreateTestSubject(
                taintStore: taintStore.Object,
                converter: converter.Object,
                sonarService: sonarService.Object);

            await testSubject.SynchronizeWithServer();

            converter.Invocations.Should().HaveCount(0);
            CheckStoreIsCleared(taintStore);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_FailureToSync_StoreCleared()
        {
            var sonarServerMock = CreateSonarService();
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("this is a test"));

            var taintStore = new Mock<ITaintStore>();
            var testSubject = CreateTestSubject(
                taintStore: taintStore.Object,
                sonarService: sonarServerMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckStoreIsCleared(taintStore);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_NonCriticalException_ExceptionCaughtAndLogged()
        {
            var logger = new TestLogger();

            var sonarServerMock = CreateSonarService();
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("this is a test"));

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                logger: logger,
                sonarService: sonarServerMock.Object);

            Func<Task> act = testSubject.SynchronizeWithServer;
            await act.Should().NotThrowAsync();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public async Task SynchronizeWithServer_CriticalException_ExceptionNotCaught()
        {
            var sonarServerMock = CreateSonarService();
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(sonarService: sonarServerMock.Object);

            Func<Task> act = testSubject.SynchronizeWithServer;
            await act.Should().ThrowAsync<StackOverflowException>();
        }

        [TestMethod]
        public async Task SynchronizeWithServer_ConnectedModeWithIssues_IssuesAddedToStore()
        {
            var serverIssue1 = new TestSonarQubeIssue();
            var serverIssue2 = new TestSonarQubeIssue();
            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();

            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            converter.Setup(x => x.Convert(serverIssue1)).Returns(issueViz1);
            converter.Setup(x => x.Convert(serverIssue2)).Returns(issueViz2);

            var taintStore = new Mock<ITaintStore>();

            var analysisInformation = new AnalysisInformation("some branch", DateTimeOffset.Now);

            var sonarServerMock = CreateSonarService();

            var serverBranchProvider = CreateServerBranchProvider("a branch");
            var bindingConfig = CreateBindingConfig(SonarLintMode.Connected, "projectKey123");
            SetupTaintIssues(sonarServerMock, "projectKey123", "a branch", serverIssue1, serverIssue2);
            SetupAnalysisInformation(sonarServerMock, "projectKey123", analysisInformation);

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                taintStore: taintStore.Object,
                converter: converter.Object,
                sonarService: sonarServerMock.Object,
                serverBranchProvider: serverBranchProvider.Object);

            await testSubject.SynchronizeWithServer();

            taintStore.Verify(x => x.Set(new[] {issueViz1, issueViz2},
                It.Is((AnalysisInformation a) =>
                    a.AnalysisTimestamp == analysisInformation.AnalysisTimestamp &&
                    a.BranchName == analysisInformation.BranchName)), Times.Once);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_StandaloneMode_UIContextIsCleared()
        {
            const uint cookie = 123;
            var sonarServiceMock = new Mock<ISonarQubeService>();

            var monitorMock = CreateMonitorSelectionMock(cookie);
            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Standalone,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object);

            await testSubject.SynchronizeWithServer();

            sonarServiceMock.Invocations.Should().HaveCount(0);
            CheckUIContextUpdated(monitorMock, cookie, 0);
            toolWindowServiceMock.Invocations.Should().HaveCount(0);
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
        public async Task SynchronizeWithServer_ServerNotConnected_UIContextIsCleared(SonarLintMode sonarLintMode)
        {
            const uint cookie = 222;
            var sonarServiceMock = CreateSonarService(isConnected: false);

            var monitorMock = CreateMonitorSelectionMock(cookie);
            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var bindingConfig = CreateBindingConfig(sonarLintMode);

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object,
                toolWindowService: toolWindowServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreNotFetched(sonarServiceMock);
            CheckUIContextUpdated(monitorMock, cookie, 0);
            toolWindowServiceMock.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        [DataRow("7.9")]
        [DataRow("8.5.9.9")]
        public async Task SynchronizeWithServer_UnsupportedServerVersion_UIContextIsCleared(string versionString)
        {
            const uint cookie = 999;
            var sonarQubeServer = CreateSonarService(isConnected: true, serverType: ServerType.SonarQube, versionString);
            var monitorMock = CreateMonitorSelectionMock(cookie);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(
                bindingConfig: BindingConfig_Connected,
                sonarService: sonarQubeServer.Object,
                vsMonitor: monitorMock.Object,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            CheckUIContextUpdated(monitorMock, cookie, 0);
            logger.AssertPartialOutputStringExists(versionString);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_UIContextIsCleared(SonarLintMode sonarLintMode)
        {
            const uint cookie = 999;

            var sonarServiceMock = CreateSonarService();

            var bindingConfig = CreateBindingConfig(sonarLintMode, "my-project-key");
            var serverBranchProvider = CreateServerBranchProvider("my-branch");

            SetupTaintIssues(sonarServiceMock, "my-project-key", "my-branch", Array.Empty<SonarQubeIssue>());

            var monitorMock = CreateMonitorSelectionMock(cookie);
            var toolWindowServiceMock = new Mock<IToolWindowService>();
            
            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                serverBranchProvider: serverBranchProvider.Object,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object,
                toolWindowService: toolWindowServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreFetched(sonarServiceMock, "my-project-key", "my-branch");
            CheckUIContextUpdated(monitorMock, cookie, 0);
            toolWindowServiceMock.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_FailureToSync_UIContextIsCleared(SonarLintMode sonarLintMode)
        {
            const uint cookie = 1001;

            var sonarServerMock = CreateSonarService();
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("this is a test"));

            var bindingConfig = CreateBindingConfig(sonarLintMode);

            var monitorMock = CreateMonitorSelectionMock(cookie);
            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                sonarService: sonarServerMock.Object,
                vsMonitor: monitorMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckUIContextUpdated(monitorMock, cookie, 0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithIssues_UIContextIsSetAndToolWindowCalled(SonarLintMode sonarLintMode)
        {
            const uint cookie = 212;

            var sonarServiceMock = CreateSonarService();
            
            var bindingConfig = CreateBindingConfig(sonarLintMode, "myProjectKey___");
            var serverBranchProvider = CreateServerBranchProvider("branchYYY");
            SetupTaintIssues(sonarServiceMock, "myProjectKey___", "branchYYY", new TestSonarQubeIssue());
            SetupAnalysisInformation(sonarServiceMock, "myProjectKey___", new AnalysisInformation("some branch", DateTimeOffset.Now));

            var monitorMock = CreateMonitorSelectionMock(cookie);
            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                serverBranchProvider: serverBranchProvider.Object,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object,
                toolWindowService: toolWindowServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreFetched(sonarServiceMock, "myProjectKey___", "branchYYY");
            CheckUIContextUpdated(monitorMock, cookie, 1);
            CheckToolWindowServiceIsCalled(toolWindowServiceMock);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_BranchInformationNotFetched(SonarLintMode sonarLintMode)
        {
            var bindingConfig = CreateBindingConfig(sonarLintMode);
            var sonarServiceMock = CreateSonarService();
            SetupTaintIssues(sonarServiceMock, Array.Empty<SonarQubeIssue>());

            var testSubject = CreateTestSubject(
                bindingConfig: bindingConfig,
                sonarService: sonarServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            sonarServiceMock.Verify(x => x.GetProjectBranchesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private static BindingConfiguration CreateBindingConfig(SonarLintMode mode = SonarLintMode.Connected, string projectKey = "any")
            => new BindingConfiguration(new BoundSonarQubeProject { ProjectKey = projectKey }, mode, "any dir");

        private static TaintIssuesSynchronizer CreateTestSubject(
            BindingConfiguration bindingConfig = null,
            ITaintStore taintStore = null,
            ITaintIssueToIssueVisualizationConverter converter = null,
            ILogger logger = null,
            ISonarQubeService sonarService = null,
            IServerBranchProvider serverBranchProvider = null,
            IVsMonitorSelection vsMonitor = null,
            IToolWindowService toolWindowService = null)
        {
            taintStore ??= Mock.Of<ITaintStore>();
            converter ??= Mock.Of<ITaintIssueToIssueVisualizationConverter>();

            var serviceProvider = CreateServiceProvider(vsMonitor);

            bindingConfig ??= CreateBindingConfig(SonarLintMode.Connected, "any branch");

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider
                .Setup(x => x.GetConfiguration())
                .Returns(bindingConfig);

            sonarService ??= CreateSonarService().Object;
            serverBranchProvider ??= Mock.Of<IServerBranchProvider>();
            toolWindowService ??= Mock.Of<IToolWindowService>();

            logger ??= Mock.Of<ILogger>();

            return new TaintIssuesSynchronizer(taintStore, sonarService, converter, configurationProvider.Object,
                toolWindowService, serverBranchProvider, serviceProvider, logger);
        }

        private static Mock<IServerBranchProvider> CreateServerBranchProvider(string branchName)
        {
            var mock = new Mock<IServerBranchProvider>();
            mock.Setup(x => x.GetServerBranchNameAsync()).ReturnsAsync(branchName);
            return mock;
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

        private static Mock<IVsMonitorSelection> CreateMonitorSelectionMock(uint cookieToReturn)
        {
            var monitorMock = new Mock<IVsMonitorSelection>();
            var localGuid = TaintIssuesExistUIContext.Guid;
            monitorMock.Setup(x => x.GetCmdUIContextCookie(ref localGuid, out cookieToReturn));

            return monitorMock;
        }

        private static IServiceProvider CreateServiceProvider(IVsMonitorSelection vsMonitor = null)
        {
            vsMonitor ??= Mock.Of<IVsMonitorSelection>();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShellMonitorSelection))).Returns(vsMonitor);
            return serviceProvider.Object;
        }

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
                    new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, DateTimeOffset.MaxValue),
                    new SonarQubeProjectBranch(mainBranchInformation.BranchName, true, mainBranchInformation.AnalysisTimestamp),
                    new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, DateTimeOffset.MinValue)
                };

            sonarQubeService.Setup(x => x.GetProjectBranchesAsync(projectKey, CancellationToken.None))
                .ReturnsAsync(projectBranches);
        }

        private void SetupTaintIssues(Mock<ISonarQubeService> sonarQubeService, params SonarQubeIssue[] issues)
        {
            sonarQubeService
                .Setup(x => x.GetTaintVulnerabilitiesAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(issues);
        }

        private void SetupTaintIssues(Mock<ISonarQubeService> sonarQubeService, string projectKey, string branch, params SonarQubeIssue[] issues)
        {
            sonarQubeService
                .Setup(x => x.GetTaintVulnerabilitiesAsync(projectKey, branch, CancellationToken.None))
                .ReturnsAsync(issues);
        }
    }
}
