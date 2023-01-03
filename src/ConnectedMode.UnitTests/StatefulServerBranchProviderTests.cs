/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class StatefulServerBranchProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<StatefulServerBranchProvider, IStatefulServerBranchProvider>(
                MefTestHelpers.CreateExport<IServerBranchProvider>(),
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>());
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_WhenCalled_UsesCache()
        {
            var serverBranchProvider = CreateServerBranchProvider("Branch");
            var activeSolutionBoundTracker = Mock.Of<IActiveSolutionBoundTracker>();

            var testSubject = CreateTestSubject(serverBranchProvider.Object, activeSolutionBoundTracker);

            //first call: Should use IServerBranchProvider
            var serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            serverBranch.Should().Be("Branch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(Times.Once);


            //second call: Should use cache
            serverBranchProvider.SetBranchNameToReturn("branch name that should not be returned - should use cached value");

            serverBranch = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            serverBranch.Should().Be("Branch");
            serverBranchProvider.VerifyGetServerBranchNameCalled(Times.Once);
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_PreSolutionBindingChanged_CacheIsCleared()
        {
            await TestEffectOfRaisingEventOnCache(asbt => asbt.PreSolutionBindingChanged += null,
                shouldClearCache: true);
        }

        [TestMethod]
        public async Task GetServerBranchNameAsync_PreSolutionBindingUpdated_CacheIsCleared()
        {
            await TestEffectOfRaisingEventOnCache(asbt => asbt.PreSolutionBindingChanged += null,
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
            await TestEffectOfRaisingEventOnCache(asbt => asbt.SolutionBindingChanged += null,
                shouldClearCache: false);
        }

        private static async Task TestEffectOfRaisingEventOnCache(Action<IActiveSolutionBoundTracker> eventAction,
            bool shouldClearCache)
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
            activeSolutionBoundTracker.Raise(eventAction, null, null);

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

        private static Mock<IServerBranchProvider> CreateServerBranchProvider(string branchName)
        {
            var serverBranchProvider = new Mock<IServerBranchProvider>();
            serverBranchProvider.SetBranchNameToReturn(branchName);

            return serverBranchProvider;
        }

        private static StatefulServerBranchProvider CreateTestSubject(IServerBranchProvider provider, IActiveSolutionBoundTracker tracker)
        {
            return new StatefulServerBranchProvider(provider, tracker);
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
