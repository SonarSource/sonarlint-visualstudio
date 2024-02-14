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
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UnitTests.Binding
{
    [TestClass]
    public class BindingInfoProviderTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<BoundConnectionInfoProvider, IBoundConnectionInfoProvider>(
                MefTestHelpers.CreateExport<ISolutionBindingRepository>());
        }

        [TestMethod]
        public void MefCtor_CheckIsSingleton()
        {
            MefTestHelpers.CheckIsSingletonMefComponent<BoundConnectionInfoProvider>();
        }

        [TestMethod]
        public void GetExistingBindings_NoBindings_ReturnsEmpty()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.GetExistingBindings();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetExistingBindings_MakeSureCallInBackground()
        {
            var threadHandling = new Mock<IThreadHandling>();

            var testSubject = CreateTestSubject(threadHandling: threadHandling.Object);

            _ = testSubject.GetExistingBindings();

            threadHandling.Verify(t => t.ThrowIfOnUIThread(), Times.Once());
            threadHandling.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetExistingBindings_HaveBindings_ReturnsBinding()
        {
            var solutionBindingRepository = new Mock<ISolutionBindingRepository>();

            var binding1 = CreateBoundSonarQubeProject("https://sonarqube.somedomain.com", null, "projectKey1");
            var binding2 = CreateBoundSonarQubeProject("https://sonarcloud.io", "organisation", "projectKey2");

            var bindings = new[] { binding1, binding2 };

            solutionBindingRepository.Setup(sbr => sbr.List()).Returns(bindings);

            var testSubject = CreateTestSubject(solutionBindingRepository: solutionBindingRepository.Object);

            var result = testSubject.GetExistingBindings().ToList();

            result.Should().HaveCount(2);
            result[0].ServerUri.ToString().Should().Be("https://sonarqube.somedomain.com/");
            result[0].Organization.Should().BeNull();
            result[1].ServerUri.ToString().Should().Be("https://sonarcloud.io/");
            result[1].Organization.Should().Be("organisation");
        }

        [TestMethod]
        public void GetExistingBindings_SameBindingMultipleTime_ReturnsDistinct()
        {
            var solutionBindingRepository = new Mock<ISolutionBindingRepository>();

            var binding1 = CreateBoundSonarQubeProject("https://sonarqube.somedomain.com", null, "projectKey1");
            var binding2 = CreateBoundSonarQubeProject("https://sonarqube.somedomain.com", null, "projectKey2");

            var bindings = new[] { binding1, binding2 };

            solutionBindingRepository.Setup(sbr => sbr.List()).Returns(bindings);

            var testSubject = CreateTestSubject(solutionBindingRepository: solutionBindingRepository.Object);

            var result = testSubject.GetExistingBindings();

            result.Should().HaveCount(1);
        }

        private static BoundConnectionInfoProvider CreateTestSubject(ISolutionBindingRepository solutionBindingRepository = null, IThreadHandling threadHandling = null)
        {
            solutionBindingRepository ??= Mock.Of<ISolutionBindingRepository>();
            threadHandling ??= new NoOpThreadHandler();

            var testSubject = new BoundConnectionInfoProvider(solutionBindingRepository, threadHandling);
            return testSubject;
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
