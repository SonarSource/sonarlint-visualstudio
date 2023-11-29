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

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Tests.Helpers
{
    [TestClass]
    public class FilePathNormalizerTests
    {
        [TestMethod]
        public void NormalizeSonarQubePath_NullPath_EmptyStringReturned()
        {
            var result = FilePathNormalizer.NormalizeSonarQubePath(null);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void NormalizeSonarQubePath_NothingToNormalize_PathUnchanged()
        {
            const string normalizedPath = "test\\a\\b\\c\\d";

            using (new AssertIgnoreScope())
            {
                var result = FilePathNormalizer.NormalizeSonarQubePath(normalizedPath);

                result.Should().Be(normalizedPath);
            }
        }

        [TestMethod]
        [DataRow("/a/b/c", "a\\b\\c")]
        [DataRow("a/B/C", "a\\B\\C")]
        public void NormalizeSonarQubePath_InvalidPath_PathNormalized(string originalPath, string expectedResult)
        {
            var result = FilePathNormalizer.NormalizeSonarQubePath(originalPath);

            result.Should().Be(expectedResult);
        }
        
        [TestMethod]
        public void ServerizeWindowsPath_NullPath_EmptyStringReturned()
        {
            var result = FilePathNormalizer.ServerizeWindowsPath(null);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void ServerizeWindowsPath_NothingToNormalize_PathUnchanged()
        {
            const string normalizedPath = "test/a/b/c/d";

            using (new AssertIgnoreScope())
            {
                var result = FilePathNormalizer.ServerizeWindowsPath(normalizedPath);

                result.Should().Be(normalizedPath);
            }
        }
        
        [TestMethod]
        [DataRow("\\a\\b\\c", "a/b/c")]
        [DataRow("a\\B\\C", "a/B/C")]
        public void ServerizeWindowsPath_InvalidPath_PathNormalized(string originalPath, string expectedResult)
        {
            var result = FilePathNormalizer.ServerizeWindowsPath(originalPath);

            result.Should().Be(expectedResult);
        }
    }
}
