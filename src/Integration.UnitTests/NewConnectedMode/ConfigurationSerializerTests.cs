/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using FluentAssertions;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Helpers;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ConfigurationSerializerTests
    {
        private Mock<IVsSolution> solutionMock;
        private ICredentialStore configurableStore;
        private Mock<ILogger> loggerMock;
        private Mock<IFile> fileMock;
        private ConfigurationSerializer testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            solutionMock = new Mock<IVsSolution>();
            configurableStore = new ConfigurableCredentialStore();
            loggerMock = new Mock<ILogger>();
            fileMock = new Mock<IFile>();
            testSubject = new ConfigurationSerializer(solutionMock.Object, configurableStore, loggerMock.Object, fileMock.Object);
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullSolution_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationSerializer(null, configurableStore, loggerMock.Object);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("solution");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullCredentialStore_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationSerializer(solutionMock.Object, null, loggerMock.Object);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("credentialStore");
        }

        [TestMethod]
        public void Ctor_InvalidArgs_NullLogger_Throws()
        {
            // Arrange
            Action act = () => new ConfigurationSerializer(solutionMock.Object, configurableStore, null);

            // Act & Assert
            act.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("logger");
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
            actual.Should().Be(@"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.sqconfig");
        }

        [TestMethod]
        public void CalculateConfigFileName_ReturnsExpectedName2()
        {
            // Arrange
            var fullSolutionFilePath = @"c:\aaa\bbbb\C C\mysolutionName.foo.xxx";

            // Act
            string actual = ConfigurationSerializer.GetConnectionFilePath(fullSolutionFilePath);

            // Assert
            actual.Should().Be(@"c:\aaa\bbbb\C C\.sonarlint\mysolutionName.foo.sqconfig");
        }

        [TestMethod]
        public void ReadSolution_NoFileName_ReturnsNull()
        {
            // Arrange
            Mock<IVsSolution> solutionMock = new Mock<IVsSolution>();
            ConfigurableCredentialStore configurableStore = new ConfigurableCredentialStore();
            Mock<ILogger> loggerMock = new Mock<ILogger>();

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
        public void ReadSolution_SolutionFileExists_NoCredentials_ReturnsConfig()
        {
            // Arrange
            var validConfig = @"{
  ""ServerUri"": ""http://xxx.www.zzz/yyy:9000"",
  ""Organization"": null,
  ""ProjectKey"": ""MyProject Key""
}";
            SetSolutionFilePath(@"c:\mysolutionfile.foo");
            SetFileContents(@"c:\.sonarlint\mysolutionfile.sqconfig", validConfig);

            // Act
            var actual = testSubject.ReadSolutionBinding();

            // Assert
            actual.Should().NotBeNull();
            actual.Credentials.Should().BeNull();
            actual.ServerUri.Should().Be("http://xxx.www.zzz/yyy:9000");
            actual.Organization.Should().BeNull();
            actual.ProjectKey.Should().Be("MyProject Key");
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
            SetFileContents(@"c:\.sonarlint\mysolutionfile.sqconfig", validConfig);

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

        private void SetSolutionFilePath(string filePath)
        {
            object outVal = filePath;
            int returnCode = filePath == null ? VSConstants.S_FALSE : VSConstants.S_OK;
            solutionMock.Setup(x => x.GetProperty((int)__VSPROPID.VSPROPID_SolutionFileName, out outVal)).Returns(returnCode);
        }

        private void SetFileContents(string fullPath, string contents)
        {
            fileMock.Setup(x => x.Exists(fullPath)).Returns(true);
            fileMock.Setup(x => x.ReadAllText(fullPath)).Returns(contents);
        }
    }
}
