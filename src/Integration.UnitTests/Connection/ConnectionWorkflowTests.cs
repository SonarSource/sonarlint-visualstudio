/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service.DataModel;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectionWorkflowTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableHost host;
        private ConfigurableIntegrationSettings settings;
        private ConfigurableProjectSystemFilter filter;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            this.host.SonarQubeService = this.sonarQubeServiceMock.Object;
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);

            this.sonarQubeServiceMock.Setup(x => x.GetAllPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubePlugin>
                {
                    new SonarQubePlugin(MinimumSupportedSonarQubePlugin.CSharp.Key, MinimumSupportedSonarQubePlugin.CSharp.MinimumVersion),
                    new SonarQubePlugin(MinimumSupportedSonarQubePlugin.VbNet.Key, MinimumSupportedSonarQubePlugin.VbNet.MinimumVersion)
                });
            this.settings = new ConfigurableIntegrationSettings();

            var mefExports = MefTestHelpers.CreateExport<IIntegrationSettings>(settings);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);

            this.filter = new ConfigurableProjectSystemFilter();
            this.serviceProvider.RegisterService(typeof(IProjectSystemFilter), this.filter);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
        }

        #region Tests

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConnectionWorkflow_ConnectionStep_WhenGivenANullHost_ThrowsArgumentNullException()
        {
            // Arrange & Act
            ConnectionWorkflow testSubject = new ConnectionWorkflow(null, new RelayCommand(() => { }));

            // Assert
            FluentAssertions.Execution.Execute.Assertion.FailWith("Expected exception of type ArgumentNullException but no exception was thrown.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConnectionWorkflow_ConnectionStep_WhenGivenANullParentCommand_ThrowsArgumentNullException()
        {
            // Arrange & Act
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, null);

            // Assert
            FluentAssertions.Execution.Execute.Assertion.FailWith("Expected exception of type ArgumentNullException but no exception was thrown.");
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_ConnectedServerCanBeGetAndSet()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(() => { }));

            // Act
            testSubject.ConnectedServer = connectionInfo;

            // Assert
            connectionInfo.Should().Be(testSubject.ConnectedServer);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenCSharpPluginAndAnyCSharpProject_SuccessfulConnection()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndAnyXProject_SuccessfulConnection("foo.csproj", ProjectSystemHelper.CSharpProjectKind);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenVBNetPluginAndAnyVBNetProject_SuccessfulConnection()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndAnyXProject_SuccessfulConnection("foo.vbproj", ProjectSystemHelper.VbProjectKind);
        }

        private async Task ConnectionWorkflow_ConnectionStep_WhenXPluginAndAnyXProject_SuccessfulConnection(string projectName, string projectKind)
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new List<SonarQubeProject> { new SonarQubeProject("project1", "") };
            this.sonarQubeServiceMock.Setup(x => x.ConnectAsync(connectionInfo, It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(0));
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(projects);
            this.projectSystemHelper.Projects = new[] { new ProjectMock(projectName) { ProjectKind = projectKind } };
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                c.Should().Be(connectionInfo, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionStepRetrievingProjects,
                Strings.ConnectionResultSuccess);
            projectChangedCallbackCalled.Should().BeTrue("ConnectedProjectsCallaback was not called");
            this.sonarQubeServiceMock.Verify(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(),
                It.IsAny<CancellationToken>()), Times.Once());
            testSubject.ConnectedServer.Should().Be(connectionInfo);
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoShowErrorMessages();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoNotification(NotificationIds.FailedToConnectId);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenMissingCSharpPluginAndVBNetPlugin_AbortsWorkflowAndDisconnects()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(() => { }));
            var controller = new ConfigurableProgressController();
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubeProject>());
            this.sonarQubeServiceMock.Setup(x => x.GetAllPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubePlugin>());
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;
            var executionEvents = new ConfigurableProgressStepExecutionEvents();

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionResultFailure);
            notifications.AssertNotification(NotificationIds.BadSonarQubePluginId, Strings.ServerHasNoSupportedPluginVersion);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenPluginOkAndNoProjects_AbortsWorkflowAndDisconnects()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new List<SonarQubeProject> { new SonarQubeProject("project1", "") };
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(projects);
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                c.Should().Be(connectionInfo, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionResultFailure);
            projectChangedCallbackCalled.Should().BeFalse("ConnectedProjectsCallaback was called");
            notifications.AssertNotification(NotificationIds.BadSonarQubePluginId, Strings.SolutionContainsNoSupportedProject);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenCSharpPluginAndNoCSharpProject_AbortsWorkflowAndDisconnects()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.vbproj", ProjectSystemHelper.VbProjectKind, MinimumSupportedSonarQubePlugin.CSharp);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenVBNetPluginAndNoVBNetProject_AbortsWorkflowAndDisconnects()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.csproj", ProjectSystemHelper.CSharpProjectKind, MinimumSupportedSonarQubePlugin.VbNet);
        }

        private async Task ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects(string projectName, string projectKind, MinimumSupportedSonarQubePlugin minimumSupportedSonarQubePlugin)
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new List<SonarQubeProject> { new SonarQubeProject("project1", "") };
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(projects);
            this.sonarQubeServiceMock.Setup(x => x.GetAllPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubePlugin> { new SonarQubePlugin(minimumSupportedSonarQubePlugin.Key, minimumSupportedSonarQubePlugin.MinimumVersion) });
            this.projectSystemHelper.Projects = new[] { new ProjectMock(projectName) { ProjectKind = projectKind } };
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                c.Should().Be(connectionInfo, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionResultFailure);
            projectChangedCallbackCalled.Should().BeFalse("ConnectedProjectsCallaback was called");
            notifications.AssertNotification(NotificationIds.BadSonarQubePluginId, string.Format(Strings.OnlySupportedPluginHasNoProjectInSolution, minimumSupportedSonarQubePlugin.Language.Name));
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_UnsuccessfulConnection()
        {
            // Arrange
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => { throw new Exception(); });
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                c.Should().Be(connectionInfo, "Unexpected connection");
                p.Should().BeNull("Not expecting any projects");
            };
            this.projectSystemHelper.Projects = new[] { new ProjectMock("foo.csproj") { ProjectKind = ProjectSystemHelper.CSharpProjectKind } };
            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionStepRetrievingProjects,
                Strings.ConnectionResultFailure);
            projectChangedCallbackCalled.Should().BeFalse("Callback should not have been called");
            this.sonarQubeServiceMock.Verify(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(),
                It.IsAny<CancellationToken>()), Times.Once());
            this.host.VisualStateManager.IsConnected.Should().BeFalse();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

            // Act (reconnect with same bad connection)
            executionEvents.Reset();
            projectChangedCallbackCalled = false;
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionStepRetrievingProjects,
                Strings.ConnectionResultFailure);
            projectChangedCallbackCalled.Should().BeFalse("Callback should not have been called");
            this.sonarQubeServiceMock.Verify(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(),
                It.IsAny<CancellationToken>()), Times.Exactly(2));
            this.host.VisualStateManager.IsConnected.Should().BeFalse();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

            // Canceled connections
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            executionEvents.Reset();
            projectChangedCallbackCalled = false;
            CancellationToken token = tokenSource.Token;
            tokenSource.Cancel();

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, token);

            // Assert
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionStepRetrievingProjects,
                Strings.ConnectionResultCancellation);
            projectChangedCallbackCalled.Should().BeFalse("Callback should not have been called");
            this.sonarQubeServiceMock.Verify(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(),
                It.IsAny<CancellationToken>()), Times.Exactly(3));
            this.host.VisualStateManager.IsConnected.Should().BeFalse();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_DownloadServiceParameters_RegexPropertyNotSet_SetsFilterWithDefaultExpression()
        {
            // Arrange
            this.sonarQubeServiceMock.Setup(x => x.GetAllPropertiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubeProperty> { new SonarQubeProperty(SonarQubeProperty.TestProjectRegexKey,
                    SonarQubeProperty.TestProjectRegexDefaultValue) });
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var expectedExpression = SonarQubeProperty.TestProjectRegexDefaultValue;
            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            // Act
            await testSubject.DownloadServiceParametersAsync(controller, progressEvents, CancellationToken.None);

            // Assert
            filter.AssertTestRegex(expectedExpression, RegexOptions.IgnoreCase);
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_DownloadServiceParameters_CustomRegexProperty_SetsFilterWithCorrectExpression()
        {
            // Arrange
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            var expectedExpression = ".*spoon.*";
            this.sonarQubeServiceMock.Setup(x => x.GetAllPropertiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubeProperty> { new SonarQubeProperty(SonarQubeProperty.TestProjectRegexKey, expectedExpression) });

            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            // Act
            await testSubject.DownloadServiceParametersAsync(controller, progressEvents, CancellationToken.None);

            // Assert
            filter.AssertTestRegex(expectedExpression, RegexOptions.IgnoreCase);
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_DownloadServiceParameters_InvalidRegex_UsesDefault()
        {
            // Arrange
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            var badExpression = "*-gf/d*-b/try\\*-/r-*yeb/\\";
            var expectedExpression = SonarQubeProperty.TestProjectRegexDefaultValue;
            this.sonarQubeServiceMock.Setup(x => x.GetAllPropertiesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubeProperty> { new SonarQubeProperty(SonarQubeProperty.TestProjectRegexKey, badExpression) });

            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            // Act
            await testSubject.DownloadServiceParametersAsync(controller, progressEvents, CancellationToken.None);

            // Assert
            filter.AssertTestRegex(expectedExpression, RegexOptions.IgnoreCase);
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
            this.outputWindowPane.AssertOutputStrings(string.Format(CultureInfo.CurrentCulture, Strings.InvalidTestProjectRegexPattern, badExpression));
        }

        [TestMethod]
        public async Task ConnectionWorkflow_DownloadServiceParameters_Cancelled_AbortsWorkflow()
        {
            // Arrange
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await testSubject.DownloadServiceParametersAsync(controller, progressEvents, cts.Token);

            // Assert
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
            controller.NumberOfAbortRequests.Should().Be(1);
        }

        #endregion Tests

        #region Helpers

        private static void AssertIfCalled()
        {
            FluentAssertions.Execution.Execute.Assertion.FailWith("Command not expected to be called");
        }

        private ConnectionWorkflow SetTestSubjectWithConnectedServer()
        {
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            testSubject.ConnectedServer = connectionInfo;
            return testSubject;
        }

        #endregion Helpers
    }
}