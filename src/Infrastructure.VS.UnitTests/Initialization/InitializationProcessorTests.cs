/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Initialization;

[TestClass]
public class InitializationProcessorTests
{
    private IAsyncLockFactory asyncLockFactory;
    private IThreadHandling threadHandling;
    private TestLogger testLogger;
    private InitializationProcessorFactory testSubjectFactory;
    private Func<IThreadHandling, Task> initialization;

    [TestInitialize]
    public void TestInitialize()
    {
        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>().Invoke());
        testLogger = new TestLogger();
        initialization = Substitute.For<Func<IThreadHandling, Task>>();
        testSubjectFactory = new InitializationProcessorFactory(asyncLockFactory, threadHandling, testLogger);
    }

    [TestMethod]
    public void IsFinalized_NotStarted_ReturnsFalse() => testSubjectFactory.Create<InitializationProcessorTests>([], _ => Task.CompletedTask).IsFinalized.Should().BeFalse();

    [TestMethod]
    public async Task IsFinalized_StartedButNotFinished_ReturnsFalse()
    {
        var barrier = new TaskCompletionSource<byte>();
        initialization.Invoke(threadHandling).Returns(barrier.Task);
        var testSubject = testSubjectFactory.Create<InitializationProcessorTests>([], initialization);

        var initializationProcessTask = testSubject.InitializeAsync();
        testSubject.IsFinalized.Should().BeFalse();

        barrier.SetResult(1);
        await initializationProcessTask;
        testSubject.IsFinalized.Should().BeTrue();
    }

    [TestMethod]
    public async Task InitializeAsync_NoDependencies_Completes()
    {
        var testSubject = testSubjectFactory.Create<InitializationProcessorTests>([], initialization);

        await testSubject.InitializeAsync();

        testSubject.IsFinalized.Should().BeTrue();
        Received.InOrder(() =>
        {
            var asyncLock = asyncLockFactory.Create();
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            var acquireAsync = asyncLock.AcquireAsync().GetAwaiter().GetResult();
            initialization.Invoke(threadHandling);
            acquireAsync.Dispose();
        });
    }

    [TestMethod]
    public async Task InitializeAsync_CompletesOnlyOnce()
    {
        var dependency1 = Substitute.For<IRequireInitialization>();
        var dependency2 = Substitute.For<IRequireInitialization>();
        var testSubject = testSubjectFactory.Create<InitializationProcessorTests>([dependency1, dependency2], initialization);

        await testSubject.InitializeAsync();
        await testSubject.InitializeAsync();
        await testSubject.InitializeAsync();

        testSubject.IsFinalized.Should().BeTrue();
        Received.InOrder(() =>
        {
            var asyncLock = asyncLockFactory.Create();
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            var acquireAsync = asyncLock.AcquireAsync().GetAwaiter().GetResult();
            dependency1.InitializationProcessor.InitializeAsync();
            dependency2.InitializationProcessor.InitializeAsync();
            initialization.Invoke(threadHandling);
            acquireAsync.Dispose();
        });
    }

    [TestMethod]
    public async Task InitializeAsync_CompletesOnlyOnceFromMultipleThreads()
    {
        var dependency1 = Substitute.For<IRequireInitialization>();
        var dependency2 = Substitute.For<IRequireInitialization>();
        var manualResetEvent = new ManualResetEvent(false);
        initialization.When(x => x.Invoke(Arg.Any<IThreadHandling>())).Do(x =>
        {
            // block initialization to increase the chance of multiple threads trying to initialize at the same time
            manualResetEvent.WaitOne(50);
        });
        var lockImplementation = new AsyncLock();
        asyncLockFactory.Create().AcquireAsync().Returns(_ => lockImplementation.AcquireAsync());
        var testSubject = testSubjectFactory.Create<InitializationProcessorTests>([dependency1, dependency2], initialization);

        var tasks = Enumerable.Range(1, 20).AsParallel().Select(_ => testSubject.InitializeAsync()).ToArray();
        manualResetEvent.Set();
        await Task.WhenAll(tasks);

        testSubject.IsFinalized.Should().BeTrue();
        asyncLockFactory.Create().Received(Quantity.Within(2, 20)).AcquireAsync();
        initialization.ReceivedWithAnyArgs(1).Invoke(default);
        dependency1.InitializationProcessor.Received(1).InitializeAsync();
        dependency2.InitializationProcessor.Received(1).InitializeAsync();
    }

    [TestMethod]
    public void InitializeAsync_DependencyThrows_ThrowsAndExecutesOnlyOnce()
    {
        var dependency = Substitute.For<IRequireInitialization>();
        dependency.InitializationProcessor.InitializeAsync().ThrowsAsync(new InvalidOperationException("My Failed Dependency"));
        var testSubject = testSubjectFactory.Create<InitializationProcessorTests>([dependency], initialization);

        var act = () => testSubject.InitializeAsync();
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();

        testSubject.IsFinalized.Should().BeTrue();
        dependency.InitializationProcessor.Received(1).InitializeAsync();
        initialization.DidNotReceiveWithAnyArgs().Invoke(default);
        testLogger.OutputStrings.Last().Should().ContainAll(nameof(InitializationProcessorTests), "My Failed Dependency");
    }

    [TestMethod]
    public void InitializeAsync_InitializationThrows_ThrowsAndExecutesOnlyOnce()
    {
        initialization.Invoke(threadHandling).ThrowsAsync(new InvalidOperationException("My Failed Operation"));
        var testSubject = testSubjectFactory.Create<InitializationProcessorTests>([], initialization);

        var act = () => testSubject.InitializeAsync();
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();

        testSubject.IsFinalized.Should().BeTrue();
        initialization.ReceivedWithAnyArgs(1).Invoke(default);
        testLogger.OutputStrings.Last().Should().ContainAll(nameof(InitializationProcessorTests), "My Failed Operation");
    }
}
