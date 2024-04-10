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

using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class FolderWorkspaceServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<FolderWorkspaceService, IFolderWorkspaceService>(
                MefTestHelpers.CreateExport<ISolutionInfoProvider>());
        }

        [TestMethod]
        public void FindRootDirectory_OpenAsFolderProject_ProblemRetrievingSolutionDirectory_Null()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(isOpenAsFolder: true, solutionDirectory: null);

            var testSubject = CreateTestSubject(solutionInfoProvider);

            var result = testSubject.FindRootDirectory();

            result.Should().BeNull();
        }

        [TestMethod]
        public void FindRootDirectory_OpenAsFolderProject_SucceededRetrievingSolutionDirectory_SolutionDirectory()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(isOpenAsFolder: true, solutionDirectory: "some directory");

            var testSubject = CreateTestSubject(solutionInfoProvider);

            var result = testSubject.FindRootDirectory();

            result.Should().Be("some directory");
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void IsFolderWorkspace_GetsResultFromSolutionInfoProvider(bool isOpenAsFolder)
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(isOpenAsFolder, "some directory");

            var testSubject = CreateTestSubject(solutionInfoProvider);

            var result = testSubject.IsFolderWorkspace();

            result.Should().Be(isOpenAsFolder);
        }

        [TestMethod]
        public void ListFiles_FolderWorkPlace_ListFilesFilteringNodeModules()
        {
            var rootFolder = "C:\\root";
            var fileSystem = CreateFileSystem(rootFolder, "C:\\root\\File1.cs", "C:\\root\\File2.cs", "C:\\Folder\\File3.cs", "C:\\node_modules\\Module1\\ModuleFile.js");
            var solutionInfoProvider = CreateSolutionInfoProvider(true, rootFolder);

            var testSubject = CreateTestSubject(solutionInfoProvider, fileSystem);

            var result = testSubject.ListFiles();

            result.Should().BeEquivalentTo(["C:\\root\\File1.cs", "C:\\root\\File2.cs", "C:\\Folder\\File3.cs"]);
        }

        [TestMethod]
        public void ListFiles_NoWorkSpaceFolder_ShouldReturnEmpty()
        {
            var fileSystem = CreateFileSystem("some root");
            var solutionInfoProvider = CreateSolutionInfoProvider(false, "some root");

            var testSubject = CreateTestSubject(solutionInfoProvider, fileSystem);

            var result = testSubject.ListFiles();

            result.Should().BeEmpty();
            fileSystem.Directory.ReceivedWithAnyArgs(0).EnumerateFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>());
        }

        private static IFileSystem CreateFileSystem(string rootFolder, params string[] returnFolders)
        {
            var directory = Substitute.For<IDirectory>();
            directory.EnumerateFiles(rootFolder, "*", SearchOption.AllDirectories).Returns(returnFolders);
            var fileSystem = Substitute.For<IFileSystem>();
            fileSystem.Directory.Returns(directory);
            return fileSystem;
        }

        private FolderWorkspaceService CreateTestSubject(ISolutionInfoProvider solutionInfoProvider, IFileSystem fileSystem = null)
        {
            fileSystem ??= Substitute.For<IFileSystem>();

            return new FolderWorkspaceService(solutionInfoProvider, fileSystem);
        }

        private static ISolutionInfoProvider CreateSolutionInfoProvider(bool isOpenAsFolder, string solutionDirectory)
        {
            var solutionInfoProvider = Substitute.For<ISolutionInfoProvider>();
            solutionInfoProvider.GetSolutionDirectory().Returns(solutionDirectory);
            solutionInfoProvider.IsFolderWorkspace().Returns(isOpenAsFolder);
            return solutionInfoProvider;
        }
    }
}
