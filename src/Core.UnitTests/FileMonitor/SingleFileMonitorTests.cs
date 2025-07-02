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

using System.IO;
using System.IO.Abstractions;
using Moq;
using SonarLint.VisualStudio.Core.FileMonitor;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.FileMonitor
{
    [TestClass]
    public class SingleFileMonitorTests
    {
        private const string DirectoryName = "MonitoredDir";
        private const string FileName = "MonitoredFile.ext";
        private const string DirectoryFullPath = $"""C:\a\b\c\{DirectoryName}""";
        private const string FileFullPath = $"""{DirectoryFullPath}\{FileName}""";

        private TestLogger testLogger;
        private IFileSystem fileSystemMock;
        private IDirectoryInfo directoryInfoMock;
        private IFileSystemWatcherFactory watcherFactoryMock;
        private Mock<IFileSystemWatcher> watcherMock;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            testLogger = new TestLogger(logToConsole: true, logThreadId: true);
            CreateFileSystemMock(FileFullPath, FileName, DirectoryFullPath, DirectoryName);
            watcherFactoryMock = CreateFactoryAndWatcherMocks(out watcherMock);
        }

        [TestMethod]
        public void FileChangedEvent_NonCriticalExceptions_AreSuppressed()
        {
            using var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);
            testSubject.FileChanged += (s, args) => throw new InvalidOperationException("XXX non-critical exception");

            watcherMock
                .Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            testLogger.AssertPartialOutputStringExists("XXX non-critical exception");
            testSubject.MonitoredFilePath.Should().Be(FileFullPath);
        }

        [TestMethod]
        public void FileChangedEvent_CriticalExceptions_AreNotSuppressed()
        {
            using var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);
            testSubject.FileChanged += (s, args) => throw new StackOverflowException("YYY critical exception");

            Action act = () => watcherMock.Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            act.Should().ThrowExactly<StackOverflowException>().WithMessage("YYY critical exception");
        }

        [TestMethod]
        public void Ctor_DirectoryDoesNotExist_IsCreated()
        {
            directoryInfoMock.Exists.Returns(false);

            using var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);

            Received.InOrder(() =>
            {
                fileSystemMock.FileInfo.FromFileName(FileFullPath);
                directoryInfoMock.Create();
            });
            testLogger.AssertPartialOutputStringExists(DirectoryFullPath);
        }

        [TestMethod]
        public void Ctor_DirectoryDoesExist_IsNotCreated()
        {
            using var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);

            directoryInfoMock.DidNotReceiveWithAnyArgs().Create();
            testLogger.AssertPartialOutputStringDoesNotExist(DirectoryFullPath);
        }

        [TestMethod]
        public void FileChanged_EventOnlyRaisesEventsIfHasListeners()
        {
            var testDir = CreateTestSpecificDirectory();
            var filePathToMonitor = Path.Combine(testDir, "settingsFile.txt");
            EventHandler dummyHandler = (sender, args) => { };
            using var testSubject = (SingleFileMonitor)CreateRealTestSubject(filePathToMonitor, testLogger);
            testSubject.MonitoredFilePath.Should().Be(filePathToMonitor);

            // 1. Nothing registered -> underlying wrapper should not be raising events
            testSubject.FileWatcherIsRaisingEvents.Should().BeFalse();

            // 2. Register a listener -> start monitoring
            testSubject.FileChanged += dummyHandler;
            testSubject.FileWatcherIsRaisingEvents.Should().BeTrue();

            // 3. Unregister the listener -> stop monitoring
            testSubject.FileChanged -= dummyHandler;
            testSubject.FileWatcherIsRaisingEvents.Should().BeFalse();
        }

        [TestMethod]
        public void AfterDispose_EventsAreIgnored()
        {
            Action op = () => watcherMock.Raise(x => x.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, "", ""));
            DoRaise_Dispose_RaiseAgain(op);

            op = () => watcherMock.Raise(x => x.Deleted += null, new FileSystemEventArgs(WatcherChangeTypes.Created, "", ""));
            DoRaise_Dispose_RaiseAgain(op);

            op = () => watcherMock.Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Created, "", ""));
            DoRaise_Dispose_RaiseAgain(op);

            op = () => watcherMock.Raise(x => x.Renamed += null, new RenamedEventArgs(WatcherChangeTypes.Created, "", "", ""));
            DoRaise_Dispose_RaiseAgain(op);

            void DoRaise_Dispose_RaiseAgain(Action raiseEvent)
            {
                var eventCount = 0;
                var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);
                testSubject.FileChanged += (s, args) => eventCount++;

                // 1. First event is handled
                raiseEvent();
                eventCount.Should().Be(1);

                // 2. Dispose then re-raise
                testSubject.Dispose();
                raiseEvent();
                eventCount.Should().Be(1);
            }
        }

        [TestMethod]
        public void Dispose_WhileHandlingFileEvent_DisposedWatcherIsNotCalled()
        {
            // Timing issue - event notifications happen on a different thread so we could
            // be in the middle of handling an event when the monitor is disposed. This
            // shouldn't cause an error.
            var timeout = Debugger.IsAttached ? 1000 * 60 * 5 : 1000;
            var eventHandlerStartedEvent = new ManualResetEvent(false);
            var disposeCalledEvent = new ManualResetEvent(false);
            var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);

            // Stage 1: listen to events from the monitor
            var disposedEventSignalled = false;
            testSubject.FileChanged += (s, args) =>
            {
                //*******************************
                // Do not assert in this callback
                //*******************************
                // We're on a background thread so assertions won't cause a test failure

                // Signal that we are in the event handler, and block until dispose is called
                eventHandlerStartedEvent.Set();
                disposedEventSignalled = disposeCalledEvent.WaitOne(timeout);
            };

            // Stage 2: raise the file system event then block until we are in the event handler
            var eventHandlerMethodTask = System.Threading.Tasks.Task.Run((Action)(() =>
                watcherMock.Raise(x => x.Created += null, new FileSystemEventArgs(WatcherChangeTypes.Created, "", ""))));

            eventHandlerStartedEvent.WaitOne(timeout).Should().BeTrue();

            // Stage 3: dispose the monitor, unblock the background thread and wait until it has finished
            watcherMock.Reset(); // reset the counts of calls to the watcher

            testSubject.Dispose();
            disposeCalledEvent.Set();

            eventHandlerMethodTask.Wait(timeout).Should().BeTrue();
            disposedEventSignalled.Should().BeTrue();

            // Expect a single call to watcher.Dispose(), and the event handlers to be unregistered
            watcherMock.Verify(x => x.Dispose(), Times.Once);
            watcherMock.VerifyRemove(x => x.Changed -= It.IsAny<FileSystemEventHandler>(), Times.Once);
            watcherMock.VerifyRemove(x => x.Created -= It.IsAny<FileSystemEventHandler>(), Times.Once);
            watcherMock.VerifyRemove(x => x.Deleted -= It.IsAny<FileSystemEventHandler>(), Times.Once);
            watcherMock.VerifyRemove(x => x.Renamed -= It.IsAny<RenamedEventHandler>(), Times.Once);

            watcherMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_DirectoryNotEmpty_DoesNotDelete()
        {
            var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);
            directoryInfoMock.EnumerateFileSystemInfos().Returns([Substitute.For<IFileSystemInfo>()]);

            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            directoryInfoMock.DidNotReceiveWithAnyArgs().Delete(default);
        }

        [TestMethod]
        public void Dispose_CleansUpEmptyDirectory()
        {
            var testSubject = new  SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);
            directoryInfoMock.EnumerateFileSystemInfos().Returns([]);

            testSubject.Dispose();
            testSubject.Dispose();
            testSubject.Dispose();

            directoryInfoMock.Received(1).Delete(false);
        }

        [TestMethod]
        public void Dispose_DirectoryDeleteThrows_ExceptionLoggedAndIgnored()
        {
            directoryInfoMock.EnumerateFileSystemInfos().Returns([]);
            var exceptionMessage = "this is a test";
            directoryInfoMock.When(x => x.Delete(Arg.Any<bool>())).Throw(new Exception(exceptionMessage));
            var testSubject = new  SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);

            var act = () => testSubject.Dispose();

            act.Should().NotThrow();
            testLogger.AssertPartialOutputStringExists(exceptionMessage);
        }

        #region Simple file operations

        // Check simple file operations are handled and don't produce duplicates

        [TestMethod]
        public void RealFile_FileCreationIsTracked()
        {
            var testDir = CreateTestSpecificDirectory();
            var filePathToMonitor = Path.Combine(testDir, "settingsFile.txt");
            using var testSubject = CreateRealTestSubject(filePathToMonitor, testLogger);
            var testWrapper = CreateWaitableFileMonitor(testSubject);

            File.WriteAllText(filePathToMonitor, "initial text");
            testWrapper.WaitForEventAndThrowIfMissing();

            testWrapper.EventCount.Should().Be(1);
        }

        [TestMethod]
        public void RealFile_FileChangeIsTracked()
        {
            var testDir = CreateTestSpecificDirectory();
            var filePathToMonitor = Path.Combine(testDir, "settingsFile.txt");
            File.WriteAllText(filePathToMonitor, "contents");
            using var testSubject = CreateRealTestSubject(filePathToMonitor, testLogger);
            var testWrapper = CreateWaitableFileMonitor(testSubject);

            File.AppendAllText(filePathToMonitor, " more text");
            testWrapper.WaitForEventAndThrowIfMissing();

            testWrapper.EventCount.Should().Be(1);
        }

        [TestMethod]
        public void RealFile_FileDeletionIsTracked()
        {
            var testDir = CreateTestSpecificDirectory();
            var filePathToMonitor = Path.Combine(testDir, "settingsFile.txt");
            File.WriteAllText(filePathToMonitor, "contents");
            using var testSubject = CreateRealTestSubject(filePathToMonitor, testLogger);
            var testWrapper = CreateWaitableFileMonitor(testSubject);

            File.Delete(filePathToMonitor);
            testWrapper.WaitForEventAndThrowIfMissing();

            testWrapper.EventCount.Should().Be(1);
        }

        [TestMethod]
        public void RealFile_RenameFromTrackedFileName_IsTracked()
        {
            var testDir = CreateTestSpecificDirectory();
            var filePathToMonitor = Path.Combine(testDir, "settingsFile.txt");
            File.WriteAllText(filePathToMonitor, "contents");
            using var testSubject = CreateRealTestSubject(filePathToMonitor, testLogger);
            var testWrapper = CreateWaitableFileMonitor(testSubject);

            var renamedFile = Path.ChangeExtension(filePathToMonitor, "moved");
            File.Move(filePathToMonitor, renamedFile);
            testWrapper.WaitForEventAndThrowIfMissing();

            testWrapper.EventCount.Should().Be(1);
        }

        [TestMethod]
        public void RealFile_RenameToTrackedFileName_IsTracked()
        {
            var testDir = CreateTestSpecificDirectory();
            var filePathToMonitor = Path.Combine(testDir, "settingsFile.txt");
            var otherFilePath = Path.ChangeExtension(filePathToMonitor, "other");
            File.WriteAllText(otherFilePath, "contents");
            using var testSubject = CreateRealTestSubject(filePathToMonitor, testLogger);
            var testWrapper = CreateWaitableFileMonitor(testSubject);

            File.Move(otherFilePath, filePathToMonitor);
            testWrapper.WaitForEventAndThrowIfMissing();

            testWrapper.EventCount.Should().Be(1);
        }

        [TestMethod]
        public void Ctor_WatchesProvidedFile()
        {
            using var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, new TestLogger());

            testSubject.MonitoredFilePath.Should().Be(FileFullPath);
            watcherMock.VerifySet(mock => mock.Path = DirectoryFullPath, Times.Once());
            watcherMock.VerifySet(mock => mock.Filter = FileName, Times.Once());
            watcherMock.VerifySet(mock => mock.NotifyFilter = (NotifyFilters.CreationTime | NotifyFilters.LastWrite |
                                                               NotifyFilters.FileName | NotifyFilters.DirectoryName), Times.Once());
        }

        [TestMethod]
        public void File_DuplicateChangesSameTime_AreIgnored()
        {
            using var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);
            var testWrapper = CreateWaitableFileMonitor(testSubject);

            watcherMock.Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, FileFullPath, ""));
            watcherMock.Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, FileFullPath, ""));

            testWrapper.EventCount.Should().Be(1);
        }

        [TestMethod]
        public void File_DuplicateChangesDifferentTime_NotIgnored()
        {
            var dateTime = DateTime.Now;
            using var testSubject = new SingleFileMonitor(watcherFactoryMock, fileSystemMock, FileFullPath, testLogger);
            var testWrapper = CreateWaitableFileMonitor(testSubject);

            fileSystemMock.File.GetLastWriteTimeUtc(FileFullPath).Returns(dateTime);
            watcherMock.Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, DirectoryFullPath, FileName));

            fileSystemMock.File.GetLastWriteTimeUtc(FileFullPath).Returns(dateTime.AddMilliseconds(1));
            watcherMock.Raise(x => x.Changed += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, DirectoryFullPath, FileName));

            testWrapper.EventCount.Should().Be(2);
        }

        private void CreateFileSystemMock(
            string filePath,
            string fileName,
            string directoryPath,
            string directoryName)
        {
            fileSystemMock = Substitute.For<IFileSystem>();
            fileSystemMock.File.GetLastWriteTimeUtc(Arg.Any<string>()).Returns(DateTime.MaxValue);

            var fileInfoMock = Substitute.For<IFileInfo>();
            directoryInfoMock = Substitute.For<IDirectoryInfo>();

            fileInfoMock.FullName.Returns(filePath);
            fileInfoMock.Name.Returns(fileName);
            fileInfoMock.DirectoryName.Returns(directoryPath);

            directoryInfoMock.Exists.Returns(true);
            directoryInfoMock.FullName.Returns(directoryPath);
            directoryInfoMock.Name.Returns(directoryName);
            directoryInfoMock.EnumerateFileSystemInfos().Returns([fileInfoMock]);

            fileInfoMock.Directory.Returns(directoryInfoMock);

            fileSystemMock.FileInfo.FromFileName(filePath).Returns(fileInfoMock);
        }

        private static IFileSystemWatcherFactory CreateFactoryAndWatcherMocks(out Mock<IFileSystemWatcher> watcherMock)
        {
            // watcherMock is left as a Moq mock because FileSystemEventHandler does not inherit from EventHandler and NSubstitute's Raise.Event doesn't work
            watcherMock = new Mock<IFileSystemWatcher>();
            var watcherFactoryMock = Substitute.For<IFileSystemWatcherFactory>();
            watcherFactoryMock.CreateNew().Returns(watcherMock.Object);
            return watcherFactoryMock;
        }

        private static WaitableFileMonitor CreateWaitableFileMonitor(ISingleFileMonitor testSubject)
        {
            var testWrapper = new WaitableFileMonitor(testSubject);

            testWrapper.EventCount.Should().Be(0);
            return testWrapper;
        }

        #endregion Simple file operations

        private static ISingleFileMonitor CreateRealTestSubject(string filePathToMonitor, ILogger logger)
        {
            return new SingleFileMonitorFactory(logger).Create(filePathToMonitor);
        }

        private string CreateTestSpecificDirectory()
        {
            var testPath = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.CreateDirectory(testPath);
            return testPath;
        }

        /// <summary>
        /// Decorator class that adds waiting for events with a timeout to
        /// make testing simpler and more reliable
        /// </summary>
        private class WaitableFileMonitor
        {
            private readonly ISingleFileMonitor singleFileMonitor;

            public WaitableFileMonitor(ISingleFileMonitor singleFileMonitor)
            {
                this.singleFileMonitor = singleFileMonitor;
                this.singleFileMonitor.FileChanged += OnFileChanged;
            }

            private readonly AutoResetEvent signal = new(false);

            public int EventCount { get; private set; }

            public void WaitForEventAndThrowIfMissing()
            {
                var timeout = Debugger.IsAttached ? 1000 * 60 * 5 : 1000;
                var signaled = signal.WaitOne(timeout);
                signaled.Should().Be(true); // throw if we timed out
            }

            private void OnFileChanged(object sender, EventArgs args)
            {
                EventCount++;
                signal.Set();
            }
        }
    }
}
