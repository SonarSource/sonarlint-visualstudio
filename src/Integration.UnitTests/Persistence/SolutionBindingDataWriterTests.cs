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
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingDataWriterTests
    {
        private Mock<ISourceControlledFileSystem> sourceControlledFileSystem;
        private Mock<ISolutionBindingCredentialsLoader> credentialsLoader;
        private Mock<ISolutionBindingFileLoader> solutionBindingFileLoader;
        private Mock<Action<string>> onSaveCallback;
        private BoundSonarQubeProject boundSonarQubeProject;
        private SolutionBindingDataWriter testSubject;

        private BasicAuthCredentials mockCredentials;
        private const string MockFilePath = "test file path";

        [TestInitialize]
        public void TestInitialize()
        {
            sourceControlledFileSystem = new Mock<ISourceControlledFileSystem>();
            credentialsLoader = new Mock<ISolutionBindingCredentialsLoader>();
            solutionBindingFileLoader = new Mock<ISolutionBindingFileLoader>();
            onSaveCallback = new Mock<Action<string>>();

            testSubject = new SolutionBindingDataWriter(sourceControlledFileSystem.Object,
                solutionBindingFileLoader.Object,
                credentialsLoader.Object);

            mockCredentials = new BasicAuthCredentials("user", "pwd".ToSecureString());

            boundSonarQubeProject = new BoundSonarQubeProject(
                new Uri("http://xxx.www.zzz/yyy:9000"),
                "MyProject Key",
                "projectName",
                mockCredentials);

            sourceControlledFileSystem
                .Setup(x => x.QueueFileWrites(new List<string>{MockFilePath}, It.IsAny<Func<bool>>()))
                .Callback((IEnumerable<string> filePath, Func<bool> method) => method());
        }

        [TestMethod]
        public void Ctor_NullSourceControlledFileSystem_Exception()
        {
            Action act = () => new SolutionBindingDataWriter(null, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("sccFileSystem");
        }

        [TestMethod]
        public void Ctor_NullSerializer_Exception()
        {
            Action act = () => new SolutionBindingDataWriter(sourceControlledFileSystem.Object, null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingFileLoader");
        }

        [TestMethod]
        public void Ctor_NullCredentialsLoader_Exception()
        {
            Action act = () => new SolutionBindingDataWriter(sourceControlledFileSystem.Object, solutionBindingFileLoader.Object, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("credentialsLoader");
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Write_ConfigFilePathIsNull_ReturnsFalse(string filePath)
        {
            var actual = testSubject.Write(filePath, boundSonarQubeProject, onSaveCallback.Object);
            actual.Should().Be(false);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Write_ConfigFilePathIsNull_FileNotWritten(string filePath)
        {
            testSubject.Write(filePath, boundSonarQubeProject, onSaveCallback.Object);

            solutionBindingFileLoader.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<BoundSonarQubeProject>()),
                Times.Never);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Write_ConfigFilePathIsNull_CredentialsNotWritten(string filePath)
        {
            testSubject.Write(filePath, boundSonarQubeProject, onSaveCallback.Object);

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Write_ConfigFilePathIsNull_OnSaveCallbackNotInvoked(string filePath)
        {
            testSubject.Write(filePath, boundSonarQubeProject, onSaveCallback.Object);

            onSaveCallback.Verify(x => x(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Write_ProjectIsNull_Exception()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>  testSubject.Write(MockFilePath, null, onSaveCallback.Object));
        }

        [TestMethod]
        public void Write_ProjectIsNull_FileNotWritten()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.Write(MockFilePath, null, onSaveCallback.Object));

            solutionBindingFileLoader.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<BoundSonarQubeProject>()), Times.Never);
        }

        [TestMethod]
        public void Write_ProjectIsNull_CredentialsNotWritten()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.Write(MockFilePath, null, onSaveCallback.Object));

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void Write_ProjectIsNull_OnSaveCallbackNotInvoked()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.Write(MockFilePath, null, onSaveCallback.Object));

            onSaveCallback.Verify(x => x(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Write_FileNotWritten_CredentialsNotWritten()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(false);

            testSubject.Write(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void Write_FileNotWritten_OnSaveCallbackNotInvoked()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(false);

            testSubject.Write(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            onSaveCallback.Verify(x => x(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Write_FileWritten_CredentialsWritten()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(true);

            testSubject.Write(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            credentialsLoader.Verify(x => x.Save(boundSonarQubeProject.Credentials, boundSonarQubeProject.ServerUri),
                Times.Once);
        }

        [TestMethod]
        public void Write_FileWritten_NoOnSaveCallback_NoException()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(true);

            Action act = () => testSubject.Write(MockFilePath, boundSonarQubeProject,null);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Write_FileWritten_OnSaveCallbackIsInvoked()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(true);
            onSaveCallback.Setup(x => x(MockFilePath));

            testSubject.Write(MockFilePath, boundSonarQubeProject, onSaveCallback.Object);

            onSaveCallback.Verify(x=> x(MockFilePath), Times.Once);
        }
    }
}
