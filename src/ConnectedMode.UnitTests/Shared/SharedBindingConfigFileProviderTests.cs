/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Shared
{
    [TestClass]
    public class SharedBindingConfigFileProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<SharedBindingConfigFileProvider, ISharedBindingConfigFileProvider>(
                MefTestHelpers.CreateExport<ILogger>(), MefTestHelpers.CreateExport<IFileSystemService>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() =>
            MefTestHelpers.CheckIsSingletonMefComponent<SharedBindingConfigFileProvider>();

        [DataRow("""{"SonarQubeUri":"https://127.0.0.1:9000","ProjectKey":"projectKey"}""")]
        [DataRow("""{"sonarQubeUri":"https://127.0.0.1:9000","projectKey":"projectKey"}""")]
        [TestMethod]
        public void Read_SQConfig_Reads(string configFileContent)
        {
            var filePath = "Some Path";
            var uri = new Uri("https://127.0.0.1:9000");

            var logger = new TestLogger();
            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Read(filePath);

            result.Uri.Should().BeEquivalentTo(uri);
            result.ProjectKey.Should().Be("projectKey");
            result.Organization.Should().BeNull();
        }

        [DataRow("""{"SonarCloudOrganization":"Some Organisation","ProjectKey":"projectKey"}""", "EU")]
        [DataRow("""{"sonarCloudOrganization":"Some Organisation","projectKey":"projectKey"}""", "EU")]
        [DataRow("""{"SonarCloudOrganization":"Some Organisation","Region": "EU","ProjectKey":"projectKey"}""", "EU")]
        [DataRow("""{"sonarCloudOrganization":"Some Organisation","region": "EU","projectKey":"projectKey"}""", "EU")]
        [DataRow("""{"SonarCloudOrganization":"Some Organisation","Region": "US","ProjectKey":"projectKey"}""", "US")]
        [DataRow("""{"sonarCloudOrganization":"Some Organisation","region": "US","projectKey":"projectKey"}""", "US")]
        [DataTestMethod]
        public void Read_SCConfig_Reads(string configFileContent, string region)
        {
            string filePath = "Some Path";
            var cloudServerRegion = CloudServerRegion.GetRegionByName(region);

            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Read(filePath);

            result.Organization.Should().Be("Some Organisation");
            result.ProjectKey.Should().Be("projectKey");
            result.Region.Should().Be(cloudServerRegion.Name);
            result.Uri.Should().BeEquivalentTo(cloudServerRegion.Url);
        }

        [TestMethod]
        public void ReadSharedBindingConfig_FileDoesNotExist_ReturnsNull()
        {
            string filePath = "Some Path";

            var file = CreateFile();
            file.Setup(f => f.ReadAllText(filePath)).Throws(new FileNotFoundException());

            var fileSystem = GetFileSystem(file.Object);
            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Read(filePath);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ReadSharedBindingConfig_FileDoesDeserialize_ReturnsNull()
        {
            string configFileContent = @"not json";
            string filePath = "Some Path";

            var file = CreateFile(filePath, configFileContent);

            var fileSystem = GetFileSystem(file.Object);
            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Read(filePath);

            result.Should().BeNull();
        }

        [DataRow("""{"SonarQubeUri":"not URI","ProjectKey":"projectKey"}""")]
        [DataRow("""{"sonarQubeUri":"not URI","projectKey":"projectKey"}""")]
        [DataTestMethod]
        public void Read_InvalidUri_ReturnsNull(string configFileContent)
        {
            var filePath = "Some Path";

            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Read(filePath);

            result.Should().BeNull();
        }

        [DataRow("""{"SonarQubeUri":"http://localhost","ProjectKey":"  "}""")]
        [DataRow("""{"sonarQubeUri":"http://localhost","projectKey":"  "}""")]
        [TestMethod]
        public void Read_InvalidProjectKey_ReturnsNull(string configFileContent)
        {
            var filePath = "Some Path";

            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Read(filePath);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Write_SQConfig_Writes()
        {
            var configFileContent = """
                                    {
                                      "sonarQubeUri": "https://127.0.0.1:9000",
                                      "projectKey": "projectKey"
                                    }
                                    """;
            var config = new SharedBindingConfigModel() { Uri = new Uri("https://127.0.0.1:9000"), ProjectKey = "projectKey" };
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Write(filePath, config);

            result.Should().BeTrue();

            file.Verify(f => f.WriteAllText(filePath, configFileContent), Times.Once);
            file.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Write_SCConfig_DefaultRegion_Writes()
        {
            string configFileContent = """
                                       {
                                         "sonarCloudOrganization": "Some Organisation",
                                         "region": "EU",
                                         "projectKey": "projectKey"
                                       }
                                       """;
            var config = new SharedBindingConfigModel() { Organization = "Some Organisation", ProjectKey = "projectKey" };
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Write(filePath, config);

            result.Should().BeTrue();

            file.Verify(f => f.WriteAllText(filePath, configFileContent), Times.Once);
            file.VerifyNoOtherCalls();
        }

        [DataRow(
            """
            {
              "sonarCloudOrganization": "Some Organisation",
              "region": "EU",
              "projectKey": "projectKey"
            }
            """,
            "EU")]
        [DataRow(
            """
            {
              "sonarCloudOrganization": "Some Organisation",
              "region": "US",
              "projectKey": "projectKey"
            }
            """,
            "US")]
        [DataTestMethod]
        public void Write_SCConfig_NonDefaultRegion_Writes(string configFileContent, string region)
        {
            var config = new SharedBindingConfigModel() { Organization = "Some Organisation", ProjectKey = "projectKey", Region = region };
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Write(filePath, config);

            result.Should().BeTrue();

            file.Verify(f => f.WriteAllText(filePath, configFileContent), Times.Once);
            file.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Write_WriteError_ReturnsFalse()
        {
            var config = new SharedBindingConfigModel() { Organization = "Some Organisation", ProjectKey = "projectKey" };
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();
            file.Setup(f => f.WriteAllText(filePath, It.IsAny<string>())).Throws(new FileNotFoundException());

            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Write(filePath, config);

            result.Should().BeFalse();

            file.Verify(f => f.WriteAllText(filePath, It.IsAny<string>()), Times.Once);
            file.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Write_DirectoryDoesNotExist_Creates()
        {
            var config = new SharedBindingConfigModel();
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();

            var directory = new Mock<IDirectory>();
            directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            directory.Setup(d => d.Exists("C:\\Solution\\.sonarlint")).Returns(false);

            var fileSystem = GetFileSystem(file.Object, directory);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.Write(filePath, config);

            result.Should().BeTrue();
            directory.Verify(d => d.CreateDirectory("C:\\Solution\\.sonarlint"), Times.Once);
            directory.Verify(d => d.Exists("C:\\Solution\\.sonarlint"), Times.Once);
            directory.VerifyNoOtherCalls();
        }

        [DataRow(true)]
        [DataRow(false)]
        [DataTestMethod]
        public void Exists_CallsFileSystemExists(bool exists)
        {
            const string filePath = "C:\\Solution\\.sonarlint\\Solution.json";
            var fileSystemService = Substitute.For<IFileSystemService>();
            fileSystemService.File.Exists(filePath).Returns(exists);
            var testSubject = CreateTestSubject(fileSystemService);

            testSubject.Exists(filePath).Should().Be(exists);
        }

        static SharedBindingConfigFileProvider CreateTestSubject(IFileSystemService fileSystem)
        {
            return new SharedBindingConfigFileProvider(Mock.Of<ILogger>(), fileSystem);
        }

        private static Mock<IFileSystemService> GetFileSystem(IFile file, Mock<IDirectory> directory = null)
        {
            if (directory == null)
            {
                directory = new Mock<IDirectory>();
                directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            }
            var fileSystem = new Mock<IFileSystemService>();
            fileSystem.Setup(fs => fs.File).Returns(file);
            fileSystem.Setup(fs => fs.Directory).Returns(directory.Object);

            return fileSystem;
        }

        private static Mock<IFile> CreateFile(string filePath = null, string configFileContent = null)
        {
            var file = new Mock<IFile>();
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                file.Setup(f => f.ReadAllText(filePath)).Returns(configFileContent);
            }
            return file;
        }
    }
}
