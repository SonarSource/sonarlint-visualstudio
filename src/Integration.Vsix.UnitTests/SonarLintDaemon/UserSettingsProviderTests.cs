/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class UserSettingsProviderTests
    {

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

            // Act
            var testSubject = new UserSettingsProvider(new TestLogger(), fileMock.Object, CreateMockFileMonitor("nonexistentFile").Object);

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
        }

        [TestMethod]
        public void Ctor_ErrorLoadingSettings_ErrorSquashed_AndEmptySettingsReturned()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(true);
            fileMock.Setup(x => x.ReadAllText("settings.file")).Throws(new System.InvalidOperationException("custom error message"));

            var logger = new TestLogger();

            // Act
            var testSubject = new UserSettingsProvider(logger, fileMock.Object, CreateMockFileMonitor("settings.file").Object);

            // Assert
            CheckSettingsAreEmpty(testSubject.UserSettings);
            logger.AssertPartialOutputStringExists("custom error message");
        }

        [TestMethod]
        public void Ctor_ErrorLoadingSettings_CriticalErrorsAreNotSquashed()
        {
            // Arrange
            var fileMock = new Mock<IFile>();
            fileMock.Setup(x => x.Exists("settings.file")).Returns(true);
            fileMock.Setup(x => x.ReadAllText("settings.file")).Throws(new System.StackOverflowException("critical custom error message"));

            var logger = new TestLogger();

            // Act
            Action act = () => new UserSettingsProvider(logger, fileMock.Object, CreateMockFileMonitor("settings.file").Object);

            // Assert
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("critical custom error message");
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
}
";
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

        private static Mock<ISingleFileMonitor> CreateMockFileMonitor(string filePathToMonitor)
        {
            var mockSettingsFileMonitor = new Mock<ISingleFileMonitor>();
            mockSettingsFileMonitor.Setup(x => x.MonitoredFilePath).Returns(filePathToMonitor);
            return mockSettingsFileMonitor;
        }

        private static void CheckSettingsAreEmpty(UserSettings userSettings)
        {
            userSettings.Should().NotBeNull();
            userSettings.Rules.Should().NotBeNull();
            userSettings.Rules.Count.Should().Be(0);
        }
    }
}
