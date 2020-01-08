/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.Integration.Vsix.Analysis.UnitTests
{
    [TestClass]
    public class UserSettingsProviderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Ctor_NullArguments()
        {
            var mockSingleFileMonitor = new Mock<ISingleFileMonitor>();

            Action act = () => new UserSettingsProvider(null, new FileWrapper(), mockSingleFileMonitor.Object);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new UserSettingsProvider(new TestLogger(), null, mockSingleFileMonitor.Object);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileWrapper");

            act = () => new UserSettingsProvider(new TestLogger(), new FileWrapper(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("settingsFileMonitor");

        }

        [TestMethod]
        public void Ctor_NoSettingsFile_EmptySettingsReturned()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("nonExistentFile")).Returns(false);
            var testLogger = new TestLogger();

            // Act
            var testSubject = new UserSettingsProvider(testLogger, fileMock.Object, CreateMockFileMonitor("nonexistentFile").Object);

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
            testLogger.AssertOutputStringExists(AnalysisStrings.Settings_UsingDefaultSettings);
        }

        [TestMethod]
        public void Ctor_ErrorLoadingSettings_ErrorSquashed_AndEmptySettingsReturned()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(true);
            fileMock.Setup(x => x.ReadAllText("settings.file")).Throws(new System.InvalidOperationException("custom error message"));

            var logger = new TestLogger(logToConsole: true);

            // Act
            var testSubject = new UserSettingsProvider(logger, fileMock.Object, CreateMockFileMonitor("settings.file").Object);

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
            logger.AssertPartialOutputStringExists("custom error message");
        }

        [TestMethod]
        public void FileChanges_EventsRaised()
        {
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(true);
            var fileMonitorMock = CreateMockFileMonitor("settings.file");

            int settingsChangedEventCount = 0;

            var invalidSettingsData = @"NOT VALID JSON";
            var validSettingsData = @"{
    'sonarlint.rules': {
        'typescript:S2685': {
            'level': 'on'
        }
    }
}";
            var logger = new TestLogger();
            var testSubject = new UserSettingsProvider(logger, fileMock.Object, fileMonitorMock.Object);
            testSubject.SettingsChanged += (s, args) => settingsChangedEventCount++;
            logger.Reset();

            // 1. Simulate the file change when the file is invalid
            fileMock.Setup(x => x.ReadAllText("settings.file")).Returns(invalidSettingsData);
            fileMonitorMock.Raise(x => x.FileChanged += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            // Assert
            settingsChangedEventCount.Should().Be(1);
            CheckSettingsAreEmpty(testSubject.UserSettings);

            // 2. Simulate another event when the file is valid - valid settings should be returned
            fileMock.Setup(x => x.ReadAllText("settings.file")).Returns(validSettingsData);
            fileMonitorMock.Raise(x => x.FileChanged += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));

            // Assert
            settingsChangedEventCount.Should().Be(2);
            testSubject.UserSettings.Should().NotBeNull();
            testSubject.UserSettings.Rules.Should().NotBeNull();
            testSubject.UserSettings.Rules.Count.Should().Be(1);
        }

        [TestMethod]
        public void EnsureFileExists_CreatedIfMissing()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            var testSubject = new UserSettingsProvider(new TestLogger(), fileMock.Object, CreateMockFileMonitor("c:\\missingFile.txt").Object);
            fileMock.Reset();
            fileMock.Setup(x => x.Exists("c:\\missingFile.txt")).Returns(false);

            // Act
            testSubject.EnsureFileExists();
            
            // Assert
            fileMock.Verify(x => x.Exists("c:\\missingFile.txt"), Times.Once);
            fileMock.Verify(x => x.WriteAllText("c:\\missingFile.txt", It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void EnsureFileExists_NotCreatedIfExists()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            var testSubject = new UserSettingsProvider(new TestLogger(), fileMock.Object, CreateMockFileMonitor("c:\\subDir1\\existingFile.txt").Object);
            fileMock.Reset();
            fileMock.Setup(x => x.Exists("c:\\subDir1\\existingFile.txt")).Returns(true);

            // Act
            testSubject.EnsureFileExists();

            // Assert
            fileMock.Verify(x => x.Exists("c:\\subDir1\\existingFile.txt"), Times.Once);
            fileMock.Verify(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void ConstructAndDispose()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            var fileMonitorMock = CreateMockFileMonitor("c:\\aaa\\bbb\\file.txt");

            // 1. Construct
            var testSubject = new UserSettingsProvider(new TestLogger(), fileMock.Object, fileMonitorMock.Object);
            testSubject.SettingsFilePath.Should().Be("c:\\aaa\\bbb\\file.txt");
            fileMonitorMock.Verify(x => x.Dispose(), Times.Never);

            // 2. Dispose
            testSubject.Dispose();
            fileMonitorMock.Verify(x => x.Dispose(), Times.Once);
        }

        [TestMethod]
        public void RealFile_DisableRule_FileDoesNotExist_FileCreated()
        {
            var dir = CreateTestSpecificDirectory();
            var settingsFile = Path.Combine(dir, "settings.txt");

            var testLogger = new TestLogger(logToConsole: true);
            var testSubject = new UserSettingsProvider(testLogger, new FileWrapper(), new SingleFileMonitor(settingsFile, testLogger));

            // Sanity check of test setup
            testSubject.UserSettings.Rules.Count.Should().Be(0);
            File.Exists(settingsFile).Should().BeFalse();

            // Act - Disable a rule
            testSubject.DisableRule("cpp:S123");

            // Check the data on disc
            File.Exists(settingsFile).Should().BeTrue();

            var reloadedSettings = LoadSettings(settingsFile);
            reloadedSettings.Rules.Count.Should().Be(1);
            reloadedSettings.Rules["cpp:S123"].Level.Should().Be(RuleLevel.Off);
        }

        [TestMethod]
        public void RealFile_DisablePreviouslyEnabledRule()
        {
            var dir = CreateTestSpecificDirectory();
            var settingsFile = Path.Combine(dir, "settings.txt");

            var initialSettings = new UserSettings
            {
                Rules = new System.Collections.Generic.Dictionary<string, RuleConfig>
                {
                    { "javascript:S111", new RuleConfig { Level = RuleLevel.On } },
                    { "cpp:S111", new RuleConfig { Level = RuleLevel.On } },
                    { "xxx:S222", new RuleConfig { Level = RuleLevel.On } }
                }
            };

            SaveSettings(settingsFile, initialSettings);
            
            var testLogger = new TestLogger(logToConsole: true);
            var testSubject = new UserSettingsProvider(testLogger, new FileWrapper(), new SingleFileMonitor(settingsFile, testLogger));

            // Sanity check of test setup
            testSubject.UserSettings.Rules.Count.Should().Be(3);

            // Act - Disable a rule
            testSubject.DisableRule("cpp:S111");

            // Check the data on disc
            File.Exists(settingsFile).Should().BeTrue();

            var reloadedSettings = LoadSettings(settingsFile);
            reloadedSettings.Rules.Count.Should().Be(3);
            reloadedSettings.Rules["javascript:S111"].Level.Should().Be(RuleLevel.On);
            reloadedSettings.Rules["cpp:S111"].Level.Should().Be(RuleLevel.Off);
            reloadedSettings.Rules["xxx:S222"].Level.Should().Be(RuleLevel.On);
        }

        [TestMethod]
        public void SettingsChangeNotificationIsRaised()
        {
            int pause = System.Diagnostics.Debugger.IsAttached ? 20000 : 300;

            var fileName = "mySettings.xxx";

            // We're deliberately returning different data on each call to IFile.ReadAllText
            // so we can check that the provider is correctly reloading and using the file data,
            // and not re-using the in-memory version.
            var originalData = @"{}";
            var modifiedData = @"{
    'sonarlint.rules': {
        'typescript:S2685': {
            'level': 'on'
        }
    }
}";
            var fileMock = CreateMockFile(fileName, originalData);
            
            var singleFileMonitorMock = CreateMockFileMonitor(fileName);
            int eventCount = 0;
            var settingsChangedEventReceived = new ManualResetEvent(initialState: false);

            var testSubject = new UserSettingsProvider(new TestLogger(), fileMock.Object, singleFileMonitorMock.Object);
            testSubject.UserSettings.Rules.Count.Should().Be(0); // sanity check of setup
            
            testSubject.SettingsChanged += (s, args) =>
                {
                    eventCount++;
                    settingsChangedEventReceived.Set();
                };

            // 1. Disable a rule
            // Should trigger a save, but should not *directly* raise a "SettingsChanged" event
            testSubject.DisableRule("dummyRule");

            // Timing - unfortunately, we can't reliably test for the absence of an event. We
            // can only wait for a certain amount of time and check no events arrive in that period.
            System.Threading.Thread.Sleep(pause);
            eventCount.Should().Be(0);

            // 2. Now simulate a file-change event
            fileMock.Setup(x => x.ReadAllText(fileName)).Returns(modifiedData);
            singleFileMonitorMock.Raise(x => x.FileChanged += null, new FileSystemEventArgs(WatcherChangeTypes.Changed, "", ""));
            settingsChangedEventReceived.WaitOne(pause);

            // Check the settings change event was raised
            eventCount.Should().Be(1);

            // Check the data was actually reloaded from the file
            testSubject.UserSettings.Rules.Count.Should().Be(1);
            testSubject.UserSettings.Rules["typescript:S2685"].Level.Should().Be(RuleLevel.On);
        }

        private string CreateTestSpecificDirectory()
        {
            var dir = Path.Combine(TestContext.DeploymentDirectory, TestContext.TestName);
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void SaveSettings(string filePath, UserSettings userSettings)
        {
            var serializer = new UserSettingsSerializer(new FileWrapper(), new TestLogger());
            serializer.SafeSave(filePath, userSettings);
        }

        private UserSettings LoadSettings(string filePath)
        {
            var serializer = new UserSettingsSerializer(new FileWrapper(), new TestLogger());
            return serializer.SafeLoad(filePath);
        }

        private static Mock<ISingleFileMonitor> CreateMockFileMonitor(string filePathToMonitor)
        {
            var mockSettingsFileMonitor = new Mock<ISingleFileMonitor>();
            mockSettingsFileMonitor.Setup(x => x.MonitoredFilePath).Returns(filePathToMonitor);
            return mockSettingsFileMonitor;
        }

        private static Mock<IFile> CreateMockFile(string filePath, string contents)
        {
            var mockFile = new Mock<IFile>();
            mockFile.Setup(x => x.Exists(filePath)).Returns(true);
            mockFile.Setup(x => x.ReadAllText(filePath)).Returns(contents);
            return mockFile;
        }

        private static void CheckSettingsAreEmpty(UserSettings userSettings)
        {
            userSettings.Should().NotBeNull();
            userSettings.Rules.Should().NotBeNull();
            userSettings.Rules.Count.Should().Be(0);
        }
    }
}
