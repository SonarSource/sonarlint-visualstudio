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

using LibGit2Sharp;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Listener.Branch;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class ServerBranchProviderTests
    {
        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private IGitWorkspaceService gitWorkspaceService;
        private IBranchMatcher branchMatcher;
        private TestLogger logger;
        private ServerBranchProvider.CreateRepositoryObject createRepoOp;
        private ServerBranchProvider testSubject;
        private IRepository repo;

        [TestInitialize]
        public void TestInitialize()
        {
            activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
            gitWorkspaceService = Substitute.For<IGitWorkspaceService>();
            branchMatcher = Substitute.For<IBranchMatcher>();
            logger = Substitute.ForPartsOf<TestLogger>();
            createRepoOp = Substitute.For<ServerBranchProvider.CreateRepositoryObject>();
            repo = Substitute.For<IRepository>();

            testSubject = new ServerBranchProvider(activeSolutionBoundTracker, gitWorkspaceService, branchMatcher, logger, createRepoOp);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<ServerBranchProvider, IServerBranchProvider>(
                MefTestHelpers.CreateExport<IActiveSolutionBoundTracker>(),
                MefTestHelpers.CreateExport<IGitWorkspaceService>(),
                MefTestHelpers.CreateExport<IBranchMatcher>(),
                MefTestHelpers.CreateExport<ILogger>());

        [TestMethod]
        public void Get_StandaloneMode_ReturnsNull()
        {
            SetUpBindingConfiguration(SonarLintMode.Standalone);
            var branches = new List<RemoteBranch> { new RemoteBranch("branch1", false), new RemoteBranch("main", true), new RemoteBranch("branch2", false) };

            var actual = testSubject.GetServerBranchName(branches);

            actual.Should().BeNull();
            _ = activeSolutionBoundTracker.Received(1).CurrentConfiguration;
            gitWorkspaceService.DidNotReceiveWithAnyArgs().GetRepoRoot();
            branchMatcher.DidNotReceiveWithAnyArgs().GetMatchingBranch(Arg.Any<string>(), Arg.Any<IRepository>(), Arg.Any<List<RemoteBranch>>());
        }

        [TestMethod]
        public void Get_NoGitRepo_ReturnsDefaultMainBranch()
        {
            SetUpBindingConfiguration(SonarLintMode.Connected);
            SetUpGitWorkspaceService(null);
            createRepoOp.Invoke(Arg.Any<string>()).Returns((IRepository)null);
            var branches = new List<RemoteBranch> { new RemoteBranch("branch1", false), new RemoteBranch("main branch name", true), new RemoteBranch("branch2", false) };

            var actual = testSubject.GetServerBranchName(branches);

            actual.Should().Be("main branch name");
            _ = activeSolutionBoundTracker.Received(1).CurrentConfiguration;
            gitWorkspaceService.Received(1).GetRepoRoot();
            gitWorkspaceService.ReceivedCalls().Should().HaveCount(1);
            branchMatcher.DidNotReceiveWithAnyArgs().GetMatchingBranch(Arg.Any<string>(), Arg.Any<IRepository>(), Arg.Any<List<RemoteBranch>>());
            createRepoOp.DidNotReceiveWithAnyArgs().Invoke(Arg.Any<string>());
        }

        [TestMethod]
        [DataRow(SonarLintMode.LegacyConnected)]
        [DataRow(SonarLintMode.Connected)]
        public void Get_ConnectedModeAndHasGitRepo_HasMatchingBranch_ReturnsExpectedBranch(SonarLintMode mode)
        {
            SetUpBindingConfiguration(mode, "my project key");
            var repoRoot = "c:\\aaa\\reporoot";
            SetUpGitWorkspaceService(repoRoot);
            SetUpBranchMatcher("my matching branch");
            createRepoOp.Invoke(repoRoot).Returns(repo);
            var branches = new List<RemoteBranch> { new RemoteBranch("branch1", false), new RemoteBranch("main", true), new RemoteBranch("branch2", false) };

            var actual = testSubject.GetServerBranchName(branches);

            actual.Should().Be("my matching branch");
            logger.AssertPartialOutputStringExists("my matching branch");
            _ = activeSolutionBoundTracker.Received(1).CurrentConfiguration;
            gitWorkspaceService.Received(1).GetRepoRoot();
            createRepoOp.Received(1).Invoke(repoRoot);
            branchMatcher.Received(1).GetMatchingBranch("my project key", repo, branches);
        }

        [TestMethod]
        public void Get_ConnectedModeAndHasGitRepo_NoMatchingBranch_ReturnsDefaultMainBranch()
        {
            SetUpBindingConfiguration(SonarLintMode.Connected, "my project key");
            var repoRoot = "x:\\";
            SetUpGitWorkspaceService(repoRoot);
            SetUpBranchMatcher(null);
            createRepoOp.Invoke(repoRoot).Returns(repo);
            var branches = new List<RemoteBranch> { new RemoteBranch("branch1", false), new RemoteBranch("some main branch", true), new RemoteBranch("branch2", false) };

            var actual = testSubject.GetServerBranchName(branches);

            actual.Should().Be("some main branch");
            branchMatcher.Received(1).GetMatchingBranch("my project key", repo, branches);
        }

        private void SetUpBindingConfiguration(SonarLintMode mode = SonarLintMode.Connected, string projectKey = "any")
        {
            var config = new BindingConfiguration(
                new BoundServerProject("solution", projectKey, new ServerConnection.SonarCloud("org")),
                mode,
                "any dir");

            activeSolutionBoundTracker.CurrentConfiguration.Returns(config);
        }

        private void SetUpGitWorkspaceService(string repoRootToReturn)
        {
            gitWorkspaceService.GetRepoRoot().Returns(repoRootToReturn);
        }

        private void SetUpBranchMatcher(string branchToReturn)
        {
            branchMatcher.GetMatchingBranch(Arg.Any<string>(), Arg.Any<IRepository>(), Arg.Any<List<RemoteBranch>>())
                .Returns(branchToReturn);
        }
    }
}
