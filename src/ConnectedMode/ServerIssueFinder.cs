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
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Suppressions;
using SonarQube.Client;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode
{
    public interface IServerIssueFinder
    {
        Task<SonarQubeIssue> FindServerIssueAsync(IFilterableIssue localIssue, CancellationToken token);
    }

    [Export(typeof(IServerIssueFinder))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class ServerIssueFinder : IServerIssueFinder
    {
        private readonly IProjectRootCalculator projectRootCalculator;
        private readonly IIssueMatcher issueMatcher;
        private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private readonly IStatefulServerBranchProvider serverBranchProvider;
        private readonly ISonarQubeService sonarQubeService;
        private readonly IThreadHandling threadHandling;

        [ImportingConstructor]
        public ServerIssueFinder(IProjectRootCalculator projectRootCalculator, IIssueMatcher issueMatcher,
            IActiveSolutionBoundTracker activeSolutionBoundTracker, IStatefulServerBranchProvider serverBranchProvider,
            ISonarQubeService sonarQubeService, IThreadHandling threadHandling)
        {
            this.projectRootCalculator = projectRootCalculator;
            this.issueMatcher = issueMatcher;
            this.activeSolutionBoundTracker = activeSolutionBoundTracker;
            this.serverBranchProvider = serverBranchProvider;
            this.sonarQubeService = sonarQubeService;
            this.threadHandling = threadHandling;
        }

        public async Task<SonarQubeIssue> FindServerIssueAsync(IFilterableIssue localIssue, CancellationToken token)
        {
            threadHandling.ThrowIfOnUIThread();
            
            var bindingConfiguration = activeSolutionBoundTracker.CurrentConfiguration;

            if (bindingConfiguration.Mode == SonarLintMode.Standalone)
            {
                return null;
            }
            
            var projectRoot = await projectRootCalculator.CalculateBasedOnLocalPathAsync(localIssue.FilePath, token);
            
            if (projectRoot == null)
            {
                return null;
            }

            var componentKey = ComponentKeyGenerator.GetComponentKey(localIssue.FilePath,
                projectRoot,
                bindingConfiguration.Project.ProjectKey);

            var serverIssues = await sonarQubeService.GetIssuesForComponentAsync(
                bindingConfiguration.Project.ProjectKey,
                await serverBranchProvider.GetServerBranchNameAsync(token),
                componentKey,
                localIssue.RuleId,
                token);

            return issueMatcher.GetFirstLikelyMatchFromSameFileOrNull(localIssue, serverIssues);
        }
    }
}
