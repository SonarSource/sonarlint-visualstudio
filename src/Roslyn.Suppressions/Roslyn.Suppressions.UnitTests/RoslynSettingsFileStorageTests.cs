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
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Roslyn.Suppressions.SettingsFile;
using static SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests.TestHelper;

namespace SonarLint.VisualStudio.Roslyn.Suppressions.UnitTests
{
    [TestClass]
    public class RoslynSettingsFileStorageTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<RoslynSettingsFileStorage, IRoslynSettingsFileStorage>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void Update_HasIssues_IssuesWrittenToFile()
        {
            var settings = new RoslynSettings
            {
                SonarProjectKey = "projectKey",
                Suppressions = new[] { CreateIssue("issue1") }
            };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            fileStorage.Update(settings, "a solution name");

            CheckFileWritten(file, settings, "a solution name");
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Get_HasIssues_IssuesReadFromFile()
        {
            var issue1 = CreateIssue("key1");
            var issue2 = CreateIssue("key2");
            var settings = new RoslynSettings
            {
                SonarProjectKey = "projectKey",
                Suppressions = new[] { issue1, issue2 }
            };
            
            var logger = new TestLogger();

            var file = CreateFileForGet(settings);
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var actual = fileStorage.Get("projectKey");
            var issuesGotten = actual.Suppressions.ToList();

            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.AssertNoOutputMessages();

            issuesGotten.Count.Should().Be(2);
            issuesGotten[0].RoslynRuleId.Should().Be(issue1.RoslynRuleId);
            issuesGotten[1].RoslynRuleId.Should().Be(issue2.RoslynRuleId);
        }

        [TestMethod]
        public void Update_SolutionNameHasInvalidChars_InvalidCharsReplaced()
        {
            var settings = new RoslynSettings { SonarProjectKey = "project:key" };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = new RoslynSettingsFileStorage(logger, fileSystem.Object);
            fileStorage.Update(settings, "my:solution");

            CheckFileWritten(file, settings, "my_solution");
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Update_ErrorOccuredWhenWritingFile_ErrorIsLogged()
        {
            var settings = new RoslynSettings { SonarProjectKey = "projectKey" };
            var logger = new TestLogger();

            var file = new Mock<IFile>();
            file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            fileStorage.Update(settings, "any");

            logger.AssertOutputStrings("[Roslyn Suppressions] Error writing settings for project projectKey. Issues suppressed on the server may not be suppressed in the IDE. Error: Test Exception");
        }

        [TestMethod]
        public void Get_ErrorOccuredWhenWritingFile_ErrorIsLoggedAndReturnsNull()
        {
            var logger = new TestLogger();

            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);
            RoslynSettingsFileStorage fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var actual = fileStorage.Get("projectKey");

            logger.AssertOutputStrings("[Roslyn Suppressions] Error loading settings for project projectKey. Issues suppressed on the server will not be suppressed in the IDE. Error: Test Exception");
            actual.Should().BeNull();
        }

