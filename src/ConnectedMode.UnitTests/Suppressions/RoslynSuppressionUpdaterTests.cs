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

using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.Helpers;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Suppressions;

[TestClass]
public class RoslynIRoslynSuppressionUpdaterTests
{
    private ICancellableActionRunner actionRunner;
    private TestLogger logger;
    private IServerQueryInfoProvider queryInfo;
    private ISonarQubeService server;
    private RoslynIRoslynSuppressionUpdater testSubject;
    private IThreadHandling threadHandling;
    private IServerIssuesStoreWriter writer;

    [TestInitialize]
    public void TestInitialize()
    {
        writer = Substitute.For<IServerIssuesStoreWriter>();
        server = Substitute.For<ISonarQubeService>();
        queryInfo = Substitute.For<IServerQueryInfoProvider>();
        logger = new TestLogger(true);
        threadHandling = new NoOpThreadHandler();
        actionRunner = new SynchronizedCancellableActionRunner(logger);
        testSubject = CreateTestSubject(actionRunner, threadHandling);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynIRoslynSuppressionUpdater, IRoslynSuppressionUpdater>(
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IServerQueryInfoProvider>(),
            MefTestHelpers.CreateExport<IServerIssuesStoreWriter>(),
            MefTestHelpers.CreateExport<ICancellableActionRunner>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    [DataRow(null, null)]
    [DataRow(null, "branch")]
    [DataRow("projectKey", null)]
    public async Task UpdateAll_MissingServerQueryInfo_ResetsStore(string projectKey, string branchName)
    {
        // Server query info is not available -> give up
        MockQueryInfoProvider(projectKey, branchName);

        await testSubject.UpdateAllServerSuppressionsAsync();

        await queryInfo.Received(1).GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>());
        server.ReceivedCalls().Should().HaveCount(0);
        writer.Received(1).Reset();
        writer.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateAll_HasServerQueryInfo_ServerQueriedAndStoreUpdated()
    {
        // Happy path - fetch and update
        MockQueryInfoProvider("project", "branch");
        var issue = CreateIssue("issue1");
        MockSonarQubeService("project", "branch", null, issue);

        await testSubject.UpdateAllServerSuppressionsAsync();

        await queryInfo.Received(1).GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>());
        await server.Received(1).GetSuppressedIssuesAsync("project", "branch", null, Arg.Any<CancellationToken>());
        writer.Received(1).AddIssues(Arg.Is<IEnumerable<SonarQubeIssue>>(x => VerifySonarQubeIssues(x, new[] { issue })), true);
        server.ReceivedCalls().Should().HaveCount(1);
        writer.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public async Task UpdateAll_RunOnBackgroundThreadInActionRunner()
    {
        var mockedActionRunner = CreateMockedActionRunner();
        var mockedThreadHandling = CreateMockedThreadHandling();
        var mockedTestSubject = CreateTestSubject(mockedActionRunner, mockedThreadHandling);

        await mockedTestSubject.UpdateAllServerSuppressionsAsync();

        Received.InOrder(() =>
        {
            mockedThreadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>());
            mockedActionRunner.RunAsync(Arg.Any<Func<CancellationToken, Task>>());
            queryInfo.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>());
        });
        queryInfo.ReceivedCalls().Should().HaveCount(1); // no other calls
    }

    [TestMethod]
    public void UpdateAll_CriticalExpression_NotHandled()
    {
        queryInfo.When(x => x.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>())).Throw(new StackOverflowException("thrown in a test"));

        var operation = testSubject.UpdateAllServerSuppressionsAsync;

