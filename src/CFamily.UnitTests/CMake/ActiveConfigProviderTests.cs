/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class ActiveConfigProviderTests
    {
        private const string ExpectedDefaultConfig = "x64-Debug";

        [TestMethod]
        public void Ctor_InvalidArgs_Throws()
        {
            Action act = () => new ActiveConfigProvider(null, Mock.Of<IFileSystem>());
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            act = () => new ActiveConfigProvider(Mock.Of<ILogger>(), null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void Get_NullRootDirectory_Throws()
        {
            var testSubject = new ActiveConfigProvider(Mock.Of<ILogger>());

            Action act = () => testSubject.GetActiveConfig(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("rootDirectory");
        }

        [TestMethod]
        public void Get_MissingSettingsFile_ReturnsDefaultConfig()
        {
            var fileSystem = CreateFileSystemWithNoFiles();
            var logger = new TestLogger(logToConsole: true);
            var testSubject = new ActiveConfigProvider(logger, fileSystem.Object);

            testSubject.GetActiveConfig("any").Should().Be(ExpectedDefaultConfig);
            fileSystem.Verify(x => x.File.Exists(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        [DataRow("not valid json", ExpectedDefaultConfig)]
        [DataRow("{ 'SettingIsMissing' : 'aaa'}", ExpectedDefaultConfig)]
        [DataRow("{ 'CurrentProjectSetting': 'my config' }", "my config")]
        public void Get_FileExists_ReturnsCorrectConfig(string settingsJson, string expectedConfig)
        {
            var fileSystem = CreateFileSystemWithFile("c:\\aaa\\.vs\\ProjectSettings.json", settingsJson);
            var logger = new TestLogger(logToConsole: true);
            var testSubject = new ActiveConfigProvider(logger, fileSystem.Object);

            testSubject.GetActiveConfig("c:\\aaa").Should().Be(expectedConfig);
        }

        private static Mock<IFileSystem> CreateFileSystemWithNoFiles()
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);
            return fileSystem;
        }

        private static Mock<IFileSystem> CreateFileSystemWithFile(string fullPath, string content)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(fullPath)).Returns(true);
            fileSystem.Setup(x => x.File.ReadAllText(fullPath)).Returns(content);
            return fileSystem;
        }
    }
}
