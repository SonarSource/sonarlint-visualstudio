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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint
{
    [TestClass]
    public class TaintIssuesSynchronizerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintIssuesSynchronizer, ITaintIssuesSynchronizer>(null, new[]
            {
                MefTestHelpers.CreateExport<ITaintStore>(Mock.Of<ITaintStore>()),
                MefTestHelpers.CreateExport<ISonarQubeService>(Mock.Of<ISonarQubeService>()),
                MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(Mock.Of<ITaintIssueToIssueVisualizationConverter>()),
                MefTestHelpers.CreateExport<IConfigurationProvider>(Mock.Of<IConfigurationProvider>()),
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

            var testSubject = CreateTestSubject(
                taintStore.Object,
                sonarQubeServer.Object,
                converter.Object,
                configurationProvider.Object, 
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

        private TaintIssuesSynchronizer CreateTestSubject(ITaintStore taintStore,
            ITaintIssueToIssueVisualizationConverter converter,
            ILogger logger = null,
            bool isServerConnected = true,
            params SonarQubeIssue[] serverIssues)
        {
            const string projectKey = "test";

            var configurationProvider = new Mock<IConfigurationProvider>();
            configurationProvider
                .Setup(x => x.GetConfiguration())
                .Returns(new BindingConfiguration(new BoundSonarQubeProject { ProjectKey = projectKey }, SonarLintMode.Connected, ""));

            var sonarQubeServer = new Mock<ISonarQubeService>();
            sonarQubeServer.Setup(x => x.IsConnected).Returns(isServerConnected);
            sonarQubeServer.Setup(x => x.GetTaintVulnerabilitiesAsync(projectKey, CancellationToken.None))
                .ReturnsAsync(serverIssues);

            logger ??= Mock.Of<ILogger>();

            return CreateTestSubject(taintStore, sonarQubeServer.Object, converter, configurationProvider.Object, logger);
        }

        private TaintIssuesSynchronizer CreateTestSubject(ITaintStore taintStore,
            ISonarQubeService sonarQubeServer,
            ITaintIssueToIssueVisualizationConverter converter,
            IConfigurationProvider configurationProvider,
            ILogger logger)
        {
            return new TaintIssuesSynchronizer(taintStore, sonarQubeServer, converter, configurationProvider,
                Mock.Of<IServiceProvider>(), logger);
        }

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
