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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Branch;

namespace SonarLint.VisualStudio.SLCore.Listeners.UnitTests
{
    [TestClass]
    public class BranchListenerTests
    {
        private IServerBranchProvider serverBranchProvider;
        private IActiveConfigScopeTracker activeConfigScopeTracker;
        private TestLogger logger;
        private BranchListener testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            serverBranchProvider = Substitute.For<IServerBranchProvider>();
            activeConfigScopeTracker = Substitute.For<IActiveConfigScopeTracker>();
            logger = Substitute.ForPartsOf<TestLogger>();

            testSubject = new BranchListener(serverBranchProvider, activeConfigScopeTracker, logger);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<BranchListener, ISLCoreListener>(
                MefTestHelpers.CreateExport<IServerBranchProvider>(),
                MefTestHelpers.CreateExport<IActiveConfigScopeTracker>(),
                MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        public void Mef_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<BranchListener>();

        [TestMethod]
        public async Task MatchSonarProjectBranchAsync_ConfigurationScopeIdDoesNotMatch_ReturnsNull()
        {
            var configScope = new ConfigurationScope("different-id");
            activeConfigScopeTracker.Current.Returns(configScope);
            var parameters = new MatchSonarProjectBranchParams("config-id", "branch1", ["branch1", "branch2"]);

            var result = await testSubject.MatchSonarProjectBranchAsync(parameters);

            result.matchedSonarBranch.Should().BeNull();
            serverBranchProvider.DidNotReceiveWithAnyArgs().GetServerBranchName(default);
        }

        [TestMethod]
        public async Task MatchSonarProjectBranchAsync_ConfigurationScopeIdMatches_ReturnsMatchingBranch()
        {
            const string configId = "config-id";
            var configScope = new ConfigurationScope(configId);
            activeConfigScopeTracker.Current.Returns(configScope);
            var parameters = new MatchSonarProjectBranchParams(configId, "main", ["branch1", "branch2", "main"]);
            const string expectedBranch = "branch1";
            serverBranchProvider.GetServerBranchName(Arg.Any<List<RemoteBranch>>()).Returns(expectedBranch);

            var result = await testSubject.MatchSonarProjectBranchAsync(parameters);

            result.matchedSonarBranch.Should().Be(expectedBranch);
            serverBranchProvider.Received(1).GetServerBranchName(Arg.Is<List<RemoteBranch>>(list =>
                list.Count == 3 &&
                list.Any(b => b.Name == "branch1" && !b.IsMain) &&
                list.Any(b => b.Name == "branch2" && !b.IsMain) &&
                list.Any(b => b.Name == "main" && b.IsMain)));
        }

        [TestMethod]
        public async Task DidChangeMatchedSonarProjectBranchAsync_UpdatesConfigScope_WhenIdMatches()
        {
            const string configId = "config-id";
            const string branchName = "new-branch";
            activeConfigScopeTracker.TryUpdateMatchedBranchOnCurrentConfigScope(configId, branchName).Returns(true);
            var parameters = new DidChangeMatchedSonarProjectBranchParams(configId, branchName);

            await testSubject.DidChangeMatchedSonarProjectBranchAsync(parameters);

            activeConfigScopeTracker.Received(1).TryUpdateMatchedBranchOnCurrentConfigScope(configId, branchName);
        }

        [TestMethod]
        public async Task DidChangeMatchedSonarProjectBranchAsync_LogsError_WhenIdDoesNotMatch()
        {
            const string configId = "config-id";
            const string branchName = "new-branch";
            activeConfigScopeTracker.TryUpdateMatchedBranchOnCurrentConfigScope(configId, branchName).Returns(false);
            var configScope = new ConfigurationScope("different-id");
            activeConfigScopeTracker.Current.Returns(configScope);
            var parameters = new DidChangeMatchedSonarProjectBranchParams(configId, branchName);

            await testSubject.DidChangeMatchedSonarProjectBranchAsync(parameters);

            activeConfigScopeTracker.Received(1).TryUpdateMatchedBranchOnCurrentConfigScope(configId, branchName);
            logger.AssertPartialOutputStringExists(string.Format(SLCoreStrings.ConfigurationScopeMismatch, configId, configScope.Id));
        }
    }
}
