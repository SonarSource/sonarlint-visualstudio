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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using SonarLint.VisualStudio.ConnectedMode.UnitTests.LibGit2SharpWrappers;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class BranchMatcherTests
    {
        [TestMethod]
        public async Task GetMatchedBranch_ChooseBranchWithSameName()
        {
            var service = CreateService("master", "branch");

            var masterCommit = new CommitWrapper(1);
            var branchCommit = new CommitWrapper(2);

            var masterBranch = CreateBranch("master", masterCommit);
            var headBranch = CreateBranch("branch", branchCommit, masterCommit);

            var repo = CreateRepo(headBranch, masterBranch);

            var testSubject = new BranchMatcher(service, repo);

            var result = await testSubject.GetMatchedBranch("projectKey");

            result.Should().Be("branch");
        }

        [TestMethod]
        public async Task GetMatchedBranch_NoBranchWithSameName_ChooseClosestMatch()
        {
            var service = CreateService("master", "dev");

            var masterCommit = new CommitWrapper(1);
            var devBranchCommit = new CommitWrapper(2);
            var headBranchCommit = new CommitWrapper(3);

            var masterBranch = CreateBranch("master", masterCommit);
            var devBranch = CreateBranch("dev", devBranchCommit, masterCommit);
            var headBranch = CreateBranch("branch", headBranchCommit, devBranchCommit, masterCommit);

            var repo = CreateRepo(headBranch, masterBranch, devBranch);

            var testSubject = new BranchMatcher(service, repo);

            var result = await testSubject.GetMatchedBranch("projectKey");

            result.Should().Be("dev");
        }

        /*
         * Master
         * |->Commit1
         * |
         * |->Commit2
         * |
         * |->Commit3
         * |    |
         * |    |---->Branch1
         * |    |       |
         * |    |       |->Commit4
         * |    |       |
         * |    |       |->Commit5
         * |    |       |
         * |    |
         * |    |---->Branch2 (closest branch)
         * |    |       |
         * |    |       |->Commit6
         * |    |       |
         * |    |
         * |    |---->Branch3 (Head)
         * |    |       |
         * |    |       |->Commit7
         * |    |       |
         * |
         * |->Commit8
         * |
         * |->Commit9
         * |
         * |->Commit10
         * |
         */
        [TestMethod]
        public async Task GetMatchedBranch_MultipleCandidate_ChooseClosestMatch()
        {
            var service = CreateService("master", "Branch1", "Branch2");

            var commit1 = new CommitWrapper(1);
            var commit2 = new CommitWrapper(2);
            var commit3 = new CommitWrapper(3);
            var commit4 = new CommitWrapper(4);
            var commit5 = new CommitWrapper(5);
            var commit6 = new CommitWrapper(6);
            var commit7 = new CommitWrapper(7);
            var commit8 = new CommitWrapper(8);
            var commit9 = new CommitWrapper(9);
            var commit10 = new CommitWrapper(10);
            
            var masterBranch = CreateBranch("master", commit10, commit9, commit8, commit3, commit2, commit1);
            var branch1 = CreateBranch("branch1", commit5, commit4, commit3, commit2, commit1);
            var branch2 = CreateBranch("branch2", commit6, commit3, commit2, commit1);
            var branch3 = CreateBranch("branch3", commit7, commit3, commit2, commit1);

            var repo = CreateRepo(branch3, masterBranch, branch1, branch2);

            var testSubject = new BranchMatcher(service, repo);

            var result = await testSubject.GetMatchedBranch("projectKey");

            result.Should().Be("branch2");
        }

        [TestMethod]
        public async Task GetMatchedBranch_NoMatchingBranch_ChooseMain()
        {
            var service = CreateService("master", "branch1", "branch2");

            var masterCommit = new CommitWrapper(1);
            var branch1Commit = new CommitWrapper(2);
            var branch2Commit = new CommitWrapper(3);
            var headCommit = new CommitWrapper(4);

            var masterBranch = CreateBranch("master", masterCommit);
            var branch1 = CreateBranch("branch1", branch1Commit, masterCommit);
            var branch2 = CreateBranch("branch2", branch2Commit, masterCommit);
            var headBranch = CreateBranch("head", headCommit);

            var repo = CreateRepo(headBranch, masterBranch, branch1, branch2);

            var testSubject = new BranchMatcher(service, repo);

            var result = await testSubject.GetMatchedBranch("projectKey");

            result.Should().Be("master");
        }

        [TestMethod]
        public async Task GetMatchedBranch_RepoHasNoHead_ReturnsNull()
        {
            var testSubject = new BranchMatcher(Mock.Of<ISonarQubeService>(), Mock.Of<IRepository>());

            var result = await testSubject.GetMatchedBranch("projectKey");

            result.Should().BeNull();
        }

        private static IRepository CreateRepo(Branch headBranch, params Branch[] branches)
        {
            var branchList = new List<Branch>();
            branchList.Add(headBranch);
            branchList.AddRange(branches);

            var allBranches = new BranchCollectionWrapper(branchList);
            
            var repo = new Mock<IRepository>();

            repo.SetupGet(r => r.Head).Returns(headBranch);
            repo.SetupGet(r => r.Branches).Returns(allBranches);
            return repo.Object;
        }

        //Order of the commits matter. Commits should be in descending order. 
        private static BranchWrapper CreateBranch(string branchName, params CommitWrapper[] commits)
        {
            var masterBranch = new BranchWrapper(branchName, new CommitLogWrapper(commits));
            return masterBranch;
        }

        private static ISonarQubeService CreateService(string mainBranch, params string[] branches)
        {
            var service = new Mock<ISonarQubeService>();

            IList<SonarQubeProjectBranch> remoteBranches = new List<SonarQubeProjectBranch>();

            remoteBranches.Add(new SonarQubeProjectBranch(mainBranch, true, DateTime.Now));
            foreach (var branch in branches)
            {
                remoteBranches.Add(new SonarQubeProjectBranch(branch, true, DateTime.Now));
            }
            service.Setup(s => s.GetProjectBranchesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(remoteBranches));
            
            return service.Object;
        }
    }
}
