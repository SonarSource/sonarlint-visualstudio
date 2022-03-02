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
        public void Update_Should_WriteSingleIssueToFilePath()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);

            fileStorage.Update("projectKey", issues);

            directory.Verify(d => d.CreateDirectory(GetTempPath()), Times.Once);
            file.Verify(f => f.WriteAllText(GetFilePath("projectKey"), JsonConvert.SerializeObject(issues)), Times.Once);
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Update_Should_WriteMultipleIssuesToFilePath()
        {
            SonarQubeIssue issue1 = CreateIssue("issueKey1");
            SonarQubeIssue issue2 = CreateIssue("issueKey2");

            var issues = new List<SonarQubeIssue> { issue1, issue2 };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);

            fileStorage.Update("projectKey", issues);

            directory.Verify(d => d.CreateDirectory(GetTempPath()), Times.Once);
            file.Verify(f => f.WriteAllText(GetFilePath("projectKey"), JsonConvert.SerializeObject(issues)), Times.Once);
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void Get_Should_ReadSingleIssueFromFilePath()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            Mock<IFile> file = CreateFileForGet("projectKey", issues);
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            directory.Verify(d => d.CreateDirectory(It.IsAny<string>()), Times.Never);
            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.AssertNoOutputMessages();

            Assert.AreEqual(1, issuesGotten?.Count);
            Assert.AreEqual(issue.IssueKey, issuesGotten[0].IssueKey);
        }

        [TestMethod]
        public void Get_Should_ReadMultipleIssuesFromFilePath()
        {
            SonarQubeIssue issue1 = CreateIssue("issueKey1");
            SonarQubeIssue issue2 = CreateIssue("issueKey2");
            var issues = new List<SonarQubeIssue> { issue1, issue2 };
            
            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = CreateFileForGet("projectKey", issues);
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            directory.Verify(d => d.CreateDirectory(It.IsAny<string>()), Times.Never);
            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.AssertNoOutputMessages();

            Assert.AreEqual(2, issuesGotten?.Count);
            Assert.AreEqual(issue1.IssueKey, issuesGotten[0].IssueKey);
            Assert.AreEqual(issue2.IssueKey, issuesGotten[1].IssueKey);
        }

        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        [TestMethod]
        public void Update_Should_ThrowWhenProjectKeyIsNull(string projectKey)
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);
            var ex = Assert.ThrowsException<ArgumentException>(() => fileStorage.Update(projectKey, issues));

            Assert.AreEqual("Argument must have at least one non white space character\r\nParameter name: sonarProjectKey", ex.Message);
        }

        [TestMethod]
        public void Update_Should_LogWhenProjectKeyHasInvalidChars()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);
            fileStorage.Update("project:Key", issues);


            directory.Verify(d => d.CreateDirectory(GetTempPath()), Times.Once);
            file.Verify(f => f.WriteAllText(GetFilePath("project_Key"), JsonConvert.SerializeObject(issues)), Times.Once);
            logger.AssertNoOutputMessages();
        }
        [DataRow(null)]
        [DataRow("")]
        [DataRow("  ")]
        [TestMethod]
        public void Get_Should_ThrowWhenProjectKeyIsNullOrEmpty(string projectKey)
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = CreateFileForGet(projectKey, issues);
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);

            var ex = Assert.ThrowsException<ArgumentException>(() => fileStorage.Get(projectKey));

            Assert.AreEqual("Argument must have at least one non white space character\r\nParameter name: sonarProjectKey", ex.Message);
        }

        [TestMethod]
        public void Get_Should_ReplaceWhenProjectKeyHasInvalidChars()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = CreateFileForGet("project_Key", issues);
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("project:Key").ToList();

            directory.Verify(d => d.CreateDirectory(It.IsAny<string>()), Times.Never);
            file.Verify(f => f.ReadAllText(GetFilePath("project_Key")), Times.Once);
            logger.AssertNoOutputMessages();

            Assert.AreEqual(1, issuesGotten?.Count);
            Assert.AreEqual(issue.IssueKey, issuesGotten[0].IssueKey);
        }

        [TestMethod]
        public void Update_Should_LogWhenAnErrorIsThrown()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);


            fileStorage.Update("projectKey", issues);

            logger.AssertOutputStringExists("SuppressedIssuesFileStorage Update Error: Test Exception");
            logger.AssertOutputStrings(1);
        }

        [TestMethod]
        public void Get_Should_LogWhenAnErrorIsThrown()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new TestLogger();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            logger.AssertOutputStringExists("SuppressedIssuesFileStorage Get Error: Test Exception");
            logger.AssertOutputStrings(1);
            Assert.AreEqual(0, issuesGotten.Count);
        }

        private static Mock<IFileSystem> CreateFileSystem(Mock<IDirectory> directory, Mock<IFile> file)
        {
            var fileSystem = new Mock<IFileSystem>();
            fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
            fileSystem.SetupGet(fs => fs.Directory).Returns(directory.Object);
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
