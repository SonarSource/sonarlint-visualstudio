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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.UnitTests;
using static SonarLint.VisualStudio.ConnectedMode.BoundSolutionGitMonitor;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests
{
    [TestClass]
    public class BoundSolutionGitMonitorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BoundSolutionGitMonitor, IBoundSolutionGitMonitor>(
                MefTestHelpers.CreateExport<IGitWorkspaceService>());
        }

        [TestMethod]
        public void Initialize_ForwardsLowLevelEvent()
        {
            string repoPath = "some path";
            int counter = 0;

            var gitWorkspaceService = CreateGitWorkSpaceService(repoPath);

            var gitEventsMonitor = new Mock<IGitEvents>();

            GitEventFactory gitEventFactory = (string path) =>
            {
                if (path != repoPath)
                {
                    throw new Exception();
                }

                return gitEventsMonitor.Object;
            };
            BoundSolutionGitMonitor testSubject = CreateTestSubject(gitWorkspaceService, gitEventFactory);

            testSubject.HeadChanged += (o, e) => counter++;

            gitEventsMonitor.Raise(em => em.HeadChanged += null, null, null);

            counter.Should().Be(1);
        }

        [TestMethod]
        public void Refresh_ChangesLowLevelMonitor()
        {
            string originalPath = "original path";
            string newPath = "new path";

            int counter = 0;

            var gitWorkspaceService = CreateGitWorkSpaceService(originalPath);

            var originalEventsMonitor = new Mock<IGitEvents>();
            var newEventsMonitor = new Mock<IGitEvents>();

            GitEventFactory gitEventFactory = (string path) =>
            {
                if (path != originalPath && path != newPath)
                {
                    throw new Exception();
                }

                return path == originalPath ? originalEventsMonitor.Object : newEventsMonitor.Object;
            };

            BoundSolutionGitMonitor testSubject = CreateTestSubject(gitWorkspaceService, gitEventFactory);
            testSubject.HeadChanged += (o, e) => counter++;

            newEventsMonitor.Raise(em => em.HeadChanged += null, null, null);
            counter.Should().Be(0);

            originalEventsMonitor.Raise(em => em.HeadChanged += null, null, null);
            counter.Should().Be(1);

            gitWorkspaceService.Setup(ws => ws.GetRepoRoot()).Returns(newPath);

            testSubject.Refresh();

            originalEventsMonitor.Raise(em => em.HeadChanged += null, null, null);
            counter.Should().Be(1);

            newEventsMonitor.Raise(em => em.HeadChanged += null, null, null);
            counter.Should().Be(2);
        }

        private Mock<IGitWorkspaceService> CreateGitWorkSpaceService(string repoPath)
        {
            var gitWorkspaceService = new Mock<IGitWorkspaceService>();
            gitWorkspaceService.Setup(ws => ws.GetRepoRoot()).Returns(repoPath);
            return gitWorkspaceService;
        }
        
        private BoundSolutionGitMonitor CreateTestSubject(Mock<IGitWorkspaceService> gitWorkspaceService, GitEventFactory gitEventFactory)
        {
            return new BoundSolutionGitMonitor(gitWorkspaceService.Object, gitEventFactory);
        }
    }
}
