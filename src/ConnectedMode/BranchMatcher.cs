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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using SonarQube.Client;

namespace SonarLint.VisualStudio.ConnectedMode
{
    public interface IBranchMatcher
    {
        Task<string> GetMatchedBranch(string projectKey);
    }

    internal class BranchMatcher : IBranchMatcher
    {
        private readonly ISonarQubeService sonarQubeService;
        private readonly IRepository gitRepo;

        public BranchMatcher(ISonarQubeService sonarQubeService, IRepository gitRepo)
        {
            this.sonarQubeService = sonarQubeService;
            this.gitRepo = gitRepo;
        }

        public async Task<string> GetMatchedBranch(string projectKey)
        {
            var head = gitRepo.Head;

            if (head == null)
            {
                return null;
            }

            var remoteBranches = await sonarQubeService.GetProjectBranchesAsync(projectKey, CancellationToken.None);

            if(remoteBranches.Any(rb=> string.Equals(rb.Name, head.FriendlyName, StringComparison.InvariantCultureIgnoreCase)))
            {
                return head.FriendlyName;
            }

            string closestBranch = null;
            int closestDistance = int.MaxValue;

            foreach (var remoteBranch in remoteBranches)
            {                
                var localBranch = gitRepo.Branches.FirstOrDefault(r => string.Equals(r.FriendlyName, remoteBranch.Name, StringComparison.InvariantCultureIgnoreCase));
                
                if(localBranch == null) { continue; }

                var distance = GetDistance(head, localBranch);

                if(distance < closestDistance)
                {
                    closestBranch = localBranch.FriendlyName;
                    closestDistance = distance;
                }
            }
            
            if(closestBranch == null)
            {
                return remoteBranches.First(rb => rb.IsMain == true).Name;
            }
            return closestBranch;
        }

        //Commits are in descending order by time
        private int GetDistance(Branch head, Branch branch)
        {
            var headCommits = head.Commits.ToList();
            for (int i = 0; i < headCommits.Count; i++)
            {
                var commitID = headCommits[i].Id;

                var branchCommitIndex = branch.Commits.ToList().FindIndex(bc => bc.Id == commitID);

                if(branchCommitIndex == -1) { continue; }

                return i + branchCommitIndex;
            }

            return int.MaxValue;
        }
    }
}
