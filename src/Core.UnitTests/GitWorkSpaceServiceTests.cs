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

using System.IO.Abstractions;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.Infrastructure.VS.UnitTests
{
    [TestClass]
    public class GitWorkSpaceServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<GitWorkSpaceService, IGitWorkspaceService>(
                MefTestHelpers.CreateExport<ISolutionInfoProvider>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void GetRepoRoot_GitFolderInSolutionFolder_ReturnsRepoFolder()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("C:\\Solution");

            var logger = new Mock<ILogger>();

            var fileSystem = CreateFileSystem("C:\\Solution\\.git");

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, logger.Object, fileSystem.Object);

            var gitRoot = testSubject.GetRepoRoot();

            gitRoot.Should().Be("C:\\Solution");
            logger.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetRepoRoot_GitFolderIsAboveSolutionFolder_ReturnsRepoFolder()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("C:\\Repo\\Code\\Solution");
            var logger = new Mock<ILogger>();

            var fileSystem = CreateFileSystem("C:\\Repo\\.git", "C:\\Repo\\Code", "C:\\Repo");

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, logger.Object, fileSystem.Object);

            var gitRoot = testSubject.GetRepoRoot();

            gitRoot.Should().Be("C:\\Repo");
            logger.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetRepoRoot_NotUnderGit_ReturnsNull()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("C:\\Solution");

            var logger = new Mock<ILogger>();

            var fileSystem = CreateFileSystem("C:\\Solution\\", "C:");

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, logger.Object, fileSystem.Object);

            var gitRoot = testSubject.GetRepoRoot();

            gitRoot.Should().BeNull();
            logger.Verify(l => l.WriteLine(CoreStrings.NoGitFolder), Times.Once);
            logger.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetRepoRoot_NoSolutionOpen_ReturnsNull()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(null);

            var logger = new Mock<ILogger>();

            var testSubject = CreateTestSubject(solutionInfoProvider.Object, logger.Object);

            var gitRoot = testSubject.GetRepoRoot();

            gitRoot.Should().BeNull();
            logger.VerifyNoOtherCalls();
        }


        private static GitWorkSpaceService CreateTestSubject(ISolutionInfoProvider solutionInfoProvider, ILogger logger, IFileSystem fileSystem = null)
        {
            fileSystem ??= CreateFileSystem().Object;

            return new GitWorkSpaceService(solutionInfoProvider, logger, fileSystem);
        }

        private static Mock<IFileSystem> CreateFileSystem(params string[] existingFolders)
        {
            var directory = new Mock<IDirectory>();
            directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(false);

            foreach (var existingFolder in existingFolders)
            {
                directory.Setup(d => d.Exists(existingFolder)).Returns(true);
            }

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Directory).Returns(directory.Object);
            
            return fileSystem;
        }

        private static Mock<ISolutionInfoProvider> CreateSolutionInfoProvider(string solutionDirectoryToReturn)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetSolutionDirectory()).Returns(solutionDirectoryToReturn);
            return solutionInfoProvider;
        }
    }
}
