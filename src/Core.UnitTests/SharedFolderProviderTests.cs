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
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests
{
    [TestClass]
    public class SharedFolderProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SharedFolderProvider, ISharedFolderProvider>(
                MefTestHelpers.CreateExport<ISolutionInfoProvider>());
        }

        [TestMethod]
        public void GetSharedFolderPath_SonarlintFolderInSolutionFolder_ReturnsFolder()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("C:\\Solution");
            var fileSystem = CreateFileSystem("C:\\Solution\\.sonarlint", "C:\\Solution", "C:");

            var testSubject = CreateTestSubject(solutionInfoProvider, fileSystem);

            var sharedFolder = testSubject.GetSharedFolderPath();

            sharedFolder.Should().Be("C:\\Solution\\.sonarlint");
        }

        [TestMethod]
        public void GetSharedFolderPath_SonarlintFolderAboveSolutionFolder_ReturnsFolder()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("C:\\Repo\\Solution");
            var fileSystem = CreateFileSystem("C:\\Repo\\.sonarlint", "C:\\Repo\\Solution", "C:\\Repo", "C:");

            var testSubject = CreateTestSubject(solutionInfoProvider, fileSystem);

            var sharedFolder = testSubject.GetSharedFolderPath();

            sharedFolder.Should().Be("C:\\Repo\\.sonarlint");
        }

        [TestMethod]
        public void GetSharedFolderPath_NoSonarLintFolder_ReturnsNull()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider("C:\\Repo\\Solution");
            var fileSystem = CreateFileSystem("C:\\Repo\\Solution", "C:\\Repo", "C:");

            var testSubject = CreateTestSubject(solutionInfoProvider, fileSystem);

            var sharedFolder = testSubject.GetSharedFolderPath();

            sharedFolder.Should().BeNull();
        }

        [TestMethod]
        public void GetSharedFolderPath_NoSolutionLoaded_ReturnsNull()
        {
            var solutionInfoProvider = CreateSolutionInfoProvider(null);
            var fileSystem = CreateFileSystem("C:\\Repo\\.sonarlint", "C:\\Repo\\Solution", "C:\\Repo", "C:");

            var testSubject = CreateTestSubject(solutionInfoProvider, fileSystem);

            var sharedFolder = testSubject.GetSharedFolderPath();

            sharedFolder.Should().BeNull();
        }

        [TestMethod]
        public void GetSharedFolderPath_SolutionUnderUserProfile_NoSharedFolder_ReturnsNull()
        {
            var userProfileDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var commonSonarLintFolder = Path.Combine(userProfileDir, ".sonarlint");

            var repoDir = Path.Combine(userProfileDir, "Repo");
            var solutionDir = Path.Combine(repoDir, "Solution");

            var solutionInfoProvider = CreateSolutionInfoProvider("C:\\Repo\\Solution");
            var fileSystem = CreateFileSystem(userProfileDir, commonSonarLintFolder, repoDir, solutionDir);

            var testSubject = CreateTestSubject(solutionInfoProvider, fileSystem);

            var sharedFolder = testSubject.GetSharedFolderPath();

            sharedFolder.Should().BeNull();
        }

        private static SharedFolderProvider CreateTestSubject(ISolutionInfoProvider solutionInfoProvider, IFileSystem fileSystem)
        {
            fileSystem ??= CreateFileSystem();

            return new SharedFolderProvider(solutionInfoProvider, fileSystem);
        }

        private static IFileSystem CreateFileSystem(params string[] existingFolders)
        {
            var directory = new Mock<IDirectory>();
            directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(false);

            foreach (var existingFolder in existingFolders)
            {
                directory.Setup(d => d.Exists(existingFolder)).Returns(true);
            }

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.Directory).Returns(directory.Object);

            return fileSystem.Object;
        }

        private static ISolutionInfoProvider CreateSolutionInfoProvider(string solutionDirectoryToReturn)
        {
            var solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            solutionInfoProvider.Setup(x => x.GetSolutionDirectory()).Returns(solutionDirectoryToReturn);
            return solutionInfoProvider.Object;
        }
    }
}
