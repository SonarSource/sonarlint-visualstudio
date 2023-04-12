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
        public void PathHelper_ForceDirectoryEnding()
        {
            // Arrange
            const string withSlash = @"X:\directories\all\the\way\";
            const string withoutSlash = @"X:\directories\all\the\way";

            // Test case: without trailing slash
            PathHelper.ForceDirectoryEnding(withoutSlash).Should().Be(withSlash, "Expected to append trailing slash '\'");

            // Test case: with trailing slash
            PathHelper.ForceDirectoryEnding(withSlash).Should().Be(withSlash, "Expected to return input string without modification");
        }

        [TestMethod]
        public void PathHelper_CalculateRelativePath()
        {
            // Up one level
            VerifyCalculateRelativePath
            (
                expected: @"..\file2.ext",
                fromPath: @"X:\dirA\file1.ext",
                toPath: @"X:\file2.ext"
            );

            // Up multiple levels
            VerifyCalculateRelativePath
            (
                expected: @"..\..\..\file2.ext",
                fromPath: @"X:\dirA\dirB\dirC\file1.ext",
                toPath: @"X:\file2.ext"
            );

            // Down one level
            VerifyCalculateRelativePath
            (
                expected: @"dirA\file2.ext",
                fromPath: @"X:\file1.txt",
                toPath: @"X:\dirA\file2.ext"
            );

            // Down multiple levels
            VerifyCalculateRelativePath
            (
                expected: @"dirA\dirB\dirC\file2.ext",
                fromPath: @"X:\file1.ext",
                toPath: @"X:\dirA\dirB\dirC\file2.ext"
            );

            // Same level
            VerifyCalculateRelativePath
            (
                expected: @"file2.ext",
                fromPath: @"X:\file1.ext",
                toPath: @"X:\file2.ext"
            );

            // Different roots
            VerifyCalculateRelativePath
            (
                expected: @"Y:\file2.ext",
                fromPath: @"X:\file1.ext",
                toPath: @"Y:\file2.ext"
            );

            // Complicated file names
            VerifyCalculateRelativePath
            (
                expected: @"file with spaces (2).ext",
                fromPath: @"X:\file with spaces (1).ext",
                toPath: @"X:\file with spaces (2).ext"
            );

            // Non canonical paths (contains . and ..)
            VerifyCalculateRelativePath
            (
                expected: @"..\..\file1.ext",
                fromPath: @"X:\dirA\..\dirA\dirB\dirC\dirD\",
                toPath: @"X:\dirA\dirB\..\dirB\file1.ext"
            );
        }

        [TestMethod]
        public void PathHelper_CalculateRelativePath_NullArgumentChecks()
        {
            // 'absolute' param
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                PathHelper.CalculateRelativePath(@"X:\a\file.proj", null);
            });

            // 'relativeTo' param
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                PathHelper.CalculateRelativePath(null, @"X:\a\file.sln");
            });
        }

        [TestMethod]
        public void PathHelper_CalculateRelativePath_InputPathsMustBeAbsolute()
        {
            // 'absolute' param
            Exceptions.Expect<ArgumentException>(() =>
            {
                PathHelper.CalculateRelativePath(@"X:\a\file.proj", @"not\absolute\file.sln");
            });

            // 'relativeTo' param
            Exceptions.Expect<ArgumentException>(() =>
            {
                PathHelper.CalculateRelativePath(@"not\absolute\file.proj", @"X:\a\file.sln");
            });
        }

        [TestMethod]
        public void PathHelper_ResolveRelativePath()
        {
            // Up one level
            VerifyResolveRelativePath
            (
                expected: @"X:\file1.ext",
                basePath: @"X:\dirA\",
                relativePath: @"..\file1.ext"
            );

            // Up multiple levels
            VerifyResolveRelativePath
            (
                expected: @"X:\file1.ext",
                basePath: @"X:\dirA\dirB\dirC\",
                relativePath: @"..\..\..\file1.ext"
            );

            // Down one level
            VerifyResolveRelativePath
            (
                expected: @"X:\dirA\file1.ext",
                basePath: @"X:\",
                relativePath: @"dirA\file1.ext"
            );

            // Down multiple levels
            VerifyResolveRelativePath
            (
                expected: @"X:\dirA\dirB\dirC\file1.ext",
                basePath: @"X:\",
                relativePath: @"dirA\dirB\dirC\file1.ext"
            );

            // Same level
            VerifyResolveRelativePath
            (
                expected: @"X:\file1.ext",
                basePath: @"X:\",
                relativePath: @"file1.ext"
            );

            // Complicated file names
            VerifyResolveRelativePath
            (
                expected: @"X:\file with spaces.ext",
                basePath: @"X:\",
                relativePath: @"file with spaces.ext"
            );
        }

        [TestMethod]
        public void PathHelper_ExpandRelativePath_NullArgumentChecks()
        {
            // 'baseDirectoryPath' param
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                PathHelper.ResolveRelativePath(null, @"..\file.proj");
            });

            // 'relativePath' param
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                PathHelper.ResolveRelativePath(@"X:\a", null);
            });
        }

        [TestMethod]
        public void PathHelper_ExpandRelativePath_BasePathMustBeAbsolute()
        {
            Exceptions.Expect<ArgumentException>(() =>
            {
                PathHelper.ResolveRelativePath(@"not\absolute\file.sln", @"..\a\relative.path");
            });
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

        [TestMethod]
        // Module-level issues i.e. no file
        [DataRow(null, null, true)]
        [DataRow(null, "", true)]
        [DataRow("", null, true)]
        [DataRow("", "", true)]

        // Module-level issues should not match non-module-level issues
        [DataRow(@"any.txt", "", false)]
        [DataRow(@"any.txt", null, false)]
        [DataRow("", @"c:\any.txt", false)]
        [DataRow(null, @"c:\any.txt", false)]

        // File issues
        [DataRow(@"same.txt", @"c:\same.txt", true)]
        [DataRow(@"SAME.TXT", @"c:\same.txt", true)]
        [DataRow(@"same.TXT", @"c:\XXXsame.txt", false)]  // partial file name -> should not match
        [DataRow(@"differentExt.123", @"a:\differentExt.999", false)] // different extension -> should not match
        [DataRow(@"aaa\partial\file.cs", @"d:\partial\file.cs", false)]
        // Only matching the local path tail, so the same server path can match multiple local files
        [DataRow(@"partial\file.cs", @"c:\aaa\partial\file.cs", true)]
        [DataRow(@"partial\file.cs", @"c:\aaa\bbb\partial\file.cs", true)]
        [DataRow(@"partial\file.cs", @"c:\aaa\bbb\ccc\partial\file.cs", true)]
        public void IsFileMatch_ReturnsExpectedResult(string serverFilePath, string localFilePath, bool expected)
        {
            // Act and assert
            PathHelper.IsServerFileMatch(localFilePath, serverFilePath).Should().Be(expected);
        }

        #region Helpers

        private static void VerifyCalculateRelativePath(string expected, string fromPath, string toPath)
        {
            string actual = PathHelper.CalculateRelativePath(fromPath, toPath);

            actual.Should().Be(expected);
        }

        private static void VerifyResolveRelativePath(string expected, string basePath, string relativePath)
        {
            string actual = PathHelper.ResolveRelativePath(relativePath, basePath);

            actual.Should().Be(expected);
        }

        private static string GetBasePath()
        {
            return Path.Combine(Path.GetTempPath(), "SLVS");
        }

        #endregion Helpers
    }
}
