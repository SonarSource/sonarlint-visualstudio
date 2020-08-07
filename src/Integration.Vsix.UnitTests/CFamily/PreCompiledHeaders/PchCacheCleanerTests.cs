/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.PreCompiledHeaders
{
    [TestClass]
    public class PchCacheCleanerTests
    {
        private MockFileSystem fileSystemMock;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new MockFileSystem();
            fileSystemMock.Directory.CreateDirectory("c:\\test\\pch");
        }

        [TestMethod]
        public void Cleanup_NoFilesInDirectory_NoException()
        {
            var testSubject = new PchCacheCleaner(fileSystemMock, "c:\\test\\pch\\myPch.abc");

            Action act = () => testSubject.Cleanup();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Cleanup_NoMatchingFilesInDirectory_NonMatchingFilesAreNotDeleted()
        {
            var nonMatchingFilePaths = new List<string>
            {
                "c:\\test\\pch\\test.abc",
                "c:\\test\\pch\\myPch.ab",
                "c:\\test\\pch\\myPch.abcd",
                "c:\\test\\pch\\amyPch.abc",
                "c:\\test\\pch\\sub\\myPch.abc",
                "c:\\test\\myPch.abc"
            };

            foreach (var filePath in nonMatchingFilePaths)
            {
                fileSystemMock.AddFile(filePath, new MockFileData(""));
            }

            var testSubject = new PchCacheCleaner(fileSystemMock, "c:\\test\\pch\\myPch.abc");
            testSubject.Cleanup();

            fileSystemMock.AllFiles.Should().BeEquivalentTo(nonMatchingFilePaths);
        }

        [TestMethod]
        public void Cleanup_HasMatchingFilesInDirectory_MatchingFilesAreDeleted()
        {
            var nonMatchingFilePaths = new List<string>
            {
                "c:\\test\\pch\\myPch.abc",
                "c:\\test\\pch\\MYpch.aBC",
                "c:\\test\\pch\\myPch.abc.a",
                "c:\\test\\pch\\myPch.abc.a.b",
                "c:\\test\\pch\\myPCH.ABC.a.b"
            };

            foreach (var filePath in nonMatchingFilePaths)
            {
                fileSystemMock.AddFile(filePath, new MockFileData(""));
            }

            var testSubject = new PchCacheCleaner(fileSystemMock, "c:\\test\\pch\\myPch.abc");
            testSubject.Cleanup();

            fileSystemMock.AllFiles.Should().BeEmpty();
        }

        [TestMethod]
        public void Cleanup_HasMatchingAndNonMatchingFilesInDirectory_OnlyMatchingFilesAreDeleted()
        {
            var matchingFile = "c:\\test\\pch\\myPch.abc.d";
            var nonMatchingFile = "c:\\test\\pch\\myPch.abcd";

            fileSystemMock.AddFile(matchingFile, new MockFileData(""));
            fileSystemMock.AddFile(nonMatchingFile, new MockFileData(""));

            var testSubject = new PchCacheCleaner(fileSystemMock, "c:\\test\\pch\\myPch.abc");
            testSubject.Cleanup();

            fileSystemMock.AllFiles.Should().BeEquivalentTo(new List<string>{nonMatchingFile});
        }

        [TestMethod]
        public void Cleanup_FailsToDeleteSomeFiles_DeletesTheOnesThatSucceed()
        {
            var matchingFile = "c:\\test\\pch\\myPch.abc.d";
            fileSystemMock.AddFile(matchingFile, new MockFileData(""));

            var matchingFailingFile = "c:\\test\\pch\\myPch.abc.de";
            var failingFileMockData = new MockFileData("")
            {
                AllowedFileShare = FileShare.None
            };
            fileSystemMock.AddFile(matchingFailingFile, failingFileMockData);

            var testSubject = new PchCacheCleaner(fileSystemMock, "c:\\test\\pch\\myPch.abc");
            testSubject.Cleanup();

            fileSystemMock.AllFiles.Should().BeEquivalentTo(new List<string> { matchingFailingFile });
        }
    }
}
