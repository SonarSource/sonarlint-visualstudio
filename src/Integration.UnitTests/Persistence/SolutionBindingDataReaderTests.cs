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
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class SolutionBindingDataReaderTests
    {
        private Mock<ISolutionBindingCredentialsLoader> credentialsLoader;
        private Mock<ISolutionBindingFileLoader> solutionBindingFileLoader;
        private BoundSonarQubeProject boundSonarQubeProject;
        private SolutionBindingDataReader testSubject;

        private BasicAuthCredentials mockCredentials;
        private const string MockFilePath = "test file path";

        [TestInitialize]
        public void TestInitialize()
        {
            credentialsLoader = new Mock<ISolutionBindingCredentialsLoader>();
            solutionBindingFileLoader = new Mock<ISolutionBindingFileLoader>();

            testSubject = new SolutionBindingDataReader(solutionBindingFileLoader.Object, credentialsLoader.Object);

            mockCredentials = new BasicAuthCredentials("user", "pwd".ToSecureString());

            boundSonarQubeProject = new BoundSonarQubeProject(
                new Uri("http://xxx.www.zzz/yyy:9000"),
                "MyProject Key",
                "projectName",
                mockCredentials);
        }

        [TestMethod]
        public void Ctor_NullSerializer_Exception()
        {
            Action act = () => new SolutionBindingDataReader(null, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("solutionBindingFileLoader");
        }

        [TestMethod]
        public void Ctor_NullCredentialsLoader_Exception()
        {
            Action act = () => new SolutionBindingDataReader(solutionBindingFileLoader.Object, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("credentialsLoader");
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
    }
}
