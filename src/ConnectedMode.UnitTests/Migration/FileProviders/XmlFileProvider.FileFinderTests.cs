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
using SonarLint.VisualStudio.ConnectedMode.Migration.FileProviders;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Migration.FileProviders
{
    [TestClass]
    public class FileFinderTests
    {
        [TestMethod]
        [DataRow("a root")]
        [DataRow("another root")]
        public void GetFiles_ExpectedRootOptionIsUsed(string expectedRoot)
        {
            var fileSystem = CreateFileSystem();
            var testSubject = CreateTestSubject(fileSystem.Object);

            _ = testSubject.GetFiles(expectedRoot, SearchOption.AllDirectories, "any");

            var x  = It.IsAny<string>();
            CheckSearch(fileSystem, expectedRoot, SearchOption.AllDirectories, "any");
        }

        [TestMethod]
        [DataRow(SearchOption.AllDirectories)]
        [DataRow(SearchOption.TopDirectoryOnly)]
        public void GetFiles_ExpectedSearchOptionIsUsed(SearchOption searchOption)
        {
            var fileSystem = CreateFileSystem();
            var testSubject = CreateTestSubject(fileSystem.Object);

            _ = testSubject.GetFiles("any", searchOption, "any");

            CheckSearch(fileSystem, "any",searchOption, "any");
        }

        [TestMethod]
        public void GetFiles_MultiplePatterns_AllPatternsSearched()
        {
            var fileSystem = CreateFileSystem();

            var testSubject = CreateTestSubject(fileSystem.Object);

            var actual = testSubject.GetFiles("root", SearchOption.TopDirectoryOnly,
                "pattern1", "pattern2", "pattern3");

            actual.Should().BeEmpty();
            CheckSearch(fileSystem, "root", SearchOption.TopDirectoryOnly, "pattern1");
            CheckSearch(fileSystem, "root", SearchOption.TopDirectoryOnly, "pattern2");
            CheckSearch(fileSystem, "root", SearchOption.TopDirectoryOnly, "pattern3");
        }

        [TestMethod]
        public void GetFiles_NoFiles_ReturnsEmpty()
        {
            var fileSystem = CreateFileSystem();

            var testSubject = CreateTestSubject(fileSystem.Object);

            var actual = testSubject.GetFiles("root", SearchOption.TopDirectoryOnly, "pattern1");

            actual.Should().BeEmpty();
            CheckSearch(fileSystem, "root", SearchOption.TopDirectoryOnly, "pattern1");
        }

        [TestMethod]
        public void GetFiles_ExpectedFilesReturned()
        {
            var filesToReturn = new string[] { "file1", "file2", "file3" };
            var fileSystem = CreateFileSystem(filesToReturn);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var actual = testSubject.GetFiles("any", SearchOption.TopDirectoryOnly, "any");

            // Expecting the project files and any files found by searching
            actual.Should().BeEquivalentTo("file1", "file2", "file3");
        }

        [TestMethod]
        public void GetFiles_FilesInExcludedDirectoriesAreNotReturned()
        {
            var filesInFileSystem = new string[] {
                "should be included1",
                "\\bin\\file1",
                "aaa\\bin\\zzz",
                "should be included2\\obj", // doesn't contain \obj\
                "ccc\\obj\\xxx",
                "should be included3"
            };

            var fileSystem = CreateFileSystem(filesInFileSystem);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var actual = testSubject.GetFiles("any", SearchOption.TopDirectoryOnly, "any");

            actual.Should().BeEquivalentTo("should be included1", "should be included2\\obj", "should be included3");
        }

        [TestMethod]
        public void GetFiles_CanReturnDuplicates()
        {
            // It's not the responsibility of this class to filter out duplicates
            var filesInFileSystem = new string[] {
                "file_1",
                "file_1",
                "file_2",
            };

            var fileSystem = CreateFileSystem(filesInFileSystem);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var actual = testSubject.GetFiles("any", SearchOption.TopDirectoryOnly, "any");

            actual.Should().BeEquivalentTo("file_1", "file_1", "file_2");
        }

        private static XmlFileProvider.FileFinder CreateTestSubject(
            IFileSystem fileSystem = null,
            ILogger logger = null)
        {
            fileSystem ??= new System.IO.Abstractions.TestingHelpers.MockFileSystem();
            logger ??= new TestLogger(logToConsole: true);

            return new XmlFileProvider.FileFinder(logger, fileSystem);
        }

        private static Mock<IFileSystem> CreateFileSystem(params string[] filesToReturn)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.Directory.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()))
                .Returns(filesToReturn);

            return fileSystem;
        }

        private void CheckSearch(Mock<IFileSystem> fileSystem, string expectedDirectory, SearchOption searchOption, string expectedPattern)
            => fileSystem.Verify(x => x.Directory.GetFiles(expectedDirectory, expectedPattern, searchOption), Times.Once);
    }
}