        [TestMethod]
        public void Update_HasNoIssues_FileWritten()
        {
            var settings = new RoslynSettings
            {
                SonarProjectKey = "projectKey",
                Suppressions = Enumerable.Empty<SuppressedIssue>()
            };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            fileStorage.Update(settings, "mySolution1");

            CheckFileWritten(file, settings, "mySolution1");
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Get_HasNoIssues_ReturnsEmpty()
        {
            var settings = new RoslynSettings
            {
                SonarProjectKey = "projectKey",
                Suppressions = Enumerable.Empty<SuppressedIssue>()
            };

            var logger = new TestLogger();

            var file = CreateFileForGet(settings);
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var actual = fileStorage.Get("projectKey");
            var issuesGotten = actual.Suppressions.ToList();

            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.AssertNoOutputMessages();

            issuesGotten.Count.Should().Be(0);
        }

        [TestMethod]
        public void Get_FileDoesNotExist_ErrorIsLoggedAndReturnsNull()
        {
            var logger = new TestLogger();

            Mock<IFileSystem> fileSystem = CreateFileSystem(fileExists:false);
            RoslynSettingsFileStorage fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var actual = fileStorage.Get("projectKey");

            logger.AssertOutputStrings("[Roslyn Suppressions] Error loading settings for project projectKey. Issues suppressed on the server will not be suppressed in the IDE. Error: Settings File was not found");
            actual.Should().BeNull();
        }

        [TestMethod] // Regression test for SLVS-2946
        public void SaveAndLoadSettings()
        {
            string serializedText = null;
            var file = CreateSaveAndReloadFile();
            var fileSystem = CreateFileSystem(file);
            var testSubject = CreateTestSubject(fileSystem: fileSystem.Object);

            var projectKey = "projectKey";
            var original = new RoslynSettings
            {
                SonarProjectKey = projectKey,
                Suppressions = new[]
                {
                    CreateIssue("rule1", "path1", null, "hash", RoslynLanguage.CSharp), // null line number
                    CreateIssue("RULE2", "PATH2", 111, null, RoslynLanguage.VB)         // null hash
                }
            };

            // Act
            testSubject.Update(original, "any");
            var reloaded = testSubject.Get(projectKey);

            reloaded.SonarProjectKey.Should().Be(projectKey);
            reloaded.Suppressions.Should().NotBeNull();
            reloaded.Suppressions.Count().Should().Be(2);

            var firstSuppression = reloaded.Suppressions.First();
            firstSuppression.RoslynRuleId.Should().Be("rule1");
            firstSuppression.FilePath.Should().Be("path1");
            firstSuppression.RoslynIssueLine.Should().BeNull();
            firstSuppression.Hash.Should().Be("hash");
            firstSuppression.RoslynLanguage.Should().Be(RoslynLanguage.CSharp);

            var secondSuppression = reloaded.Suppressions.Last();
            secondSuppression.RoslynRuleId.Should().Be("RULE2");
            secondSuppression.FilePath.Should().Be("PATH2");
            secondSuppression.RoslynIssueLine.Should().Be(111);
            secondSuppression.Hash.Should().BeNull();
            secondSuppression.RoslynLanguage.Should().Be(RoslynLanguage.VB);

            Mock<IFile> CreateSaveAndReloadFile()
            {
                // Create a file that that handles "saving" data and
                // "reading" the saved file back again.
                var file = new Mock<IFile>();

                // "Save" the data that was written
                file.Setup(x => x.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<string, string>((_, contents) => { serializedText = contents; });

                // "Load" the saved data
                // Note: using a function here, so the method returns the value of serializedText when the
                // method is called, rather than when the mock is created (which would always be null)
                file.Setup(x => x.ReadAllText(It.IsAny<string>()))
                    .Returns(
                        () => {
                            serializedText.Should().NotBeNull("Test error: data has not been saved");
                            return serializedText;
                        });

                return file;
            }
        }

        private RoslynSettingsFileStorage CreateTestSubject(ILogger logger = null, IFileSystem fileSystem = null)
        {
            logger ??= Mock.Of<ILogger>();
            fileSystem ??= CreateFileSystem().Object;
            return new RoslynSettingsFileStorage(logger, fileSystem);
        }

        private Mock<IFileSystem> CreateFileSystem(Mock<IFile> file = null, bool fileExists = true)
        {
            file ??= new Mock<IFile>();
            file.Setup(f => f.Exists(It.IsAny<string>())).Returns(fileExists);

            var directoryObject = Mock.Of<IDirectory>();
            var fileSystem = new Mock<IFileSystem>();

            fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
            fileSystem.SetupGet(fs => fs.Directory).Returns(directoryObject);
            
            return fileSystem;
        }

        private static void CheckFileWritten(Mock<IFile> file, RoslynSettings settings, string solutionName)
        {
            var expectedFilePath = GetFilePath(solutionName);
            var expectedContent = JsonConvert.SerializeObject(settings, Formatting.Indented);

            file.Verify(f => f.WriteAllText(expectedFilePath, expectedContent), Times.Once);
        }

        private static string GetFilePath(string projectKey) => RoslynSettingsFileInfo.GetSettingsFilePath(projectKey);

        private Mock<IFile> CreateFileForGet(RoslynSettings settings)
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath(settings.SonarProjectKey))).Returns(JsonConvert.SerializeObject(settings));
            return file;
        }

    }
}
