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
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Roslyn.Suppression.SettingsFile;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Suppression
{
    [TestClass]
    public class SuppressedIssuesFileStorageTests
    {

        [TestMethod]
        public void Update_HasIssues_IssuesWrittenToFile()
        {
            SonarQubeIssue issue1 = CreateIssue("issueKey1");
            SonarQubeIssue issue2 = CreateIssue("issueKey2");

            var issues = new List<SonarQubeIssue> { issue1, issue2 };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            fileStorage.Update("projectKey", issues);

            file.Verify(f => f.WriteAllText(GetFilePath("projectKey"), JsonConvert.SerializeObject(issues)), Times.Once);
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Get_HasIssues_IssuesReadFromFile()
        {
            SonarQubeIssue issue1 = CreateIssue("issueKey1");
            SonarQubeIssue issue2 = CreateIssue("issueKey2");
            var issues = new List<SonarQubeIssue> { issue1, issue2 };
            
            var logger = new TestLogger();

            var file = CreateFileForGet("projectKey", issues);
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.AssertNoOutputMessages();

            issuesGotten.Count.Should().Be(2);
            issuesGotten[0].IssueKey.Should().Be(issue1.IssueKey);
            issuesGotten[1].IssueKey.Should().Be(issue2.IssueKey);
        }

        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        [TestMethod]
        public void Update_ProjectKeyIsEmpty_ExceptionThrown(string projectKey)
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var fileStorage = CreateTestSubject();

            Action act = () => fileStorage.Update(projectKey, issues);
            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("sonarProjectKey");
        }

        [TestMethod]
        public void Update_ProjectKeyHasInvalidChars_InvalidCharsReplaced()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);
            fileStorage.Update("project:Key", issues);

            file.Verify(f => f.WriteAllText(GetFilePath("project_Key"), JsonConvert.SerializeObject(issues)), Times.Once);
            logger.AssertNoOutputMessages();
        }

        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        [TestMethod]
        public void Get_ProjectKeyIsEmpty_ExceptionThrown(string projectKey)
        {
            var fileStorage = CreateTestSubject();

            Action act = () => fileStorage.Get(projectKey);
            act.Should().ThrowExactly<ArgumentException>().And.ParamName.Should().Be("sonarProjectKey");
        }

        [TestMethod]
        public void Get_ProjectKeyHasInvalidChars_InvalidCharsReplaced()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var file = CreateFileForGet("project_Key", issues);
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("project:Key").ToList();

            file.Verify(f => f.ReadAllText(GetFilePath("project_Key")), Times.Once);
            logger.AssertNoOutputMessages();

            issuesGotten.Count.Should().Be(1);
            issuesGotten[0].IssueKey.Should().Be(issue.IssueKey);
        }

        [TestMethod]
        public void Update_ErrorOccuredWhenWritingFile_ErrorIsLogged()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            fileStorage.Update("projectKey", issues);

            logger.AssertOutputStrings("[Roslyn Suppressions] Error writing settings for project projectKey. Issues suppressed on the server may not be suppressed in the IDE. Error: Test Exception");
        }

        [TestMethod]
        public void Get_ErrorOccuredWhenWritingFile_ErrorIsLoggedAndReturnedEmpty()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);
            SuppressedIssuesFileStorage fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            logger.AssertOutputStrings("[Roslyn Suppressions] Error loading settings for project projectKey. Issues suppressed on the server will not be suppressed in the IDE. Error: Test Exception");
            issuesGotten.Count.Should().Be(0);
        }

        [TestMethod]
        public void Update_HasNoIssues_FileWritten()
        {
            var issues = Enumerable.Empty<SonarQubeIssue>();

            var logger = new TestLogger();

            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            fileStorage.Update("projectKey", issues);

            file.Verify(f => f.WriteAllText(GetFilePath("projectKey"), "[]"), Times.Once);
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Get_HasNoIssues_ReturnsEmpty()
        {
            var issues = Enumerable.Empty<SonarQubeIssue>();

            var logger = new TestLogger();

            var file = CreateFileForGet("projectKey", issues);
            Mock<IFileSystem> fileSystem = CreateFileSystem(file);

            var fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.AssertNoOutputMessages();

            issuesGotten.Count.Should().Be(0);
        }

        [TestMethod]
        public void Get_FileDoesNotExist_ErrorIsLoggedAndReturnedEmpty()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            Mock<IFileSystem> fileSystem = CreateFileSystem(fileExists:false);
            SuppressedIssuesFileStorage fileStorage = CreateTestSubject(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            logger.AssertOutputStrings("[Roslyn Suppressions] Error loading settings for project projectKey. Issues suppressed on the server will not be suppressed in the IDE. Error: Settings File was not found");
            issuesGotten.Count.Should().Be(0);
        }

        private SuppressedIssuesFileStorage CreateTestSubject(ILogger logger = null, IFileSystem fileSystem = null)
        {
            logger = logger ?? Mock.Of<ILogger>();
            fileSystem = fileSystem ?? CreateFileSystem().Object;
            return new SuppressedIssuesFileStorage(logger, fileSystem);
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

        private static SonarQubeIssue CreateIssue(string issueKey)
        {
            return new SonarQubeIssue(issueKey, 
                "path", 
                "hash", 
                "message", 
                "moduleKey", 
                "ruleId", 
                true, 
                SonarQubeIssueSeverity.Blocker, 
                DateTimeOffset.Now, 
                DateTimeOffset.Now, 
                new IssueTextRange(0, 1, 2, 3),
                new List<IssueFlow> { 
                    new IssueFlow(
                        new List<IssueLocation> { 
                            new IssueLocation("filepath",
                                "moduleKey",
                                new IssueTextRange(10, 11, 12, 13), "locationMEssage") }) });
        }

        private static string GetTempPath() => Path.Combine(Path.GetTempPath(), "SLVS", "Roslyn");

        private static string GetFilePath(string projectKey) => Path.Combine(GetTempPath(), projectKey + ".json");

        private Mock<IFile> CreateFileForGet(string projectKey, IEnumerable<SonarQubeIssue> issues)
        {
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath(projectKey))).Returns(JsonConvert.SerializeObject(issues));
            return file;
        }
    }
}
