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
using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConfigurationSerializerTests
    {
        private Mock<IVsSolution> solutionMock;
        private ConfigurableSourceControlledFileSystem configurableSccFileSystem;
        private ICredentialStoreService configurableStore;
        private Mock<ILogger> loggerMock;
        private Mock<IFileSystem> fileMock;
        private ConfigurationSerializer testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            solutionMock = new Mock<IVsSolution>();
            configurableSccFileSystem = new ConfigurableSourceControlledFileSystem();
            configurableStore = new ConfigurableCredentialStore();
            loggerMock = new Mock<ILogger>();
            fileMock = new Mock<IFileSystem>();
            testSubject = new ConfigurationSerializer(solutionMock.Object, configurableSccFileSystem, configurableStore, loggerMock.Object, fileMock.Object);
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullSolution_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationSerializer(null, configurableSccFileSystem, configurableStore, loggerMock.Object);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solution");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullCredentialStore_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationSerializer(solutionMock.Object, configurableSccFileSystem, null, loggerMock.Object);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("store");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullLogger_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationSerializer(solutionMock.Object, configurableSccFileSystem, configurableStore, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void CalculateConfigFileName_NullInput_ReturnsNull()
        {
            // Arrange & Act & Assert
            ConfigurationSerializer.GetConnectionFilePath(null).Should().BeNull();
        }

        [TestMethod]
        public void CalculateConfigFileName_ReturnsExpectedName()
        {
            // Arrange
            var fullSolutionFilePath = @"c:\aaa\bbbb\C C\mysolutionName.sln";

            // Act
            string actual = ConfigurationSerializer.GetConnectionFilePath(fullSolutionFilePath);

            // Assert
            actual.Should().Be(@"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.slconfig");
        }

        [TestMethod]
        public void CalculateConfigFileName_ReturnsExpectedName2()
        {
            // Arrange
            var fullSolutionFilePath = @"c:\aaa\bbbb\C C\mysolutionName.foo.xxx";

            // Act
            string actual = ConfigurationSerializer.GetConnectionFilePath(fullSolutionFilePath);

            // Assert
            actual.Should().Be(@"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.foo.slconfig");
        }

        [TestMethod]
        public void ReadSolution_NoFileName_ReturnsNull()
        {
            // Arrange
            SetSolutionFilePath(null);

            // Act
            var actual = testSubject.ReadSolutionBinding();

            // Assert
            actual.Should().BeNull();
            solutionMock.VerifyAll(); // check the solution interface was called with the correct args
        }

        [TestMethod]
        public void ReadSolution_MissingSolutionFile_ReturnsNull()
        {
            // Arrange
            SetSolutionFilePath(@"c:\file that does not.exist");

            // Act
            var actual = testSubject.ReadSolutionBinding();

            // Assert
            actual.Should().BeNull();
            solutionMock.VerifyAll(); // check the solution interface was called with the correct args
        }

        [TestMethod]
        public void ReadSolution_SolutionFileExists_NoCredentials_WithQPs_ReturnsConfig()
        {
            // Arrange
            var validConfig = @"{
  'ServerUri': 'http://xxx.www.zzz/yyy:9000',
  'Organization': null,
  'ProjectKey': 'MyProject Key',
  'ProjectName': 'junk',
  'Profiles': {
    'CSharp': {
      'ProfileKey': 'AW7HxNmwSOyv3m_jy9io',
      'ProfileTimestamp': '2019-12-02T17:59:57+00:01'
    },
    'VB': {
      'ProfileKey': 'AW7HxNTWSOyv3m_jy9H0',
      'ProfileTimestamp': '2019-12-02T17:59:55+00:02'
    },
    'C++': {
      'ProfileKey': 'AW7HxNXDSOyv3m_jy9PK',
      'ProfileTimestamp': '2019-12-02T17:59:56+00:03'
    },
    'C': {
      'ProfileKey': 'AW7HxNZhSOyv3m_jy9Vg',
      'ProfileTimestamp': '2019-12-02T17:59:56+00:04'
    }
  }
}";
            SetSolutionFilePath(@"c:\mysolutionfile.foo");
            SetFileContents(@"c:\.sonarlint\mysolutionfile.slconfig", validConfig);

            // Act
            var actual = testSubject.ReadSolutionBinding();

            // Assert
            actual.Should().NotBeNull();
            actual.Credentials.Should().BeNull();
            actual.ServerUri.Should().Be("http://xxx.www.zzz/yyy:9000");
            actual.Organization.Should().BeNull();
            actual.ProjectKey.Should().Be("MyProject Key");

            actual.Profiles.Count.Should().Be(4);
            actual.Profiles.Keys.Should().BeEquivalentTo(Core.Language.C, Core.Language.Cpp, Core.Language.VBNET, Core.Language.CSharp);
            actual.Profiles[Core.Language.CSharp].ProfileKey.Should().Be("AW7HxNmwSOyv3m_jy9io");
            actual.Profiles[Core.Language.VBNET].ProfileKey.Should().Be("AW7HxNTWSOyv3m_jy9H0");
            actual.Profiles[Core.Language.Cpp].ProfileKey.Should().Be("AW7HxNXDSOyv3m_jy9PK");
            actual.Profiles[Core.Language.C].ProfileKey.Should().Be("AW7HxNZhSOyv3m_jy9Vg");

            actual.Profiles[Core.Language.CSharp].ProfileTimestamp.Should().Be(DateTime.Parse("2019-12-02T17:59:57+00:01"));
            actual.Profiles[Core.Language.VBNET].ProfileTimestamp.Should().Be(DateTime.Parse("2019-12-02T17:59:55+00:02"));
            actual.Profiles[Core.Language.Cpp].ProfileTimestamp.Should().Be(DateTime.Parse("2019-12-02T17:59:56+00:03"));
            actual.Profiles[Core.Language.C].ProfileTimestamp.Should().Be(DateTime.Parse("2019-12-02T17:59:56+00:04"));
        }

        [TestMethod]
        public void ReadSolution_SolutionFileExists_CredentialsExist_ReturnsConfig()
        {
            // Arrange
            var validConfig = @"{
  ""ServerUri"": ""https://xxx:123"",
  ""Organization"": { ""Key"" : ""OrgKey"", ""Name"" : ""OrgName"" },
  ""ProjectKey"": ""key111""
}";
            SetSolutionFilePath(@"c:\mysolutionfile.foo");
            SetFileContents(@"c:\.sonarlint\mysolutionfile.slconfig", validConfig);

            Credential cred = new Credential("user1");
            configurableStore.WriteCredentials(new TargetUri("https://xxx:123"), cred);

            // Act
            var actual = testSubject.ReadSolutionBinding();

            // Assert
            actual.Should().NotBeNull();

            actual.Credentials.Should().NotBeNull();
            var actualCreds = actual.Credentials as BasicAuthCredentials;
            actualCreds.Should().NotBeNull();
            actualCreds.UserName.Should().Be("user1");

            actual.ServerUri.Should().Be("https://xxx:123");
            actual.Organization.Should().NotBeNull();
            actual.Organization.Key.Should().Be("OrgKey");
            actual.Organization.Name.Should().Be("OrgName");
            actual.ProjectKey.Should().Be("key111");
        }

        [TestMethod]
        public void WriteSolution_NoCredentials_FileSavedOk()
        {
            // Arrange
            SetSolutionFilePath(@"c:\mysolutionfile.foo");
            var expectedFilePath = @"c:\.sonarlint\mysolutionfile.slconfig";

            var boundProject = new BoundSonarQubeProject
            {
                ProjectKey = "mykey",
                ServerUri = new Uri("http://localhost:9000"),
            };

            // Act
            var actualFilePath = testSubject.WriteSolutionBinding(boundProject); // queue the changes
            configurableSccFileSystem.WritePendingNoErrorsExpected(); // write the queued changes

            // Assert
            actualFilePath.Should().Be(expectedFilePath);
            fileMock.Verify(x => x.File.WriteAllText(expectedFilePath, It.IsAny<string>()), Times.Once);
            var savedCredentials = configurableStore.ReadCredentials(new Uri("http://localhost:9000"));
            savedCredentials.Should().BeNull();
        }

        [TestMethod]
        public void WriteSolution_HasCredentials_FileSavedAndCredentialsSavedOk()
        {
            // Arrange
            SetSolutionFilePath(@"c:\mysolutionfile.foo");
            var expectedFilePath = @"c:\.sonarlint\mysolutionfile.slconfig";

            // Currently we can only handle saving basic auth credentials
            BasicAuthCredentials creds = new BasicAuthCredentials("user1", "1234".ToSecureString());

            var boundProject = new BoundSonarQubeProject
            {
                ProjectKey = "mykey",
                ServerUri = new Uri("http://localhost:9000"),
                Credentials = creds
            };

            // Step 1 - file write should be queued but not written
            var actualFilePath = testSubject.WriteSolutionBinding(boundProject);

            actualFilePath.Should().Be(expectedFilePath);
            configurableSccFileSystem.AssertQueuedOperationCount(1);
            fileMock.Verify(x => x.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            // Step 2 - execute the queued write
            configurableSccFileSystem.WritePendingNoErrorsExpected(); // write the queued changes

            actualFilePath.Should().Be(expectedFilePath);
            fileMock.Verify(x => x.File.WriteAllText(expectedFilePath, It.IsAny<string>()), Times.Once);
            var savedCredentials = configurableStore.ReadCredentials(new Uri("http://localhost:9000"));
            savedCredentials.Should().NotBeNull();
            savedCredentials.Username.Should().Be("user1");
            savedCredentials.Password.Should().Be("1234");
        }

        [TestMethod]
        public void WriteSolution_HasCredentials_FileSavedAndTokenOnlyCredentialSavedOk()
        {
            // Arrange
            SetSolutionFilePath(@"c:\mysolutionfile.foo");
            var expectedFilePath = @"c:\.sonarlint\mysolutionfile.slconfig";

            // Currently we can only handle saving basic auth credentials
            BasicAuthCredentials creds = new BasicAuthCredentials("user1", "".ToSecureString());

            var boundProject = new BoundSonarQubeProject
            {
                ProjectKey = "mykey",
                ServerUri = new Uri("http://localhost:9000"),
                Credentials = creds
            };

            // Act
            var actualFilePath = testSubject.WriteSolutionBinding(boundProject); // queue the changes
            configurableSccFileSystem.WritePendingNoErrorsExpected(); // write the queued changes

            // Assert
            actualFilePath.Should().Be(expectedFilePath);
            fileMock.Verify(x => x.File.WriteAllText(expectedFilePath, It.IsAny<string>()), Times.Once);
            var savedCredentials = configurableStore.ReadCredentials(new Uri("http://localhost:9000"));
            savedCredentials.Should().NotBeNull();
            savedCredentials.Username.Should().Be("user1");
            savedCredentials.Password.Should().Be("");
        }

        [TestMethod]
        public void WriteSolution_WriteOpFails_CredentialsNotSaved()
        {
            // Arrange
            SetSolutionFilePath(@"c:\my solution file.foo");
            var expectedFilePath = @"c:\.sonarlint\my solution file.slconfig";
            fileMock.Setup(x => x.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws<System.IO.IOException>();

            var boundProject = new BoundSonarQubeProject
            {
                ProjectKey = "mykey",
                ServerUri = new Uri("http://localhost:9000"),
            };

            // Act
            var actualFilePath = testSubject.WriteSolutionBinding(boundProject); // queue the changes
            configurableSccFileSystem.WritePendingErrorsExpected(); // write the queued changes

            // Assert
            actualFilePath.Should().Be(expectedFilePath);
            var savedCredentials = configurableStore.ReadCredentials(new Uri("http://localhost:9000"));
            savedCredentials.Should().BeNull();
        }

        [TestMethod]
        public void WriteSolution_WriteThrowsCriticalException_NotSuppressed()
        {
            // Arrange
            SetSolutionFilePath(@"c:\mysolutionfile.foo");
            fileMock.Setup(x => x.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws<StackOverflowException>();

            var boundProject = new BoundSonarQubeProject
            {
                ProjectKey = "mykey",
                ServerUri = new Uri("http://localhost:9000"),
            };

            testSubject.WriteSolutionBinding(boundProject); // queue the changes
            Action act = () => configurableSccFileSystem.WritePendingFiles();

            // Act & Assert
            act.Should().ThrowExactly<StackOverflowException>();
        }
        private void SetSolutionFilePath(string filePath)
        {
            object outVal = filePath;
            int returnCode = filePath == null ? VSConstants.S_FALSE : VSConstants.S_OK;
            solutionMock.Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out outVal)).Returns(returnCode);
            fileMock.Setup(x => x.File.Exists(filePath)).Returns(false);
            fileMock.Setup(x => x.Directory.Exists(Path.GetDirectoryName(filePath))).Returns(true);
        }

        private void SetFileContents(string fullPath, string contents)
        {
            fileMock.Setup(x => x.File.Exists(fullPath)).Returns(true);
            fileMock.Setup(x => x.File.ReadAllText(fullPath)).Returns(contents);
        }
    }
}
