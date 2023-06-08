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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingDataWriterTests
    {
        private Mock<ISolutionBindingCredentialsLoader> credentialsLoader;
        private Mock<ISolutionBindingFileLoader> solutionBindingFileLoader;
        private BoundSonarQubeProject boundSonarQubeProject;
        private SolutionBindingDataWriter testSubject;

        private BasicAuthCredentials mockCredentials;
        private const string MockFilePath = "test file path";

        [TestInitialize]
        public void TestInitialize()
        {
            credentialsLoader = new Mock<ISolutionBindingCredentialsLoader>();
            solutionBindingFileLoader = new Mock<ISolutionBindingFileLoader>();

            testSubject = new SolutionBindingDataWriter(solutionBindingFileLoader.Object,
                credentialsLoader.Object);

            mockCredentials = new BasicAuthCredentials("user", "pwd".ToSecureString());

            boundSonarQubeProject = new BoundSonarQubeProject(
                new Uri("http://xxx.www.zzz/yyy:9000"),
                "MyProject Key",
                "projectName",
                mockCredentials);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SolutionBindingDataWriter, ISolutionBindingDataWriter>(
                MefTestHelpers.CreateExport<ICredentialStoreService>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Write_ConfigFilePathIsNull_ReturnsFalse(string filePath)
        {
            var actual = testSubject.Write(filePath, boundSonarQubeProject);
            actual.Should().Be(false);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Write_ConfigFilePathIsNull_FileNotWritten(string filePath)
        {
            testSubject.Write(filePath, boundSonarQubeProject);

            solutionBindingFileLoader.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<BoundSonarQubeProject>()),
                Times.Never);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void Write_ConfigFilePathIsNull_CredentialsNotWritten(string filePath)
        {
            testSubject.Write(filePath, boundSonarQubeProject);

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void Write_ProjectIsNull_Exception()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>  testSubject.Write(MockFilePath, null));
        }

        [TestMethod]
        public void Write_ProjectIsNull_FileNotWritten()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.Write(MockFilePath, null));

            solutionBindingFileLoader.Verify(x => x.Save(It.IsAny<string>(), It.IsAny<BoundSonarQubeProject>()), Times.Never);
        }

        [TestMethod]
        public void Write_ProjectIsNull_CredentialsNotWritten()
        {
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.Write(MockFilePath, null));

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void Write_FileNotWritten_CredentialsNotWritten()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(false);

            testSubject.Write(MockFilePath, boundSonarQubeProject);

            credentialsLoader.Verify(x => x.Save(It.IsAny<ICredentials>(), It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void Write_FileWritten_CredentialsWritten()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(true);

            testSubject.Write(MockFilePath, boundSonarQubeProject);

            credentialsLoader.Verify(x => x.Save(boundSonarQubeProject.Credentials, boundSonarQubeProject.ServerUri),
                Times.Once);
        }

        [TestMethod]
        public void Write_FileWritten_NoOnSaveCallback_NoException()
        {
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(true);

            Action act = () => testSubject.Write(MockFilePath, boundSonarQubeProject);
            act.Should().NotThrow();
        }
    }
}
