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
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class ServerConnectionConfigurationProviderTests
    {
        [TestMethod]
        public void GetServerConnectionConfiguration_ConvertsSQBindindCorrectly()
        {
            var project = CreateBoundSonarQubeProject("http://someuri.com");

            var solutionBindingRepository = CreateRepository(project);

            var testSubject = CreateTestSubject(solutionBindingRepository.Object);

            var bindings = testSubject.GetServerConnectionConfiguration<SonarQubeConnectionConfigurationDto>().ToList();

            bindings.Should().HaveCount(1);
            bindings[0].connectionId.Should().Be("sq|http://someuri.com/");
            bindings[0].serverUrl.Should().Be("http://someuri.com/");
            bindings[0].disableNotification.Should().BeTrue();
        }

        [TestMethod]
        public void GetServerConnectionConfiguration_ConvertsSCBindindCorrectly()
        {
            var project = CreateBoundSonarQubeProject("https://sonarcloud.io", "org");

            var solutionBindingRepository = CreateRepository(project);

            var testSubject = CreateTestSubject(solutionBindingRepository.Object);

            var bindings = testSubject.GetServerConnectionConfiguration<SonarCloudConnectionConfigurationDto>().ToList();

            bindings.Should().HaveCount(1);
            bindings[0].connectionId.Should().Be("sc|org");
            bindings[0].organization.Should().Be("org");
            bindings[0].disableNotification.Should().BeTrue();
        }

        [TestMethod]
        public void GetServerConnectionConfiguration_HaveBothSQAndSC_ReturnsCorrectly()
        {
            var project1 = CreateBoundSonarQubeProject("http://someuri.com");
            var project2 = CreateBoundSonarQubeProject("http://someuri2.com");
            var project3 = CreateBoundSonarQubeProject("http://145.68.22.15:8964");
            var project4 = CreateBoundSonarQubeProject("https://sonarcloud.io", "org1");

            var solutionBindingRepository = CreateRepository(project1, project2, project3, project4);

            var testSubject = CreateTestSubject(solutionBindingRepository.Object);

            var sqBindings = testSubject.GetServerConnectionConfiguration<SonarQubeConnectionConfigurationDto>();
            var scBindings = testSubject.GetServerConnectionConfiguration<SonarCloudConnectionConfigurationDto>();

            sqBindings.Should().HaveCount(3);
            scBindings.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetServerConnectionConfiguration_HaveMultipleBindingWithSameUri_Aggregates()
        {
            var project1 = CreateBoundSonarQubeProject("http://someuri.com");
            var project2 = CreateBoundSonarQubeProject("http://someuri.com");
            var project3 = CreateBoundSonarQubeProject("http://145.68.22.15:8964");
            var project4 = CreateBoundSonarQubeProject("https://sonarcloud.io", "org1");
            var project5 = CreateBoundSonarQubeProject("https://sonarcloud.io", "org2");

            var solutionBindingRepository = CreateRepository(project1, project2, project3, project4, project5);

            var testSubject = CreateTestSubject(solutionBindingRepository.Object);

            var sqBindings = testSubject.GetServerConnectionConfiguration<SonarQubeConnectionConfigurationDto>();
            var scBindings = testSubject.GetServerConnectionConfiguration<SonarCloudConnectionConfigurationDto>();

            sqBindings.Should().HaveCount(2);
            scBindings.Should().HaveCount(1);
        }

        [TestMethod]
        public void GetServerConnectionConfiguration_CallsRepoOnce()
        {
            var solutionBindingRepository = CreateRepository();

            var testSubject = CreateTestSubject(solutionBindingRepository.Object);

            _ = testSubject.GetServerConnectionConfiguration<SonarQubeConnectionConfigurationDto>();
            _ = testSubject.GetServerConnectionConfiguration<SonarQubeConnectionConfigurationDto>();

            solutionBindingRepository.Verify(sbr => sbr.List(), Times.Once());
            solutionBindingRepository.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetServerConnectionConfiguration_ThrowsOnUIThread()
        {
            var threadHandling = new Mock<IThreadHandling>();

            var testSubject = CreateTestSubject(threadHandling: threadHandling.Object);

            _ = testSubject.GetServerConnectionConfiguration<SonarQubeConnectionConfigurationDto>();

            threadHandling.Verify(th => th.ThrowIfOnUIThread(), Times.Once);
            threadHandling.VerifyNoOtherCalls();
        }

        private static ServerConnectionConfigurationProvider CreateTestSubject(ISolutionBindingRepository solutionBindingRepository = null, IThreadHandling threadHandling = null)
        {
            solutionBindingRepository ??= CreateRepository().Object;
            threadHandling ??= Mock.Of<IThreadHandling>();
            var connectionIdHelper = CreateConnectionIdHelper().Object;

            return new ServerConnectionConfigurationProvider(solutionBindingRepository, threadHandling, connectionIdHelper);
        }

        private static Mock<ISolutionBindingRepository> CreateRepository(params BoundSonarQubeProject[] projects)
        {
            var solutionBindingRepository = new Mock<ISolutionBindingRepository>();
            solutionBindingRepository.Setup(sbr => sbr.List()).Returns(projects);
            return solutionBindingRepository;
        }

        private static Mock<IConnectionIdHelper> CreateConnectionIdHelper()
        {
            var connectionIdHelper = new Mock<IConnectionIdHelper>();
            connectionIdHelper.Setup(c => c.GetConnectionIdFromUri(It.IsAny<Uri>(), It.IsAny<string>())).Returns((Uri uri, string organization) => new ConnectionIdHelper().GetConnectionIdFromUri(uri, organization));
            return connectionIdHelper;
        }

        private BoundSonarQubeProject CreateBoundSonarQubeProject(string serverUriString, string organisationKey = null)
        {
            var serverUri = new Uri(serverUriString);
            //To make sure if the organisation is null program do not break
            var organization = organisationKey is not null ? new SonarQubeOrganization(organisationKey, null) : null;

            return new BoundSonarQubeProject { ServerUri = serverUri, Organization = organization };
        }
    }
}
