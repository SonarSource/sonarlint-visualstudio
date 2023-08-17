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

namespace SonarLint.VisualStudio.CFamily.CMake.UnitTests
{
    [TestClass]
    public class BuildConfigProviderTests
    {
        private const string ExpectedDefaultConfig = "x64-Debug";

        [TestMethod]
        public void Ctor_InvalidArgs_Throws()
        {
            Action act = () => new BuildConfigProvider(null, Mock.Of<IFileSystem>());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new BuildConfigProvider(Mock.Of<ILogger>(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Get_NullRootDirectory_Throws()
        {
            var testSubject = new BuildConfigProvider(Mock.Of<ILogger>());

            Action act = () => testSubject.GetActiveConfig(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rootDirectory");
        }

        [TestMethod]
        public void Get_MissingSettingsFile_ReturnsDefaultConfig()
        {
            const string rootDir = "c:\\aaa";
            var fullSettingsPath = CalcFullSettingsPath(rootDir);

            var fileSystem = CreateFileSystemWithNoFiles();
            var logger = new TestLogger(logToConsole: true);
            var testSubject = new BuildConfigProvider(logger, fileSystem.Object);

            testSubject.GetActiveConfig(rootDir).Should().Be(ExpectedDefaultConfig);

            AssertSettingsFileExistenceChecked(fileSystem, fullSettingsPath);
        }

        [TestMethod]
        [DataRow("not valid json", ExpectedDefaultConfig)]
        [DataRow("{ 'SettingIsMissing' : 'aaa'}", ExpectedDefaultConfig)]
        [DataRow("{ 'CurrentProjectSetting': 'my config' }", "my config")]
        public void Get_FileExists_ReturnsCorrectConfig(string settingsJson, string expectedConfig)
        {
            const string rootDir = "c:\\xxx";
            var fullSettingsPath = CalcFullSettingsPath(rootDir);

            var fileSystem = CreateFileSystemWithFile(fullSettingsPath, settingsJson);
            var logger = new TestLogger(logToConsole: true);
            var testSubject = new BuildConfigProvider(logger, fileSystem.Object);

            testSubject.GetActiveConfig(rootDir).Should().Be(expectedConfig);

            AssertSettingsFileExistenceChecked(fileSystem, fullSettingsPath);
            AssertSettingsFileRead(fileSystem, fullSettingsPath);
        }

        [TestMethod]
        public void Get_ReadingFile_NonCriticalError_DefaultConfigReturned()
        {
            bool exceptionThrown = false;

            const string rootDir = "c:\\xxx";
            var fullSettingsPath = CalcFullSettingsPath(rootDir);
            var fileSystem = CreateFileSystemWithExistingFile(fullSettingsPath);
            fileSystem.Setup(x => x.File.ReadAllText(It.IsAny<string>()))
                .Callback(() => exceptionThrown = true)
                .Throws(new IOException("thrown by a test"));

            var logger = new TestLogger(logToConsole: true);
            var testSubject = new BuildConfigProvider(logger, fileSystem.Object);

            testSubject.GetActiveConfig(rootDir).Should().Be(ExpectedDefaultConfig);

            exceptionThrown.Should().BeTrue();
        }

        [TestMethod]
        public void Get_ReadingFile_CriticalError_NotHandled()
        {
            const string rootDir = "c:\\xxx";
            var fullSettingsPath = CalcFullSettingsPath(rootDir);
            var fileSystem = CreateFileSystemWithExistingFile(fullSettingsPath);
            fileSystem.Setup(x => x.File.ReadAllText(It.IsAny<string>()))
                .Throws(new StackOverflowException("thrown by a test"));

            var logger = new TestLogger(logToConsole: true);
            var testSubject = new BuildConfigProvider(logger, fileSystem.Object);

            Action act = () => testSubject.GetActiveConfig(rootDir);

            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Be("thrown by a test");
        }

        private static Mock<IFileSystem> CreateFileSystemWithNoFiles()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);
            return fileSystem;
        }

        private static Mock<IFileSystem> CreateFileSystemWithFile(string fullPath, string content)
        {
            var fileSystem = CreateFileSystemWithExistingFile(fullPath);
            fileSystem.Setup(x => x.File.ReadAllText(fullPath)).Returns(content);
            return fileSystem;
        }

        private static Mock<IFileSystem> CreateFileSystemWithExistingFile(string fullPath)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(fullPath)).Returns(true);
            return fileSystem;
        }

        private static void AssertSettingsFileExistenceChecked(Mock<IFileSystem> fileSystem, string fullSettingsPath)
        {
            fileSystem.Verify(x => x.File.Exists(fullSettingsPath), Times.Once);
        }

        private static void AssertSettingsFileRead(Mock<IFileSystem> fileSystem, string fullPath) =>
            fileSystem.Verify(x => x.File.ReadAllText(fullPath), Times.Once);

        private static string CalcFullSettingsPath(string rootDirectory) =>
            Path.Combine(rootDirectory, ".vs", "ProjectSettings.json");
    }
}
