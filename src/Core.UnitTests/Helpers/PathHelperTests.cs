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

        [TestMethod]
        public void CalculateServerRoot_SimpleCase()
        {
            PathHelper.CalculateServerRoot(@"C:\dir\dir\projectroot\projectdir\projectdir\projectfile.cs",
                new[] { @"projectdir\projectdir\projectfile.cs" }).Should().Be(@"C:\dir\dir\projectroot");
        }
        
        [TestMethod]
        public void CalculateServerRoot_OnlyOneMatch()
        {
            PathHelper.CalculateServerRoot(@"C:\dir\dir\projectroot\projectdir\projectdir\projectfile.cs", 
                new[]
                {
                    @"a\b\c",
                    @"a\b\c\d\e\f",
                    @"projectdir\projectdir\projectfile.cs",
                    @"projectdir\projectdir\notprojectfile.cs",
                    @"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                }).Should().Be(@"C:\dir\dir\projectroot");
        }
        
        [TestMethod]
        public void CalculateServerRoot_SelectsLongestMatchOutOfMultiple()
        {
            PathHelper.CalculateServerRoot(@"C:\dir\dir\projectroot\projectdir\projectdir\projectfile.cs", 
                new[]
                {
                    @"projectfile.cs",
                    @"projectdir\projectfile.cs",
                    @"projectdir\projectdir\projectfile.cs",
                    @"projectdir\projectdir\projectdir\projectfile.cs"
                }).Should().Be(@"C:\dir\dir\projectroot");
        }
        
        [TestMethod]
        public void CalculateServerRoot_NoMatch()
        {
            PathHelper.CalculateServerRoot(@"C:\dir\dir\projectroot\projectdir\projectdir\projectfile.cs", 
                new[]
                {
                    @"notprojectfile.cs",
                    @"projectdir\notprojectfile.cs",
                    @"projectdir\projectdir\notprojectfile.cs",
                    @"projectdir\projectdir\projectdir\notprojectfile.cs"
                }).Should().Be(null);
        }
        
        [TestMethod]
        public void CalculateServerRoot_EmptyList()
        {
            PathHelper.CalculateServerRoot(@"C:\dir\dir\projectroot\projectdir\projectdir\projectfile.cs", Array.Empty<string>())
                .Should().Be(null);
        }
        
        [TestMethod]
        public void CalculateServerRoot_NullList()
        {
            PathHelper.CalculateServerRoot(@"C:\dir\dir\projectroot\projectdir\projectdir\projectfile.cs", null)
                .Should().Be(null);
        }

        #region Helpers

        private static string GetBasePath()
        {
            return Path.Combine(Path.GetTempPath(), "SLVS");
        }

        #endregion Helpers
    }
}
