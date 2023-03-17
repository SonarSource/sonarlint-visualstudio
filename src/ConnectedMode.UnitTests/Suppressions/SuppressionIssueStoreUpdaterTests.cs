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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using static SonarLint.VisualStudio.TestInfrastructure.NoOpThreadHandler;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions
{
    [TestClass]
    public class SuppressionIssueStoreUpdaterTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SuppressionIssueStoreUpdater, ISuppressionIssueStoreUpdater>(
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IServerQueryInfoProvider>(),
                MefTestHelpers.CreateExport<IServerIssuesStoreWriter>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow(null, "branch")]
        [DataRow("projectKey", null)]
        public async Task UpdateAll_MissingServerQueryInfo_NoOp(string projectKey, string branchName)
        {
            // Server query info is not available -> give up
            var queryInfo = CreateQueryInfoProvider(projectKey, branchName);
            var server = new Mock<ISonarQubeService>();
            var writer = new Mock<IServerIssuesStoreWriter>();

            var testSubject = CreateTestSubject(queryInfo.Object, server.Object, writer.Object);

            await testSubject.UpdateAllServerSuppressionsAsync();

            queryInfo.Verify(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()), Times.Once());
            server.Invocations.Should().HaveCount(0);
            writer.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public async Task UpdateAll_HasServerQueryInfo_ServerQueriedAndStoreUpdated()
        {
            // Happy path - fetch and update
            var queryInfo = CreateQueryInfoProvider("project", "branch");
            
            var issue = CreateIssue("issue1");
            var server = CreateSonarQubeService("project", "branch", null, issue);
            
            var writer = new Mock<IServerIssuesStoreWriter>();

            var testSubject = CreateTestSubject(queryInfo.Object, server.Object, writer.Object);

            await testSubject.UpdateAllServerSuppressionsAsync();

            queryInfo.Verify(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()), Times.Once());
            server.Verify(x => x.GetSuppressedIssuesAsync("project", "branch", null, It.IsAny<CancellationToken>()), Times.Once());
            writer.Verify(x => x.AddIssues(new[] { issue }, true), Times.Once);

            server.Invocations.Should().HaveCount(1);
            writer.Invocations.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task UpdateAll_RunOnBackgroundThread()
        {
            var callSequence = new List<string>();

            var queryInfo = new Mock<IServerQueryInfoProvider>();
            var threadHandling = new Mock<IThreadHandling>();

            queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
                .Callback<CancellationToken>(x => callSequence.Add("GetProjectKeyAndBranchAsync"));

            threadHandling.Setup(x => x.SwitchToBackgroundThread())
                .Returns(new NoOpAwaitable())
                .Callback(() => callSequence.Add("SwitchToBackgroundThread"));

            var testSubject = CreateTestSubject(queryInfo.Object,
                threadHandling: threadHandling.Object);

            await testSubject.UpdateAllServerSuppressionsAsync();

            queryInfo.Invocations.Should().HaveCount(1);
            threadHandling.Invocations.Should().HaveCount(1);

            callSequence.Should().ContainInOrder("SwitchToBackgroundThread", "GetProjectKeyAndBranchAsync");
        }

        [TestMethod]
        public void UpdateAll_CriticalExpression_NotHandled()
        {
            var queryInfo = new Mock<IServerQueryInfoProvider>();
            queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
                .Throws(new StackOverflowException("thrown in a test"));

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(queryInfo.Object, logger: logger);

            Func<Task> operation = testSubject.UpdateAllServerSuppressionsAsync;
            operation.Should().Throw<StackOverflowException>().And.Message.Should().Be("thrown in a test");

            logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
        }

        [TestMethod]
        public void UpdateAll_NonCriticalExpression_IsSuppressed()
        {
            var queryInfo = new Mock<IServerQueryInfoProvider>();
            queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
                .Throws(new InvalidOperationException("thrown in a test"));

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(queryInfo.Object, logger: logger);

            Func<Task> operation = testSubject.UpdateAllServerSuppressionsAsync;
            operation.Should().NotThrow();

            logger.AssertPartialOutputStringExists("thrown in a test");
        }

        [TestMethod]
        public void UpdateAll_OperationCancelledException_CancellationMessageLogged()
        {
            var queryInfo = new Mock<IServerQueryInfoProvider>();
            queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
                .Throws(new OperationCanceledException("thrown in a test"));

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(queryInfo.Object, logger: logger);

            Func<Task> operation = testSubject.UpdateAllServerSuppressionsAsync;
            operation.Should().NotThrow();

            logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
            logger.AssertOutputStringExists(Resources.Suppressions_FetchOperationCancelled);
        }

        [TestMethod]
        public async Task UpdateAll_CallInProgress_CallIsCancelled()
        {
            var testTimeout = GetThreadedTestTimout();

            ManualResetEvent signalToBlockFirstCall = new ManualResetEvent(false);
            ManualResetEvent signalToBlockSecondCall = new ManualResetEvent(false);

            bool isFirstCall = true;

            var server = new Mock<ISonarQubeService>();
            var writer = new Mock<IServerIssuesStoreWriter>();

            var queryInfo = new Mock<IServerQueryInfoProvider>();
            queryInfo.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(("projectKey", "branch"))
                .Callback<CancellationToken>((token) =>
                {
                    if (isFirstCall)
                    {
                        Log("[1] In first call");
                        isFirstCall = false;

                        Log("[1] Unblocking second call...");
                        signalToBlockSecondCall.Set();

                        Log("[1] Waiting to be unblocked...");
                        signalToBlockFirstCall.WaitOne(testTimeout);

                        Log("[1] First call finished");
                    }
                    else
                    {
                        Log("[2] In second call");

                        Log("[2] Unblocking the first call...");
                        signalToBlockFirstCall.Set();
                        Log("[2] Second call finished");
                    }
                });

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(queryInfo.Object,
                server.Object,
                writer.Object,
                logger:logger,
                // Note: need a real thread-handling implementation here as the test
                // needs multiple threads.
                threadHandling: ThreadHandling.Instance);

            // 1. First call - don't wait for it to finish, since it should be blocked
            testSubject.UpdateAllServerSuppressionsAsync().Forget();

            Log("[main test] Waiting to be unblocked...");
            signalToBlockSecondCall.WaitOne(testTimeout)
                .Should().BeTrue(); // not expecting the test to timeout

            // 2. Second call - "await" means we're waiting for it to run to completion
            await testSubject.UpdateAllServerSuppressionsAsync();

            queryInfo.Invocations.Should().HaveCount(2);
            var call1Token = (CancellationToken)queryInfo.Invocations[0].Arguments[0];
            var call2Token = (CancellationToken)queryInfo.Invocations[1].Arguments[0];

            call1Token.IsCancellationRequested.Should().BeTrue();
            call2Token.IsCancellationRequested.Should().BeFalse();

            logger.AssertOutputStringExists(Resources.Suppressions_FetchOperationCancelled);

            // If cancellation worked then we should only have made one call to each
            // of the SonarQubeService and store writer
            server.Invocations.Should().HaveCount(1);
            writer.Invocations.Should().HaveCount(1);

            void Log(string message)
            {
                Console.WriteLine($"[Thread {System.Threading.Thread.CurrentThread.ManagedThreadId}] {message}");
            }
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task UpdateSuppressedIssues_EmptyIssues_NoChangesToTheStoreAndNoServerCalls(bool isResolved)
        {
            var queryInfo = CreateQueryInfoProvider("proj1", "branch1");
            var storeWriter = new Mock<IServerIssuesStoreWriter>();
            var server = new Mock<ISonarQubeService>();

            var testSubject = CreateTestSubject(queryInfo.Object, server.Object, storeWriter.Object);

            await testSubject.UpdateSuppressedIssuesAsync(isResolved, Array.Empty<string>(), CancellationToken.None);

            queryInfo.Invocations.Count.Should().Be(0);
            storeWriter.Invocations.Count.Should().Be(0);
            server.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task UpdateSuppressedIssues_AllIssuesAreFoundInStore_NoServerCalls(bool isResolved)
        {
            var queryInfo = CreateQueryInfoProvider("proj1", "branch1");
            var storeWriter = CreateIssuesStore(CreateIssue("issue1"), CreateIssue("issue2"));
            var server = new Mock<ISonarQubeService>();

            var testSubject = CreateTestSubject(queryInfo.Object, server.Object, storeWriter.Object);

            await testSubject.UpdateSuppressedIssuesAsync(isResolved, new[]{"issue1", "issue2"}, CancellationToken.None);

            storeWriter.Verify(x=> x.Get(), Times.Once);
            storeWriter.Verify(x=> x.UpdateIssues(isResolved, new[] { "issue1", "issue2" }), Times.Once);
            storeWriter.VerifyNoOtherCalls();

            server.Invocations.Count.Should().Be(0);
            queryInfo.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task UpdateSuppressedIssues_IssuesAreNotFoundInStore_IssuesAreNotSuppressed_NoServerCalls()
        {
            var queryInfo = CreateQueryInfoProvider("proj1", "branch1");
            var storeWriter = CreateIssuesStore(CreateIssue("issue1"), CreateIssue("issue3"));
            var server = new Mock<ISonarQubeService>();

            var testSubject = CreateTestSubject(queryInfo.Object, server.Object, storeWriter.Object);

            await testSubject.UpdateSuppressedIssuesAsync(false, new[] { "issue1", "issue2", "issue3" }, CancellationToken.None);

            storeWriter.Verify(x => x.Get(), Times.Once);
            storeWriter.Verify(x => x.UpdateIssues(false, new[] { "issue1", "issue2", "issue3" }), Times.Once);
            storeWriter.VerifyNoOtherCalls();

            // the missing issue is not suppressed, so we will not fetch it
            server.Invocations.Count.Should().Be(0);
            queryInfo.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        public async Task UpdateSuppressedIssues_IssuesAreNotFoundInStore_IssuesAreSuppressed_IssuesFetched()
        {
            var queryInfo = CreateQueryInfoProvider("proj1", "branch1");
            var storeWriter = CreateIssuesStore(CreateIssue("issue1"));
            var expectedFetchedIssues = new[] {CreateIssue("issue2"), CreateIssue("issue3")};

            var server = CreateSonarQubeService(
                "proj1",
                "branch1",
                new[] {"issue2", "issue3"},
                expectedFetchedIssues);

            var testSubject = CreateTestSubject(queryInfo.Object, server.Object, storeWriter.Object);

            await testSubject.UpdateSuppressedIssuesAsync(true, new[] { "issue1", "issue2", "issue3" }, CancellationToken.None);

            // the missing issues are suppressed, so we need to fetch them and add them to the store
            server.Verify(x => x.GetSuppressedIssuesAsync(
                    "proj1",
                    "branch1",
                    new[] { "issue2", "issue3" },
                    CancellationToken.None),
                Times.Once);

            storeWriter.Verify(x => x.Get(), Times.Once);
            storeWriter.Verify(x => x.AddIssues(expectedFetchedIssues, false), Times.Once);
            storeWriter.Verify(x => x.UpdateIssues(true, new[] { "issue1", "issue2", "issue3" }), Times.Once);
            storeWriter.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void UpdateSuppressedIssues_CriticalExpression_NotHandled()
        {
            var storeWriter = new Mock<IServerIssuesStoreWriter>();
            storeWriter.Setup(x => x.Get())
                .Throws(new StackOverflowException("thrown in a test"));

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(writer: storeWriter.Object, logger: logger);

            Func<Task> operation = () => testSubject.UpdateSuppressedIssuesAsync(true, new[]{"issue1"}, CancellationToken.None);
            operation.Should().Throw<StackOverflowException>().And.Message.Should().Be("thrown in a test");

            logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
        }

        [TestMethod]
        public void UpdateSuppressedIssues_NonCriticalExpression_IsSuppressed()
        {
            var storeWriter = new Mock<IServerIssuesStoreWriter>();
            storeWriter.Setup(x => x.Get())
                .Throws(new InvalidOperationException("thrown in a test"));

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(writer: storeWriter.Object, logger: logger);

            Func<Task> operation = () => testSubject.UpdateSuppressedIssuesAsync(true, new[] { "issue1" }, CancellationToken.None);
            operation.Should().NotThrow();

            logger.AssertPartialOutputStringExists("thrown in a test");
        }

        [TestMethod]
        public void UpdateSuppressedIssues_OperationCancelledException_CancellationMessageLogged()
        {
            var storeWriter = new Mock<IServerIssuesStoreWriter>();
            storeWriter.Setup(x => x.Get())
                .Throws(new OperationCanceledException("thrown in a test"));

            var logger = new TestLogger(logToConsole: true);

            var testSubject = CreateTestSubject(writer: storeWriter.Object, logger: logger);

            Func<Task> operation = () => testSubject.UpdateSuppressedIssuesAsync(true, new[] { "issue1" }, CancellationToken.None);
            operation.Should().NotThrow();

            logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
            logger.AssertOutputStringExists(Resources.Suppressions_UpdateOperationCancelled);
        }

        private static SuppressionIssueStoreUpdater CreateTestSubject(IServerQueryInfoProvider queryInfo = null,
            ISonarQubeService server = null,
            IServerIssuesStoreWriter writer = null,
            ILogger logger = null,
            IThreadHandling threadHandling = null)
        {
            writer ??= Mock.Of<IServerIssuesStoreWriter>();
            server ??= Mock.Of<ISonarQubeService>();
            queryInfo ??= Mock.Of<IServerQueryInfoProvider>();
            logger ??= new TestLogger(logToConsole: true);
            threadHandling ??= new NoOpThreadHandler();

            return new SuppressionIssueStoreUpdater(server, queryInfo, writer, logger, threadHandling);
        }

        private static Mock<IServerQueryInfoProvider> CreateQueryInfoProvider(string projectKey, string branchName)
        {
            var mock = new Mock<IServerQueryInfoProvider>();
            mock.Setup(x => x.GetProjectKeyAndBranchAsync(It.IsAny<CancellationToken>())).ReturnsAsync((projectKey, branchName));
            return mock;
        }

        private static Mock<ISonarQubeService> CreateSonarQubeService(string projectKey, string branchName, string[] issueKeys, params SonarQubeIssue[] issuesToReturn)
        {
            var mock = new Mock<ISonarQubeService>();
            mock.Setup(x => x.GetSuppressedIssuesAsync(projectKey, branchName, issueKeys, It.IsAny<CancellationToken>()))
                .ReturnsAsync(issuesToReturn);

            return mock;
        }

        private static SonarQubeIssue CreateIssue(string issueKey) =>
            new SonarQubeIssue(issueKey,
                null, null, null, null, null, true,
                SonarQubeIssueSeverity.Info, DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue, null, null);

        private static TimeSpan GetThreadedTestTimout()
            // This test uses a number of manual signals to control the order of execution.
            // We want a longer timeout when debugging.
            => System.Diagnostics.Debugger.IsAttached ?
                TimeSpan.FromMinutes(2) : TimeSpan.FromMilliseconds(200);

        private static Mock<IServerIssuesStoreWriter> CreateIssuesStore(params SonarQubeIssue[] issuesInStore)
        {
            var store = new Mock<IServerIssuesStoreWriter>();

            store.Setup(x => x.Get()).Returns(issuesInStore);

            return store;
        }
    }
}
