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

using NSubstitute.ExceptionExtensions;
using NSubstitute.ReceivedExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests.Initialization;

[TestClass]
public class InitializationHelperTests
{
    private IAsyncLockFactory asyncLockFactory;
    private IThreadHandling threadHandling;
    private TestLogger testLogger;
    private InitializationHelper testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        threadHandling = Substitute.For<IThreadHandling>();
        threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>().Invoke());
        testLogger = new TestLogger();
        testSubject = new InitializationHelper(asyncLockFactory, threadHandling, testLogger);
    }

    [TestMethod]
    public async Task InitializeAsync_NoDependencies_Completes()
    {
        var initialization = Substitute.For<Func<IThreadHandling, Task>>();

        await testSubject.InitializeAsync(default, [], initialization);

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
        var initialization = Substitute.For<Func<IThreadHandling, Task>>();

        await testSubject.InitializeAsync(default, [dependency1, dependency2], initialization);
        await testSubject.InitializeAsync(default, [dependency1, dependency2], initialization);
        await testSubject.InitializeAsync(default, [dependency1, dependency2], initialization);

        Received.InOrder(() =>
        {
            var asyncLock = asyncLockFactory.Create();
            threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
            var acquireAsync = asyncLock.AcquireAsync().GetAwaiter().GetResult();
            dependency1.InitializeAsync();
            dependency2.InitializeAsync();
            initialization.Invoke(threadHandling);
            acquireAsync.Dispose();
        });
    }

    [TestMethod]
    public async Task InitializeAsync_CompletesOnlyOnceFromMultipleThreads()
    {
        var dependency1 = Substitute.For<IRequireInitialization>();
        var dependency2 = Substitute.For<IRequireInitialization>();
        var lockImplementation = new AsyncLock();
        asyncLockFactory.Create().AcquireAsync().Returns(_ => lockImplementation.AcquireAsync());
        var initialization = Substitute.For<Func<IThreadHandling, Task>>();

        await Task.WhenAll(
            Enumerable.Range(1, 20)
                .AsParallel()
                .Select(_ => testSubject.InitializeAsync(default, [dependency1, dependency2], initialization)));

        asyncLockFactory.Create().Received(Quantity.Within(2, 20)).AcquireAsync();
        initialization.ReceivedWithAnyArgs(1).Invoke(default);
        dependency1.Received(1).InitializeAsync();
        dependency2.Received(1).InitializeAsync();
    }

    [TestMethod]
    public void InitializeAsync_DependencyThrows_ThrowsAndExecutesOnlyOnce()
    {
        var dependency = Substitute.For<IRequireInitialization>();
        dependency.InitializeAsync().ThrowsAsync(new InvalidOperationException("My Failed Dependency"));
        var initialization = Substitute.For<Func<IThreadHandling, Task>>();

        var act = () => testSubject.InitializeAsync("Owner 1", [dependency], initialization);
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();

        dependency.Received(1).InitializeAsync();
        initialization.DidNotReceiveWithAnyArgs().Invoke(default);
        testLogger.OutputStrings.Last().Should().ContainAll("Owner 1", "My Failed Dependency");
    }

    [TestMethod]
    public void InitializeAsync_InitializationThrows_ThrowsAndExecutesOnlyOnce()
    {
        var initialization = Substitute.For<Func<IThreadHandling, Task>>();
        initialization.Invoke(threadHandling).ThrowsAsync(new InvalidOperationException("My Failed Operation"));

        var act = () => testSubject.InitializeAsync("Owner 1", [], initialization);
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();
        act.Should().ThrowAsync<InvalidOperationException>();

        initialization.ReceivedWithAnyArgs(1).Invoke(default);
        testLogger.OutputStrings.Last().Should().ContainAll("Owner 1", "My Failed Operation");
    }
}
