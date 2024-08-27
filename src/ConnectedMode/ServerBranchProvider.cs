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

using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client;

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
        private readonly ISonarQubeService sonarQubeService;
        private readonly IBranchMatcher branchMatcher;
        private readonly ILogger logger;
        private readonly CreateRepositoryObject createRepo;

        [ImportingConstructor]
        public ServerBranchProvider(IConfigurationProvider configurationProvider, 
            IGitWorkspaceService gitWorkspaceService, 
            ISonarQubeService sonarQubeService,
            IBranchMatcher branchMatcher, 
            ILogger logger)
            : this(configurationProvider, gitWorkspaceService, sonarQubeService, branchMatcher, logger, DoCreateRepo)
        {
        }

        internal /* for testing */ ServerBranchProvider(IConfigurationProvider configurationProvider,
            IGitWorkspaceService gitWorkspaceService,
            ISonarQubeService sonarQubeService,
            IBranchMatcher branchMatcher,
            ILogger logger,
            CreateRepositoryObject createRepo)
        {
            this.configurationProvider = configurationProvider;
            this.gitWorkspaceService = gitWorkspaceService;
            this.sonarQubeService = sonarQubeService;
            this.branchMatcher = branchMatcher;
            this.logger = logger;
            this.createRepo = createRepo;
        }

        public async Task<string> GetServerBranchNameAsync(CancellationToken token)
        {
            var config = configurationProvider.GetConfiguration();

            if (config.Mode == SonarLintMode.Standalone)
            {
                logger.LogVerbose(Resources.BranchProvider_NotInConnectedMode);
                return null;
            }

            var matchingBranchName = await CalculateMatchingBranchAsync(config, token);

            if (matchingBranchName == null)
            {
                logger.LogVerbose(Resources.BranchProvider_FailedToCalculateMatchingBranch);

                var remoteBranches = await sonarQubeService.GetProjectBranchesAsync(config.Project.ProjectKey, token);
                matchingBranchName = remoteBranches.First(rb => rb.IsMain).Name;
            }

            Debug.Assert(matchingBranchName != null);

            logger.WriteLine(Resources.BranchProvider_MatchingServerBranchName, matchingBranchName);
            return matchingBranchName;
        }

        private async Task<string> CalculateMatchingBranchAsync(LegacyBindingConfiguration config, CancellationToken token)
        {
            var gitRepoRoot = gitWorkspaceService.GetRepoRoot();

            if (gitRepoRoot == null)
            {
                logger.LogVerbose(Resources.BranchProvider_CouldNotDetectGitRepo);
                return null;
            }

            var repo = createRepo(gitRepoRoot);
            var branchName = await branchMatcher.GetMatchingBranch(config.Project.ProjectKey, repo, token);

            return branchName;
        }

        private static IRepository DoCreateRepo(string repoRootPath) => new Repository(repoRootPath);
    }
}
