/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
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
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
        }

        [TestMethod]
        public async Task SynchronizeWithServer_NotInConnectedMode_StoreCleared()
        {
            var sonarQubeServer = new Mock<ISonarQubeService>();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();
            var configurationProvider = new Mock<IConfigurationProvider>();

            configurationProvider.Setup(x => x.GetConfiguration()).Returns(BindingConfiguration.Standalone);

            var testSubject = new TaintIssuesSynchronizer(
                taintStore.Object,
                sonarQubeServer.Object,
                converter.Object,
                configurationProvider.Object,
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger>());

            await testSubject.SynchronizeWithServer();

            taintStore.Verify(x => x.Set(Enumerable.Empty<IAnalysisIssueVisualization>()), Times.Once());

            sonarQubeServer.Invocations.Count.Should().Be(0);
            converter.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_SonarQubeServerNotYetConnected_NoChanges()
        {
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, isServerConnected: false);
            await testSubject.SynchronizeWithServer();

            converter.Invocations.Count.Should().Be(0);
            taintStore.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_NoIssues_StoreSynced()
        {
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, Mock.Of<ILogger>());
            await testSubject.SynchronizeWithServer();

            converter.Invocations.Count.Should().Be(0);
            taintStore.Verify(x => x.Set(It.Is((IEnumerable<IAnalysisIssueVisualization> list) => !list.Any())), Times.Once);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_FailureToSync_StoreCleared()
        {
            var serverIssue = new TestSonarQubeIssue();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            converter.Setup(x => x.Convert(serverIssue)).Throws(new NotImplementedException("this is a test"));
            var taintStore = new Mock<ITaintStore>();
            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, serverIssues: new[] { serverIssue });

            await testSubject.SynchronizeWithServer();

            taintStore.Verify(x => x.Set(Enumerable.Empty<IAnalysisIssueVisualization>()), Times.Once());
        }

        [TestMethod]
        public async Task SynchronizeWithServer_NonCriticalException_ExceptionCaughtAndLogged()
        {
            var serverIssue = new TestSonarQubeIssue();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            converter.Setup(x => x.Convert(serverIssue)).Throws(new NotImplementedException("this is a test"));
            var taintStore = new Mock<ITaintStore>();
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, logger, serverIssues: new[] { serverIssue });

            Func<Task> act = async () => await testSubject.SynchronizeWithServer();
            await act.Should().NotThrowAsync();

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public async Task SynchronizeWithServer_CriticalException_ExceptionNotCaught()
        {
            var serverIssue = new TestSonarQubeIssue();
            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            converter.Setup(x => x.Convert(serverIssue)).Throws<StackOverflowException>();
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, serverIssues: new[] {serverIssue});

            Func<Task> act = async () => await testSubject.SynchronizeWithServer();
            await act.Should().ThrowAsync<StackOverflowException>();

            taintStore.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_IssuesConverted_IssuesAddedToStore()
        {
            var serverIssue1 = new TestSonarQubeIssue();
            var serverIssue2 = new TestSonarQubeIssue();
            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();

            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            converter.Setup(x => x.Convert(serverIssue1)).Returns(issueViz1);
            converter.Setup(x => x.Convert(serverIssue2)).Returns(issueViz2);

            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(taintStore.Object, converter.Object, serverIssues: new []{serverIssue1, serverIssue2});
            await testSubject.SynchronizeWithServer();

            taintStore.Verify(x => x.Set(new[] {issueViz1, issueViz2}), Times.Once);
        }

        [TestMethod]
        public async Task SynchronizeWithServer_StandaloneMode_UIContextIsCleared()
        {
            const uint cookie = 123;
            var sonarServiceMock = new Mock<ISonarQubeService>();

            var monitorMock = CreateMonitorSelectionMock(cookie);

            var testSubject = CreateTestSubject(mode: SonarLintMode.Standalone,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object);

            await testSubject.SynchronizeWithServer();

            sonarServiceMock.Invocations.Count().Should().Be(0);
            CheckUIContextUpdated(monitorMock, cookie, 0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ServerNotConnected_UIContextIsCleared(SonarLintMode sonarLintMode)
        {
            const uint cookie = 222;
            var sonarServiceMock = CreateSonarServerMock(isServerConnected: false);

            var monitorMock = CreateMonitorSelectionMock(cookie);

            var testSubject = CreateTestSubject(
                mode: sonarLintMode,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreNotFetched(sonarServiceMock);
            CheckUIContextUpdated(monitorMock, cookie, 0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithNoIssues_UIContextIsCleared(SonarLintMode sonarLintMode)
        {
            const uint cookie = 999;
            var sonarServiceMock = CreateSonarServerMock(isServerConnected: true /* no issues returned */);

            var monitorMock = CreateMonitorSelectionMock(cookie);

            var testSubject = CreateTestSubject(
                mode: sonarLintMode,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreFetched(sonarServiceMock);
            CheckUIContextUpdated(monitorMock, cookie, 0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.Connected)]
        [DataRow(SonarLintMode.LegacyConnected)]
        public async Task SynchronizeWithServer_ConnectedModeWithIssues_UIContextIsSet(SonarLintMode sonarLintMode)
        {
            const uint cookie = 212;
            var sonarServiceMock = CreateSonarServerMock(isServerConnected: true, serverIssues: new TestSonarQubeIssue());

            var monitorMock = CreateMonitorSelectionMock(cookie);

            var testSubject = CreateTestSubject(
                mode: sonarLintMode,
                sonarService: sonarServiceMock.Object,
                vsMonitor: monitorMock.Object);

            await testSubject.SynchronizeWithServer();

            CheckConnectedStatusIsChecked(sonarServiceMock);
            CheckIssuesAreFetched(sonarServiceMock);
            CheckUIContextUpdated(monitorMock, cookie, 1);
        }

        private TaintIssuesSynchronizer CreateTestSubject(ITaintStore taintStore  = null,
            ITaintIssueToIssueVisualizationConverter converter = null,
            ILogger logger = null,
            bool isServerConnected = true,
            params SonarQubeIssue[] serverIssues)
        {
            var sonarServiceMock = CreateSonarServerMock(isServerConnected, serverIssues);
            return CreateTestSubject(taintStore, converter, logger, SonarLintMode.Connected, sonarServiceMock.Object);
        }

        private TaintIssuesSynchronizer CreateTestSubject(ITaintStore taintStore = null,
            ITaintIssueToIssueVisualizationConverter converter = null,
            ILogger logger = null,
            SonarLintMode mode = SonarLintMode.Connected,
            ISonarQubeService sonarService = null,
            IVsMonitorSelection vsMonitor = null)
        {
            taintStore ??= Mock.Of<ITaintStore>();
            converter ??= Mock.Of<ITaintIssueToIssueVisualizationConverter>();

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(SVsShellMonitorSelection))).Returns(vsMonitor);

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider
                .Setup(x => x.GetConfiguration())
                .Returns(new BindingConfiguration(new BoundSonarQubeProject { ProjectKey = SharedProjectKey }, mode, ""));

            sonarService ??= CreateSonarServerMock(true).Object;

            logger ??= Mock.Of<ILogger>();

            return new TaintIssuesSynchronizer(taintStore, sonarService, converter, configurationProvider.Object, serviceProvider.Object, logger);
        }

        private static Mock<ISonarQubeService> CreateSonarServerMock(bool isServerConnected, params SonarQubeIssue[] serverIssues)
        {
            var sonarServerMock = new Mock<ISonarQubeService>();
            sonarServerMock.Setup(x => x.IsConnected).Returns(isServerConnected);
            sonarServerMock.Setup(x => x.GetTaintVulnerabilitiesAsync(SharedProjectKey, CancellationToken.None))
                .ReturnsAsync(serverIssues);

            return sonarServerMock;
        }

        private static Mock<IVsMonitorSelection> CreateMonitorSelectionMock(uint cookieToReturn)
        {
            var monitorMock = new Mock<IVsMonitorSelection>();
            var localGuid = TaintIssuesExistUIContext.Guid;
            monitorMock.Setup(x => x.GetCmdUIContextCookie(ref localGuid, out cookieToReturn));

            return monitorMock;
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

        private class TestSonarQubeIssue : SonarQubeIssue
        {
            public TestSonarQubeIssue()
                : base("test", "test", "test", "test", "test", "test", true, SonarQubeIssueSeverity.Info,
                      DateTimeOffset.MinValue, DateTimeOffset.MinValue, null, null)
            {
            }
        }
    }
}
