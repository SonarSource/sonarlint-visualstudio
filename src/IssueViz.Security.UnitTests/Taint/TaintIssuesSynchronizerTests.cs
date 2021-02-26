/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
        private const string SharedProjectKey = "test";

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintIssuesSynchronizer, ITaintIssuesSynchronizer>(null, new[]
            {
                MefTestHelpers.CreateExport<ITaintStore>(Mock.Of<ITaintStore>()),
                MefTestHelpers.CreateExport<ISonarQubeService>(Mock.Of<ISonarQubeService>()),
                MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(Mock.Of<ITaintIssueToIssueVisualizationConverter>()),
                MefTestHelpers.CreateExport<IConfigurationProvider>(Mock.Of<IConfigurationProvider>()),
                MefTestHelpers.CreateExport<SVsServiceProvider>(CreateServiceProvider()),
                MefTestHelpers.CreateExport<IToolWindowService>(Mock.Of<IToolWindowService>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public async Task SynchronizeWithServer_NotInConnectedMode_StoreCleared()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(
                taintStore: taintStore.Object,
                sonarService: sonarQubeServer.Object,
                converter: converter.Object,
                mode: SonarLintMode.Standalone);

            await testSubject.SynchronizeWithServer();

            CheckStoreIsCleared(taintStore);

            sonarQubeServer.Invocations.Count.Should().Be(0);
            converter.Invocations.Count.Should().Be(0);
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
                taintStore: taintStore.Object,
                sonarService: sonarQubeServer.Object,
                mode: SonarLintMode.Connected,
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
            var sonarServer = CreateSonarService(isConnected: true, serverType, serverVersion);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(
                sonarService: sonarServer.Object,
                mode: SonarLintMode.Connected,
                logger: logger);

            await testSubject.SynchronizeWithServer();

            logger.AssertPartialOutputStringDoesNotExist("requires SonarQube v8.6 or later");
            CheckIssuesAreFetched(sonarServer);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_SonarQubeServerNotYetConnected_NoChanges()
        {
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();
            var sonarQubeService = CreateSonarService(isConnected: false);

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, sonarService: sonarQubeService.Object);
            await testSubject.SynchronizeWithServer();

            converter.Invocations.Count.Should().Be(0);
            taintStore.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_StoreCleared()
        {
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, Mock.Of<ILogger>());
            await testSubject.SynchronizeWithServer();

            converter.Invocations.Count.Should().Be(0);
            CheckStoreIsCleared(taintStore);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_FailureToSync_StoreCleared()
        {
            var sonarServerMock = CreateSonarService();
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(SharedProjectKey, CancellationToken.None))
                .Throws(new Exception("this is a test"));

            var taintStore = new Mock<ITaintStore>();
            var testSubject = CreateTestSubject(taintStore.Object, sonarService: sonarServerMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckStoreIsCleared(taintStore);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_NonCriticalException_ExceptionCaughtAndLogged()
        {
            var logger = new TestLogger();

            var sonarServerMock = CreateSonarService();
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(SharedProjectKey, CancellationToken.None))
                .Throws(new Exception("this is a test"));

            var testSubject = CreateTestSubject(logger: logger, sonarService: sonarServerMock.Object);

            Func<Task> act = async () => await testSubject.SynchronizeWithServer();
            await act.Should().NotThrowAsync();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public async Task SynchronizeWithServer_CriticalException_ExceptionNotCaught()
        {
            var sonarServerMock = CreateSonarService();
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(SharedProjectKey, CancellationToken.None))
                .Throws(new StackOverflowException());

            var testSubject = CreateTestSubject(sonarService: sonarServerMock.Object);

            Func<Task> act = async () => await testSubject.SynchronizeWithServer();
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
            SetupTaintIssues(sonarServerMock, serverIssue1, serverIssue2);
            SetupAnalysisInformation(sonarServerMock, analysisInformation);

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, sonarService: sonarServerMock.Object);
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

            var testSubject = CreateTestSubject(mode: SonarLintMode.Standalone,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object);

            await testSubject.SynchronizeWithServer();

            sonarServiceMock.Invocations.Count().Should().Be(0);
            CheckUIContextUpdated(monitorMock, cookie, 0);
            toolWindowServiceMock.Invocations.Count.Should().Be(0);
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

            var testSubject = CreateTestSubject(
                mode: sonarLintMode,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object,
                toolWindowService: toolWindowServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreNotFetched(sonarServiceMock);
            CheckUIContextUpdated(monitorMock, cookie, 0);
            toolWindowServiceMock.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_UIContextIsCleared(SonarLintMode sonarLintMode)
        {
            const uint cookie = 999;

            var sonarServiceMock = CreateSonarService();
            SetupTaintIssues(sonarServiceMock, Array.Empty<SonarQubeIssue>());

            var monitorMock = CreateMonitorSelectionMock(cookie);
            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                mode: sonarLintMode,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object,
                toolWindowService: toolWindowServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreFetched(sonarServiceMock);
            CheckUIContextUpdated(monitorMock, cookie, 0);
            toolWindowServiceMock.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithIssues_UIContextIsSetAndToolWindowCalled(SonarLintMode sonarLintMode)
        {
            const uint cookie = 212;

            var sonarServiceMock = CreateSonarService();
            SetupTaintIssues(sonarServiceMock, new TestSonarQubeIssue());
            SetupAnalysisInformation(sonarServiceMock, new AnalysisInformation("some branch", DateTimeOffset.Now));

            var monitorMock = CreateMonitorSelectionMock(cookie);
            var toolWindowServiceMock = new Mock<IToolWindowService>();

            var testSubject = CreateTestSubject(
                mode: sonarLintMode,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object,
                toolWindowService: toolWindowServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreFetched(sonarServiceMock);
            CheckUIContextUpdated(monitorMock, cookie, 1);
            CheckToolWindowServiceIsCalled(toolWindowServiceMock);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_BranchInformationNotFetched(SonarLintMode sonarLintMode)
        {
            var sonarServiceMock = CreateSonarService();
            SetupTaintIssues(sonarServiceMock, Array.Empty<SonarQubeIssue>());

            var testSubject = CreateTestSubject(sonarService: sonarServiceMock.Object);

            await testSubject.SynchronizeWithServer();

            sonarServiceMock.Verify(x => x.GetProjectBranchesAsync(SharedProjectKey, CancellationToken.None), Times.Never);
        }

        private static TaintIssuesSynchronizer CreateTestSubject(ITaintStore taintStore = null,
            ITaintIssueToIssueVisualizationConverter converter = null,
            ILogger logger = null,
            SonarLintMode mode = SonarLintMode.Connected,
            ISonarQubeService sonarService = null,
            IVsMonitorSelection vsMonitor = null,
            IToolWindowService toolWindowService = null)
        {
            taintStore ??= Mock.Of<ITaintStore>();
            converter ??= Mock.Of<ITaintIssueToIssueVisualizationConverter>();

            var serviceProvider = CreateServiceProvider(vsMonitor);

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider
                .Setup(x => x.GetConfiguration())
                .Returns(new BindingConfiguration(new BoundSonarQubeProject { ProjectKey = SharedProjectKey }, mode, ""));

            sonarService ??= CreateSonarService().Object;
            toolWindowService ??= Mock.Of<IToolWindowService>();

            logger ??= Mock.Of<ILogger>();

            return new TaintIssuesSynchronizer(taintStore, sonarService, converter, configurationProvider.Object,
                toolWindowService, serviceProvider, logger);
        }

        private static Mock<ISonarQubeService> CreateSonarService(bool isConnected = true,
            ServerType serverType = ServerType.SonarQube,
            string versionString = "9.9")
        {
            var sonarQubeService = new Mock<ISonarQubeService>();
            sonarQubeService.Setup(x => x.IsConnected).Returns(isConnected);
            sonarQubeService.Setup(x => x.ServerInfo).Returns(new ServerInfo(new Version(versionString), serverType));

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

        private static void CheckUIContextUpdated(Mock<IVsMonitorSelection> monitorMock, uint expectedCookie, int expectedState)
        {
            monitorMock.Verify(x => x.SetCmdUIContext(expectedCookie, expectedState), Times.Once);
        }

        private static void CheckConnectedStatusIsChecked(Mock<ISonarQubeService> serviceMock) =>
            serviceMock.Verify(x => x.IsConnected, Times.Once);

        private static void CheckIssuesAreFetched(Mock<ISonarQubeService> serviceMock) =>
            serviceMock.Verify(x => x.GetTaintVulnerabilitiesAsync(SharedProjectKey, It.IsAny<CancellationToken>()), Times.Once);

        private static void CheckIssuesAreNotFetched(Mock<ISonarQubeService> serviceMock) =>
            serviceMock.Verify(x => x.GetTaintVulnerabilitiesAsync(SharedProjectKey, It.IsAny<CancellationToken>()), Times.Never);

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

        private void SetupAnalysisInformation(Mock<ISonarQubeService> sonarQubeService, AnalysisInformation mainBranchInformation)
        {
            var projectBranches = new[]
                {
                    new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, DateTimeOffset.MaxValue),
                    new SonarQubeProjectBranch(mainBranchInformation.BranchName, true, mainBranchInformation.AnalysisTimestamp),
                    new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, DateTimeOffset.MinValue)
                };

            sonarQubeService.Setup(x => x.GetProjectBranchesAsync(SharedProjectKey, CancellationToken.None))
                .ReturnsAsync(projectBranches);
        }

        private void SetupTaintIssues(Mock<ISonarQubeService> sonarQubeService, params SonarQubeIssue[] issues)
        {
            sonarQubeService
                .Setup(x => x.GetTaintVulnerabilitiesAsync(SharedProjectKey, CancellationToken.None))
                .ReturnsAsync(issues);
        }
    }
}
