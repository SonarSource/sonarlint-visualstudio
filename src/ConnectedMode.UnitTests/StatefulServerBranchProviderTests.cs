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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Service.Branch;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class StatefulServerBranchProviderTests
    {
        private readonly ActiveSolutionBindingEventArgs ConnectedModeBinding = new(new BindingConfiguration(default, SonarLintMode.Connected, default));
        private readonly ActiveSolutionBindingEventArgs StandaloneModeBinding = new(BindingConfiguration.Standalone);

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<StatefulServerBranchProvider, IStatefulServerBranchProvider>(
                MefTestHelpers.CreateExport<IServerBranchProvider>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
                MefTestHelpers.CreateExport<ISLCoreServiceProvider>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IThreadHandling>());
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_WhenCalled_UsesCache()
        {
            var serverBranchProvider = CreateServerBranchProvider("Branch");
            var activeSolutionBoundTracker = Mock.Of<IActiveSolutionBoundTracker>();

            var threadHandling = new Mock<IThreadHandling>();
            threadHandling.Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<string>>>()))
                .Returns<Func<Task<string>>>(x => x()); // passthrough - call whatever was passed

            var testSubject = CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker, threadHandling: threadHandling.Object);

            // First call: Should use IServerBranchProvider
            var serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            threadHandling.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<string>>>()), Times.Once);
            threadHandling.Invocations.Should().HaveCount(1);

            serverBranch.Should().Be("Branch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(Times.Once);

            // Second call: Should use cache
            serverBranchProvider.SetBranchNameToReturn("branch name that should not be returned - should use cached value");

            serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            // Not expecting any more threading calls since using the cache
            threadHandling.Invocations.Should().HaveCount(1);
            serverBranch.Should().Be("Branch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(Times.Once);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task GetServerBranchNameAsync_PreSolutionBindingChanged_CacheIsCleared(bool isConnected)
        {
            await TestEffectOfRaisingEventOnCache(asbt => asbt.PreSolutionBindingChanged += null,
                eventArg: isConnected ? ConnectedModeBinding : StandaloneModeBinding,
                shouldClearCache: true);
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_PreSolutionBindingUpdated_CacheIsCleared()
        {
            await TestEffectOfRaisingEventOnCache(asbt => asbt.PreSolutionBindingUpdated += null,
                shouldClearCache: true);
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_SolutionBindingChanged_CacheIsNotCleared()
        {
            await TestEffectOfRaisingEventOnCache(asbt => asbt.SolutionBindingChanged += null,
                shouldClearCache: false);
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_SolutionBindingUpdated_CacheIsNotCleared()
        {
            await TestEffectOfRaisingEventOnCache(asbt => asbt.SolutionBindingUpdated += null,
                shouldClearCache: false);
        }

        private static async Task TestEffectOfRaisingEventOnCache(Action<IActiveSolutionBoundTracker> eventAction,
            bool shouldClearCache,
            object eventArg = null)
        {
            var serverBranchProvider = CreateServerBranchProvider("OriginalBranch");
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();

            var testSubject = CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker.Object);

            //first call: Should use IServerBranchProvider
            var serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            serverBranch.Should().Be("OriginalBranch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(Times.Once);

            serverBranchProvider.SetBranchNameToReturn("NewBranch");

            // Raise event - should *not* trigger clearing the cache
            activeSolutionBoundTracker.Raise(eventAction, null, eventArg);

            //second call: may or may not use the cache
            serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            if (shouldClearCache)
            {
                serverBranch.Should().Be("NewBranch");
                serverBranchProvider.VerifyGetServerBranchNameCalled(Times.Exactly(2));
            }
            else
            {
                serverBranch.Should().Be("OriginalBranch");
                serverBranchProvider.VerifyGetServerBranchNameCalled(Times.Once);
            }
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingChanged_Connected_CallsDidVcsRepositoryChangeWithCorrectId()
        {
            // Arrange
            const string expectedConfigScopeId = "expected-id";
            var activeConfigScopeTracker = new Mock<IActiveConfigScopeTracker>();
            activeConfigScopeTracker.Setup(x => x.Current).Returns(new Core.ConfigurationScope.ConfigurationScope(expectedConfigScopeId));

            var sonarProjectBranchSlCoreService = new Mock<ISonarProjectBranchSlCoreService>();
            var serviceProvider = new Mock<ISLCoreServiceProvider>();
            var service = sonarProjectBranchSlCoreService.Object;
            serviceProvider.Setup(x => x.TryGetTransientService(out service)).Returns(true);

            var serverBranchProvider = CreateServerBranchProvider("OriginalBranch");
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker.Object, activeConfigScopeTracker.Object, serviceProvider.Object);

            // Act
            activeSolutionBoundTracker.Raise(x => x.PreSolutionBindingChanged += null, null, ConnectedModeBinding);

            // Assert
            sonarProjectBranchSlCoreService.Verify(x =>
                x.DidVcsRepositoryChange(It.Is<DidVcsRepositoryChangeParams>(p => p.configurationScopeId == expectedConfigScopeId)));
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingChanged_Standalone_Ignores()
        {
            // Arrange
            var activeConfigScopeTracker = new Mock<IActiveConfigScopeTracker>();
            var serviceProvider = new Mock<ISLCoreServiceProvider>();
            var serverBranchProvider = CreateServerBranchProvider("OriginalBranch");
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker.Object, activeConfigScopeTracker.Object, serviceProvider.Object);

            // Act
            activeSolutionBoundTracker.Raise(x => x.PreSolutionBindingChanged += null, null, StandaloneModeBinding);

            // Assert
            activeConfigScopeTracker.VerifyNoOtherCalls();
            serviceProvider.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingUpdated_CallsDidVcsRepositoryChangeWithCorrectId()
        {
            // Arrange
            const string expectedConfigScopeId = "expected-id";
            var activeConfigScopeTracker = new Mock<IActiveConfigScopeTracker>();
            activeConfigScopeTracker.Setup(x => x.Current).Returns(new Core.ConfigurationScope.ConfigurationScope(expectedConfigScopeId));

            var sonarProjectBranchSlCoreService = new Mock<ISonarProjectBranchSlCoreService>();
            var serviceProvider = new Mock<ISLCoreServiceProvider>();
            var service = sonarProjectBranchSlCoreService.Object;
            serviceProvider.Setup(x => x.TryGetTransientService(out service)).Returns(true);

            var serverBranchProvider = CreateServerBranchProvider("OriginalBranch");
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker.Object, activeConfigScopeTracker.Object, serviceProvider.Object);

            // Act
            activeSolutionBoundTracker.Raise(x => x.PreSolutionBindingUpdated += null, null, null);

            // Assert
            sonarProjectBranchSlCoreService.Verify(x =>
                x.DidVcsRepositoryChange(It.Is<DidVcsRepositoryChangeParams>(p => p.configurationScopeId == expectedConfigScopeId)));
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingChanged_WhenServiceProviderReturnsFalse_LogsError()
        {
            // Arrange
            var sonarProjectBranchSlCoreService = new Mock<ISonarProjectBranchSlCoreService>();
            var serviceProvider = new Mock<ISLCoreServiceProvider>();
            var service = sonarProjectBranchSlCoreService.Object;
            serviceProvider.Setup(x => x.TryGetTransientService(out service)).Returns(false);

            var serverBranchProvider = CreateServerBranchProvider("OriginalBranch");
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var logger = new TestLogger();
            CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker.Object, slCoreServiceProvider: serviceProvider.Object, logger: logger);

            activeSolutionBoundTracker.Raise(x => x.PreSolutionBindingChanged += null, null, ConnectedModeBinding);

            logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
            sonarProjectBranchSlCoreService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NotifySlCoreBranchChange_BindingUpdated_WhenServiceProviderReturnsFalse_LogsError()
        {
            // Arrange
            var sonarProjectBranchSlCoreService = new Mock<ISonarProjectBranchSlCoreService>();
            var serviceProvider = new Mock<ISLCoreServiceProvider>();
            var service = sonarProjectBranchSlCoreService.Object;
            serviceProvider.Setup(x => x.TryGetTransientService(out service)).Returns(false);

            var serverBranchProvider = CreateServerBranchProvider("OriginalBranch");
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();
            var logger = new TestLogger();
            CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker.Object, slCoreServiceProvider: serviceProvider.Object, logger: logger);

            activeSolutionBoundTracker.Raise(x => x.PreSolutionBindingUpdated += null, null, null);

            logger.AssertPartialOutputStringExists(SLCoreStrings.ServiceProviderNotInitialized);
            sonarProjectBranchSlCoreService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnhooksEventHandlers()
        {
            var serverBranchProvider = Mock.Of<IServerBranchProvider>();
            var activeSolutionBoundTracker = new Mock<IActiveSolutionBoundTracker>();

            var testSubject = CreateTestSubject(serverBranchProvider, activeSolutionBoundTracker.Object);

            testSubject.Dispose();

            // Should only unhook the "Pre-" event handlers
            activeSolutionBoundTracker.VerifyRemove(x => x.PreSolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Once);
            activeSolutionBoundTracker.VerifyRemove(x => x.PreSolutionBindingUpdated -= It.IsAny<EventHandler>(), Times.Once);

            activeSolutionBoundTracker.VerifyRemove(x => x.SolutionBindingChanged -= It.IsAny<EventHandler<ActiveSolutionBindingEventArgs>>(), Times.Never);
            activeSolutionBoundTracker.VerifyRemove(x => x.SolutionBindingUpdated -= It.IsAny<EventHandler>(), Times.Never);
        }

        private static Mock<IServerBranchProvider> CreateServerBranchProvider(string branchName)
        {
            var serverBranchProvider = new Mock<IServerBranchProvider>();
            serverBranchProvider.SetBranchNameToReturn(branchName);

            return serverBranchProvider;
        }

        private static StatefulServerBranchProvider CreateTestSubject(
            IServerBranchProvider provider,
            IActiveSolutionBoundTracker tracker,
            IActiveConfigScopeTracker activeConfigScopeTracker = null,
            ISLCoreServiceProvider slCoreServiceProvider = null,
            ILogger logger = null,
            IThreadHandling threadHandling = null)
        {
            threadHandling ??= new NoOpThreadHandler();
            activeConfigScopeTracker ??= Substitute.For<IActiveConfigScopeTracker>();
            slCoreServiceProvider ??= Substitute.For<ISLCoreServiceProvider>();
            logger ??= new TestLogger();
            return new StatefulServerBranchProvider(provider, tracker, activeConfigScopeTracker, slCoreServiceProvider, logger, threadHandling);
        }
    }

    internal static class StatefulServerBranchProviderTestsExtensions
    {
        public static void VerifyGetServerBranchNameCalled(this Mock<IServerBranchProvider> serverBranchProvider, Func<Times> times)
            => serverBranchProvider.Verify(sbp => sbp.GetServerBranchNameAsync(It.IsAny<CancellationToken>()), times());

        public static void VerifyGetServerBranchNameCalled(this Mock<IServerBranchProvider> serverBranchProvider, Times times)
            => serverBranchProvider.Verify(sbp => sbp.GetServerBranchNameAsync(It.IsAny<CancellationToken>()), times);

        public static void SetBranchNameToReturn(this Mock<IServerBranchProvider> serverBranchProvider, string branchName)
            => serverBranchProvider.Setup(sbp => sbp.GetServerBranchNameAsync(It.IsAny<CancellationToken>())).ReturnsAsync(branchName);
    }
}
