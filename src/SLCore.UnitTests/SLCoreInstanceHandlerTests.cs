/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Threading.Tasks;
using NSubstitute.ExceptionExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.SLCore.State;

namespace SonarLint.VisualStudio.SLCore.UnitTests;

[TestClass]
public class SLCoreInstanceHandlerTests
{
    [TestMethod]
    public void MefCtor_CheckIsExported()
    {
        MefTestHelpers.CheckTypeCanBeImported<SLCoreInstanceHandler, ISLCoreInstanceHandler>(
            MefTestHelpers.CreateExport<ISLCoreInstanceFactory>(),
            MefTestHelpers.CreateExport<IAliveConnectionTracker>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ILogger>());
    }

    [TestMethod]
    public void MefCtor_CheckIsSingleton()
    {
        MefTestHelpers.CheckIsSingletonMefComponent<SLCoreInstanceHandler>();
    }
    
    [TestMethod]
    public void StartInstance_UpdatesCounterAndInitializesOnABackgroundThread()
    {
        var slCoreHandler = CreateTestSubject(out var factory, out var threadHandling, out var logger, out _, out _);
        SetUpHandleFactory(factory, out var handle, out _);

        var task = slCoreHandler.StartInstanceAsync();

        slCoreHandler.CurrentStartNumber.Should().Be(1);
        slCoreHandler.currentInstanceHandle.Should().BeSameAs(handle);
        logger.AssertOutputStrings(SLCoreStrings.SLCoreHandler_CreatingInstance, SLCoreStrings.SLCoreHandler_StartingInstance);
        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            factory.CreateInstance();
            handle.InitializeAsync();
            _ = handle.ShutdownTask;
        });
    }
    
    [TestMethod]
    public void StartInstance_AlreadyStarted_Throws()
    {
        var slCoreHandler = CreateTestSubject(out _, out _, out _, out _, out _);
        slCoreHandler.currentInstanceHandle = Substitute.For<ISLCoreInstanceHandle>();
        var act = () => slCoreHandler.StartInstanceAsync();

        act.Should().Throw<InvalidOperationException>().WithMessage(SLCoreStrings.SLCoreHandler_InstanceAlreadyRunning);

        slCoreHandler.CurrentStartNumber.Should().Be(0);
    }
    
    [TestMethod]
    public void StartInstance_InstanceCreationFailed_LogsAndExits()
    {
        var slCoreHandler = CreateTestSubject(out var factory, out _, out var logger, out _, out _);
        factory.CreateInstance().Throws(new Exception());

        var task = slCoreHandler.StartInstanceAsync();

        slCoreHandler.CurrentStartNumber.Should().Be(1);
        slCoreHandler.currentInstanceHandle.Should().BeNull();
        logger.AssertOutputStrings(SLCoreStrings.SLCoreHandler_CreatingInstance, SLCoreStrings.SLCoreHandler_CreatingInstanceError);
    }
    
    [TestMethod]
    public async Task StartInstance_InstanceDies_RaisesEventAndResets()
    {
        var slCoreHandler = CreateTestSubject(out var factory, out var threadHandling, out var logger, out var activeConfigScopeTracker, out _);
        SetUpHandleFactory(factory, out var handle, out var handleLifetimeTaskSource);

        var task = slCoreHandler.StartInstanceAsync();
        handleLifetimeTaskSource.SetResult(true);
        await task;

        slCoreHandler.CurrentStartNumber.Should().Be(1);
        slCoreHandler.currentInstanceHandle.Should().BeNull();
        logger.AssertOutputStrings(SLCoreStrings.SLCoreHandler_CreatingInstance, SLCoreStrings.SLCoreHandler_StartingInstance, SLCoreStrings.SLCoreHandler_InstanceDied);
        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            factory.CreateInstance();
            handle.InitializeAsync();
            _ = handle.ShutdownTask;
            handle.Dispose();
            activeConfigScopeTracker.Reset();
        });
    }
    
    [TestMethod]
    public async Task StartInstance_PreviousInstanceIsDead_AllowsToStartAgain()
    {
        var slCoreHandler = CreateTestSubject(out var factory, out _, out var logger, out _, out _);
        SetUpHandleFactory(factory, out _, out var handleLifetimeTaskSource);

        var task1 = slCoreHandler.StartInstanceAsync();
        handleLifetimeTaskSource.SetResult(true);
        await task1;
        SetUpHandleFactory(factory, out var newHandle, out _);
        var task2 = slCoreHandler.StartInstanceAsync();

        slCoreHandler.CurrentStartNumber.Should().Be(2);
        slCoreHandler.currentInstanceHandle.Should().BeSameAs(newHandle);
        logger.AssertOutputStrings(SLCoreStrings.SLCoreHandler_CreatingInstance,
            SLCoreStrings.SLCoreHandler_StartingInstance,
            SLCoreStrings.SLCoreHandler_InstanceDied,
            SLCoreStrings.SLCoreHandler_CreatingInstance,
            SLCoreStrings.SLCoreHandler_StartingInstance);
    }
    
    [TestMethod]
    public void StartInstance_InstanceInitializationThrows_RaisesEventAndResets()
    {
        var slCoreHandler = CreateTestSubject(out var factory, out var threadHandling, out var logger, out var activeConfigScopeTracker, out _);
        SetUpHandleFactory(factory, out var handle, out _);
        handle.InitializeAsync().Returns(Task.FromException(new Exception()));

        var task = slCoreHandler.StartInstanceAsync();

        slCoreHandler.CurrentStartNumber.Should().Be(1);
        slCoreHandler.currentInstanceHandle.Should().BeNull();
        logger.AssertOutputStrings(SLCoreStrings.SLCoreHandler_CreatingInstance,
            SLCoreStrings.SLCoreHandler_StartingInstance,
            SLCoreStrings.SLCoreHandler_StartingInstanceError,
            SLCoreStrings.SLCoreHandler_InstanceDied);
        Received.InOrder(() =>
        {
            threadHandling.ThrowIfOnUIThread();
            factory.CreateInstance();
            handle.InitializeAsync();
            handle.Dispose();
            activeConfigScopeTracker.Reset();
        });
    }
    
    [TestMethod]
    public async Task Dispose_DisposesLatestHandle()
    {
        var slCoreHandler = CreateTestSubject(out var factory, out _, out var logger, out var scopeTracker, out var connectionTracker);
        SetUpHandleFactory(factory, out var handle, out var handleLifetimeTaskSource);
        handle.When(slCoreHandle => slCoreHandle.Dispose()).Do(_ => handleLifetimeTaskSource.SetResult(true));
        var task = slCoreHandler.StartInstanceAsync();

        slCoreHandler.Dispose();
        await task;

        logger.AssertOutputStrings(SLCoreStrings.SLCoreHandler_CreatingInstance, SLCoreStrings.SLCoreHandler_StartingInstance, SLCoreStrings.SLCoreHandler_InstanceDied);
        Received.InOrder(() =>
        {
            handle.Dispose();
            connectionTracker.Dispose();
            scopeTracker.Dispose();
        });
    }
    
    [TestMethod]
    public async Task Dispose_PreventsStartingNewInstance()
    {
        var slCoreHandler = CreateTestSubject(out _, out _, out _, out _, out _);

        slCoreHandler.Dispose();
        var act = async () => await slCoreHandler.StartInstanceAsync();

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    private void SetUpHandleFactory(ISLCoreInstanceFactory instanceFactory, out ISLCoreInstanceHandle instanceHandle, out TaskCompletionSource<bool> handleLifetimeTaskSource)
    {
        handleLifetimeTaskSource = new TaskCompletionSource<bool>();
        instanceHandle = Substitute.For<ISLCoreInstanceHandle>();
        instanceHandle.ShutdownTask.Returns(handleLifetimeTaskSource.Task);
        instanceFactory.CreateInstance().Returns(instanceHandle);
    }

    private SLCoreInstanceHandler CreateTestSubject(out ISLCoreInstanceFactory slCoreInstanceFactory, 
        out IThreadHandling threadHandling,
        out TestLogger logger,
        out IActiveConfigScopeTracker activeConfigScopeTracker,
        out IAliveConnectionTracker aliveConnectionTracker)
    {
        slCoreInstanceFactory = Substitute.For<ISLCoreInstanceFactory>();
        aliveConnectionTracker = Substitute.For<IAliveConnectionTracker>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        threadHandling = Substitute.For<IThreadHandling>();
        logger = new TestLogger();
        return new SLCoreInstanceHandler(slCoreInstanceFactory,
            aliveConnectionTracker,
            activeConfigScopeTracker,
            threadHandling,
            logger);
    }
}
