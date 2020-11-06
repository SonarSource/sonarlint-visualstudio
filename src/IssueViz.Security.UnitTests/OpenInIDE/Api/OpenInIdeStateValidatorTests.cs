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
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE.Api;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE.Api
{
    [TestClass]
    public class OpenInIdeStateValidatorTests
    {
        private static readonly TestConfigurationSetup RequestConfiguration = new TestConfigurationSetup("https://server", "project", "org");
        private static readonly TestConfigurationSetup WrongServerConfiguration = new TestConfigurationSetup("https://other.server", "project", "org");
        private static readonly TestConfigurationSetup WrongProjectConfiguration = new TestConfigurationSetup("https://server", "other project", "org");
        private static readonly TestConfigurationSetup WrongOrganizationNullConfiguration = new TestConfigurationSetup("https://server", "project", null);
        private static readonly TestConfigurationSetup WrongOrganizationConfiguration = new TestConfigurationSetup("https://server", "project", "other org");

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<OpenInIdeStateValidator, IOpenInIDEStateValidator>(null, new[]
            {
                MefTestHelpers.CreateExport<IConfigurationProvider>(Mock.Of<IConfigurationProvider>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>()),
            });
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_NotInConnectedMode_False()
        {
            VerifyValidationFailed(null, "you must be in Connected Mode");
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongServer_False()
        {
            VerifyValidationFailed(WrongServerConfiguration, "not bound to the requested Sonar project");
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongOrganization_SolutionConnectedToNullOrganization_False()
        {
            VerifyValidationFailed(WrongOrganizationNullConfiguration, "not bound to the requested Sonar project");
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongOrganization_SolutionConnectedToAnotherOrganization_False()
        {
            VerifyValidationFailed(WrongOrganizationConfiguration, "not bound to the requested Sonar project");
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongProject_False()
        {
            VerifyValidationFailed(WrongProjectConfiguration, "not bound to the requested Sonar project");
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_CorrectConnection_True()
        {
            var configProvider = SetupConfigurationProvider(CreateBindingConfiguration(RequestConfiguration));
            var logger = new TestLogger();

            var testSubject = new OpenInIdeStateValidator(configProvider, logger);
            var result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);

            result.Should().BeTrue();
            logger.AssertNoOutputMessages();
        }

        private void VerifyValidationFailed(TestConfigurationSetup solutionTestConfigurationSetup, string failureReasonString)
        {
            var configProvider = solutionTestConfigurationSetup == null
                ? SetupConfigurationProvider(BindingConfiguration.Standalone)
                : SetupConfigurationProvider(CreateBindingConfiguration(solutionTestConfigurationSetup));

            var logger = new TestLogger();

            var testSubject = new OpenInIdeStateValidator(configProvider, logger);
            var result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);

            result.Should().BeFalse();
            logger.AssertPartialOutputStringExists(failureReasonString);
        }

        private BindingConfiguration CreateBindingConfiguration(TestConfigurationSetup testConfigurationSetup)
        {
            var organization = testConfigurationSetup.OrganizationKey == null ? null : new SonarQubeOrganization(testConfigurationSetup.OrganizationKey, "org name");
            var boundSonarQubeProject = new BoundSonarQubeProject(new Uri(testConfigurationSetup.ServerUrl), testConfigurationSetup.ProjectKey, "project name", null, organization);
            var bindingConfiguration = new BindingConfiguration(boundSonarQubeProject, SonarLintMode.Connected, null);

            return bindingConfiguration;
        }

        private static IConfigurationProvider SetupConfigurationProvider(BindingConfiguration bindingConfiguration)
        {
            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(bindingConfiguration);

            return configProvider.Object;
        }

        private class TestConfigurationSetup
        {
            public TestConfigurationSetup(string serverUrl, string projectKey, string organizationKey)
            {
                ServerUrl = serverUrl;
                ProjectKey = projectKey;
                OrganizationKey = organizationKey;
            }

            public string ServerUrl { get; }
            public string ProjectKey { get; }
            public string OrganizationKey { get; }
        }
    }
}
