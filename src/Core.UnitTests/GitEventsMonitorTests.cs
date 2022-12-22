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
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class GitEventsMonitorTests
    {
        [TestMethod]
        public void HeadChanged_EventInvoked()
        {
            int counter = 0;

            var gitWorkspaceService = CreateWorkSpaceService("C:\\Some Path");

            var fileSystemWatcher = new Mock<IFileSystemWatcher>();

            var fileSystemWatcherFactory = new Mock<IFileSystemWatcherFactory>();
            fileSystemWatcherFactory.Setup(f => f.FromPath("C:\\Some Path\\.git")).Returns(fileSystemWatcher.Object);

            GitEventsMonitor testSubject = CreateTestSubject(gitWorkspaceService, fileSystemWatcherFactory);

            fileSystemWatcher.VerifySet(w => w.Filter = "HEAD", Times.Once);
            fileSystemWatcher.VerifySet(w => w.EnableRaisingEvents = true, Times.Once);

            testSubject.HeadChanged += (o, e) => counter++;

            fileSystemWatcher.Raise(w => w.Changed += null, null, null);

            counter.Should().Be(1);
        }       

        [TestMethod]
        public void NoGit_FileWatcherNotCreated()
        {
            var gitWorkspaceService = CreateWorkSpaceService(null);

            var fileSystemWatcherFactory = new Mock<IFileSystemWatcherFactory>();

            _ = CreateTestSubject(gitWorkspaceService, fileSystemWatcherFactory);

            fileSystemWatcherFactory.VerifyNoOtherCalls();
        }


        private IGitWorkspaceService CreateWorkSpaceService(string path)
        {
            var gitWorkspaceService = new Mock<IGitWorkspaceService>();
            gitWorkspaceService.Setup(s => s.GetRepoRoot()).Returns(path);
            return gitWorkspaceService.Object;
        }

        private GitEventsMonitor CreateTestSubject(IGitWorkspaceService gitWorkspaceService, Mock<IFileSystemWatcherFactory> fileSystemWatcherFactory)
        {
            return new GitEventsMonitor(gitWorkspaceService, fileSystemWatcherFactory.Object);
        }
    }
}
