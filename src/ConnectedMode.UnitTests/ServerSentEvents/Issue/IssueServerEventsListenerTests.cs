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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.ServerSentEvents.Issue;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.ServerSentEvents.Issue
{
    [TestClass]
    public class IssueServerEventsListenerTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueServerEventsListener, IIssueServerEventsListener>(
                MefTestHelpers.CreateExport<IIssueServerEventSource>(),
                MefTestHelpers.CreateExport<ISuppressionIssueStoreUpdater>(),
                MefTestHelpers.CreateExport<IStatefulServerBranchProvider>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public async Task OnEvent_SingleEvent_MultipleTuples_StoreIsUpdatedWithIssuesFromCurrentBranch()
        {
            var event1 = CreateServerEvent(isResolved: true,
                new BranchAndIssueKey("issueKey1", "branch1"),
                new BranchAndIssueKey("issueKey2", "branch2"),
                new BranchAndIssueKey("issueKey3", "branch1"));
            
            var issueServerEventSource = SetupIssueServerEventSource(event1);
            var storeUpdater = new Mock<ISuppressionIssueStoreUpdater>();
            var branchProvider = CreateBranchProvider("branch1");

            var testSubject = CreateTestSubject(issueServerEventSource.Object, storeUpdater.Object, branchProvider.Object);

            await testSubject.ListenAsync();

            issueServerEventSource.Verify(x => x.GetNextEventOrNullAsync(), Times.Exactly(2));
            issueServerEventSource.VerifyNoOtherCalls();

            storeUpdater.Verify(x => x.UpdateSuppressedIssuesAsync(
                    true,
                    new[] {"issueKey1", "issueKey3"},
                    It.IsAny<CancellationToken>()),
                Times.Once);
            storeUpdater.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task OnEvent_MultipleEvents_StoreIsUpdatedWithIssuesFromCurrentBranch()
        {
            var event1 = CreateServerEvent(isResolved: true, new BranchAndIssueKey("issueKey1", "branch1"));
            var event2 = CreateServerEvent(isResolved: true, new BranchAndIssueKey("issueKey2", "branch2"));
            var event3 = CreateServerEvent(isResolved: false, new BranchAndIssueKey("issueKey3", "branch1"));

            var issueServerEventSource = SetupIssueServerEventSource(event1, event2, event3);
            var storeUpdater = new Mock<ISuppressionIssueStoreUpdater>();
            var branchProvider = CreateBranchProvider("branch1");

            var testSubject = CreateTestSubject(issueServerEventSource.Object, storeUpdater.Object, branchProvider.Object);

            await testSubject.ListenAsync();

            issueServerEventSource.Verify(x => x.GetNextEventOrNullAsync(), Times.Exactly(4));
            issueServerEventSource.VerifyNoOtherCalls();

            storeUpdater.Verify(x => x.UpdateSuppressedIssuesAsync(
                    true,
                    new[] { "issueKey1" },
                    It.IsAny<CancellationToken>()),
                Times.Once);

            storeUpdater.Verify(x => x.UpdateSuppressedIssuesAsync(
                    false,
                    new[] {"issueKey3" },
                    It.IsAny<CancellationToken>()),
                Times.Once);

            storeUpdater.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void OnEvent_FailureToProcessIssueEvent_CriticalException_StopsListeningToServerEventsSource()
        {
            var issueServerEventSource = new Mock<IIssueServerEventSource>();
            issueServerEventSource
                .Setup(x => x.GetNextEventOrNullAsync())
                .Throws(new StackOverflowException("this is a test"));

            var testSubject = CreateTestSubject(issueServerEventSource: issueServerEventSource.Object);

            Func<Task> func = async () => await testSubject.ListenAsync();

            func.Should().Throw<StackOverflowException>().And.Message.Should().Be("this is a test");
            issueServerEventSource.Verify(x => x.GetNextEventOrNullAsync(), Times.Once);
            issueServerEventSource.VerifyNoOtherCalls();
        }

        [TestMethod]
        public async Task Dispose_StopsListeningToServerEventsSource()
        {
            var issueServerEventSource = SetupIssueServerEventSource();
            var testSubject = CreateTestSubject(issueServerEventSource: issueServerEventSource.Object);

            var listenTask = testSubject.ListenAsync();
            testSubject.Dispose();

            await listenTask;

            issueServerEventSource.Verify(x => x.GetNextEventOrNullAsync(), Times.Once);
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

        private static Mock<IIssueServerEventSource> SetupIssueServerEventSource(params IIssueChangedServerEvent[] serverEvents)
        {
            var issueServerEventSource = new Mock<IIssueServerEventSource>();

            var sequence = issueServerEventSource.SetupSequence(x => x.GetNextEventOrNullAsync());

            foreach (var serverEvent in serverEvents)
            {
                sequence.ReturnsAsync(serverEvent);
            }

            // Signal that the task is finished
            sequence.ReturnsAsync((IIssueChangedServerEvent)null);

            return issueServerEventSource;
        }

        private static IssueServerEventsListener CreateTestSubject(
            IIssueServerEventSource issueServerEventSource = null,
            ISuppressionIssueStoreUpdater suppressionIssueStoreUpdater = null,
            IStatefulServerBranchProvider branchProvider = null,
            IThreadHandling threadHandling = null,
            ILogger logger = null)
        {
            issueServerEventSource ??= Mock.Of<IIssueServerEventSource>();
            suppressionIssueStoreUpdater ??= Mock.Of<ISuppressionIssueStoreUpdater>();
            branchProvider ??= Mock.Of<IStatefulServerBranchProvider>();
            threadHandling ??= new NoOpThreadHandler();
            logger ??= Mock.Of<ILogger>();

            return new IssueServerEventsListener(issueServerEventSource, suppressionIssueStoreUpdater, branchProvider, threadHandling, logger);
        }

        private static IIssueChangedServerEvent CreateServerEvent(bool isResolved, params BranchAndIssueKey[] branchAndIssueKeys)
        {
            var serverEvent = new Mock<IIssueChangedServerEvent>();
            serverEvent.Setup(x => x.IsResolved).Returns(isResolved);
            serverEvent.Setup(x => x.BranchAndIssueKeys).Returns(branchAndIssueKeys);

            return serverEvent.Object;
        }

        private Mock<IStatefulServerBranchProvider> CreateBranchProvider(string serverBranch)
        {
            var branchProvider = new Mock<IStatefulServerBranchProvider>();

            branchProvider
                .Setup(x => x.GetServerBranchNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(serverBranch);

            return branchProvider;
        }
    }
}
