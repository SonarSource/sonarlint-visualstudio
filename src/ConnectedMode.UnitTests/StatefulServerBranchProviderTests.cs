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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Branch;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class StatefulServerBranchProviderTests
    {
        private readonly ActiveSolutionBindingEventArgs connectedModeBinding = new(new BindingConfiguration(default, SonarLintMode.Connected, default));
        private readonly ActiveSolutionBindingEventArgs standaloneModeBinding = new(BindingConfiguration.Standalone);
        private IThreadHandling threadHandling;
        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private ISLCoreServiceProvider slCoreServiceProvider;
        private TestLogger logger;
        private IAsyncLockFactory asyncLockFactory;
        private StatefulServerBranchProvider testSubject;
        private IServerBranchProvider serverBranchProvider;
        private IActiveConfigScopeTracker activeConfigScopeTracker;
        private ISonarProjectBranchSlCoreService sonarProjectBranchSlCoreService;
        private IAsyncLock asyncLock;

        [TestInitialize]
        public void TestInitialize()
        {
            serverBranchProvider = Substitute.For<IServerBranchProvider>();
            activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
            activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
            logger = new TestLogger();
            threadHandling = new NoOpThreadHandler();
            slCoreServiceProvider = Substitute.For<ISLCoreServiceProvider>();
            MockSonarProjectBranchSlCoreService();
            MockAsyncLock();
            testSubject = new StatefulServerBranchProvider(serverBranchProvider, activeSolutionBoundTracker, activeConfigScopeTracker, slCoreServiceProvider, logger, threadHandling, asyncLockFactory);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<StatefulServerBranchProvider, IStatefulServerBranchProvider>(
                MefTestHelpers.CreateExport<IServerBranchProvider>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
                MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>(),
                MefTestHelpers.CreateExport<IAsyncLockFactory>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<StatefulServerBranchProvider>();

        [TestMethod]
        public async Task GetServerBranchNameAsync_WhenCalled_UsesCache()
        {
            MockGetServerBranchName("Branch");

            // First call: Should use IServerBranchProvider
            var serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);
            serverBranch.Should().Be("Branch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(1);

            // Second call: Should use cache
            serverBranchProvider.SetBranchNameToReturn("branch name that should not be returned - should use cached value");
            serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            // Not expecting any more threading calls since using the cache
            serverBranch.Should().Be("Branch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(1);
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_RunsOnBackgroundThread()
        {
            MockGetServerBranchName("Branch");
            var mockedThreadHandling = MockThreadHandling();
            testSubject = new StatefulServerBranchProvider(serverBranchProvider, activeSolutionBoundTracker, activeConfigScopeTracker, slCoreServiceProvider, logger, mockedThreadHandling,
                asyncLockFactory);

            var serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            await mockedThreadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<string>>>());
            mockedThreadHandling.ReceivedCalls().Should().HaveCount(1);
            serverBranch.Should().Be("Branch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(1);
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_WhenCalled_UpdatesCacheThreadSafe()
        {
            MockGetServerBranchName("Branch");

            var serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            serverBranch.Should().Be("Branch");
            await asyncLock.Received(1).AcquireAsync();
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task GetServerBranchNameAsync_PreSolutionBindingChanged_CacheIsCleared(bool isConnected) =>
            await TestEffectOfRaisingEventOnCacheAsync(RaisePreSolutionBindingUpdated, eventArg: isConnected ? connectedModeBinding : standaloneModeBinding, shouldClearCache: true);

        [TestMethod]
        public async Task GetServerBranchNameAsync_PreSolutionBindingUpdated_CacheIsCleared() => await TestEffectOfRaisingEventOnCacheAsync(RaisePreSolutionBindingUpdated, shouldClearCache: true);

        [TestMethod]
        public async Task GetServerBranchNameAsync_SolutionBindingChanged_CacheIsNotCleared() => await TestEffectOfRaisingEventOnCacheAsync(RaiseSolutionBindingUpdated, shouldClearCache: false);

        [TestMethod]
        public async Task GetServerBranchNameAsync_SolutionBindingUpdated_CacheIsNotCleared() => await TestEffectOfRaisingEventOnCacheAsync(RaiseSolutionBindingUpdated, shouldClearCache: false);

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingChanged_Connected_CallsDidVcsRepositoryChangeWithCorrectId()
        {
            const string expectedConfigScopeId = "expected-id";
            activeConfigScopeTracker.Current.Returns(new Core.ConfigurationScope.ConfigurationScope(expectedConfigScopeId));
            MockSonarProjectBranchSlCoreService();
            MockGetServerBranchName("OriginalBranch");

            RaisePreSolutionBindingUpdated(connectedModeBinding);

            sonarProjectBranchSlCoreService.Received(1)
                .DidVcsRepositoryChange(Arg.Is<DidVcsRepositoryChangeParams>(p => p.configurationScopeId == expectedConfigScopeId));
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingChanged_Standalone_Ignores()
        {
            MockGetServerBranchName("OriginalBranch");

            RaisePreSolutionBindingUpdated(standaloneModeBinding);

            activeConfigScopeTracker.ReceivedCalls().Should().HaveCount(1); // no other calls
            slCoreServiceProvider.ReceivedCalls().Should().HaveCount(1); // no other calls
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingUpdated_CallsDidVcsRepositoryChangeWithCorrectId()
        {
            const string expectedConfigScopeId = "expected-id";
            activeConfigScopeTracker.Current.Returns(new Core.ConfigurationScope.ConfigurationScope(expectedConfigScopeId));
            MockSonarProjectBranchSlCoreService();
            MockGetServerBranchName("OriginalBranch");

            RaisePreSolutionBindingUpdated(null);

            sonarProjectBranchSlCoreService.Received(1).DidVcsRepositoryChange(Arg.Is<DidVcsRepositoryChangeParams>(p => p.configurationScopeId == expectedConfigScopeId));
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingChanged_WhenServiceProviderReturnsFalse_LogsError()
        {
            MockSonarProjectBranchSlCoreService(wasFound: false);
            MockGetServerBranchName("OriginalBranch");

            RaisePreSolutionBindingUpdated(connectedModeBinding);

            logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
            slCoreServiceProvider.ReceivedCalls().Should().HaveCount(1); // no other calls
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingUpdated_WhenServiceProviderReturnsFalse_LogsError()
        {
            MockSonarProjectBranchSlCoreService(wasFound: false);
            MockGetServerBranchName("OriginalBranch");

            RaisePreSolutionBindingUpdated(null);

            logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
            slCoreServiceProvider.ReceivedCalls().Should().HaveCount(1); // no other calls
        }

        [TestMethod]
        public void Dispose_UnhooksEventHandlers()
        {
            testSubject.Dispose();

            // Should only unhook the "Pre-" event handlers
            activeSolutionBoundTracker.Received(1).PreSolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
            activeSolutionBoundTracker.Received(1).PreSolutionBindingUpdated -= Arg.Any<EventHandler>();

            activeSolutionBoundTracker.DidNotReceive().SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
            activeSolutionBoundTracker.DidNotReceive().SolutionBindingUpdated -= Arg.Any<EventHandler>();
        }

        private void MockGetServerBranchName(string branchName) => serverBranchProvider.GetServerBranchNameAsync(Arg.Any<CancellationToken>()).Returns(branchName);

        private void MockAsyncLock()
        {
            asyncLock = Substitute.For<IAsyncLock>();
            asyncLockFactory = Substitute.For<IAsyncLockFactory>();
            asyncLockFactory.Create().Returns(asyncLock);
        }

        private async Task TestEffectOfRaisingEventOnCacheAsync(
            Action<EventArgs> eventAction,
            bool shouldClearCache,
            EventArgs eventArg = null)
        {
            MockGetServerBranchName("OriginalBranch");

            //first call: Should use IServerBranchProvider
            var serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            serverBranch.Should().Be("OriginalBranch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(1);

            serverBranchProvider.SetBranchNameToReturn("NewBranch");

            // Raise event - should *not* trigger clearing the cache
            eventAction(eventArg);

            //second call: may or may not use the cache
            serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            if (shouldClearCache)
            {
                serverBranch.Should().Be("NewBranch");
                serverBranchProvider.VerifyGetServerBranchNameCalled(2);
                asyncLock.Received(1).Acquire();
            }
            else
            {
                serverBranch.Should().Be("OriginalBranch");
                serverBranchProvider.VerifyGetServerBranchNameCalled(1);
                asyncLock.DidNotReceive().Acquire();
            }
        }

        private void RaisePreSolutionBindingUpdated(EventArgs eventArg) => activeSolutionBoundTracker.PreSolutionBindingUpdated += Raise.EventWith(null, eventArg);

        private void RaiseSolutionBindingUpdated(EventArgs eventArg) => activeSolutionBoundTracker.SolutionBindingUpdated += Raise.EventWith(null, eventArg);

        private void MockSonarProjectBranchSlCoreService(bool wasFound = true)
        {
            sonarProjectBranchSlCoreService = Substitute.For<ISonarProjectBranchSlCoreService>();
            slCoreServiceProvider.TryGetTransientService(out ISonarProjectBranchSlCoreService _).Returns(x =>
            {
                x[0] = sonarProjectBranchSlCoreService;
                return wasFound;
            });
        }

        private IThreadHandling MockThreadHandling()
        {
            var mockedThreadHandling = Substitute.For<IThreadHandling>();
            mockedThreadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<string>>>()).Returns(info => info.Arg<Func<Task<string>>>()());
            return mockedThreadHandling;
        }
    }

    internal static class StatefulServerBranchProviderTestsExtensions
    {
        public static void VerifyGetServerBranchNameCalled(this IServerBranchProvider serverBranchProvider, int times) =>
            serverBranchProvider.Received(times).GetServerBranchNameAsync(Arg.Any<CancellationToken>());

        public static void SetBranchNameToReturn(this IServerBranchProvider serverBranchProvider, string branchName) =>
            serverBranchProvider.GetServerBranchNameAsync(Arg.Any<CancellationToken>()).Returns(branchName);
    }
}