        operation.Should().Throw<StackOverflowException>().And.Message.Should().Be("thrown in a test");
        logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
    }

    [TestMethod]
    public void UpdateAll_NonCriticalExpression_IsSuppressed()
    {
        queryInfo.When(x => x.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>())).Throw(new InvalidOperationException("thrown in a test"));

        var operation = testSubject.UpdateAllServerSuppressionsAsync;

        operation.Should().NotThrow();
        logger.AssertPartialOutputStringExists("thrown in a test");
    }

    [TestMethod]
    public void UpdateAll_OperationCancelledException_CancellationMessageLogged()
    {
        queryInfo.When(x => x.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>())).Throw(new OperationCanceledException("thrown in a test"));

        var operation = testSubject.UpdateAllServerSuppressionsAsync;

        operation.Should().NotThrow();
        logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
        logger.AssertOutputStringExists(Resources.Suppressions_FetchOperationCancelled);
    }

    [TestMethod]
    [Ignore] // Flaky. See https://github.com/SonarSource/sonarlint-visualstudio/issues/4307
    public async Task UpdateAll_CallInProgress_CallIsCancelled()
    {
        var testTimeout = GetThreadedTestTimeout();

        var signalToBlockFirstCall = new ManualResetEvent(false);
        var signalToBlockSecondCall = new ManualResetEvent(false);

        var isFirstCall = true;

        queryInfo.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>())
            .Returns(("projectKey", "branch"));
        queryInfo.When(x => x.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>()))
            .Do(x =>
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

        var mockedTestSubject = CreateTestSubject(
            // Note: need a real thread-handling implementation here as the test
            // needs multiple threads.
            actionRunner, ThreadHandling.Instance);

        // 1. First call - don't wait for it to finish, since it should be blocked
        mockedTestSubject.UpdateAllServerSuppressionsAsync().Forget();

        Log("[main test] Waiting to be unblocked...");
        signalToBlockSecondCall.WaitOne(testTimeout)
            .Should().BeTrue(); // not expecting the test to timeout

        // 2. Second call - "await" means we're waiting for it to run to completion
        await mockedTestSubject.UpdateAllServerSuppressionsAsync();

        queryInfo.ReceivedCalls().Should().HaveCount(2);
        var call1Token = (CancellationToken)queryInfo.ReceivedCalls().ToList()[0].GetArguments()[0];
        var call2Token = (CancellationToken)queryInfo.ReceivedCalls().ToList()[1].GetArguments()[0];

        call1Token.IsCancellationRequested.Should().BeTrue();
        call2Token.IsCancellationRequested.Should().BeFalse();

        logger.AssertOutputStringExists(Resources.Suppressions_FetchOperationCancelled);

        // If cancellation worked then we should only have made one call to each
        // of the SonarQubeService and store writer
        server.ReceivedCalls().Should().HaveCount(1);
        writer.ReceivedCalls().Should().HaveCount(1);

        static void Log(string message) => Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] {message}");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task UpdateSuppressedIssues_EmptyIssues_NoChangesToTheStoreAndNoServerCalls(bool isResolved)
    {
        MockQueryInfoProvider("proj1", "branch1");

        await testSubject.UpdateSuppressedIssuesAsync(isResolved, [], CancellationToken.None);

        queryInfo.ReceivedCalls().Count().Should().Be(0);
        writer.ReceivedCalls().Count().Should().Be(0);
        server.ReceivedCalls().Count().Should().Be(0);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task UpdateSuppressedIssues_AllIssuesAreFoundInStore_NoServerCalls(bool isResolved)
    {
        MockQueryInfoProvider("proj1", "branch1");
        MockIssuesStore(CreateIssue("issue1"), CreateIssue("issue2"));

        await testSubject.UpdateSuppressedIssuesAsync(isResolved, ["issue1", "issue2"], CancellationToken.None);

        writer.Received(1).Get();
        writer.Received(1).UpdateIssues(isResolved, Arg.Is<IEnumerable<string>>(actual => VerifyExistingIssues(actual, new[] { "issue1", "issue2" })));
        writer.ReceivedCalls().Should().HaveCount(2); // no other calls
        server.ReceivedCalls().Count().Should().Be(0);
        queryInfo.ReceivedCalls().Count().Should().Be(0);
    }

    [TestMethod]
    public async Task UpdateSuppressedIssues_IssuesAreNotFoundInStore_IssuesAreNotSuppressed_NoServerCalls()
    {
        MockQueryInfoProvider("proj1", "branch1");
        MockIssuesStore(CreateIssue("issue1"), CreateIssue("issue3"));

        await testSubject.UpdateSuppressedIssuesAsync(false, ["issue1", "issue2", "issue3"], CancellationToken.None);

        writer.Received(1).Get();
        writer.Received(1).UpdateIssues(false, Arg.Is<IEnumerable<string>>(x => VerifyExistingIssues(x, new[] { "issue1", "issue2", "issue3" })));
        writer.ReceivedCalls().Should().HaveCount(2);
        // the missing issue is not suppressed, so we will not fetch it
        server.ReceivedCalls().Count().Should().Be(0);
        queryInfo.ReceivedCalls().Count().Should().Be(0);
    }

    [TestMethod]
    public async Task UpdateSuppressedIssues_IssuesAreNotFoundInStore_IssuesAreSuppressed_IssuesFetched()
    {
        MockQueryInfoProvider("proj1", "branch1");
        MockIssuesStore(CreateIssue("issue1"));
        var expectedFetchedIssues = new[] { CreateIssue("issue2"), CreateIssue("issue3") };
        MockSonarQubeService(
            "proj1",
            "branch1",
            ["issue2", "issue3"],
            expectedFetchedIssues);

        await testSubject.UpdateSuppressedIssuesAsync(true, ["issue1", "issue2", "issue3"], CancellationToken.None);

        // the missing issues are suppressed, so we need to fetch them and add them to the store
        await server.Received(1).GetSuppressedIssuesAsync(
            "proj1",
            "branch1",
            Arg.Is<string[]>(x => VerifyExistingIssues(x, new[] { "issue2", "issue3" })),
            CancellationToken.None);
        writer.Received(1).Get();
        writer.Received(1).AddIssues(Arg.Is<IEnumerable<SonarQubeIssue>>(x => VerifySonarQubeIssues(x, expectedFetchedIssues)), false);
        writer.Received(1).UpdateIssues(true, Arg.Is<IEnumerable<string>>(x => VerifyExistingIssues(x, new[] { "issue1", "issue2", "issue3" })));
        writer.ReceivedCalls().Should().HaveCount(3);
    }

    [TestMethod]
    public void UpdateSuppressedIssues_CriticalExpression_NotHandled()
    {
        writer.When(x => x.Get()).Throw(new StackOverflowException("thrown in a test"));

        var operation = () => testSubject.UpdateSuppressedIssuesAsync(true, ["issue1"], CancellationToken.None);

        operation.Should().Throw<StackOverflowException>().And.Message.Should().Be("thrown in a test");
        logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
    }

    [TestMethod]
    public void UpdateSuppressedIssues_NonCriticalExpression_IsSuppressed()
    {
        writer.When(x => x.Get()).Throw(new InvalidOperationException("thrown in a test"));

        var operation = () => testSubject.UpdateSuppressedIssuesAsync(true, ["issue1"], CancellationToken.None);

        operation.Should().NotThrow();
        logger.AssertPartialOutputStringExists("thrown in a test");
    }

    [TestMethod]
    public void UpdateSuppressedIssues_OperationCancelledException_CancellationMessageLogged()
    {
        writer.When(x => x.Get()).Throw(new OperationCanceledException("thrown in a test"));

        var operation = () => testSubject.UpdateSuppressedIssuesAsync(true, ["issue1"], CancellationToken.None);

        operation.Should().NotThrow();
        logger.AssertPartialOutputStringDoesNotExist("thrown in a test");
        logger.AssertOutputStringExists(Resources.Suppressions_UpdateOperationCancelled);
    }

    private RoslynIRoslynSuppressionUpdater CreateTestSubject(ICancellableActionRunner mockedActionRunner, IThreadHandling mockedThreadHandling) =>
        new(server, queryInfo, writer, mockedActionRunner, logger, mockedThreadHandling);

    private void MockQueryInfoProvider(string projectKey, string branchName) => queryInfo.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>()).Returns((projectKey, branchName));

    private void MockSonarQubeService(
        string projectKey,
        string branchName,
        string[] issueKeys,
        params SonarQubeIssue[] issuesToReturn) =>
        server.GetSuppressedIssuesAsync(projectKey, branchName, Arg.Is<string[]>(x => issueKeys == null || x.SequenceEqual(issueKeys)), Arg.Any<CancellationToken>()).Returns(issuesToReturn);

    private static SonarQubeIssue CreateIssue(string issueKey) =>
        new(issueKey,
            null, null, null, null, null, true,
            SonarQubeIssueSeverity.Info, DateTimeOffset.MinValue,
            DateTimeOffset.MaxValue, null, null);

    private static TimeSpan GetThreadedTestTimeout()
        // This test uses a number of manual signals to control the order of execution.
        // We want a longer timeout when debugging.
        =>
            Debugger.IsAttached ? TimeSpan.FromMinutes(2) : TimeSpan.FromMilliseconds(200);

    private void MockIssuesStore(params SonarQubeIssue[] issuesInStore) => writer.Get().Returns(issuesInStore);

    private static bool VerifyExistingIssues(IEnumerable<string> actualIssues, IEnumerable<string> expectedIssues) => actualIssues.SequenceEqual(expectedIssues);

    private static bool VerifySonarQubeIssues(IEnumerable<SonarQubeIssue> actualIssues, SonarQubeIssue[] expectedIssues) => actualIssues.SequenceEqual(expectedIssues);

    private static IThreadHandling CreateMockedThreadHandling()
    {
        var mockedThreadHandling = Substitute.For<IThreadHandling>();
        mockedThreadHandling.When(x => x.RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>())).Do(x =>
        {
            var func = x.Arg<Func<Task<bool>>>();
            func();
        });
        return mockedThreadHandling;
    }

    private static ICancellableActionRunner CreateMockedActionRunner()
    {
        var mockedActionRunner = Substitute.For<ICancellableActionRunner>();
        mockedActionRunner.When(x => x.RunAsync(Arg.Any<Func<CancellationToken, Task>>()))
            .Do(callInfo =>
            {
                var func = callInfo.Arg<Func<CancellationToken, Task>>();
                func(new CancellationToken());
            });
        return mockedActionRunner;
    }
}
