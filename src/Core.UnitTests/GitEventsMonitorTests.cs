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
using System.IO.Abstractions;
using System.Threading.Tasks;
using System.Windows.Threading;
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

            var fileSystemWatcher = new Mock<IFileSystemWatcher>();
            var fileSystemWatcherFactory = CreateFileSystemWatcherFactory(fileSystemWatcher, "C:\\Some Path\\.git");

            var testSubject = CreateTestSubject("C:\\Some Path", fileSystemWatcherFactory);

            fileSystemWatcher.VerifySet(w => w.Filter = "HEAD", Times.Once);
            fileSystemWatcher.VerifySet(w => w.EnableRaisingEvents = true, Times.Once);

            testSubject.HeadChanged += (o, e) => counter++;

            fileSystemWatcher.Raise(w => w.Renamed += null, null, null);

            Dispatcher.CurrentDispatcher.Invoke(() => { }); // Force thread dispatcher to process other threads

            counter.Should().Be(1);
        }

        [TestMethod]
        public void HeadChanged_MonitorDisposed_FileSystemWatcherDisposed()
        {
            var fileSystemWatcher = new Mock<IFileSystemWatcher>();

            var fileSystemWatcherFactory = CreateFileSystemWatcherFactory(fileSystemWatcher, "C:\\Some Path\\.git");

            var testSubject = CreateTestSubject("C:\\Some Path", fileSystemWatcherFactory);

            testSubject.Dispose();

            fileSystemWatcher.Verify(w => w.Dispose(), Times.Once);
        }

        [TestMethod]
        public void HeadChanged_NoListeners_DoNotThrow()
        {
            var fileSystemWatcher = new Mock<IFileSystemWatcher>();

            var fileSystemWatcherFactory = CreateFileSystemWatcherFactory(fileSystemWatcher, "C:\\Some Path\\.git");

            _ = CreateTestSubject("C:\\Some Path", fileSystemWatcherFactory);

            Action act = () => fileSystemWatcher.Raise(w => w.Renamed += null, null, null);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void NoGit_FileWatcherNotCreated()
        {
            var fileSystemWatcherFactory = new Mock<IFileSystemWatcherFactory>();

            _ = CreateTestSubject(null, fileSystemWatcherFactory.Object);

            fileSystemWatcherFactory.VerifyNoOtherCalls();
        }

        private GitEventsMonitor CreateTestSubject(string repoFolder, IFileSystemWatcherFactory fileSystemWatcherFactory)
        {
            return new GitEventsMonitor(repoFolder, fileSystemWatcherFactory);
        }

        private IFileSystemWatcherFactory CreateFileSystemWatcherFactory(Mock<IFileSystemWatcher> fileSystemWatcher, string path)
        {
            var fileSystemWatcherFactory = new Mock<IFileSystemWatcherFactory>();
            fileSystemWatcherFactory.Setup(f => f.FromPath(path)).Returns(fileSystemWatcher.Object);
            return fileSystemWatcherFactory.Object;
        }
    }
}
