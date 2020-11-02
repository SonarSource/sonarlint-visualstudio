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
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE;
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
                MefTestHelpers.CreateExport<IInfoBarManager>(Mock.Of<IInfoBarManager>()),
                MefTestHelpers.CreateExport<IConfigurationProvider>(Mock.Of<IConfigurationProvider>()),
                MefTestHelpers.CreateExport<ILogger>(Mock.Of<ILogger>()),
            });
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_NotInConnectedMode_False()
        {
            var expectedFailureMessage = OpenInIDEResources.RequestValidator_InvalidStateReason_NotInConnectedMode;

            VerifyValidationFailed(null, expectedFailureMessage);
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongServer_False()
        {
            var expectedFailureMessage = string.Format(OpenInIDEResources.RequestValidator_InvalidStateReason_WrongServer, WrongServerConfiguration.ServerUrl);

            VerifyValidationFailed(WrongServerConfiguration, expectedFailureMessage);
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongOrganization_SolutionConnectedToNullOrganization_False()
        {
            var expectedFailureMessage = string.Format(OpenInIDEResources.RequestValidator_InvalidStateReason_WrongOrganization, "");

            VerifyValidationFailed(WrongOrganizationNullConfiguration, expectedFailureMessage);
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongOrganization_SolutionConnectedToAnotherOrganization_False()
        {
            var expectedFailureMessage = string.Format(OpenInIDEResources.RequestValidator_InvalidStateReason_WrongOrganization, WrongOrganizationConfiguration.OrganizationKey);

            VerifyValidationFailed(WrongOrganizationConfiguration, expectedFailureMessage);
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_WrongProject_False()
        {
            var expectedFailureMessage = string.Format(OpenInIDEResources.RequestValidator_InvalidStateReason_WrongProject, WrongProjectConfiguration.ProjectKey);

            VerifyValidationFailed(WrongProjectConfiguration, expectedFailureMessage);
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_CorrectConnection_True()
        {
            var configProvider = SetupConfigurationProvider(CreateBindingConfiguration(RequestConfiguration));
            var infoBarManager = new Mock<IInfoBarManager>();
            var logger = new TestLogger();

            var testSubject = new OpenInIdeStateValidator(infoBarManager.Object, configProvider, logger);
            var result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);

            result.Should().BeTrue();
            infoBarManager.VerifyNoOtherCalls();
            logger.AssertNoOutputMessages();
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_HasPreviousInfoBar_ConnectionIsNowCorrect_InfoBarRemovedAndNoNewInfoBarShown()
        {
            var configProvider = new Mock<IConfigurationProvider>();
            configProvider
                .SetupSequence(x => x.GetConfiguration())
                .Returns(BindingConfiguration.Standalone)
                .Returns(CreateBindingConfiguration(RequestConfiguration));

            var infoBar = new Mock<IInfoBar>();
            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButton(new Guid(HotspotsToolWindow.ToolWindowId), It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeStateValidator(infoBarManager.Object, configProvider.Object, Mock.Of<ILogger>());
            var result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);
            result.Should().BeFalse();

            infoBarManager.Reset();

            result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);
            result.Should().BeTrue();

            infoBarManager.Verify(x=> x.DetachInfoBar(infoBar.Object), Times.Once);
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CanHandleOpenInIDERequest_HasPreviousInfoBar_ConnectionIsStillWrong_InfoBarReplaced()
        {
            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(BindingConfiguration.Standalone);

            var firstInfoBar = new Mock<IInfoBar>();
            var secondInfoBar = new Mock<IInfoBar>();
            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .SetupSequence(x => x.AttachInfoBarWithButton(new Guid(HotspotsToolWindow.ToolWindowId), It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(firstInfoBar.Object)
                .Returns(secondInfoBar.Object);

            var testSubject = new OpenInIdeStateValidator(infoBarManager.Object, configProvider.Object, Mock.Of<ILogger>());
            var result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);
            result.Should().BeFalse();

            result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);
            result.Should().BeFalse();

            firstInfoBar.VerifyNoOtherCalls();
            secondInfoBar.VerifyNoOtherCalls();
            infoBarManager.VerifyAll();
            infoBarManager.Verify(x => x.DetachInfoBar(firstInfoBar.Object), Times.Once);
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_HasPreviousInfoBar_InfoBarRemoved()
        {
            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(BindingConfiguration.Standalone);

            var infoBar = new Mock<IInfoBar>();
            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButton(new Guid(HotspotsToolWindow.ToolWindowId), It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeStateValidator(infoBarManager.Object, configProvider.Object, Mock.Of<ILogger>());
            testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);

            infoBarManager.VerifyAll();
            infoBarManager.VerifyNoOtherCalls();

            testSubject.Dispose();

            infoBarManager.Verify(x=> x.DetachInfoBar(infoBar.Object), Times.Once);
            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_NoPreviousInfoBar_NoException()
        {
            var configProvider = SetupConfigurationProvider(CreateBindingConfiguration(RequestConfiguration));
            var infoBarManager = new Mock<IInfoBarManager>();

            var testSubject = new OpenInIdeStateValidator(infoBarManager.Object, configProvider, Mock.Of<ILogger>());
            testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);

            testSubject.Dispose();

            infoBarManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void InfoBarIsManuallyClosed_InfoBarDetachedFromToolWindow()
        {
            var configProvider = new Mock<IConfigurationProvider>();
            configProvider.Setup(x => x.GetConfiguration()).Returns(BindingConfiguration.Standalone);

            var infoBar = new Mock<IInfoBar>();
            var infoBarManager = new Mock<IInfoBarManager>();
            infoBarManager
                .Setup(x => x.AttachInfoBarWithButton(new Guid(HotspotsToolWindow.ToolWindowId), It.IsAny<string>(), It.IsAny<string>(), default))
                .Returns(infoBar.Object);

            var testSubject = new OpenInIdeStateValidator(infoBarManager.Object, configProvider.Object, Mock.Of<ILogger>());
            testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);

            infoBarManager.VerifyAll();
            infoBarManager.VerifyNoOtherCalls();

            infoBar.Raise(x=> x.Closed += null, EventArgs.Empty);

            infoBarManager.Verify(x => x.DetachInfoBar(infoBar.Object), Times.Once);
            infoBarManager.VerifyNoOtherCalls();
        }

        private void VerifyValidationFailed(TestConfigurationSetup solutionTestConfigurationSetup, string failureReasonString)
        {
            var configProvider = solutionTestConfigurationSetup == null
                ? SetupConfigurationProvider(BindingConfiguration.Standalone)
                : SetupConfigurationProvider(CreateBindingConfiguration(solutionTestConfigurationSetup));

            var infoBarManager = SetupInfoBarManager();
            var logger = new TestLogger();

            var testSubject = new OpenInIdeStateValidator(infoBarManager.Object, configProvider, logger);
            var result = testSubject.CanHandleOpenInIDERequest(new Uri(RequestConfiguration.ServerUrl), RequestConfiguration.ProjectKey, RequestConfiguration.OrganizationKey);

            result.Should().BeFalse();
            infoBarManager.VerifyAll();
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

        private static Mock<IInfoBarManager> SetupInfoBarManager()
        {
            var infoBarManager = new Mock<IInfoBarManager>();

            infoBarManager
                .Setup(x => x.AttachInfoBarWithButton(new Guid(HotspotsToolWindow.ToolWindowId), OpenInIDEResources.RequestValidator_InfoBarMessage, "Show Output Window", default))
                .Returns(Mock.Of<IInfoBar>());

            return infoBarManager;
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
