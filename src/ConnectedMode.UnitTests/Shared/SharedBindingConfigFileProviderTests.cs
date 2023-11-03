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
using System.IO;
using System.IO.Abstractions;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Shared
{
    [TestClass]
    public class SharedBindingConfigFileProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SharedBindingConfigFileProvider, ISharedBindingConfigFileProvider>(
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void ReadSharedBindingConfigFile_SQConfig_Reads()
        {
            var configFileContent = @"{""SonarQubeUri"":""https://127.0.0.1:9000"",""ProjectKey"":""projectKey""}";
            var filePath = "Some Path";
            var uri = new Uri("https://127.0.0.1:9000");

            var logger = new TestLogger();
            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.ReadSharedBindingConfigFile(filePath);

            result.Uri.Should().BeEquivalentTo(uri);
            result.ProjectKey.Should().Be("projectKey");
            result.Organization.Should().BeNull();
        }

        [TestMethod]
        public void ReadSharedBindingConfigFile_SCConfig_Reads()
        {
            string configFileContent = @"{""SonarCloudOrganization"":""Some Organisation"",""ProjectKey"":""projectKey""}";
            string filePath = "Some Path";
            var uri = SharedBindingConfigFileProvider.SonarCloudUri;

            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.ReadSharedBindingConfigFile(filePath);

            result.Organization.Should().Be("Some Organisation");
            result.ProjectKey.Should().Be("projectKey");
            result.Uri.Should().BeEquivalentTo(uri);
        }

        [TestMethod]
        public void ReadSharedBindingConfig_FileDoesNotExist_ReturnsNull()
        {
            string filePath = "Some Path";

            var file = CreateFile();
            file.Setup(f => f.ReadAllText(filePath)).Throws(new FileNotFoundException());

            var fileSystem = GetFileSystem(file.Object);
            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.ReadSharedBindingConfigFile(filePath);

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

            var result = testSubject.ReadSharedBindingConfigFile(filePath);

            result.Should().BeNull();
        }

        [TestMethod]
        public void ReadSharedBindingConfigFile_InvalidUri_ReturnsNull()
        {
            var configFileContent = @"{""SonarQubeUri"":""not URI"",""ProjectKey"":""projectKey""}";
            var filePath = "Some Path";

            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.ReadSharedBindingConfigFile(filePath);

            result.Should().BeNull();
        }
        
        [TestMethod]
        public void ReadSharedBindingConfigFile_InvalidProjectKey_ReturnsNull()
        {
            var configFileContent = @"{""SonarQubeUri"":""http://localhost"",""ProjectKey"":""  ""}";
            var filePath = "Some Path";

            var file = CreateFile(filePath, configFileContent);
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.ReadSharedBindingConfigFile(filePath);

            result.Should().BeNull();
        }

        [TestMethod]
        public void WriteSharedBindingConfigFile_SQConfig_Writes()
        {
            var configFileContent = @"{
  ""SonarQubeUri"": ""https://127.0.0.1:9000"",
  ""ProjectKey"": ""projectKey""
}";
            var config = new SharedBindingConfigModel() { Uri = new Uri("https://127.0.0.1:9000"), ProjectKey = "projectKey" };
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.WriteSharedBindingConfigFile(filePath, config);

            result.Should().BeTrue();

            file.Verify(f => f.WriteAllText(filePath, configFileContent), Times.Once);
            file.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void WriteSharedBindingConfigFile_SCConfig_Writes()
        {
            string configFileContent = @"{
  ""SonarCloudOrganization"": ""Some Organisation"",
  ""ProjectKey"": ""projectKey""
}";
            var config = new SharedBindingConfigModel() { Organization = "Some Organisation", ProjectKey = "projectKey" };
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();
            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.WriteSharedBindingConfigFile(filePath, config);

            result.Should().BeTrue();

            file.Verify(f => f.WriteAllText(filePath, configFileContent), Times.Once);
            file.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void WriteSharedBindingConfigFile_WriteError_ReturnsFalse()
        {
            var config = new SharedBindingConfigModel() { Organization = "Some Organisation", ProjectKey = "projectKey" };
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();
            file.Setup(f => f.WriteAllText(filePath, It.IsAny<string>())).Throws(new FileNotFoundException());

            var fileSystem = GetFileSystem(file.Object);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.WriteSharedBindingConfigFile(filePath, config);

            result.Should().BeFalse();

            file.Verify(f => f.WriteAllText(filePath, It.IsAny<string>()), Times.Once);
            file.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void WriteSharedBindingConfigFile_DirectoryDoesNotExist_Creates()
        {
            var config = new SharedBindingConfigModel();
            var filePath = "C:\\Solution\\.sonarlint\\Solution.json";

            var file = CreateFile();

            var directory = new Mock<IDirectory>();
            directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            directory.Setup(d => d.Exists("C:\\Solution\\.sonarlint")).Returns(false);

            var fileSystem = GetFileSystem(file.Object, directory);

            var testSubject = CreateTestSubject(fileSystem.Object);

            var result = testSubject.WriteSharedBindingConfigFile(filePath, config);

            result.Should().BeTrue();
            directory.Verify(d => d.CreateDirectory("C:\\Solution\\.sonarlint"), Times.Once);
            directory.Verify(d => d.Exists("C:\\Solution\\.sonarlint"), Times.Once);
            directory.VerifyNoOtherCalls();
        }

        static SharedBindingConfigFileProvider CreateTestSubject(IFileSystem fileSystem)
        {
            return new SharedBindingConfigFileProvider(Mock.Of<ILogger>(), fileSystem);
        }

        private static Mock<IFileSystem> GetFileSystem(IFile file, Mock<IDirectory> directory = null)
        {
            if (directory == null)
            {
                directory = new Mock<IDirectory>();
                directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);
            }
            var fileSystem = new Mock<IFileSystem>();
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
