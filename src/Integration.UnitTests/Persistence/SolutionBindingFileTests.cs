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
using Moq;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingFileTests
    {
        private Mock<ISourceControlledFileSystem> sourceControlledFileSystem;
        private Mock<ISolutionBindingCredentialsLoader> credentialsLoader;
        private Mock<ISolutionBindingSerializer> serializer;
        private Mock<Predicate<string>> onSaveCallback;
        private BoundSonarQubeProject boundSonarQubeProject;
        private SolutionBindingFile testSubject;

        private BasicAuthCredentials mockCredentials;
        private const string MockFilePath = "test file path";

        [TestInitialize]
        public void TestInitialize()
        {
            sourceControlledFileSystem = new Mock<ISourceControlledFileSystem>();
            credentialsLoader = new Mock<ISolutionBindingCredentialsLoader>();
            serializer = new Mock<ISolutionBindingSerializer>();
            onSaveCallback = new Mock<Predicate<string>>();

            testSubject = new SolutionBindingFile(sourceControlledFileSystem.Object,
                serializer.Object,
                credentialsLoader.Object);

            mockCredentials = new BasicAuthCredentials("user", "pwd".ToSecureString());

            boundSonarQubeProject = new BoundSonarQubeProject(
                new Uri("http://xxx.www.zzz/yyy:9000"),
                "MyProject Key",
                "projectName",
                mockCredentials);

            sourceControlledFileSystem
                .Setup(x => x.QueueFileWrite(MockFilePath, It.IsAny<Func<bool>>()))
                .Callback((string filePath, Func<bool> method) => method());
        }

        [TestMethod]
        public void Ctor_NullSourceControlledFileSystem_Exception()
        {
            Action act = () => new SolutionBindingFile(null, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sccFileSystem");
        }

        [TestMethod]
        public void Ctor_NullSerializer_Exception()
        {
            Action act = () => new SolutionBindingFile(sourceControlledFileSystem.Object, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingSerializer");
        }

        [TestMethod]
        public void Ctor_NullCredentialsLoader_Exception()
        {
            Action act = () => new SolutionBindingFile(sourceControlledFileSystem.Object, serializer.Object, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("credentialsLoader");
        }

        [TestMethod]
        public void ReadBindingInformation_ProjectIsNull_Null()
        {
            serializer.Setup(x => x.DeserializeFromFile(MockFilePath)).Returns(null as BoundSonarQubeProject);

            var actual = testSubject.ReadSolutionBinding(MockFilePath);
            actual.Should().Be(null);
        }

        [TestMethod]
        public void ReadBindingInformation_ProjectIsNull_CredentialsNotRead()
        {
            serializer.Setup(x => x.DeserializeFromFile(MockFilePath)).Returns(null as BoundSonarQubeProject);

            testSubject.ReadSolutionBinding(MockFilePath);

            credentialsLoader.Verify(x => x.Load(It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void ReadBindingInformation_ProjectIsNotNull_ReturnsProjectWithCredentials()
        {
            boundSonarQubeProject.ServerUri = new Uri("http://sonarsource.com");
            boundSonarQubeProject.Credentials = null;

            serializer.Setup(x => x.DeserializeFromFile(MockFilePath)).Returns(boundSonarQubeProject);
            credentialsLoader.Setup(x => x.Load(boundSonarQubeProject.ServerUri)).Returns(mockCredentials);

            var actual = testSubject.ReadSolutionBinding(MockFilePath);
            actual.Credentials.Should().Be(mockCredentials);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void WriteSolutionBinding_ConfigFilePathIsNull_ReturnsFalse(string filePath)
        {
            var actual = testSubject.WriteSolutionBinding(filePath, boundSonarQubeProject, onSaveCallback.Object);
            actual.Should().Be(false);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void WriteSolutionBinding_ConfigFilePathIsNull_FileNotWritten(string filePath)
        {
            testSubject.WriteSolutionBinding(filePath, boundSonarQubeProject, onSaveCallback.Object);

            serializer.Verify(x => x.SerializeToFile(It.IsAny<string>(), It.IsAny<BoundSonarQubeProject>()),
                Times.Never);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void WriteSolutionBinding_ConfigFilePathIsNull_CredentialsNotWritten(string filePath)
        {
            testSubject.WriteSolutionBinding(filePath, boundSonarQubeProject, onSaveCallback.Object);

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void WriteSolutionBinding_ConfigFilePathIsNull_OnSaveCallbackNotInvoked(string filePath)
        {
            testSubject.WriteSolutionBinding(filePath, boundSonarQubeProject, onSaveCallback.Object);

            onSaveCallback.Verify(x => x(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void WriteSolutionBinding_ProjectIsNull_Exception()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>  testSubject.WriteSolutionBinding(MockFilePath, null, onSaveCallback.Object));
        }

        [TestMethod]
        public void WriteSolutionBinding_ProjectIsNull_FileNotWritten()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.WriteSolutionBinding(MockFilePath, null, onSaveCallback.Object));

            serializer.Verify(x => x.SerializeToFile(It.IsAny<string>(), It.IsAny<BoundSonarQubeProject>()), Times.Never);
        }

        [TestMethod]
        public void WriteSolutionBinding_ProjectIsNull_CredentialsNotWritten()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.WriteSolutionBinding(MockFilePath, null, onSaveCallback.Object));

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void WriteSolutionBinding_ProjectIsNull_OnSaveCallbackNotInvoked()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.WriteSolutionBinding(MockFilePath, null, onSaveCallback.Object));

            onSaveCallback.Verify(x => x(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void WriteSolutionBinding_FileNotWritten_CredentialsNotWritten()
        {
            serializer.Setup(x => x.SerializeToFile(MockFilePath, boundSonarQubeProject)).Returns(false);

            testSubject.WriteSolutionBinding(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void WriteSolutionBinding_FileNotWritten_OnSaveCallbackNotInvoked()
        {
            serializer.Setup(x => x.SerializeToFile(MockFilePath, boundSonarQubeProject)).Returns(false);

            testSubject.WriteSolutionBinding(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            onSaveCallback.Verify(x => x(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void WriteSolutionBinding_FileWritten_CredentialsWritten()
        {
            serializer.Setup(x => x.SerializeToFile(MockFilePath, boundSonarQubeProject)).Returns(true);

            testSubject.WriteSolutionBinding(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            credentialsLoader.Verify(x => x.Save(boundSonarQubeProject.Credentials, boundSonarQubeProject.ServerUri),
                Times.Once);
        }

        [TestMethod]
        public void WriteSolutionBinding_FileWritten_OnSaveCallbackIsInvoked()
        {
            serializer.Setup(x => x.SerializeToFile(MockFilePath, boundSonarQubeProject)).Returns(true);
            onSaveCallback.Setup(x => x(MockFilePath)).Returns(true);

            testSubject.WriteSolutionBinding(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            onSaveCallback.Verify(x=> x(MockFilePath), Times.Once);
        }
    }
}
