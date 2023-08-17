/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using LibGit2Sharp;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class ServerBranchProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<ServerBranchProvider, IServerBranchProvider>(
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<IGitWorkspaceService>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IBranchMatcher>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public async Task Get_StandaloneMode_ReturnsNull()
        {
            var configProvider = CreateConfigProvider(CreateBindingConfig(SonarLintMode.Standalone));
            var gitWorkspace = new Mock<IGitWorkspaceService>();
            var branchMatcher = new Mock<IBranchMatcher>();
            var logger = new TestLogger(logToConsole: true);
            var sonarQubeService = CreateSonarQubeService();

            var testSubject = CreateTestSubject(configProvider.Object,
                gitWorkspace.Object,
                branchMatcher: branchMatcher.Object,
                sonarQubeService: sonarQubeService.Object,
                logger: logger);

            var actual = await testSubject.GetServerBranchNameAsync(CancellationToken.None);
            
            actual.Should().BeNull();

            configProvider.VerifyAll();
            gitWorkspace.Invocations.Should().HaveCount(0);
            branchMatcher.Invocations.Should().HaveCount(0);
            sonarQubeService.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public async Task Get_NoGitRepo_ReturnsDefaultMainBranch()
        {
            var configProvider = CreateConfigProvider(CreateBindingConfig(SonarLintMode.Connected));
            var gitWorkspace = CreateGitWorkspace(repoRootToReturn: null);

            var branchMatcher = new Mock<IBranchMatcher>();
            var logger = new TestLogger(logToConsole: true);
            var createRepoOp = CreateCreateRepoOp(repository: null);

            var sonarQubeService = CreateSonarQubeService(mainBranchName: "main branch name");

            var testSubject = CreateTestSubject(configProvider.Object,
                gitWorkspace.Object,
                sonarQubeService: sonarQubeService.Object,
                branchMatcher: branchMatcher.Object,
                logger: logger,
                createRepoOp: createRepoOp.Object);

            var actual = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            actual.Should().Be("main branch name");
            
            configProvider.VerifyAll();
            gitWorkspace.Verify(x => x.GetRepoRoot(), Times.Once);
            gitWorkspace.Invocations.Should().HaveCount(1);
            branchMatcher.Invocations.Should().HaveCount(0);
            createRepoOp.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        [DataRow(SonarLintMode.LegacyConnected)]
        [DataRow(SonarLintMode.Connected)]
        public async Task Get_ConnectedModeAndHasGitRepo_HasMatchingBranch_ReturnsExpectedBranch(SonarLintMode mode)
        {
            var configProvider = CreateConfigProvider(CreateBindingConfig(mode, "my project key"));
            var gitWorkspace = CreateGitWorkspace("c:\\aaa\\reporoot");
            
            var branchMatcher = CreateBranchMatcher(branchToReturn: "my matching branch");
            var logger = new TestLogger(logToConsole: true);

            var repo = Mock.Of<IRepository>();
            var createRepoOp = CreateCreateRepoOp(repository: repo);
            var sonarQubeService = CreateSonarQubeService();

            var testSubject = CreateTestSubject(configProvider.Object,
                gitWorkspace.Object,
                branchMatcher: branchMatcher.Object,
                sonarQubeService: sonarQubeService.Object,
                logger: logger,
                createRepoOp: createRepoOp.Object);

            var actual = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            actual.Should().Be("my matching branch");
            logger.AssertPartialOutputStringExists("my matching branch");

            configProvider.VerifyAll();
            gitWorkspace.Verify(x => x.GetRepoRoot(), Times.Once);
            createRepoOp.Verify(x => x.Invoke("c:\\aaa\\reporoot"), Times.Once);
            branchMatcher.Verify(x => x.GetMatchingBranch("my project key", repo, It.IsAny<CancellationToken>()), Times.Once);

            gitWorkspace.Invocations.Should().HaveCount(1);
            branchMatcher.Invocations.Should().HaveCount(1);
            createRepoOp.Invocations.Should().HaveCount(1);
            sonarQubeService.Invocations.Should().HaveCount(0);
        }

        [TestMethod]
        public async Task Get_ConnectedModeAndHasGitRepo_NoMatchingBranch_ReturnsDefaultMainBranch()
        {
            var configProvider = CreateConfigProvider(CreateBindingConfig(SonarLintMode.Connected, "my project key"));
            var gitWorkspace = CreateGitWorkspace("x:\\");

            var branchMatcher = CreateBranchMatcher(branchToReturn: null);
            var logger = new TestLogger(logToConsole: true);

            var repo = Mock.Of<IRepository>();
            var createRepoOp = CreateCreateRepoOp(repository: repo);
            var sonarQubeService = CreateSonarQubeService(mainBranchName: "some main branch");

            var testSubject = CreateTestSubject(configProvider.Object,
                gitWorkspace.Object,
                branchMatcher: branchMatcher.Object,
                sonarQubeService: sonarQubeService.Object,
                logger:logger,
                createRepoOp:createRepoOp.Object);

            var actual = await testSubject.GetServerBranchNameAsync(CancellationToken.None);

            actual.Should().Be("some main branch");

            branchMatcher.Verify(x => x.GetMatchingBranch("my project key", repo, It.IsAny<CancellationToken>()), Times.Once);
        }

        private static ServerBranchProvider CreateTestSubject(
            IConfigurationProvider configurationProvider = null,
            IGitWorkspaceService gitWorkspaceService = null,
            ISonarQubeService sonarQubeService = null,
            IBranchMatcher branchMatcher = null,
            ILogger logger = null,
            ServerBranchProvider.CreateRepositoryObject createRepoOp = null)
        {
            configurationProvider ??= Mock.Of<IConfigurationProvider>();
            gitWorkspaceService ??= Mock.Of<IGitWorkspaceService>();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            branchMatcher ??= Mock.Of<IBranchMatcher>();
            logger ??= new TestLogger();
            createRepoOp ??= (string repoRoot) => null;

            var testSubject = new ServerBranchProvider(configurationProvider, gitWorkspaceService, sonarQubeService, branchMatcher, logger, createRepoOp);
            return testSubject;
        }

        private static BindingConfiguration CreateBindingConfig(SonarLintMode mode = SonarLintMode.Connected, string projectKey = "any")
            => new(new BoundSonarQubeProject { ProjectKey = projectKey }, mode, "any dir");

        private static Mock<IConfigurationProvider> CreateConfigProvider(BindingConfiguration config = null)
        {
            config ??= CreateBindingConfig();

            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(config);
            return configProvider;
        }

        private static Mock<IGitWorkspaceService> CreateGitWorkspace(string repoRootToReturn)
        {
            var gitWorkspace = new Mock<IGitWorkspaceService>();
            gitWorkspace.Setup(x => x.GetRepoRoot()).Returns(repoRootToReturn);
            return gitWorkspace;
        }

        private static Mock<ServerBranchProvider.CreateRepositoryObject> CreateCreateRepoOp(IRepository repository)
        {
            var createOp = new Mock<ServerBranchProvider.CreateRepositoryObject>();
            createOp.Setup(x => x.Invoke(It.IsAny<string>())).Returns(repository);
            return createOp;
        }

        private static Mock<IBranchMatcher> CreateBranchMatcher(string branchToReturn)
        {
            var branchMatcher = new Mock<IBranchMatcher>();
            branchMatcher.Setup(x => x.GetMatchingBranch(It.IsAny<string>(), It.IsAny<IRepository>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(branchToReturn);
            return branchMatcher;
        }

        private Mock<ISonarQubeService> CreateSonarQubeService(string mainBranchName = "some branch")
        {
            var sonarQubeService = new Mock<ISonarQubeService>();

            var serverBranches = new[]
            {
                new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, default),
                new SonarQubeProjectBranch(mainBranchName, true, default),
                new SonarQubeProjectBranch(Guid.NewGuid().ToString(), false, default)
            };

            sonarQubeService
                .Setup(x => x.GetProjectBranchesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serverBranches);

            return sonarQubeService;
        }
    }
}
