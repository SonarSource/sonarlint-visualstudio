/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Core.UnitTests.Helpers
{
    [TestClass]
    public class PathHelperTests
    {
        [TestMethod]
        public void PathHelper_EscapeFileName()
        {
            // Arrange
            const string unescapedString = "A string | with / special : and / invalid \\ characters * all < over > the \" place ?";
            const string expectedEscaped = "A string _ with _ special _ and _ invalid _ characters _ all _ over _ the _ place _";

            // Act
            string actualEscaped = PathHelper.EscapeFileName(unescapedString);

            // Assert
            actualEscaped.Should().Be(expectedEscaped);
        }

        [TestMethod]
        public void PathHelper_IsPathRootedUnder_RootedPath_IsTrue()
        {
            // Arrange
            const string root = @"X:\All\Files\Live\Here";
            const string rootedFile = @"X:\All\Files\Live\Here\likeme.ext";

            // Act
            bool rootedIsRooted = PathHelper.IsPathRootedUnderRoot(rootedFile, root);

            // Assert
            rootedIsRooted.Should().BeTrue($"Path '{rootedFile}' should be rooted under '{root}'");
        }

        [TestMethod]
        public void PathHelper_IsPathRootedUnder_UnrootedPath_IsFalse()
        {
            // Arrange
            const string root = @"X:\All\Files\Live\Here";

            const string unrootedFile = @"X:\im_not_under_the_root.ext";

            // Act
            bool unrootedIsRooted = PathHelper.IsPathRootedUnderRoot(unrootedFile, root);

            // Assert
            unrootedIsRooted.Should().BeFalse($"Path '{unrootedFile}' should not be rooted under '{root}'");
        }

        [TestMethod]
        public void PathHelper_IsPathRootedUnder_PathsContainRelativeComponents()
        {
            // Arrange
            const string root = @"X:\All\Files\..\Files\Live\Here";
            const string rootedFile = @"X:\All\..\All\Files\Live\Here\likeme.ext";

            // Act
            bool rootedIsRooted = PathHelper.IsPathRootedUnderRoot(rootedFile, root);

            // Assert
            rootedIsRooted.Should().BeTrue($"Path '{rootedFile}' should be rooted under '{root}'");
        }

        [TestMethod]
        [DataRow(@"c:\aaa\bbb", @"c:\aaa\bbb")]
        [DataRow(@"c:\aaa\bbb", @"c:\AAA\BBB")]
        [DataRow(@"c:\a\b\..\.\x", @"c:\a\x")]
        [DataRow(@"e:\aaa\file.txt", @"e:\aaa\FILE.TXT")]
        public void IsMatchingPath_ReturnsTrue(string path1, string path2)
        {
            PathHelper.IsMatchingPath(path1, path2).Should().BeTrue();
        }

        [TestMethod]
        [DataRow("aaa", "aaaa")]
        [DataRow(@"c:\aa", @"c:\aa\file.txt")]
        [DataRow(@"d:\a\b\file.txt.xxx", @"d:\a\b\file.txt.yyy")]
        public void IsMatchingPath_ReturnsFalse(string path1, string path2)
        {
            PathHelper.IsMatchingPath(path1, path2).Should().BeFalse();
        }

        [TestMethod]
        [DataRow(null, "valid")]
        [DataRow("", "valid")]
        [DataRow("valid", null)]
        [DataRow("valid", "")]
        public void IsMatchingPath_InvalidPath_Throws(string path1, string path2)
        {
            Action act = () => PathHelper.IsMatchingPath(path1, path2);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void GetTempDirForTask_IsEmpty_ReturnsBasePath()
        {
            var expectedPath = GetBasePath();

            var actualPath = PathHelper.GetTempDirForTask(false);

            actualPath.Should().Be(expectedPath);
        }

        [TestMethod]
        public void GetTempDirForTask_HasOnePath_CombinesPaths()
        {
            var expectedPath = Path.Combine(GetBasePath(), "Path");

            var actualPath = PathHelper.GetTempDirForTask(false, "Path");

            actualPath.Should().Be(expectedPath);
        }

        [TestMethod]
        public void GetTempDirForTask_PathsIsNull_Throws()
        {
            Action act = () => PathHelper.GetTempDirForTask(false, null);
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void GetTempDirForTask_HasMultiplePaths_CombinesPaths()
        {
            var expectedPath = Path.Combine(GetBasePath(), "Path1", "Path2");

            var actualPath = PathHelper.GetTempDirForTask(false, "Path1", "Path2");

            actualPath.Should().Be(expectedPath);
        }

        [TestMethod]
        public void GetTempDirForTask_HasMultiplePathsWithNull_Throws()
        {
            Action act = () => PathHelper.GetTempDirForTask(false, "Path1", null, "Path2");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void GetTempDirForTask_HasMultiplePathsWithEmpty_Ignores()
        {
            var expectedPath = Path.Combine(GetBasePath(), "Path1", "Path2");

            var actualPath = PathHelper.GetTempDirForTask(false, "Path1", "", "Path2");

            actualPath.Should().Be(expectedPath);
        }

        [TestMethod]
        public void GetTempDirForTask_perVSInstanceTrue_AddsGuid()
        {
            var expectedPath = GetBasePath();

            var actualPath = PathHelper.GetTempDirForTask(true);

            actualPath.Should().Contain(expectedPath);
            Guid.TryParse(actualPath.Split('\\').Last(), out _).Should().BeTrue();
        }

        [TestMethod]
        public void GetTempDirForTask_perVSInstanceTrueHasPath_AddsGuid()
        {
            var expectedPath = Path.Combine(GetBasePath(), "Path");

            var actualPath = PathHelper.GetTempDirForTask(true, "Path");

            actualPath.Should().Contain(expectedPath);
            Guid.TryParse(actualPath.Split('\\').Last(), out _).Should().BeTrue();
        }

        #region Helpers

        private static string GetBasePath()
        {
            return Path.Combine(Path.GetTempPath(), "SLVS");
        }

        #endregion Helpers
    }
}
