﻿/*
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
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
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
        public void GetExistingBindings_BindingFolderDoNotExist_ReturnsEmpty()
        {
            var fileSytem = new MockFileSystem();

            var testSubject = CreateTestSubject(fileSytem);

            var result = testSubject.GetExistingBindings();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetExistingBindings_NoBindings_ReturnsEmpty()
        {
            var fileSytem = new MockFileSystem();
            fileSytem.AddDirectory("C:\\Bindings");

            var testSubject = CreateTestSubject(fileSytem);

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
            var fileSytem = new MockFileSystem();
            fileSytem.AddDirectory("C:\\Bindings");
            fileSytem.AddDirectory("C:\\Bindings\\Binding1");
            fileSytem.AddDirectory("C:\\Bindings\\Binding2");

            var solutionBindingFileLoader = new Mock<ISolutionBindingFileLoader>();

            var binding1 = CreateBoundSonarQubeProject("https://sonarqube.somedomain.com", null, "projectKey1");
            var binding2 = CreateBoundSonarQubeProject("https://sonarcloud.io", "organisation", "projectKey2");

            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding1\\binding.config")).Returns(binding1);
            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding2\\binding.config")).Returns(binding2);

            var testSubject = CreateTestSubject(fileSytem, solutionBindingFileLoader: solutionBindingFileLoader.Object);

            var result = testSubject.GetExistingBindings().ToList();

            result.Should().HaveCount(2);
            result[0].ServerUri.ToString().Should().Be("https://sonarqube.somedomain.com/");
            result[0].Organization.Should().BeNull();
            result[0].ProjectKey.Should().Be("projectKey1");
            result[1].ServerUri.ToString().Should().Be("https://sonarcloud.io/");
            result[1].Organization.Should().Be("organisation");
            result[1].ProjectKey.Should().Be("projectKey2");
        }

        [TestMethod]
        public void GetExistingBindings_BindingConfigMissing_SkipFile()
        {
            var fileSytem = new MockFileSystem();
            fileSytem.AddDirectory("C:\\Bindings");
            fileSytem.AddDirectory("C:\\Bindings\\Binding1");
            fileSytem.AddDirectory("C:\\Bindings\\Binding2");

            var solutionBindingFileLoader = new Mock<ISolutionBindingFileLoader>();

            var binding1 = CreateBoundSonarQubeProject("https://sonarqube.somedomain.com", null, "projectKey1");
            var binding2 = CreateBoundSonarQubeProject("https://sonarcloud.io", "organisation", "projectKey2");

            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding1\\binding.config")).Returns(binding1);
            solutionBindingFileLoader.Setup(sbf => sbf.Load("C:\\Bindings\\Binding2\\binding.config")).Returns((BoundSonarQubeProject)null);

            var testSubject = CreateTestSubject(fileSytem, solutionBindingFileLoader: solutionBindingFileLoader.Object);

            var result = testSubject.GetExistingBindings();

            result.Should().HaveCount(1);
        }

        private static IUnintrusiveBindingPathProvider CreateUnintrusiveBindingPathProvider()
        {
            var unintrusiveBindingPathProvider = new Mock<IUnintrusiveBindingPathProvider>();
            unintrusiveBindingPathProvider.SetupGet(u => u.SLVSRootBindingFolder).Returns("C:\\Bindings");
            return unintrusiveBindingPathProvider.Object;
        }

        private static BindingInfoProvider CreateTestSubject(IFileSystem fileSytem = null, ISolutionBindingFileLoader solutionBindingFileLoader = null, IThreadHandling threadHandling = null)
        {
            var unintrusiveBindingPathProvider = CreateUnintrusiveBindingPathProvider();

            fileSytem ??= new MockFileSystem();
            solutionBindingFileLoader ??= Mock.Of<ISolutionBindingFileLoader>();
            threadHandling ??= new NoOpThreadHandler();

            var testSubject = new BindingInfoProvider(unintrusiveBindingPathProvider, solutionBindingFileLoader, fileSytem, threadHandling);
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
