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
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Roslyn.Suppressions.Settings.Cache;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class RoslynSettingsFileWatcherTests
    {
        [TestMethod]
        public void Ctor_RegisterToFileWatcherEvents()
        {
            var fileSystemWatcher = CreateFileSystemWatcher();

            CreateTestSubject(fileSystemWatcher: fileSystemWatcher.Object);

            fileSystemWatcher.VerifyAdd(x => x.Created += It.IsAny<FileSystemEventHandler>(), Times.Once);
            fileSystemWatcher.VerifyAdd(x => x.Deleted += It.IsAny<FileSystemEventHandler>(), Times.Once);
            fileSystemWatcher.VerifyAdd(x => x.Changed += It.IsAny<FileSystemEventHandler>(), Times.Once);
            fileSystemWatcher.VerifySet(x=> x.Filter = "*.json", Times.Once);
            fileSystemWatcher.VerifySet(x=> x.EnableRaisingEvents = true, Times.Once);

            fileSystemWatcher.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromFileWatcherEvents()
        {
            var fileSystemWatcher = CreateFileSystemWatcher();

            var testSubject = CreateTestSubject(fileSystemWatcher: fileSystemWatcher.Object);

            fileSystemWatcher.VerifyRemove(x => x.Created -= It.IsAny<FileSystemEventHandler>(), Times.Never);
            fileSystemWatcher.VerifyRemove(x => x.Deleted -= It.IsAny<FileSystemEventHandler>(), Times.Never);
            fileSystemWatcher.VerifyRemove(x => x.Changed -= It.IsAny<FileSystemEventHandler>(), Times.Never);
            fileSystemWatcher.Verify(x => x.Dispose(), Times.Never);

            testSubject.Dispose();

            fileSystemWatcher.VerifyRemove(x => x.Created -= It.IsAny<FileSystemEventHandler>(), Times.Once);
            fileSystemWatcher.VerifyRemove(x => x.Deleted -= It.IsAny<FileSystemEventHandler>(), Times.Once);
            fileSystemWatcher.VerifyRemove(x => x.Changed -= It.IsAny<FileSystemEventHandler>(), Times.Once);
            fileSystemWatcher.Verify(x=> x.Dispose(), Times.Once);
        }

        [TestMethod]
        [Description("It is a deliberate design choice to let the caller handle initialization exceptions.")]
        public void Ctor_FailedToCreateFileSystemWatcher_ExceptionsNotHandled()
        {
            var fileSystemWatcher = new Mock<IFileSystemWatcher>();
            fileSystemWatcher
                .SetupSet(x => x.EnableRaisingEvents = true)
                .Throws(new NotImplementedException("some exception"));

            Action act = () => CreateTestSubject(fileSystemWatcher: fileSystemWatcher.Object);

            act.Should().Throw<NotImplementedException>().And.Message.Should().Be("some exception");
        }

        [TestMethod]
        [DataRow(WatcherChangeTypes.Created)]
        [DataRow(WatcherChangeTypes.Deleted)]
        [DataRow(WatcherChangeTypes.Changed)]
        public void OnFileSystemChanges_SettingsKeyNotFound_CacheNotInvalidated(WatcherChangeTypes changeType)
        {
            var fileSystemWatcher = new Mock<IFileSystemWatcher>();
            var settingsCache = new Mock<ISettingsCache>();

            CreateTestSubject(settingsCache.Object, fileSystemWatcher.Object);

            settingsCache.Invocations.Count.Should().Be(0);

            RaiseFileSystemEvent(fileSystemWatcher, changeType, null);

            settingsCache.Invocations.Count.Should().Be(0);
        }

        [TestMethod]
        [DataRow(WatcherChangeTypes.Created)]
        [DataRow(WatcherChangeTypes.Deleted)]
        [DataRow(WatcherChangeTypes.Changed)]
        public void OnFileSystemChanges_CacheInvalidated(WatcherChangeTypes changeType)
        {
            var fileSystemWatcher = new Mock<IFileSystemWatcher>();
            var settingsCache = new Mock<ISettingsCache>();

            CreateTestSubject(settingsCache.Object, fileSystemWatcher.Object);

            settingsCache.Invocations.Count.Should().Be(0);

            RaiseFileSystemEvent(fileSystemWatcher, changeType, "c:\\a\\b\\c\\some file.txt");

            settingsCache.Verify(x=> x.Invalidate("some file"), Times.Once);
            settingsCache.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(WatcherChangeTypes.Created)]
        [DataRow(WatcherChangeTypes.Deleted)]
        [DataRow(WatcherChangeTypes.Changed)]
        public void OnFileSystemChanges_ExceptionInvalidatingCache_ExceptionHandled(WatcherChangeTypes changeType)
        {
            var fileSystemWatcher = new Mock<IFileSystemWatcher>();

            var settingsCache = new Mock<ISettingsCache>();
            settingsCache
                .Setup(x => x.Invalidate("some file"))
                .Throws(new NotImplementedException("some exception"));

            var logger = new TestLogger();

            CreateTestSubject(settingsCache.Object, fileSystemWatcher.Object, logger: logger);

            settingsCache.Invocations.Count.Should().Be(0);

            Action act = () => RaiseFileSystemEvent(fileSystemWatcher, changeType, "c:\\a\\b\\c\\some file.txt");

            act.Should().NotThrow();
            logger.AssertPartialOutputStringExists("some exception");
        }

        private static Mock<IFileSystemWatcher> CreateFileSystemWatcher()
        {
            var fileSystemWatcher = new Mock<IFileSystemWatcher>();

            fileSystemWatcher.SetupAdd(x => x.Created += null);
            fileSystemWatcher.SetupAdd(x => x.Deleted += null);
            fileSystemWatcher.SetupAdd(x => x.Changed += null);

            fileSystemWatcher.SetupRemove(x => x.Created -= null);
            fileSystemWatcher.SetupRemove(x => x.Deleted -= null);
            fileSystemWatcher.SetupRemove(x => x.Changed -= null);

            return fileSystemWatcher;
        }

        private static void RaiseFileSystemEvent(Mock<IFileSystemWatcher> fileSystemWatcher, WatcherChangeTypes changeType, string fileName)
        {
            var eventArgs = new FileSystemEventArgs(changeType, "some dir", fileName);

            switch (changeType)
            {
                case WatcherChangeTypes.Created:
                    fileSystemWatcher.Raise(x => x.Created += null, eventArgs);
                    break;
                case WatcherChangeTypes.Deleted:
                    fileSystemWatcher.Raise(x => x.Deleted += null, eventArgs);
                    break;
                case WatcherChangeTypes.Changed:
                    fileSystemWatcher.Raise(x => x.Changed += null, eventArgs);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(changeType), changeType, null);
            }
        }

        private static SuppressedIssuesFileWatcher CreateTestSubject(ISettingsCache settingsCache = null,
            IFileSystemWatcher fileSystemWatcher = null,
            IFileSystem fileSystem = null,
            ILogger logger = null)
        {
            fileSystemWatcher ??= Mock.Of<IFileSystemWatcher>();
            fileSystem ??= Mock.Of<IFileSystem>();
            logger ??= Mock.Of<ILogger>();

            Mock.Get(fileSystem)
                .Setup(x => x.FileSystemWatcher.FromPath(RoslynSettingsFileInfo.Directory))
                .Returns(fileSystemWatcher);

            Mock.Get(fileSystem).Setup(x => x.Directory.CreateDirectory(RoslynSettingsFileInfo.Directory));

            settingsCache ??= Mock.Of<ISettingsCache>();

            return new SuppressedIssuesFileWatcher(settingsCache, logger, fileSystem);
        }
    }
}
