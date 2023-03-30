/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.ServerSentEvents;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.ServerSentEvents
{
    [TestClass]
    public class TaintServerEventsListenerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<TaintServerEventsListener, ITaintServerEventsListener>(
                MefTestHelpers.CreateExport<IStatefulServerBranchProvider>(),
                MefTestHelpers.CreateExport<ITaintStore>(),
                MefTestHelpers.CreateExport<ITaintServerEventSource>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<ITaintIssueToIssueVisualizationConverter>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public async Task OnEvent_UnrecognizedServerEvent_EventIsIgnored()
        {
            var taintStore = new Mock<ITaintStore>();
            var logger = new Mock<ILogger>();
            var taintServerEventSource = SetupTaintServerEventSource(Mock.Of<ITestEvent>());

            var testSubject = CreateTestSubject(
                taintServerEventSource: taintServerEventSource.Object,
                taintStore: taintStore.Object,
                logger: logger.Object);

            await testSubject.ListenAsync();

            taintStore.Invocations.Count.Should().Be(0);

            logger.Verify(x =>
                    x.LogVerbose(It.Is<string>(s => s.Contains("ITestEvent"))),
                Times.Once);
        }

        [TestMethod]
        public async Task OnEvent_TaintClosedServerEvent_IssueIsRemovedFromStore()
        {
            const string issueKey = "some issue1";

            var serverEvent = CreateTaintClosedServerEvent(issueKey);
            var taintServerEventSource = SetupTaintServerEventSource(serverEvent);
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(
                taintServerEventSource: taintServerEventSource.Object,
                taintStore: taintStore.Object);

            await testSubject.ListenAsync();

            taintStore.Verify(x => x.Remove(issueKey), Times.Once);
            taintStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task OnEvent_TaintRaisedServerEvent_EventIsNotForCurrentBranch_IssueIgnored()
        {
            var taintIssue = Mock.Of<ITaintIssue>();
            var serverEvent = CreateTaintRaisedServerEvent(taintIssue, "some branch");
            var taintServerEventSource = SetupTaintServerEventSource(serverEvent);
            var serverBranchProvider = SetupBranchProvider("another branch");

            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(
                taintServerEventSource: taintServerEventSource.Object,
                taintStore: taintStore.Object,
                serverBranchProvider: serverBranchProvider.Object);

            await testSubject.ListenAsync();

            serverBranchProvider.Verify(x => x.GetServerBranchNameAsync(It.IsAny<CancellationToken>()), Times.Once);
            taintStore.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task OnEvent_TaintRaisedServerEvent_EventIsForCurrentBranch_IssueIsAddedToStore()
        {
            const string branchName = "master";

            var taintIssue = Mock.Of<ITaintIssue>();
            var serverEvent = CreateTaintRaisedServerEvent(taintIssue, branchName);
            var taintServerEventSource = SetupTaintServerEventSource(serverEvent);
            var convertedIssueViz = Mock.Of<IAnalysisIssueVisualization>();

            var converter = new Mock<ITaintIssueToIssueVisualizationConverter>();
            converter.Setup(x => x.Convert(taintIssue))
                .Returns(convertedIssueViz);

            var serverBranchProvider = SetupBranchProvider(branchName);
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(
                taintServerEventSource: taintServerEventSource.Object,
                taintStore: taintStore.Object,
                taintToIssueVizConverter: converter.Object,
                serverBranchProvider: serverBranchProvider.Object);

            await testSubject.ListenAsync();

            taintStore.Verify(x => x.Add(convertedIssueViz), Times.Once);
            taintStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task OnEvent_FailureToProcessTaintEvent_NonCriticalException_EventIsIgnored()
        {
            var serverEvent1 = CreateTaintClosedServerEvent("some issue1");
            var serverEvent2 = CreateTaintClosedServerEvent("some issue2");
            var taintServerEventSource = SetupTaintServerEventSource(serverEvent1, serverEvent2);
            var taintStore = new Mock<ITaintStore>();

            taintStore
                .Setup(x => x.Remove("some issue1"))
                .Throws(new NotImplementedException("this is a test"));

            var logger = new Mock<ILogger>();

            var testSubject = CreateTestSubject(
                taintServerEventSource: taintServerEventSource.Object,
                taintStore: taintStore.Object,
                logger: logger.Object);

            await testSubject.ListenAsync();

            taintStore.Verify(x => x.Remove("some issue1"), Times.Once);
            taintStore.Verify(x => x.Remove("some issue2"), Times.Once);
            taintStore.VerifyNoOtherCalls();
            logger.Verify(x=> x.LogVerbose(It.Is<string>(s=> s.Contains("this is a test")), Array.Empty<object>()), Times.Once());
        }

        [TestMethod]
        public void OnEvent_FailureToProcessTaintEvent_CriticalException_StopsListeningToServerEventsSource()
        {
            var serverEvent1 = CreateTaintClosedServerEvent("some issue1");
            var serverEvent2 = CreateTaintClosedServerEvent("some issue2");
            var taintServerEventSource = SetupTaintServerEventSource(serverEvent1, serverEvent2);
            var taintStore = new Mock<ITaintStore>();

            taintStore
                .Setup(x => x.Remove("some issue1"))
                .Throws(new StackOverflowException("this is a test"));

            var testSubject = CreateTestSubject(
                taintServerEventSource: taintServerEventSource.Object,
                taintStore: taintStore.Object);

            Func<Task> func = async () => await testSubject.ListenAsync();

            func.Should().Throw<StackOverflowException>().And.Message.Should().Be("this is a test");

            taintStore.Verify(x => x.Remove("some issue1"), Times.Once);
            taintStore.Verify(x => x.Remove("some issue2"), Times.Never);
            taintStore.VerifyNoOtherCalls();
            taintServerEventSource.Verify(x => x.GetNextEventOrNullAsync(), Times.Once);
            taintServerEventSource.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Dispose_StopsListeningToServerEventsSource()
        {
            var firstListenTask = new TaskCompletionSource<ITaintServerEvent>();

            var event1 = CreateTaintClosedServerEvent("issue1");
            var event2 = CreateTaintClosedServerEvent("issue2");

            var taintServerEventSource = new Mock<ITaintServerEventSource>();
            taintServerEventSource
                .SetupSequence(x => x.GetNextEventOrNullAsync())
                .Returns(firstListenTask.Task)
                .ReturnsAsync(event2)
                .ReturnsAsync((ITaintServerEvent) null);
            
            var taintStore = new Mock<ITaintStore>();

            var testSubject = CreateTestSubject(
                taintServerEventSource: taintServerEventSource.Object,
                taintStore: taintStore.Object);

            var listenTask = testSubject.ListenAsync();

            testSubject.Dispose();

            firstListenTask.SetResult(event1);

            await listenTask;

            taintServerEventSource.Verify(x => x.GetNextEventOrNullAsync(), Times.Once);
            taintStore.Verify(x=> x.Remove("issue1"), Times.Once);
            taintStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Description("Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/3946")]
        public void Dispose_CalledASecondTime_NoException()
        {
            var testSubject = CreateTestSubject();

            testSubject.Dispose();

            Action act = () => testSubject.Dispose();
            act.Should().NotThrow();
        }

        private static ITaintVulnerabilityClosedServerEvent CreateTaintClosedServerEvent(string taintKey)
        {
            var serverEvent = new Mock<ITaintVulnerabilityClosedServerEvent>();
            serverEvent.Setup(x => x.Key).Returns(taintKey);

            return serverEvent.Object;
        }

        private static ITaintVulnerabilityRaisedServerEvent CreateTaintRaisedServerEvent(ITaintIssue taintIssue, string issueBranch)
        {
            var serverEvent = new Mock<ITaintVulnerabilityRaisedServerEvent>();
            serverEvent.Setup(x => x.Branch).Returns(issueBranch);
            serverEvent.Setup(x => x.Issue).Returns(taintIssue);

            return serverEvent.Object;
        }

        private static Mock<ITaintServerEventSource> SetupTaintServerEventSource(params ITaintServerEvent[] serverEvents)
        {
            var taintServerEventSource = new Mock<ITaintServerEventSource>();

            var sequence = taintServerEventSource.SetupSequence(x => x.GetNextEventOrNullAsync());

            foreach (var serverEvent in serverEvents)
            {
                sequence.ReturnsAsync(serverEvent);
            }

            // Signal that the task is finished
            sequence.ReturnsAsync((ITaintServerEvent)null);

            return taintServerEventSource;
        }

        private static Mock<IStatefulServerBranchProvider> SetupBranchProvider(string currentBranch)
        {
            var branchProvider = new Mock<IStatefulServerBranchProvider>();
            branchProvider
                .Setup(x => x.GetServerBranchNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentBranch);

            return branchProvider;
        }

        private static TaintServerEventsListener CreateTestSubject(
            IStatefulServerBranchProvider serverBranchProvider = null,
            ITaintServerEventSource taintServerEventSource = null, 
            ITaintStore taintStore = null,
            ITaintIssueToIssueVisualizationConverter taintToIssueVizConverter = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            serverBranchProvider ??= Mock.Of<IStatefulServerBranchProvider>();
            taintServerEventSource ??= Mock.Of<ITaintServerEventSource>();
            taintStore ??= Mock.Of<ITaintStore>();
            taintToIssueVizConverter ??= Mock.Of<ITaintIssueToIssueVisualizationConverter>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= Mock.Of<ILogger>();

            return new TaintServerEventsListener(serverBranchProvider, taintServerEventSource, taintStore, threadHandling, taintToIssueVizConverter, logger);
        }

        /// <summary>
        /// Used to test unrecognized taint events
        /// </summary>
        public interface ITestEvent : ITaintServerEvent
        {
        }
    }
}
