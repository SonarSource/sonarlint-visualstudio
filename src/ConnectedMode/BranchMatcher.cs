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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode
{
    public interface IBranchMatcher
    {
        /// <summary>
        /// Calculates the Sonar server branch that is the closest match to the head branch.
        /// Returns null if there are no head branches
        /// </summary>
        /// <remarks>
        /// Compares branches by name and calculates distances by the number of different commits.
        /// Defaults to branch marked as "main" when no match found.
        /// This is the same algorithm as the other SonarLint flavours (?except in how we treated branch histories.
        /// We treat branch histories as series of linear commits ordered by time, even if there are merges).
        /// </remarks>
        Task<string> GetMatchingBranch(string projectKey, IRepository gitRepo);
    }

    [Export(typeof(IBranchMatcher))]
    // Note: this class doesn't *need* to be a singleton; it doesn't hold any unique state.
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class BranchMatcher : IBranchMatcher
    {
        private readonly ISonarQubeService sonarQubeService;

        [ImportingConstructor]
        public BranchMatcher(ISonarQubeService sonarQubeService)
        {
            this.sonarQubeService = sonarQubeService;
        }

        public async Task<string> GetMatchingBranch(string projectKey, IRepository gitRepo)
        {
            Debug.Assert(sonarQubeService.IsConnected,
                "Not expecting GetMatchedBranch to be called unless we are in Connected Mode");

            var head = gitRepo.Head;

            if (head == null)
            {
                return null;
            }

            var remoteBranches = await sonarQubeService.GetProjectBranchesAsync(projectKey, CancellationToken.None);

            if (remoteBranches.Any(rb => string.Equals(rb.Name, head.FriendlyName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return head.FriendlyName;
            }

            string closestBranch = null;
            int closestDistance = int.MaxValue;

            Lazy<Commit[]> headCommits = new Lazy<Commit[]>(() => head.Commits.ToArray());

            foreach (var remoteBranch in remoteBranches)
            {
                var localBranch = gitRepo.Branches.FirstOrDefault(r => string.Equals(r.FriendlyName, remoteBranch.Name, StringComparison.InvariantCultureIgnoreCase));

                if (localBranch == null) { continue; }

                var distance = GetDistance(headCommits.Value, localBranch, closestDistance);

                if (distance < closestDistance)
                {
                    closestBranch = localBranch.FriendlyName;
                    closestDistance = distance;
                }
            }

            if (closestBranch == null)
            {
                return remoteBranches.First(rb => rb.IsMain).Name;
            }
            return closestBranch;
        }

        //Commits are in descending order by time
        private int GetDistance(Commit[] headCommits, Branch branch, int closestDistance)
        {
            for (int i = 0; i < headCommits.Length; i++)
            {
                if(i >= closestDistance) { break; }

                var commitID = headCommits[i].Id;
                var branchCommitIndex = GetIndexOfCommit(branch.Commits, commitID);

                if (branchCommitIndex == -1) { continue; }

                return i + branchCommitIndex;
            }

            return int.MaxValue;
        }

        internal /*For Testing*/ int GetIndexOfCommit(ICommitLog commits, ObjectId commitId)
        {
            int i = 0;
            foreach (var commit in commits)
            {
                if(commit.Id == commitId) { return i; }
                i++;
            }
            return -1;
        }
    }
}
