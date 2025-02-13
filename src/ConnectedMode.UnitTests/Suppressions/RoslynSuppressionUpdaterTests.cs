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
public class RoslynSuppressionUpdaterTests
{
    private ICancellableActionRunner actionRunner;
    private ILogger logger;
    private IServerQueryInfoProvider queryInfo;
    private ISonarQubeService server;
    private RoslynSuppressionUpdater testSubject;
    private IThreadHandling threadHandling;
    private readonly EventHandler<SuppressionsEventArgs> suppressedIssuesReloaded = Substitute.For<EventHandler<SuppressionsEventArgs>>();
    private readonly EventHandler<SuppressionsUpdateEventArgs> suppressedIssuesUpdated = Substitute.For<EventHandler<SuppressionsUpdateEventArgs>>();

    [TestInitialize]
    public void TestInitialize()
    {
        server = Substitute.For<ISonarQubeService>();
        queryInfo = Substitute.For<IServerQueryInfoProvider>();
        logger = Substitute.For<ILogger>();
        logger.ForContext(Arg.Any<string[]>()).Returns(logger);
        threadHandling = new NoOpThreadHandler();
        actionRunner = new SynchronizedCancellableActionRunner(logger);
        testSubject = CreateTestSubject(actionRunner, threadHandling);
        testSubject.SuppressedIssuesReloaded += suppressedIssuesReloaded;
        testSubject.SuppressedIssuesUpdated += suppressedIssuesUpdated;
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<RoslynSuppressionUpdater, IRoslynSuppressionUpdater>(
            MefTestHelpers.CreateExport<ISonarQubeService>(),
            MefTestHelpers.CreateExport<IServerQueryInfoProvider>(),
            MefTestHelpers.CreateExport<IServerIssuesStoreWriter>(),
            MefTestHelpers.CreateExport<ICancellableActionRunner>(),
            MefTestHelpers.CreateExport<ILogger>());

    [TestMethod]
    public void Ctor_SetsLoggerContext() => logger.Received(1).ForContext(nameof(RoslynSuppressionUpdater));

    [TestMethod]
    [DataRow(null, null)]
    [DataRow(null, "branch")]
    [DataRow("projectKey", null)]
    public async Task UpdateAll_MissingServerQueryInfo_DoesNotInvokeEvent(string projectKey, string branchName)
    {
        // Server query info is not available -> give up
        MockQueryInfoProvider(projectKey, branchName);

        await testSubject.UpdateAllServerSuppressionsAsync();

        await queryInfo.Received(1).GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>());
        server.ReceivedCalls().Should().HaveCount(0);
        VerifySuppressedIssuesReloadedNotInvoked();
    }

    [TestMethod]
    public async Task UpdateAll_HasServerQueryInfo_ServerQueriedAndEventInvoked()
    {
        // Happy path - fetch and update
        MockQueryInfoProvider("project", "branch");
        var issue = CreateIssue("issue1");
        MockSonarQubeService("project", "branch", null, issue);

        await testSubject.UpdateAllServerSuppressionsAsync();

        await queryInfo.Received(1).GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>());
        await server.Received(1).GetSuppressedIssuesAsync("project", "branch", null, Arg.Any<CancellationToken>());
        server.ReceivedCalls().Should().HaveCount(1);
        VerifySuppressedIssuesReloadedInvoked(expectedAllSuppressedIssues: [issue]);
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
        AssertMessageArgsDoesNotExist("thrown in a test");
    }

    [TestMethod]
    public void UpdateAll_NonCriticalExpression_IsSuppressed()
    {
        queryInfo.When(x => x.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>())).Throw(new InvalidOperationException("thrown in a test"));

        var operation = testSubject.UpdateAllServerSuppressionsAsync;

        operation.Should().NotThrow();
        AssertMessageArgsExists("thrown in a test");
    }

    [TestMethod]
    public void UpdateAll_OperationCancelledException_CancellationMessageLogged()
    {
        queryInfo.When(x => x.GetProjectKeyAndBranchAsync(Arg.Any<CancellationToken>())).Throw(new OperationCanceledException("thrown in a test"));

        var operation = testSubject.UpdateAllServerSuppressionsAsync;

        operation.Should().NotThrow();
        AssertMessageArgsDoesNotExist("thrown in a test");
        AssertMessageExists(Resources.Suppressions_FetchOperationCancelled);
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

        AssertMessageExists(Resources.Suppressions_FetchOperationCancelled);

        // If cancellation worked then we should only have made one call to each
        // of the SonarQubeService and store writer
        server.ReceivedCalls().Should().HaveCount(1);
        VerifySuppressedIssuesReloadedInvoked([]);

        static void Log(string message) => Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] {message}");
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void UpdateSuppressedIssues_EmptyIssues_NoEventInvokedAndNoServerCalls(bool isResolved)
    {
        testSubject.UpdateSuppressedIssues(isResolved, []);

        VerifySuppressedIssuesUpdatedNotInvoked();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void UpdateSuppressedIssues_IssuesFetched(bool isResolved)
    {
        string[] issueKeys = ["issue1", "issue2"];

        testSubject.UpdateSuppressedIssues(isResolved, issueKeys);

        VerifySuppressedIssuesUpdatedInvoked(issueKeys, isResolved);
    }

    private RoslynSuppressionUpdater CreateTestSubject(ICancellableActionRunner mockedActionRunner, IThreadHandling mockedThreadHandling) =>
        new(server, queryInfo, mockedActionRunner, logger, mockedThreadHandling);

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

    private void VerifySuppressedIssuesReloadedNotInvoked() => suppressedIssuesReloaded.DidNotReceiveWithAnyArgs().Invoke(testSubject, Arg.Any<SuppressionsEventArgs>());

    private void VerifySuppressedIssuesReloadedInvoked(IEnumerable<SonarQubeIssue> expectedAllSuppressedIssues) =>
        suppressedIssuesReloaded.Received(1)
            .Invoke(testSubject, Arg.Is<SuppressionsEventArgs>(x => x.SuppressedIssues.SequenceEqual(expectedAllSuppressedIssues)));

    private void VerifySuppressedIssuesUpdatedNotInvoked() => suppressedIssuesUpdated.DidNotReceiveWithAnyArgs().Invoke(testSubject, Arg.Any<SuppressionsUpdateEventArgs>());

    private void VerifySuppressedIssuesUpdatedInvoked(IEnumerable<string> expectedSuppressedIssuesKeys, bool isResolved) =>
        suppressedIssuesUpdated.Received(1)
            .Invoke(testSubject, Arg.Is<SuppressionsUpdateEventArgs>(x => x.SuppressedIssueKeys.SequenceEqual(expectedSuppressedIssuesKeys) && x.IsResolved == isResolved));

    private void AssertMessageArgsDoesNotExist(params object[] args) => logger.DidNotReceive().WriteLine(Arg.Any<string>(), Arg.Is<object[]>(x => x.SequenceEqual(args)));

    private void AssertMessageArgsExists(params object[] args) => logger.Received(1).WriteLine(Arg.Any<string>(), Arg.Is<object[]>(x => x.SequenceEqual(args)));

    private void AssertMessageExists(string message) => logger.Received(1).WriteLine(Arg.Is<string>(x => x == message), Arg.Any<object[]>());
}
