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

using NSubstitute.ReturnsExtensions;
using SonarLint.VisualStudio.CFamily;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Analysis;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily;

[TestClass]
public class ActiveCompilationDatabaseTrackerTests
{
    private const string DefaultConfigScope = "scope1";
    private const string CmakeJson = "cmake.json";
    private const string VcxJson = "vcx.json";
    private ICMakeCompilationDatabaseLocator cMakeCompilationDatabaseLocator;
    private IActiveVcxCompilationDatabase activeVcxCompilationDatabase;
    private IActiveConfigScopeTracker activeConfigScopeTracker;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private ICFamilyAnalysisConfigurationSLCoreService cFamilyAnalysisConfigurationSlCore;
    private IAsyncLockFactory asyncLockFactory;
    private IThreadHandling threadHandling;
    private TestLogger testLogger;
    private ISLCoreServiceProvider serviceProvider;
    private IAsyncLock asyncLock;
    private EventHandler databasePathChangedHandler;

    private readonly IRequireInitialization[] initializationDependencies = [];

    [TestInitialize]
    public void TestInitialize()
    {
        cMakeCompilationDatabaseLocator = Substitute.For<ICMakeCompilationDatabaseLocator>();
        activeVcxCompilationDatabase = Substitute.For<IActiveVcxCompilationDatabase>();
        activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
        initializationProcessorFactory = Substitute.For<IInitializationProcessorFactory>();
        cFamilyAnalysisConfigurationSlCore = Substitute.For<ICFamilyAnalysisConfigurationSLCoreService>();
        asyncLockFactory = Substitute.For<IAsyncLockFactory>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        testLogger = Substitute.ForPartsOf<TestLogger>();
        serviceProvider = Substitute.For<ISLCoreServiceProvider>();
        databasePathChangedHandler = Substitute.For<EventHandler>();
        SetUpServiceProvider();
        asyncLock = Substitute.For<IAsyncLock>();
        asyncLockFactory.Create().Returns(asyncLock);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<ActiveCompilationDatabaseTracker, IActiveCompilationDatabaseTracker>(
            MefTestHelpers.CreateExport<ICMakeCompilationDatabaseLocator>(),
            MefTestHelpers.CreateExport<IActiveVcxCompilationDatabase>(),
            MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
            MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
            MefTestHelpers.CreateExport<IAsyncLockFactory>(),
            MefTestHelpers.CreateExport<IThreadHandling>(),
            MefTestHelpers.CreateExport<ISLCoreServiceProvider>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ActiveCompilationDatabaseTracker>();

    [TestMethod]
    public void Ctor_RunsInitialization()
    {
        var testSubject = CreateAndInitializeTestSubject();

        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<ActiveCompilationDatabaseTracker>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(initializationDependencies)),
                Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync();
            activeVcxCompilationDatabase.EnsureDatabaseInitializedAsync();
            activeConfigScopeTracker.CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
            var releaser = asyncLock.AcquireAsync().GetAwaiter().GetResult();
            releaser.Dispose();
            testSubject.InitializationProcessor.InitializeAsync(); // called by CreateAndInitializeTestSubject
        });
        testSubject.CurrentDatabase.Should().BeNull();
        VerifyOnBackgroundThreadLockTakenAndReleased(1);
    }

    [TestMethod]
    public void Ctor_WhenUninitialized_DoesNotReactToConfigScopeChangedEvents()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);

        RaiseEventNewConfigScope();
        _ = activeConfigScopeTracker.DidNotReceiveWithAnyArgs().Current;

        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        _ = activeConfigScopeTracker.Received(1).Current;
        RaiseEventNewConfigScope();
        _ = activeConfigScopeTracker.Received(2).Current;
    }

    [TestMethod]
    public void DisposedBeforeInitialized_InitializationDoesNothing()
    {
        var testSubject = CreateUninitializedTestSubject(out var barrier);
        testSubject.Dispose();

        barrier.SetResult(1);
        testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

        activeVcxCompilationDatabase.DidNotReceiveWithAnyArgs().EnsureDatabaseInitializedAsync();
        activeVcxCompilationDatabase.DidNotReceiveWithAnyArgs().DropDatabaseAsync();
        activeConfigScopeTracker.DidNotReceiveWithAnyArgs().CurrentConfigurationScopeChanged += Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        activeConfigScopeTracker.DidNotReceiveWithAnyArgs().CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
        _ = activeConfigScopeTracker.DidNotReceiveWithAnyArgs().Current;
    }

    [TestMethod]
    public void Ctor_ConfigScopeExists_InitializesWithCMakeDatabase()
    {
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);

        var testSubject = CreateAndInitializeTestSubject();

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(CmakeJson, CompilationDatabaseType.CMake));
        VerifyCalledService(DefaultConfigScope, CmakeJson);
        VerifyOnBackgroundThreadLockTakenAndReleased(1);
    }

    [TestMethod]
    public void Ctor_ConfigScopeExists_CMakeDatabaseNotAvailable_InitializesWithVcxDatabase()
    {
        SetCurrentConfiguration(DefaultConfigScope, null, VcxJson);

        var testSubject = CreateAndInitializeTestSubject();

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(VcxJson, CompilationDatabaseType.VCX));
        VerifyCalledService(DefaultConfigScope, VcxJson);
        VerifyOnBackgroundThreadLockTakenAndReleased(1);
    }

    [TestMethod]
    public void Ctor_NoConfigurationScope_DoesNotUpdateSLCore()
    {
        activeConfigScopeTracker.Current.ReturnsNull();
        cMakeCompilationDatabaseLocator.Locate().ReturnsNull();

        var testSubject = CreateAndInitializeTestSubject();

        testSubject.CurrentDatabase.Should().BeNull();
        cFamilyAnalysisConfigurationSlCore.DidNotReceiveWithAnyArgs().DidChangePathToCompileCommands(default);
    }

    [TestMethod]
    public void CurrentDatabase_AcquiresLock()
    {
        var testSubject = CreateAndInitializeTestSubject();
        asyncLock.ClearReceivedCalls();

        _ = testSubject.CurrentDatabase;

        asyncLock.Received(1).Acquire();
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_NoConfigurationScope_DoesNotUpdateSLCore()
    {
        var testSubject = CreateAndInitializeTestSubject();
        activeConfigScopeTracker.Current.ReturnsNull();
        cMakeCompilationDatabaseLocator.Locate().ReturnsNull();

        RaiseEventNewConfigScope();

        testSubject.CurrentDatabase.Should().BeNull();
        cFamilyAnalysisConfigurationSlCore.DidNotReceiveWithAnyArgs().DidChangePathToCompileCommands(default);
        databasePathChangedHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
        VerifyOnBackgroundThreadLockTakenAndReleased(1 + 1);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_PrefersCMakeDatabase_WhenAvailable()
    {
        var testSubject = CreateAndInitializeTestSubject();
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);

        RaiseEventNewConfigScope();

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(CmakeJson, CompilationDatabaseType.CMake));
        VerifyCalledService(DefaultConfigScope, CmakeJson);
        databasePathChangedHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
        VerifyOnBackgroundThreadLockTakenAndReleased(1 + 1);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_FallsBackToVcxDatabase_WhenCMakeNotAvailable()
    {
        var testSubject = CreateAndInitializeTestSubject();
        SetCurrentConfiguration(DefaultConfigScope, null, VcxJson);

        RaiseEventNewConfigScope();

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(VcxJson, CompilationDatabaseType.VCX));
        VerifyCalledService(DefaultConfigScope, VcxJson);
        databasePathChangedHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
        VerifyOnBackgroundThreadLockTakenAndReleased(1 + 1);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_SameConfigurationScopeId_UpdateSLCore()
    {
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);
        var testSubject = CreateAndInitializeTestSubject();
        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(CmakeJson, CompilationDatabaseType.CMake)); // sanity check
        VerifyOnBackgroundThreadLockTakenAndReleased(1);

        SetCurrentConfiguration(DefaultConfigScope, null, VcxJson);
        RaiseEventNewConfigScope();

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(VcxJson, CompilationDatabaseType.VCX));
        VerifyCalledService(DefaultConfigScope, VcxJson);
        databasePathChangedHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
        VerifyOnBackgroundThreadLockTakenAndReleased(1 + 1);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_DifferentConfigurationScope_UpdateSLCore()
    {
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);
        var testSubject = CreateAndInitializeTestSubject();
        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(CmakeJson, CompilationDatabaseType.CMake)); // sanity check
        VerifyOnBackgroundThreadLockTakenAndReleased(1);

        const string scope2 = "scope2";
        SetCurrentConfiguration(scope2, null, VcxJson);
        RaiseEventNewConfigScope();

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(VcxJson, CompilationDatabaseType.VCX));
        VerifyCalledService(scope2, VcxJson);
        databasePathChangedHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
        VerifyOnBackgroundThreadLockTakenAndReleased(1 + 1);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_SameConfigurationScopeId_NotRedeclaredButUpdated_DoesNothing()
    {
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);
        var testSubject = CreateAndInitializeTestSubject();
        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(CmakeJson, CompilationDatabaseType.CMake)); // sanity check
        VerifyOnBackgroundThreadLockTakenAndReleased(1);

        cFamilyAnalysisConfigurationSlCore.ClearReceivedCalls();
        cMakeCompilationDatabaseLocator.ClearReceivedCalls();
        activeVcxCompilationDatabase.ClearReceivedCalls();
        activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith<ConfigurationScopeChangedEventArgs>(new(false));

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(CmakeJson, CompilationDatabaseType.CMake));
        cFamilyAnalysisConfigurationSlCore.DidNotReceiveWithAnyArgs().DidChangePathToCompileCommands(default);
        cMakeCompilationDatabaseLocator.DidNotReceiveWithAnyArgs().Locate();
        _ = activeVcxCompilationDatabase.DidNotReceiveWithAnyArgs().DatabasePath;
        databasePathChangedHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
        VerifyOnBackgroundThreadLockTakenAndReleased(1); // still one invocation, as the event is just ignored
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_ServiceNotAvailable_SetsToNull()
    {
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);
        var testSubject = CreateAndInitializeTestSubject();
        serviceProvider.TryGetTransientService(out ICFamilyAnalysisConfigurationSLCoreService _).Returns(false);
        const string scope2 = "scope2";

        SetCurrentConfiguration(scope2, null, VcxJson);
        RaiseEventNewConfigScope();

        testSubject.CurrentDatabase.Should().BeNull();
        cFamilyAnalysisConfigurationSlCore.DidNotReceive().DidChangePathToCompileCommands(Arg.Is<DidChangePathToCompileCommandsParams>(x => x.configurationScopeId == scope2));
        databasePathChangedHandler.Received(1).Invoke(testSubject, EventArgs.Empty);
        VerifyOnBackgroundThreadLockTakenAndReleased(1 + 1);
    }

    [TestMethod]
    public void CurrentConfigurationScopeChanged_ServiceNotAvailableAndThenAvailable_Recovers()
    {
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);
        var testSubject = CreateAndInitializeTestSubject();
        serviceProvider.TryGetTransientService(out ICFamilyAnalysisConfigurationSLCoreService _).Returns(false);

        const string scope2 = "scope2";
        SetCurrentConfiguration(scope2, null, VcxJson);
        RaiseEventNewConfigScope();
        testSubject.CurrentDatabase.Should().BeNull(); // sanity check
        databasePathChangedHandler.Received(1).Invoke(testSubject, EventArgs.Empty);

        SetUpServiceProvider();
        // recovery to the same id causes an update despite the optimization to ignore updates to the same id
        SetCurrentConfiguration(DefaultConfigScope, CmakeJson, null);
        RaiseEventNewConfigScope();

        testSubject.CurrentDatabase.Should().BeEquivalentTo(new CompilationDatabaseInfo(CmakeJson, CompilationDatabaseType.CMake));
        databasePathChangedHandler.Received(2).Invoke(testSubject, EventArgs.Empty);
        VerifyOnBackgroundThreadLockTakenAndReleased(1 + 1 + 1);
    }

    [TestMethod]
    public void Dispose_UnsubscribesAndDisposes()
    {
        var testSubject = CreateAndInitializeTestSubject();

        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        Received.InOrder(() =>
        {
            activeConfigScopeTracker.CurrentConfigurationScopeChanged -= Arg.Any<EventHandler<ConfigurationScopeChangedEventArgs>>();
            threadHandling.Run(Arg.Any<Func<Task<int>>>());
            activeVcxCompilationDatabase.DropDatabaseAsync();
            activeVcxCompilationDatabase.Dispose();
            asyncLock.Dispose();
        });
    }

    private void RaiseEventNewConfigScope() => activeConfigScopeTracker.CurrentConfigurationScopeChanged += Raise.EventWith<ConfigurationScopeChangedEventArgs>(new(true));

    private void SetCurrentConfiguration(string configurationScopeId, string cmakePath, string vcxPath)
    {
        var newConfigScope = new ConfigurationScope(configurationScopeId);
        activeConfigScopeTracker.Current.Returns(newConfigScope);
        cMakeCompilationDatabaseLocator.Locate().Returns(cmakePath);
        activeVcxCompilationDatabase.DatabasePath.Returns(vcxPath);
    }

    private void VerifyCalledService(string configurationScopeId, string databasePath) =>
        cFamilyAnalysisConfigurationSlCore.Received()
            .DidChangePathToCompileCommands(Arg.Is<DidChangePathToCompileCommandsParams>(
                x => x.configurationScopeId == configurationScopeId && x.pathToCompileCommands == databasePath));

    private void VerifyOnBackgroundThreadLockTakenAndReleased(int count) =>
        Received.InOrder(() =>
        {
            for (var i = 0; i < count; i++)
            {
                threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
                var acquireAsync = asyncLock.AcquireAsync().GetAwaiter().GetResult();
                acquireAsync.Dispose();
            }
        });

    private void SetUpServiceProvider() =>
        serviceProvider.TryGetTransientService(out Arg.Any<ICFamilyAnalysisConfigurationSLCoreService>()).Returns(info =>
        {
            info[0] = cFamilyAnalysisConfigurationSlCore;
            return true;
        });

    private ActiveCompilationDatabaseTracker CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
    {
        var tcs = barrier = new TaskCompletionSource<byte>();
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ActiveCompilationDatabaseTracker>(threadHandling, testLogger, processor =>
        {
            MockableInitializationProcessor.ConfigureWithWait(processor, tcs);
        });
        return new ActiveCompilationDatabaseTracker(
            cMakeCompilationDatabaseLocator,
            activeVcxCompilationDatabase,
            activeConfigScopeTracker,
            initializationProcessorFactory,
            asyncLockFactory,
            threadHandling,
            serviceProvider);
    }

    private ActiveCompilationDatabaseTracker CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ActiveCompilationDatabaseTracker>(threadHandling, testLogger);
        var tracker = new ActiveCompilationDatabaseTracker(
            cMakeCompilationDatabaseLocator,
            activeVcxCompilationDatabase,
            activeConfigScopeTracker,
            initializationProcessorFactory,
            asyncLockFactory,
            threadHandling,
            serviceProvider);
        tracker.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        tracker.DatabaseChanged += databasePathChangedHandler;

        return tracker;
    }
}
