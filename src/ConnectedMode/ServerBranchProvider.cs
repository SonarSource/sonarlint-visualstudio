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

using System.ComponentModel.Composition;
using LibGit2Sharp;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Listener.Branch;

namespace SonarLint.VisualStudio.ConnectedMode;

[Export(typeof(IServerBranchProvider))]
internal class ServerBranchProvider : IServerBranchProvider
{
    /// <summary>
    /// Factory method to create and return an <see cref="IRepository"/> instance.
    /// </summary>
    /// <remarks>Only used for testing</remarks>
    internal delegate IRepository CreateRepositoryObject(string repoRootPath);

    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IGitWorkspaceService gitWorkspaceService;
    private readonly IBranchMatcher branchMatcher;
    private readonly ILogger logger;
    private readonly CreateRepositoryObject createRepo;

    [ImportingConstructor]
    public ServerBranchProvider(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IGitWorkspaceService gitWorkspaceService,
        IBranchMatcher branchMatcher,
        ILogger logger)
        : this(activeSolutionBoundTracker, gitWorkspaceService, branchMatcher, logger, DoCreateRepo)
    {
    }

    internal /* for testing */ ServerBranchProvider(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IGitWorkspaceService gitWorkspaceService,
        IBranchMatcher branchMatcher,
        ILogger logger,
        CreateRepositoryObject createRepo)
    {
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.gitWorkspaceService = gitWorkspaceService;
        this.branchMatcher = branchMatcher;
        this.logger = logger;
        this.createRepo = createRepo;
    }

    public string GetServerBranchName(List<RemoteBranch> branches)
    {
        var config = activeSolutionBoundTracker.CurrentConfiguration;

        if (config.Mode == SonarLintMode.Standalone)
        {
            logger.LogVerbose(Resources.BranchProvider_NotInConnectedMode);
            return null;
        }

        var matchingBranchName = CalculateMatchingBranch(config, branches);

        if (matchingBranchName == null)
        {
            logger.LogVerbose(Resources.BranchProvider_FailedToCalculateMatchingBranch);

            matchingBranchName = branches.First(rb => rb.IsMain).Name;
        }

        Debug.Assert(matchingBranchName != null);

        logger.WriteLine(Resources.BranchProvider_MatchingServerBranchName, matchingBranchName);
        return matchingBranchName;
    }

    private string CalculateMatchingBranch(BindingConfiguration config, List<RemoteBranch> branches)
    {
        var gitRepoRoot = gitWorkspaceService.GetRepoRoot();

        if (gitRepoRoot == null)
        {
            logger.LogVerbose(Resources.BranchProvider_CouldNotDetectGitRepo);
            return null;
        }

        var repo = createRepo(gitRepoRoot);
        var branchName = branchMatcher.GetMatchingBranch(config.Project.ServerProjectKey, repo, branches);

        return branchName;
    }

    private static IRepository DoCreateRepo(string repoRootPath) => new Repository(repoRootPath);
}
