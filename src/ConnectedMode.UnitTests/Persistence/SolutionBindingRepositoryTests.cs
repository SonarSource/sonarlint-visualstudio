/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using System.Linq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Persistence
{
    [TestClass]
    public class SolutionBindingRepositoryTests
    {
        private Mock<IUnintrusiveBindingPathProvider> unintrusiveBindingPathProvider;
        private Mock<ISolutionBindingCredentialsLoader> credentialsLoader;
        private Mock<ISolutionBindingFileLoader> solutionBindingFileLoader;

        private BoundSonarQubeProject boundSonarQubeProject;
        private ISolutionBindingRepository testSubject;

        private BasicAuthCredentials mockCredentials;
        private const string MockFilePath = "test file path";

        [TestInitialize]
        public void TestInitialize()
        {
            unintrusiveBindingPathProvider = CreateUnintrusiveBindingPathProvider("C:\\Bindings\\Binding1\\binding.config", "C:\\Bindings\\Binding2\\binding.config");

            credentialsLoader = new Mock<ISolutionBindingCredentialsLoader>();
            solutionBindingFileLoader = new Mock<ISolutionBindingFileLoader>();

            testSubject = new SolutionBindingRepository(unintrusiveBindingPathProvider.Object, solutionBindingFileLoader.Object, credentialsLoader.Object);

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
            MefTestHelpers.CheckTypeCanBeImported<SolutionBindingRepository, ISolutionBindingRepository>(
                MefTestHelpers.CreateExport<IUnintrusiveBindingPathProvider>(),
                MefTestHelpers.CreateExport<ICredentialStoreService>(),
                MefTestHelpers.CreateExport<ILogger>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<SolutionBindingRepository>();
        }

        [TestMethod]
        public void Read_ProjectIsNull_Null()
        {
            solutionBindingFileLoader.Setup(x => x.Load(MockFilePath)).Returns(null as BoundSonarQubeProject);

            var actual = testSubject.Read(MockFilePath);
            actual.Should().Be(null);
        }

        [TestMethod]
        public void Read_ProjectIsNull_CredentialsNotRead()
        {
            solutionBindingFileLoader.Setup(x => x.Load(MockFilePath)).Returns(null as BoundSonarQubeProject);

            testSubject.Read(MockFilePath);

            credentialsLoader.Verify(x => x.Load(It.IsAny<Uri>()), Times.Never);
        }

        [TestMethod]
        public void Read_ProjectIsNotNull_ReturnsProjectWithCredentials()
        {
            boundSonarQubeProject.ServerUri = new Uri("http://sonarsource.com");
            boundSonarQubeProject.Credentials = null;

            solutionBindingFileLoader.Setup(x => x.Load(MockFilePath)).Returns(boundSonarQubeProject);
            credentialsLoader.Setup(x => x.Load(boundSonarQubeProject.ServerUri)).Returns(mockCredentials);

            var actual = testSubject.Read(MockFilePath);
            actual.Credentials.Should().Be(mockCredentials);
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
            Assert.ThrowsException<ArgumentNullException>(() => testSubject.Write(MockFilePath, null));
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
        
        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Write_EventTriggered_DependingOnFileWriteStatus(bool triggered)
        {
            var eventTriggered = false;
            testSubject.BindingUpdated += (_, _) => eventTriggered = true;
            solutionBindingFileLoader.Setup(x => x.Save(MockFilePath, boundSonarQubeProject)).Returns(triggered);

            testSubject.Write(MockFilePath, boundSonarQubeProject);

            eventTriggered.Should().Be(triggered);
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

        [TestMethod]
        public void List_FilesExist_Returns()
        {
            var binding1 = CreateBoundSonarQubeProject("https://sonarqube.somedomain.com", null, "projectKey1");
            var binding2 = CreateBoundSonarQubeProject("https://sonarcloud.io", "organisation", "projectKey2");

            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding1\\binding.config")).Returns(binding1);
            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding2\\binding.config")).Returns(binding2);

            var expected = new[] { binding1, binding2 };

            var testSubject = new SolutionBindingRepository(unintrusiveBindingPathProvider.Object, solutionBindingFileLoader.Object, credentialsLoader.Object);

            var result = testSubject.List();

            credentialsLoader.VerifyNoOtherCalls();

            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public void List_FilesMissing_Skips()
        {
            var binding = CreateBoundSonarQubeProject("https://sonarqube.somedomain.com", null, "projectKey1");

            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding1\\binding.config")).Returns(binding);
            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding2\\binding.config")).Returns((BoundSonarQubeProject)null);

            var testSubject = new SolutionBindingRepository(unintrusiveBindingPathProvider.Object, solutionBindingFileLoader.Object, credentialsLoader.Object);

            var result = testSubject.List();

            result.Should().HaveCount(1);
            result.ElementAt(0).Should().BeEquivalentTo(binding);
        }

        private static Mock<IUnintrusiveBindingPathProvider> CreateUnintrusiveBindingPathProvider(params string[] bindigFolders)
        {
            var unintrusiveBindingPathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            unintrusiveBindingPathProvider.Setup(u => u.GetBindingPaths()).Returns(bindigFolders);
            return unintrusiveBindingPathProvider;
        }

        private static BoundSonarQubeProject CreateBoundSonarQubeProject(string uri, string organizationKey, string projectKey)
        {
            var organization = CreateOrganization(organizationKey);

            var serverUri = new Uri(uri);

            return new BoundSonarQubeProject(serverUri, projectKey, null, organization: organization);
        }

        private static SonarQubeOrganization CreateOrganization(string organizationKey) => organizationKey == null ? null : new SonarQubeOrganization(organizationKey, null);
    }
}
