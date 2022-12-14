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

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode
{
    [Export(typeof(IServerBranchProvider))]
    internal class ServerBranchProvider : IServerBranchProvider
    {
        /// <summary>
        /// Factory method to create and return an <see cref="IRepository"/> instance.
        /// </summary>
        /// <remarks>Only used for testing</remarks>
        internal delegate IRepository CreateRepositoryObject(string repoRootPath);

        private readonly IConfigurationProvider configurationProvider;
        private readonly IGitWorkspaceService gitWorkspaceService;
        private readonly IBranchMatcher branchMatcher;
        private readonly ILogger logger;
        private readonly CreateRepositoryObject createRepo;

        [ImportingConstructor]
        public ServerBranchProvider(IConfigurationProvider configurationProvider, IGitWorkspaceService gitWorkspaceService, IBranchMatcher branchMatcher, ILogger logger)
            : this(configurationProvider, gitWorkspaceService, branchMatcher, logger, DoCreateRepo)
        {
        }

        internal /* for testing */ ServerBranchProvider(IConfigurationProvider configurationProvider,
            IGitWorkspaceService gitWorkspaceService,
            IBranchMatcher branchMatcher,
            ILogger logger,
            CreateRepositoryObject createRepo)
        {
            this.configurationProvider = configurationProvider;
            this.gitWorkspaceService = gitWorkspaceService;
            this.branchMatcher = branchMatcher;
            this.logger = logger;
            this.createRepo = createRepo;
        }

        public async Task<string> GetServerBranchNameAsync(CancellationToken token)
        {
            string branchName = null;

            var config = configurationProvider.GetConfiguration();
            if (config.Mode == SonarLintMode.Standalone)
            {
                logger.LogVerbose(Resources.BranchProvider_NotInConnectedMode);
                return null;
            }

            var gitRepoRoot = gitWorkspaceService.GetRepoRoot();
            if (gitRepoRoot == null)
            {
                logger.LogVerbose(Resources.BranchProvider_CouldNotDetectGitRepo);
                return null;
            }

            var repo = createRepo(gitRepoRoot);
            branchName = await branchMatcher.GetMatchingBranch(config.Project.ProjectKey, repo);

            logger.WriteLine(Resources.BranchProvider_MarchingServerBranchName, branchName ?? Resources.NullBranchName);

            return branchName;
        }

        private static IRepository DoCreateRepo(string repoRootPath) => new Repository(repoRootPath);
    }
}
