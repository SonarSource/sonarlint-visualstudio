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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintDaemon
{
    [TestClass]
    public class LegacyInstallationCleanupTests
    {

        [TestMethod]
        public void StaticMethod_InvalidArg_Throws()
        {
            // Arrange
            Action act = () => LegacyInstallationCleanup.CleanupDaemonFiles(null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Clean_FolderDoesNotExist_NoError()
        {
            // Arrange
            // "Strict" mocks, so will throw if any methods on the mock are called
            // (we don't expect either the logger or directory wrapper to be used if
            // the legacy folder does not exist)
            var mockDirectory = new Mock<IDirectory>(MockBehavior.Strict);
            var mockLogger = new Mock<ILogger>(MockBehavior.Strict);

            mockDirectory.Setup(x => x.GetDirectories(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { });

            var cleaner = new LegacyInstallationCleanup(
                mockLogger.Object,
                "c:\\dummy directory\\that does\\not\\exist",
                mockDirectory.Object);

            // Act
            cleaner.Clean();

            // Assert
            AssertDeleteCalledExpectedTimes(mockDirectory, 0);
        }

        [TestMethod]
        public void Clean_MatchingDaemonFoldersExist_Deleted()
        {
            // Arrange
            var mockDirectory = new Mock<IDirectory>();
            var mockLogger = new Mock<ILogger>();

            SetupDirectories(mockDirectory, "c:\\aaa\\rootfolder",
                "sonarlint-daemon-match1-windows",
                "sonarlint-daemon-2.17.0.899-windows");

            var cleaner = new LegacyInstallationCleanup(
                mockLogger.Object,
                "c:\\aaa\\rootfolder",
                mockDirectory.Object);

            // Act
            cleaner.Clean();

            // Assert
            AssertRenamedFolderDeleted(mockDirectory, "c:\\aaa\\rootfolder\\sonarlint-daemon-match1-windows");
            AssertRenamedFolderDeleted(mockDirectory, "c:\\aaa\\rootfolder\\sonarlint-daemon-2.17.0.899-windows");
            AssertDeleteCalledExpectedTimes(mockDirectory, 2);
        }

        [TestMethod]
        public void Clean_MatchingDaemonFoldersNotFound_NoError_NotDeleted()
        {
            // Arrange
            var mockDirectory = new Mock<IDirectory>();
            var mockLogger = new Mock<ILogger>();

            SetupDirectories(mockDirectory, "c:\\",
                "aaa",
                "bbb");

            // Mark the first directory as not existing
            mockDirectory.Setup(x => x.Exists("c:\\aaa")).Returns(false);

            var cleaner = new LegacyInstallationCleanup(
                mockLogger.Object,
                "c:\\",
                mockDirectory.Object);

            // Act
            cleaner.Clean();

            // Assert
            AssertRenamedFolderDeleted(mockDirectory, "c:\\bbb");
            AssertDeleteCalledExpectedTimes(mockDirectory, 1);
        }

        [TestMethod]
        public void Clean_ErrorDuringDelete_ErrorSuppressedAndOtherFoldersDeleted()
        {
            // Arrange
            var mockDirectory = new Mock<IDirectory>();
            var mockLogger = new Mock<ILogger>();

            SetupDirectories(mockDirectory, "c:\\aaa\\rootfolder",
                "xxx",
                "yyy");

            // Set up to throw when the first directory is deleted
            mockDirectory
                .Setup(x => x.Delete(It.Is<string>(n => n.StartsWith("c:\\aaa\\rootfolder\\xxx")), true))
                .Throws(new FileNotFoundException());

            var cleaner = new LegacyInstallationCleanup(
                mockLogger.Object,
                "c:\\aaa\\rootfolder",
                mockDirectory.Object);

            // Act
            cleaner.Clean();

            // Assert
            AssertRenamedFolderDeleted(mockDirectory, "c:\\aaa\\rootfolder\\xxx");
            AssertRenamedFolderDeleted(mockDirectory, "c:\\aaa\\rootfolder\\yyy");

            AssertDeleteCalledExpectedTimes(mockDirectory, 2);
        }

        private static void SetupDirectories(Mock<IDirectory> mock, string rootFolderName, params string[] subFolderNames)
        {
            var fullReturnPaths = subFolderNames.Select(f => Path.Combine(rootFolderName, f)).ToArray();
            mock.Setup(x => x.GetDirectories(It.Is<string>(f => f == rootFolderName), It.IsAny<string>())).Returns(fullReturnPaths);

            mock.Setup(x => x.Exists(It.IsAny<string>())).Returns<string>(n => n == rootFolderName || fullReturnPaths.Contains(n));
        }

        private static void AssertDeleteCalledExpectedTimes(Mock<IDirectory> mock, int count)
        {
            mock.Verify(x => x.Delete(It.IsAny<string>(), true), Times.Exactly(count));
        }

        private static void AssertRenamedFolderDeleted(Mock<IDirectory> mock, string partialFolderName)
        {
            // Should not be trying to delete the exact folder name - we expect
            // to have been renamed by appending a suffix
            mock.Verify(x => x.Delete(partialFolderName, true), Times.Never);
            mock.Verify(x => x.Delete(It.Is<string>(n => n.StartsWith(partialFolderName)), true), Times.Once);
        }
    }
}
