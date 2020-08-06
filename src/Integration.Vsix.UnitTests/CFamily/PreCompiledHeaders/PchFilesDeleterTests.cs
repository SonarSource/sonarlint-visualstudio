using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

namespace SonarLint.VisualStudio.Integration.UnitTests.CFamily.PreCompiledHeaders
{
    [TestClass]
    public class PchFilesDeleterTests
    {
        private MockFileSystem fileSystemMock;

        [TestInitialize]
        public void TestInitialize()
        {
            fileSystemMock = new MockFileSystem();
            fileSystemMock.Directory.CreateDirectory("c:\\test\\pch");
        }

        [TestMethod]
        public void DeletePchFiles_NoFilesInDirectory_NoException()
        {
            var testSubject = new PchFilesDeleter(fileSystemMock, "c:\\test\\pch\\myPch.abc");

            Action act = () => testSubject.DeletePchFiles();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void DeletePchFiles_NoMatchingFilesInDirectory_NonMatchingFilesAreNotDeleted()
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

            var testSubject = new PchFilesDeleter(fileSystemMock, "c:\\test\\pch\\myPch.abc");
            testSubject.DeletePchFiles();

            fileSystemMock.AllFiles.Should().BeEquivalentTo(nonMatchingFilePaths);
        }

        [TestMethod]
        public void DeletePchFiles_HasMatchingFilesInDirectory_MatchingFilesAreDeleted()
        {
            var nonMatchingFilePaths = new List<string>
            {
                "c:\\test\\pch\\myPch.abc",
                "c:\\test\\pch\\myPch.abc.a",
                "c:\\test\\pch\\myPch.abc.a.b"
            };

            foreach (var filePath in nonMatchingFilePaths)
            {
                fileSystemMock.AddFile(filePath, new MockFileData(""));
            }

            var testSubject = new PchFilesDeleter(fileSystemMock, "c:\\test\\pch\\myPch.abc");
            testSubject.DeletePchFiles();

            fileSystemMock.AllFiles.Should().BeEmpty();
        }

        [TestMethod]
        public void DeletePchFiles_HasMatchingAndNonMatchingFilesInDirectory_OnlyMatchingFilesAreDeleted()
        {
            var matchingFile = "c:\\test\\pch\\myPch.abc.d";
            var nonMatchingFile = "c:\\test\\pch\\myPch.abcd";

            fileSystemMock.AddFile(matchingFile, new MockFileData(""));
            fileSystemMock.AddFile(nonMatchingFile, new MockFileData(""));

            var testSubject = new PchFilesDeleter(fileSystemMock, "c:\\test\\pch\\myPch.abc");
            testSubject.DeletePchFiles();

            fileSystemMock.AllFiles.Should().BeEquivalentTo(new List<string>{nonMatchingFile});
        }
    }
}
