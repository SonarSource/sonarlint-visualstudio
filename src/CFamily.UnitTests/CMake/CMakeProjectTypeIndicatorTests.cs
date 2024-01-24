/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.CFamily.CMake.UnitTests
{
    [TestClass]
    public class CMakeProjectTypeIndicatorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CMakeProjectTypeIndicator, ICMakeProjectTypeIndicator>(
                MefTestHelpers.CreateExport<IFolderWorkspaceService>());
        }

        [TestMethod]
        public void IsCMake_NotOpenAsFolder_False()
        {
            var folderWorkspaceService = new Mock<IFolderWorkspaceService>();
            folderWorkspaceService.Setup(x => x.IsFolderWorkspace()).Returns(false);

            var testSubject = CreateTestSubject(folderWorkspaceService.Object);

            var result = testSubject.IsCMake();

            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsCMake_CouldNotRetrieveRootDirectory_ArgumentNullException()
        {
            var folderWorkspaceService = SetupOpenAsFolder(rootDirectory: null);

            var testSubject = CreateTestSubject(folderWorkspaceService.Object);

            Action act = () => testSubject.IsCMake();

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("path");
        }

        [TestMethod]
        [DataRow("c:\\some directory\\CMakeLists.txt")]
        [DataRow("c:\\some directory\\sub\\CMakeLists.txt")]
        [DataRow("c:\\some directory\\sub\\folder\\CMakeLists.txt")]
        public void IsCMake_OpenAsFolderProject_HasCmakeFiles_True(string cmakeListsLocation)
        {
            var folderWorkspaceService = SetupOpenAsFolder("c:\\some directory");
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory("c:\\some directory");
            fileSystem.AddFile(cmakeListsLocation, new MockFileData(""));

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, fileSystem);

            var actualLanguage = testSubject.IsCMake();

            actualLanguage.Should().BeTrue();
        }

        [TestMethod]
        public void IsCMake_OpenAsFolderProject_NoCmakeFiles_False()
        {
            var folderWorkspaceService = SetupOpenAsFolder("c:\\some directory");
            var fileSystem = new MockFileSystem();
            fileSystem.AddDirectory("c:\\some directory");
            fileSystem.AddFile("c:\\anotherRoot\\CMakeLists.txt", new MockFileData(""));

            var testSubject = CreateTestSubject(folderWorkspaceService.Object, fileSystem);

            var actualLanguage = testSubject.IsCMake();

            actualLanguage.Should().BeFalse();
        }

        private static CMakeProjectTypeIndicator CreateTestSubject(IFolderWorkspaceService folderWorkspaceService = null, IFileSystem fileSystem = null)
        {
            folderWorkspaceService ??= Mock.Of<IFolderWorkspaceService>();
            fileSystem ??= new MockFileSystem();

            return new CMakeProjectTypeIndicator(folderWorkspaceService, fileSystem);
        }

        private static Mock<IFolderWorkspaceService> SetupOpenAsFolder(string rootDirectory)
        {
            var folderWorkspaceService = new Mock<IFolderWorkspaceService>();

            folderWorkspaceService
                .Setup(x => x.IsFolderWorkspace())
                .Returns(true);

            folderWorkspaceService.Setup(x => x.FindRootDirectory())
                .Returns(rootDirectory);

            return folderWorkspaceService;
        }
    }
}
