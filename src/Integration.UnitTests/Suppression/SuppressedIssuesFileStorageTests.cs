using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.Suppression;
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

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);


            fileStorage.Update("projectKey", issues);

            directory.Verify(d => d.CreateDirectory(GetTempPath()), Times.Once);
            file.Verify(f => f.WriteAllText(GetFilePath("projectKey"), JsonConvert.SerializeObject(issues)), Times.Once);
            logger.Verify(l => l.WriteLine(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void Update_Should_WriteMultipleIssuesToFilePath()
        {
            SonarQubeIssue issue1 = CreateIssue("issueKey1");
            SonarQubeIssue issue2 = CreateIssue("issueKey2");

            var issues = new List<SonarQubeIssue> { issue1, issue2 };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);


            fileStorage.Update("projectKey", issues);

            directory.Verify(d => d.CreateDirectory(GetTempPath()), Times.Once);
            file.Verify(f => f.WriteAllText(GetFilePath("projectKey"), JsonConvert.SerializeObject(issues)), Times.Once);
            logger.Verify(l => l.WriteLine(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);
        }

        [TestMethod]
        public void Get_Should_ReadSingleIssueFromFilePath()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Returns(JsonConvert.SerializeObject(issues));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            directory.Verify(d => d.CreateDirectory(It.IsAny<string>()), Times.Never);
            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.Verify(l => l.WriteLine(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);

            Assert.AreEqual(1, issuesGotten?.Count);
            Assert.AreEqual(issue.IssueKey, issuesGotten[0].IssueKey);
        }

        [TestMethod]
        public void Get_Should_ReadMultipleIssuesFromFilePath()
        {
            SonarQubeIssue issue1 = CreateIssue("issueKey1");
            SonarQubeIssue issue2 = CreateIssue("issueKey2");
            var issues = new List<SonarQubeIssue> { issue1, issue2 };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Returns(JsonConvert.SerializeObject(issues));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            directory.Verify(d => d.CreateDirectory(It.IsAny<string>()), Times.Never);
            file.Verify(f => f.ReadAllText(GetFilePath("projectKey")), Times.Once);
            logger.Verify(l => l.WriteLine(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never);

            Assert.AreEqual(2, issuesGotten?.Count);
            Assert.AreEqual(issue1.IssueKey, issuesGotten[0].IssueKey);
            Assert.AreEqual(issue2.IssueKey, issuesGotten[1].IssueKey);
        }

        [TestMethod]
        public void Update_Should_LogWhenProjectKeyIsNull()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);
            fileStorage.Update(null, issues);

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name should not be empty"), Times.Once);
            file.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Update_Should_LogWhenProjectKeyIsEmpty()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);
            fileStorage.Update(string.Empty, issues);

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name should not be empty"), Times.Once);
            file.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Update_Should_LogWhenProjectKeyIsWhiteSpace()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);
            fileStorage.Update("  ", issues);

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name should not be empty"), Times.Once);
            file.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Update_Should_LogWhenProjectKeyHasInvalidChars()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);
            fileStorage.Update("project:Key", issues);

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name has illegal characters"), Times.Once);
            file.Verify(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Get_Should_LogWhenProjectKeyIsNull()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Returns(JsonConvert.SerializeObject(issues));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);

            var issuesGotten = fileStorage.Get(null).ToList();

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name should not be empty"), Times.Once);
            Assert.AreEqual(0, issuesGotten.Count);
        }

        [TestMethod]
        public void Get_Should_LogWhenProjectKeyIsEmpty()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Returns(JsonConvert.SerializeObject(issues));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);

            var issuesGotten = fileStorage.Get(string.Empty).ToList();

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name should not be empty"), Times.Once);
            Assert.AreEqual(0, issuesGotten.Count);
        }

        [TestMethod]
        public void Get_Should_LogWhenProjectKeyIsWhiteSpace()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Returns(JsonConvert.SerializeObject(issues));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);

            var issuesGotten = fileStorage.Get("   ").ToList();

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name should not be empty"), Times.Once);
            Assert.AreEqual(0, issuesGotten.Count);
        }

        [TestMethod]
        public void Get_Should_LogWhenProjectKeyHasInvalidChars()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Returns(JsonConvert.SerializeObject(issues));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);

            var issuesGotten = fileStorage.Get("project:Key").ToList();

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage: File name has illegal characters"), Times.Once);
            Assert.AreEqual(0, issuesGotten.Count);
        }

        [TestMethod]
        public void Update_Should_LogWhenAnErrorIsThrown()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);


            fileStorage.Update("projectKey", issues);

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage Update Error: Test Exception"), Times.Once);
        }

        [TestMethod]
        public void Get_Should_LogWhenAnErrorIsThrown()
        {
            SonarQubeIssue issue = CreateIssue("issueKey");
            var issues = new List<SonarQubeIssue> { issue };

            var logger = new Mock<ILogger>();

            var directory = new Mock<IDirectory>();
            var file = new Mock<IFile>();
            file.Setup(f => f.ReadAllText(GetFilePath("projectKey"))).Throws(new Exception("Test Exception"));
            Mock<IFileSystem> fileSystem = CreateFileSystem(directory, file);

            var fileStorage = new SuppressedIssuesFileStorage(logger.Object, fileSystem.Object);

            var issuesGotten = fileStorage.Get("projectKey").ToList();

            logger.Verify(l => l.WriteLine("SuppressedIssuesFileStorage Get Error: Test Exception"), Times.Once);
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

    }
}
