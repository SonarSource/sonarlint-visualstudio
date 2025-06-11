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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests;

[TestClass]
public class BoundSolutionGitMonitorTests
{
    private IGitWorkspaceService gitWorkspaceService;
    private BoundSolutionGitMonitor.GitEventFactory factory;
    private IGitEvents gitEvents;
    private TestLogger logger;
    private IThreadHandling threadHandling;
    private IInitializationProcessorFactory initializationProcessorFactory;

    [TestInitialize]
    public void TestInitialize()
    {
        gitEvents = Substitute.For<IGitEvents>();
        gitWorkspaceService = Substitute.For<IGitWorkspaceService>();
        factory = Substitute.For<BoundSolutionGitMonitor.GitEventFactory>();
        factory.Invoke(Arg.Any<string>()).Returns(gitEvents);
        logger = Substitute.ForPartsOf<TestLogger>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<BoundSolutionGitMonitor, IBoundSolutionGitMonitor>(
            MefTestHelpers.CreateExport<IGitWorkspaceService>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() =>
        MefTestHelpers.CheckIsSingletonMefComponent<BoundSolutionGitMonitor>();

    [TestMethod]
    public void Initialize_NoRepo_FactoryNotCalledAndNoError()
    {
        SetUpGitWorkSpaceService(null);

        var testSubject = CreateAndInitializeTestSubject();

        gitWorkspaceService.Received(1).GetRepoRoot();
        factory.DidNotReceiveWithAnyArgs().Invoke(default);
    }

    [TestMethod]
    public void Initialize_ForwardsLowLevelEvent()
    {
        string repoPath = "some path";
        SetUpGitWorkSpaceService(repoPath);

        // first, check the factory is called
        BoundSolutionGitMonitor testSubject = CreateAndInitializeTestSubject();
        factory.Received().Invoke(repoPath);

        // second, register for then trigger an event
        int counter = 0;
        testSubject.HeadChanged += (o, e) => counter++;

        gitEvents.HeadChanged += Raise.Event();
        counter.Should().Be(1);
    }

    [TestMethod]
    public void Refresh_ChangesLowLevelMonitor()
    {
        string originalPath = "original path";
        string newPath = "new path";

        int counter = 0;

        SetUpGitWorkSpaceService(originalPath);

        var originalEventsMonitor = Substitute.For<IGitEvents>();
        var newEventsMonitor = Substitute.For<IGitEvents>();

       factory = path =>
        {
            if (path != originalPath && path != newPath)
            {
                throw new Exception("Test Error: Wrong path is passed to low level event monitor");
            }

            return path == originalPath ? originalEventsMonitor : newEventsMonitor;
        };

        BoundSolutionGitMonitor testSubject = CreateAndInitializeTestSubject();
        testSubject.HeadChanged += (o, e) => counter++;

        newEventsMonitor.HeadChanged += Raise.Event();
        counter.Should().Be(0);

        originalEventsMonitor.HeadChanged += Raise.Event();
        counter.Should().Be(1);

        gitWorkspaceService.GetRepoRoot().Returns(newPath);
        originalEventsMonitor.DidNotReceive().HeadChanged -= Arg.Any<EventHandler>();

        // Act
        testSubject.Refresh();

        // Old event handler should be unregistered
        originalEventsMonitor.Received(1).HeadChanged -= Arg.Any<EventHandler>();
        originalEventsMonitor.HeadChanged += Raise.Event();
        counter.Should().Be(1);

        newEventsMonitor.HeadChanged += Raise.Event();
        counter.Should().Be(2);
    }

    [TestMethod]
    public void Refresh_NotInitialized_DoesNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out _);

        testSubject.Refresh();

        gitWorkspaceService.DidNotReceive().GetRepoRoot();
        factory.DidNotReceiveWithAnyArgs().Invoke(default);
    }

    [TestMethod]
    public void Refresh_Disposed_Throws()
    {
        var testSubject = CreateAndInitializeTestSubject();
        testSubject.Dispose();

        var act = () => testSubject.Refresh();

        act.Should().Throw<ObjectDisposedException>();
    }

    [TestMethod]
    public void Dispose_UnregistersGitEventHandlerAndDisposesIGitEvents()
    {
        SetUpGitWorkSpaceService("any");

        var testSubject = CreateAndInitializeTestSubject();

        gitEvents.Received(1).HeadChanged += Arg.Any<EventHandler>();
        gitEvents.DidNotReceive().HeadChanged -= Arg.Any<EventHandler>();
        gitEvents.DidNotReceive().Dispose();

        // Act
        testSubject.Dispose();

        gitEvents.Received(1).HeadChanged += Arg.Any<EventHandler>();
        gitEvents.Received(1).HeadChanged -= Arg.Any<EventHandler>();
        gitEvents.Received(1).Dispose();
    }

    [TestMethod]
    public void OnHeadChanged_NonCriticalExceptionInHandler_IsSuppressed()
    {
        SetUpGitWorkSpaceService("any");
        var testSubject = CreateAndInitializeTestSubject();
        testSubject.HeadChanged += (sender, args) => throw new InvalidOperationException("thrown from a test");

        var op = () => gitEvents.HeadChanged += Raise.Event();

        op.Should().NotThrow();
    }

    [TestMethod]
    public void OnHeadChanged_CriticalExceptionInHandler_IsNotSuppressed()
    {
        SetUpGitWorkSpaceService("any");
        var testSubject = CreateAndInitializeTestSubject();
        testSubject.HeadChanged += (sender, args) => throw new StackOverflowException("thrown from a test");

        var op = () => gitEvents.HeadChanged += Raise.Event();

        op.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("thrown from a test");
    }

    [TestMethod]
    public void BoundSolutionGitMonitor_InitializesCorrectly()
    {
        var testSubject = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<BoundSolutionGitMonitor>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0), Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync();
            gitWorkspaceService.GetRepoRoot();
            factory.Invoke(Arg.Any<string>());
            testSubject.InitializationProcessor.InitializeAsync();
        });
    }

    [TestMethod]
    public void BoundSolutionGitMonitor_DelaysServiceCallsUntilInitialization()
    {
        var testSubject = CreateUninitializedTestSubject(out var tcs);

        gitWorkspaceService.DidNotReceive().GetRepoRoot();
        factory.DidNotReceiveWithAnyArgs().Invoke(default);

        tcs.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        gitWorkspaceService.Received(1).GetRepoRoot();
        factory.ReceivedWithAnyArgs(1).Invoke(default);
    }

    [TestMethod]
    public void BoundSolutionGitMonitor_DisposeBeforeInitialized_DisposeAndInitializeDoNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out var tcs);

        testSubject.Dispose();
        tcs.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        gitWorkspaceService.DidNotReceive().GetRepoRoot();
        factory.DidNotReceiveWithAnyArgs().Invoke(default);
    }


    private BoundSolutionGitMonitor CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<BoundSolutionGitMonitor>(threadHandling, logger, proc => MockableInitializationProcessor.ConfigureWithWait(proc, tcs));
        return new BoundSolutionGitMonitor(gitWorkspaceService, logger, initializationProcessorFactory, factory);
    }


    private BoundSolutionGitMonitor CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<BoundSolutionGitMonitor>(threadHandling, logger);
        var testSubject = new BoundSolutionGitMonitor(gitWorkspaceService, logger, initializationProcessorFactory, factory);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return testSubject;
    }

    private void SetUpGitWorkSpaceService(string repoPath)
    {
        gitWorkspaceService.GetRepoRoot().Returns(repoPath);
    }
}
