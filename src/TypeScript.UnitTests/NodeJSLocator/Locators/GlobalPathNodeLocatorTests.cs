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
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.TypeScript.NodeJSLocator.Locators;

namespace SonarLint.VisualStudio.TypeScript.UnitTests.NodeJSLocator.Locators
{
    [TestClass]
    public class GlobalPathNodeLocatorTests
    {
        private readonly string programFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs\\node.exe");

        [TestMethod]
        public void Locate_FileNotFound_Null()
        {
            using var scope = CreateEnvironmentVariableScope();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(It.IsAny<string>())).Returns(false);

            var testSubject = CreateTestSubject(fileSystem.Object);
            var result = testSubject.Locate();

            result.Should().BeNull();
        }

        [TestMethod]
        public void Locate_FileFoundInPath_FilePath()
        {
            const string filePath = "c:\\test\\node.exe";
            using var scope = CreateEnvironmentVariableScope(Path.GetDirectoryName(filePath));

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(filePath)).Returns(true);

            var testSubject = CreateTestSubject(fileSystem.Object);
            var result = testSubject.Locate();

            result.Should().Be(filePath);

            fileSystem.VerifyAll();
            fileSystem.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Locate_FileNotFoundInPath_FileFoundInProgramFiles_FilePath()
        {
            using var scope = CreateEnvironmentVariableScope();

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(programFilesPath)).Returns(true);

            var testSubject = CreateTestSubject(fileSystem.Object);
            var result = testSubject.Locate();

            result.Should().Be(programFilesPath);

            fileSystem.Verify(x=> x.File.Exists(programFilesPath), Times.Once);
        }

        private GlobalPathNodeLocator CreateTestSubject(IFileSystem fileSystem)
        {
            return new GlobalPathNodeLocator(fileSystem, Mock.Of<ILogger>());
        }

        private EnvironmentVariableScope CreateEnvironmentVariableScope(string path = null)
        {
            var scope = new EnvironmentVariableScope();

            // remove any existing node.exe from machine's PATH for testing purposes
            scope.SetPath(path ?? " ");

            return scope;
        }
    }
}
