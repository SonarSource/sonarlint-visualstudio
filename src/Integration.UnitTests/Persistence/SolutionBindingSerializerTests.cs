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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Persistence;
using Moq;
using SonarLint.VisualStudio.Core.SystemAbstractions;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingSerializerTests
    {
        private Mock<ILogger> logger;
        private Mock<IFile> file;
        private Mock<IDirectory> directory;
        private SolutionBindingSerializer testSubject;

        private const string MockFilePath = "c:\\test.txt";
        private const string MockDirectory = "c:\\";

        [TestInitialize]
        public void TestInitialize()
        {
            logger = new Mock<ILogger>();
            file = new Mock<IFile>();
            directory = new Mock<IDirectory>();

            testSubject = new SolutionBindingSerializer(logger.Object,
                file.Object,
                directory.Object);
        }

        [TestMethod]
        public void Ctor_NullLogger_Exception()
        {
            Action act = () => new SolutionBindingSerializer(null, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void Ctor_NullFileSystem_Exception()
        {
            Action act = () => new SolutionBindingSerializer(logger.Object, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileWrapper");
        }

        [TestMethod]
        public void SerializeToFile_DirectoryDoesNotExist_DirectoryIsCreated()
        {
            directory.Setup(x => x.Exists(MockDirectory)).Returns(false);

            testSubject.SerializeToFile(MockFilePath, new BoundSonarQubeProject());

            directory.Verify(x => x.Create(MockDirectory), Times.Once);
        }

        [TestMethod]
        public void SerializeToFile_DirectoryExists_DirectoryNotCreated()
        {
            directory.Setup(x => x.Exists(MockDirectory)).Returns(true);

            testSubject.SerializeToFile(MockFilePath, new BoundSonarQubeProject());

            directory.Verify(x => x.Create(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void SerializeToFile_ReturnsTrue()
        {
            var actual = testSubject.SerializeToFile(MockFilePath, new BoundSonarQubeProject());
            actual.Should().BeTrue();
        }

        [TestMethod]
        public void SerializeToFile_FileSerializedAndWritten()
        {
            var boundSonarQubeProject = new BoundSonarQubeProject(
                new Uri("http://xxx.www.zzz/yyy:9000"),
                "MyProject Key",
                "projectName");

            var expectedContent = @"{
  ""ServerUri"": ""http://xxx.www.zzz/yyy:9000"",
  ""Organization"": null,
  ""ProjectKey"": ""MyProject Key"",
  ""ProjectName"": ""projectName"",
  ""Profiles"": null
}";

            file.Setup(x => x.WriteAllText(MockFilePath, expectedContent));

            testSubject.SerializeToFile(MockFilePath, boundSonarQubeProject);

            file.Verify(x => x.WriteAllText(MockFilePath, expectedContent), Times.Once);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void DeserializeFromFile_FilePathIsNull_Null(string filePath)
        {
            var actual = testSubject.DeserializeFromFile(filePath);
            actual.Should().Be(null);
        }

        [TestMethod]
        public void DeserializeFromFile_FileDoesNotExist_Null()
        {
            file.Setup(x => x.Exists(MockFilePath)).Returns(false);

            var actual = testSubject.DeserializeFromFile(MockFilePath);
            actual.Should().Be(null);
        }

        [TestMethod]
        public void DeserializeFromFile_FileExists_DeserializedProject()
        {
            var fileContent = @"{
  ""ServerUri"": ""http://xxx.www.zzz/yyy:9000"",
  ""Organization"": null,
  ""ProjectKey"": ""MyProject Key"",
  ""ProjectName"": ""projectName"",
  ""Profiles"": null
}";

            var expectedProject = new BoundSonarQubeProject(
                new Uri("http://xxx.www.zzz/yyy:9000"),
                "MyProject Key",
                "projectName");


            file.Setup(x => x.Exists(MockFilePath)).Returns(true);
            file.Setup(x => x.ReadAllText(MockFilePath)).Returns(fileContent);

            var actual = testSubject.DeserializeFromFile(MockFilePath);
            actual.Should().BeEquivalentTo(expectedProject);
        }
    }
}
