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
using Newtonsoft.Json;
using SonarLint.VisualStudio.CFamily.CMake;
using SonarLint.VisualStudio.Core.CFamily;

namespace SonarLint.VisualStudio.CFamily.UnitTests.CMake
{
    [TestClass]
    public class CompilationConfigProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<CompilationConfigProvider, ICompilationConfigProvider>(
                MefTestHelpers.CreateExport<ICompilationDatabaseLocator>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void GetConfig_NullFilePath_ArgumentNullException(string analyzedFilePath)
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.GetConfig(analyzedFilePath);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("filePath");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void GetConfig_NoCompilationDatabase_Null(string compilationDatabaseFilePath)
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator(compilationDatabaseFilePath);
            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object);

            var result = testSubject.GetConfig("some file");
            result.Should().BeNull();

            compilationDatabaseLocator.Verify(x=> x.Locate(), Times.Once);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("[]")]
        public void GetConfig_EmptyCompilationDatabase_Null(string compilationDatabaseContents)
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", compilationDatabaseContents);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object, logger);

            var result = testSubject.GetConfig("some file");
            result.Should().BeNull();

            fileSystem.Verify(x => x.File.ReadAllText("some db"), Times.Once);

            logger.AssertOutputStringExists(string.Format(Resources.EmptyCompilationDatabaseFile, "some db"));
        }

        [TestMethod]
        public void GetConfig_ProblemReadingDatabaseFile_NonCriticalException_Null()
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", exToThrow: new NotSupportedException("this is a test"));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object, logger);

            var result = testSubject.GetConfig("some file");
            result.Should().BeNull();

            fileSystem.Verify(x => x.File.ReadAllText("some db"), Times.Once);

            logger.AssertPartialOutputStringExists("this is a test");
        }

        [TestMethod]
        public void GetConfig_ProblemReadingDatabaseFile_CriticalException_ExceptionThrown()
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", exToThrow: new StackOverflowException());

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object);

            Action act = () => testSubject.GetConfig("some file");

            act.Should().Throw<StackOverflowException>();
        }

        [TestMethod]
        public void GetConfig_ProblemParsingDatabaseFile_Null()
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", "not valid json");
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object, logger);

            var result = testSubject.GetConfig("some file");
            result.Should().BeNull();

            fileSystem.Verify(x => x.File.ReadAllText("some db"), Times.Once);

            logger.AssertPartialOutputStringExists("JsonReaderException");
        }

        [TestMethod]
        [DataRow("[{}]")] // no entries in file
        [DataRow("[{\"file\" : \"some file.c\" }]")] // different extension
        [DataRow("[{\"file\" : \"sub/some file.cpp\" }]")] // different path
        public void GetConfig_CodeFile_EntryNotFoundInDatabase_Null(string databaseFileContents)
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", databaseFileContents);
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object, logger);

            var result = testSubject.GetConfig("some file.cpp");
            result.Should().BeNull();

            fileSystem.Verify(x => x.File.ReadAllText("some db"), Times.Once);

            logger.AssertPartialOutputStringExists(string.Format(Resources.NoCompilationDatabaseEntry, "some file.cpp"));
        }

        [TestMethod]
        [DataRow("c:\\some file.cpp")] // exact match
        [DataRow("c:/some file.cpp")] // different format 
        [DataRow("c:/SOME file.cpp")] // case-sensitivity on name
        [DataRow("c:/some file.CPP")] // case-sensitivity on extension
        public void GetConfig_CodeFile_EntryFound_ReturnsEntry(string entryFilePath)
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var compilationDatabaseContent = new[]
            {
                new CompilationDatabaseEntry {File = entryFilePath, Command = "some command", Directory = "some dir"}
            };
            var fileSystem = SetupDatabaseFileContents("some db", JsonConvert.SerializeObject(compilationDatabaseContent));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object, logger);

            var result = testSubject.GetConfig("c:\\some file.cpp");
            result.Should().BeEquivalentTo(compilationDatabaseContent[0]);
        }

        [TestMethod]
        public void GetConfig_CodeFile_DatabaseContainsMultipleEntriesForSameFile_ReturnsFirstOne()
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var compilationDatabaseContent = new[]
            {
                new CompilationDatabaseEntry {File = "some other file", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "some file", Command = "cmd2", Directory = "dir2"},
                new CompilationDatabaseEntry {File = "some file", Command = "cmd3", Directory = "dir3"}
            };

            var fileSystem = SetupDatabaseFileContents("some db", JsonConvert.SerializeObject(compilationDatabaseContent));
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object, logger);

            var result = testSubject.GetConfig("some file");

            result.Should().BeEquivalentTo(compilationDatabaseContent[1]);
        }

        [TestMethod]
        public void GetConfig_HeaderFile_NoEntriesInCompilationDatabase_Null()
        {
            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", "[{}]");
            var logger = new TestLogger();

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object, logger);

            var result = testSubject.GetConfig("some header.h");
            result.Should().BeNull();

            fileSystem.Verify(x => x.File.ReadAllText("some db"), Times.Once);

            logger.AssertPartialOutputStringExists(string.Format(Resources.NoCompilationDatabaseEntryForHeaderFile, "some header.h"));
        }

        [TestMethod]
        [DataRow("c:\\a.cpp")]
        [DataRow("c:\\some\\folder\\a.c")]
        [DataRow("D:\\A.cxx")]
        [DataRow("c:\\test\\a.CC")]
        public void GetConfig_HeaderFile_CodeWithSameNameDifferentPath_ReturnsCodeFileEntry(string matchingCodeFile)
        {
            const string headerFilePath = "c:\\test\\a.h";

            var compilationDatabaseContent = new[]
            {
                new CompilationDatabaseEntry {File = "c:\\test\\a.b", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = matchingCodeFile, Command = "cmd2", Directory = "dir2"},
                new CompilationDatabaseEntry {File = "c:\\test\\aa.cpp", Command = "cmd3", Directory = "dir3"}
            };

            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", JsonConvert.SerializeObject(compilationDatabaseContent));

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object);

            var result = testSubject.GetConfig(headerFilePath);
            result.Should().BeEquivalentTo(compilationDatabaseContent[1]);
        }

        [TestMethod]
        public void GetConfig_HeaderFile_CodeWithSameNameExactPath_GivenPriorityOverDifferentPath()
        {
            const string headerFilePath = "c:\\test\\a.hxx";

            var compilationDatabaseContent = new[]
            {
                new CompilationDatabaseEntry {File = "c:\\a.c", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\other\\a.c", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\test\\a.c", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\test\\a.b", Command = "cmd1", Directory = "dir1"}
            };

            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", JsonConvert.SerializeObject(compilationDatabaseContent));
            
            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object);

            var result = testSubject.GetConfig(headerFilePath);
            result.Should().BeEquivalentTo(compilationDatabaseContent[2]);
        }

        [TestMethod]
        public void GetConfig_HeaderFile_NoMatchingName_HasCodeFileUnderPath_ReturnsCodeFileEntry()
        {
            const string headerFilePath = "c:\\a\\b\\test.hh";

            var compilationDatabaseContent = new[]
            {
                new CompilationDatabaseEntry {File = "c:\\a\\wrongRoot2.cpp", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\b\\wrongRoot3.cpp", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\a\\b\\c\\correctRoot1.cpp", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\a\\b\\correctRoot2.cpp", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\wrongRoot3.cpp", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\a\\b\\correctRoot3.cpp", Command = "cmd1", Directory = "dir1"}
            };

            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", JsonConvert.SerializeObject(compilationDatabaseContent));

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object);

            var result = testSubject.GetConfig(headerFilePath);
            result.Should().BeEquivalentTo(compilationDatabaseContent[2]);
        }

        [TestMethod]
        public void GetConfig_HeaderFile_NoMatchingName_NoMatchingPath_ReturnsFirstCodeFileEntry()
        {
            const string headerFilePath = "c:\\a\\b\\test.hpp";

            var compilationDatabaseContent = new[]
            {
                new CompilationDatabaseEntry {File = "c:\\wrongRoot.cpp", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\a\\c\\wrongRoot2.cpp", Command = "cmd1", Directory = "dir1"},
                new CompilationDatabaseEntry {File = "c:\\b\\wrongRoot3.cpp", Command = "cmd1", Directory = "dir1"}
            };

            var compilationDatabaseLocator = SetupCompilationDatabaseLocator("some db");
            var fileSystem = SetupDatabaseFileContents("some db", JsonConvert.SerializeObject(compilationDatabaseContent));

            var testSubject = CreateTestSubject(compilationDatabaseLocator.Object, fileSystem.Object);

            var result = testSubject.GetConfig(headerFilePath);
            result.Should().BeEquivalentTo(compilationDatabaseContent[0]);
        }

        private static Mock<IFileSystem> SetupDatabaseFileContents(string databaseFilePath, string content = null, Exception exToThrow = null)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(x => x.File.Exists(databaseFilePath)).Returns(true);

            if (exToThrow == null)
            {
                fileSystem.Setup(x => x.File.ReadAllText(databaseFilePath)).Returns(content);
            }
            else
            {
                fileSystem.Setup(x => x.File.ReadAllText(databaseFilePath)).Throws(exToThrow);
            }

            return fileSystem;
        }

        private Mock<ICompilationDatabaseLocator> SetupCompilationDatabaseLocator(string compilationDatabaseFilePath)
        {
            var compilationDatabaseLocator = new Mock<ICompilationDatabaseLocator>();

            compilationDatabaseLocator.Setup(x => x.Locate()).Returns(compilationDatabaseFilePath);

            return compilationDatabaseLocator;
        }

        private CompilationConfigProvider CreateTestSubject(ICompilationDatabaseLocator compilationDatabaseLocator = null,
            IFileSystem fileSystem = null,
            ILogger logger = null)
        {
            compilationDatabaseLocator ??= Mock.Of<ICompilationDatabaseLocator>();
            fileSystem ??= Mock.Of<IFileSystem>();
            logger ??= Mock.Of<ILogger>();

            return new CompilationConfigProvider(compilationDatabaseLocator, fileSystem, logger);
        }
    }
}
