/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.Integration.UnitTests;
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
            MefTestHelpers.CheckTypeCanBeImported<RoslynSettingsFileStorage, IRoslynSettingsFileStorage>(null, new[]
            {
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>())
            });
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

            fileStorage.Update(settings);

            CheckFileWritten(file, settings);
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
        public void Update_ProjectKeyHasInvalidChars_InvalidCharsReplaced()
        {
            var settings = new RoslynSettings { SonarProjectKey = "project:key" };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = new RoslynSettingsFileStorage(logger, fileSystem.Object);
            fileStorage.Update(settings);

            CheckFileWritten(file, settings);
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

            fileStorage.Update(settings);

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

            fileStorage.Update(settings);

            CheckFileWritten(file, settings);
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

        private RoslynSettingsFileStorage CreateTestSubject(ILogger logger = null, IFileSystem fileSystem = null)
        {
            logger = logger ?? Mock.Of<ILogger>();
            fileSystem = fileSystem ?? CreateFileSystem().Object;
            return new RoslynSettingsFileStorage(logger, fileSystem);
        }

        private Mock<IFileSystem> CreateFileSystem(Mock<IFile> file = null, bool fileExists = true)
        {
            file = file ?? new Mock<IFile>();
            file.Setup(f => f.Exists(It.IsAny<string>())).Returns(fileExists);

            var directoryObject = Mock.Of<IDirectory>();
            var fileSystem = new Mock<IFileSystem>();

            
            fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
            fileSystem.SetupGet(fs => fs.Directory).Returns(directoryObject);
            
            return fileSystem;
        }

        private static void CheckFileWritten(Mock<IFile> file, RoslynSettings settings)
        {
            var expectedFilePath = GetFilePath(settings.SonarProjectKey);
            var expectedContent = JsonConvert.SerializeObject(settings);

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
