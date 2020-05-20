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
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Binding;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class AdditionalFileConflictCheckerTests
    {
        private Mock<IFileSystem> fileSystemMock;
        private AdditionalFileConflictChecker testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new Mock<IFileSystem>();

            testSubject = new AdditionalFileConflictChecker(fileSystemMock.Object);
        }

        [TestMethod]
        public void Ctor_NullFileSystem_ArgumentNullException()
        {
            Action act = () => new AdditionalFileConflictChecker((IFileSystem) null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");
        }

        [TestMethod]
        public void HasConflictingAdditionalFile_ConflictingFileFoundUnderRootFolder_True()
        {
            const string conflictingFilePath = "c:\\dummy\\SonarLint.xml";
            SetupFileUnderRootFolder(conflictingFilePath, exists:true);

            var projectMock = new ProjectMock("c:\\dummy\\test.csproj");

            var result = testSubject.HasConflictingAdditionalFile(projectMock, "SonarLint.xml", out var conflictingPath);

            result.Should().BeTrue();
            conflictingPath.Should().Be(conflictingFilePath);
        }

        [TestMethod]
        [DataRow("AdditionalFiles", true)]
        [DataRow("Foo", false)]
        [DataRow("Content", false)]
        public void HasConflictingAdditionalFile_ConflictingFileIsALink_ReturnsIfFileIsAdditionalFile(string itemType, bool isConflicting)
        {
            const string conflictingFilePath = "c:\\dummy\\SonarLint.xml";
            SetupFileUnderRootFolder(conflictingFilePath, exists: false);

            var projectMock = new ProjectMock("c:\\dummy\\test.csproj");
            projectMock.AddProjectItem("C:\\test\\folder\\sonarlint.xml", itemType);

            var result = testSubject.HasConflictingAdditionalFile(projectMock, "SonarLint.xml", out var conflictingPath);

            result.Should().Be(isConflicting);
            conflictingPath.Should().Be(isConflicting ? "C:\\test\\folder\\sonarlint.xml" : "");
        }

        [TestMethod]
        [DataRow("AdditionalFiles", true)]
        [DataRow("Foo", false)]
        [DataRow("Content", false)]
        public void HasConflictingAdditionalFile_ConflictingFileFoundInProject_ReturnsIfFileIsAdditionalFile(string itemType, bool isConflicting)
        {
            const string conflictingFilePath = "c:\\dummy\\SonarLint.xml";
            SetupFileUnderRootFolder(conflictingFilePath, exists: false);

            var projectMock = new ProjectMock("c:\\dummy\\test.csproj");
            projectMock.AddProjectItem("sonarLINT.xml", itemType);

            var result = testSubject.HasConflictingAdditionalFile(projectMock, "SonarLint.xml", out var conflictingPath);
            
            result.Should().Be(isConflicting);
            conflictingPath.Should().Be(isConflicting ? "c:\\dummy\\sonarLINT.xml" : "");
        }

        [TestMethod]
        [DataRow("MySonarLint.xml")]
        [DataRow("foo.xml")]
        public void HasConflictingAdditionalFile_NoConflictingFile_False(string otherFileName)
        {
            const string conflictingFilePath = "c:\\dummy\\SonarLint.xml";
            SetupFileUnderRootFolder(conflictingFilePath, exists: false);

            var projectMock = new ProjectMock("c:\\dummy\\test.csproj");
            projectMock.AddProjectItem(otherFileName, "AdditionalFiles");

            var result = testSubject.HasConflictingAdditionalFile(projectMock, "SonarLint.xml", out var conflictingPath);

            result.Should().BeFalse();
            conflictingPath.Should().BeEmpty();
        }

        private void SetupFileUnderRootFolder(string conflictingFilePath, bool exists)
        {
            fileSystemMock.Setup(x => x.File.Exists(conflictingFilePath)).Returns(exists);
        }
    }
}
