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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectionWorkflowTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableHost host;
        private ConfigurableSonarLintSettings settings;
        private ConfigurableProjectSystemFilter filter;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private Mock<ICredentialStoreService> credentialStoreMock;

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
            this.settings = new ConfigurableSonarLintSettings();

            var mefExports = MefTestHelpers.CreateExport<ISonarLintSettings>(settings);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);

            this.filter = new ConfigurableProjectSystemFilter();
            this.serviceProvider.RegisterService(typeof(IProjectSystemFilter), this.filter);

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);

            this.credentialStoreMock = new Mock<ICredentialStoreService>();
            this.serviceProvider.RegisterService(typeof(ICredentialStoreService), this.credentialStoreMock.Object);
        }

        #region Tests

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenGivenANullHost_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new ConnectionWorkflow(null, new RelayCommand(() => { }));

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenGivenANullParentCommand_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new ConnectionWorkflow(this.host, null);

            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("parentCommand");
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_ConnectedServerCanBeGetAndSet()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
#pragma warning disable IDE0017 // Simplify object initialization
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(() => { }));
#pragma warning restore IDE0017 // Simplify object initialization

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
            var connectionInfo = new ConnectionInformation(new Uri("http://server"), "user", "pass".ToSecureString());
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
            AssertServiceDisconnectNotCalled();
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

            AssertCredentialsStored(connectionInfo);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_Credentials_Invalid()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"), "user", "pass".ToSecureString());
            this.sonarQubeServiceMock.Setup(x => x.ConnectAsync(connectionInfo, It.IsAny<CancellationToken>()))
                .Throws(new Exception());

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(1);
            AssertServiceDisconnectCalled();
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.ConnectionStepValidatinCredentials,
                Strings.ConnectionResultFailure);

            this.sonarQubeServiceMock.Verify(
                x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>()),
                Times.Once());

            testSubject.ConnectedServer.Should().Be(null);

            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoShowErrorMessages();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId);

            AssertCredentialsNotStored(); // Connection was rejected by SonarQube
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
            AssertServiceDisconnectCalled();
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionResultFailure);
            notifications.AssertNotification(NotificationIds.BadSonarQubePluginId, Strings.ServerHasNoSupportedPluginVersion);

            AssertCredentialsNotStored(); // Username and password are null
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
            AssertServiceDisconnectCalled();
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionResultFailure);
            projectChangedCallbackCalled.Should().BeFalse("ConnectedProjectsCallaback was called");
            notifications.AssertNotification(NotificationIds.BadSonarQubePluginId, Strings.SolutionContainsNoSupportedProject);

            AssertCredentialsNotStored(); // Username and password are null
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenCSharpAndVbPluginAndNoCSharpOrVbProject_AbortsWorkflowAndDisconnects()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.xxx", Guid.NewGuid().ToString(),
                MinimumSupportedSonarQubePlugin.CSharp, MinimumSupportedSonarQubePlugin.VbNet);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenVBNetPluginAndNoVBNetProject_AbortsWorkflowAndDisconnects()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.csproj", ProjectSystemHelper.CSharpProjectKind, MinimumSupportedSonarQubePlugin.VbNet);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenCppPluginAndNoCppProject_AbortsWorkflowAndDisconnects()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.vbproj", ProjectSystemHelper.VbProjectKind, MinimumSupportedSonarQubePlugin.CFamily);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_WhenMultiplePluginsAndUnknownProject_AbortsWorkflowAndDisconnects()
        {
            await ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.proj", Guid.NewGuid().ToString(),
                MinimumSupportedSonarQubePlugin.CSharp, MinimumSupportedSonarQubePlugin.VbNet, MinimumSupportedSonarQubePlugin.CFamily);
        }

        private async Task ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects(string projectName, string projectKind,
            params MinimumSupportedSonarQubePlugin[] minimumSupportedSonarQubePlugins)
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new List<SonarQubeProject> { new SonarQubeProject("project1", "") };
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(projects);
            this.sonarQubeServiceMock.Setup(x => x.GetAllPluginsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(minimumSupportedSonarQubePlugins.Select(p => new SonarQubePlugin(p.Key, p.MinimumVersion)).ToList());
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
            AssertServiceDisconnectCalled();
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.ConnectionStepValidatinCredentials,
                Strings.DetectingSonarQubePlugins,
                Strings.ConnectionResultFailure);
            projectChangedCallbackCalled.Should().BeFalse("ConnectedProjectsCallaback was called");

            var languageList = string.Join(", ", minimumSupportedSonarQubePlugins.SelectMany(x => x.Languages.Select(l => l.Name)));
            notifications.AssertNotification(NotificationIds.BadSonarQubePluginId, string.Format(Strings.OnlySupportedPluginsHaveNoProjectInSolution, languageList));

            AssertCredentialsNotStored(); // Username and password are null
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

            AssertCredentialsNotStored(); // Username and password are null
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_10KProjectsAndKeyIsNotPartOfIt()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"), "user", "pass".ToSecureString());
            var projects = Enumerable.Range(1, 10000)
                .Select(i => new SonarQubeProject($"project-{i}", $"Project {i}"))
                .ToList();
            this.sonarQubeServiceMock.Setup(x => x.ConnectAsync(connectionInfo, It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(0));
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(projects);
            this.projectSystemHelper.Projects = new[] { new ProjectMock("tttt.csproj") { ProjectKind = ProjectSystemHelper.CSharpProjectKind } };
            bool projectChangedCallbackCalled = false;

            const string boundProjectKey = "whatever-key";
            const string boundProjectName = "whatever-name";

            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                c.Should().Be(connectionInfo, "Unexpected connection");
                var expectedList = new List<SonarQubeProject>(projects);
                expectedList.Insert(0, new SonarQubeProject(boundProjectKey, boundProjectName));
                p.Should().BeEquivalentTo(expectedList);
            };
            this.host.VisualStateManager.BoundProjectKey = boundProjectKey;
            this.host.VisualStateManager.BoundProjectName = boundProjectName;

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
            AssertServiceDisconnectNotCalled();
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

            AssertCredentialsStored(connectionInfo);

            this.outputWindowPane.AssertOutputStrings(4);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_ConnectionStep_10KProjectsAndKeyIsPartOfIt()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"), "user", "pass".ToSecureString());
            var projects = Enumerable.Range(1, 10000)
                .Select(i => new SonarQubeProject($"project-{i}", $"Project {i}"))
                .ToList();
            this.sonarQubeServiceMock.Setup(x => x.ConnectAsync(connectionInfo, It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(0));
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(projects);
            this.projectSystemHelper.Projects = new[] { new ProjectMock("tttt.csproj") { ProjectKind = ProjectSystemHelper.CSharpProjectKind } };
            bool projectChangedCallbackCalled = false;

            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                c.Should().Be(connectionInfo, "Unexpected connection");
                p.Should().Equal(projects);
            };
            this.host.VisualStateManager.BoundProjectKey = projects[0].Key;
            this.host.VisualStateManager.BoundProjectName = projects[0].Name;

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
            AssertServiceDisconnectNotCalled();
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

            AssertCredentialsStored(connectionInfo);

            this.outputWindowPane.AssertOutputStrings(2);
        }

        [TestMethod]
        public async Task ConnectionWorkflow_DownloadServiceParameters_RegexPropertyNotSet_SetsFilterWithDefaultExpression()
        {
            // Arrange
            this.sonarQubeServiceMock.Setup(x => x.GetAllPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
            this.sonarQubeServiceMock.Setup(x => x.GetAllPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
            this.sonarQubeServiceMock.Setup(x => x.GetAllPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
            AssertServiceDisconnectCalled();
        }

        [TestMethod]
        public async Task ConnectionWorkflow_DownloadServiceParameters_NoTestProjectRegexProperty()
        {
            // Arrange
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            this.sonarQubeServiceMock.Setup(x => x.GetAllPropertiesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<SonarQubeProperty> { });

            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            // Act
            await testSubject.DownloadServiceParametersAsync(controller, progressEvents, CancellationToken.None);

            // Assert
            filter.AssertTestRegex(SonarQubeProperty.TestProjectRegexDefaultValue, RegexOptions.IgnoreCase);
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_WhenUnsupportedVersionOfVSAndCSharpPluginEquals70_ReturnsFalseAndWriteExpectedText()
        {
            TestPluginSupport(
                expectedResult: false,
                vsVersion: "14.0",
                installedPlugin: new SonarQubePlugin("csharp", "7.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.CSharp,
                expectedMessage: "   Discovered an unsupported plugin: Plugin: 'SonarC#', Language(s): 'C#', Installed version: '7.0', Minimum version: '5.0', Maximum version: '7.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_WhenUnsupportedVersionOfVSAndCSharpPluginEquals60_ReturnsTrue()
        {
            TestPluginSupport(
                expectedResult: true,
                vsVersion: "14.0",
                installedPlugin: new SonarQubePlugin("csharp", "6.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.CSharp,
                expectedMessage: "   Discovered a supported plugin: Plugin: 'SonarC#', Language(s): 'C#', Installed version: '6.0', Minimum version: '5.0', Maximum version: '7.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_WhenSupportedVersionOfVSAndCSharpPluginEquals70_ReturnsTrueAndWriteExpectedText()
        {
            TestPluginSupport(
                expectedResult: true,
                vsVersion: "14.0.25420.00",
                installedPlugin: new SonarQubePlugin("csharp", "7.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.CSharp,
                expectedMessage: "   Discovered a supported plugin: Plugin: 'SonarC#', Language(s): 'C#', Installed version: '7.0', Minimum version: '5.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_WhenUnsupportedVersionOfVSAndVBNetPluginEquals50_ReturnsFalseAndWriteExpectedText()
        {
            TestPluginSupport(
                expectedResult: false,
                vsVersion: "14.0",
                installedPlugin: new SonarQubePlugin("vbnet", "5.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.VbNet,
                expectedMessage: "   Discovered an unsupported plugin: Plugin: 'SonarVB', Language(s): 'VB.NET', Installed version: '5.0', Minimum version: '3.0', Maximum version: '5.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_WhenUnsupportedVersionOfVSAndVBNetPluginEquals40_ReturnsTrue()
        {
            TestPluginSupport(
                expectedResult: true,
                vsVersion: "14.0",
                installedPlugin: new SonarQubePlugin("vbnet", "4.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.VbNet,
                expectedMessage: "   Discovered a supported plugin: Plugin: 'SonarVB', Language(s): 'VB.NET', Installed version: '4.0', Minimum version: '3.0', Maximum version: '5.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_WhenSupportedVersionOfVSAndVBNetPluginEquals50_ReturnsTrueAndWriteExpectedText()
        {
            TestPluginSupport(
                expectedResult: true,
                vsVersion: "14.0.25420.00",
                installedPlugin: new SonarQubePlugin("vbnet", "5.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.VbNet,
                expectedMessage: "   Discovered a supported plugin: Plugin: 'SonarVB', Language(s): 'VB.NET', Installed version: '5.0', Minimum version: '3.0'");
        }

        private static void TestPluginSupport(bool expectedResult, string vsVersion, SonarQubePlugin installedPlugin,
            MinimumSupportedSonarQubePlugin minimumSupportedPlugin, 
            string expectedMessage)
        {
            // Arrange
            VisualStudioHelpers.VisualStudioVersion = vsVersion;
            var logger = new TestLogger();

            // Act
            var result = ConnectionWorkflow.IsSonarQubePluginSupported(new[] { installedPlugin }, minimumSupportedPlugin, logger);

            // Assert
            result.Should().Be(expectedResult);
            logger.AssertOutputStrings(expectedMessage);
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_OldVersionOfVSAndSupportedCppPluginReturnsTrue()
        {
            TestPluginSupport(
                expectedResult: true,
                vsVersion: "14",
                installedPlugin: new SonarQubePlugin("cpp", "6.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.CFamily,
                expectedMessage: "   Discovered a supported plugin: Plugin: 'SonarCFamily', Language(s): 'C++, C', Installed version: '6.0', Minimum version: '6.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_NewerVersionOfVSAndSupportedCppPluginReturnsTrue()
        {
            TestPluginSupport(
                expectedResult: true,
                vsVersion: "15",
                installedPlugin: new SonarQubePlugin("cpp", "6.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.CFamily,
                expectedMessage: "   Discovered a supported plugin: Plugin: 'SonarCFamily', Language(s): 'C++, C', Installed version: '6.0', Minimum version: '6.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_OldVersionOfVSAndUnsupportedCppPlugin_ReturnsFalse()
        {
            TestPluginSupport(
                expectedResult: false,
                vsVersion: "14",
                installedPlugin: new SonarQubePlugin("cpp", "5.0"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.CFamily,
                expectedMessage: "   Discovered an unsupported plugin: Plugin: 'SonarCFamily', Language(s): 'C++, C', Installed version: '5.0', Minimum version: '6.0'");
        }

        [TestMethod]
        public void IsSonarQubePluginSupported_NewerVersionOfVSAndUnsupportedCppPlugin_ReturnsFalse()
        {
            TestPluginSupport(
                expectedResult: false,
                vsVersion: "15",
                installedPlugin: new SonarQubePlugin("cpp", "5.9"),
                minimumSupportedPlugin: MinimumSupportedSonarQubePlugin.CFamily,
                expectedMessage: "   Discovered an unsupported plugin: Plugin: 'SonarCFamily', Language(s): 'C++, C', Installed version: '5.9', Minimum version: '6.0'");
        }

        #endregion Tests

        #region Helpers

        private static void AssertIfCalled()
        {
            FluentAssertions.Execution.Execute.Assertion.FailWith("Command not expected to be called");
        }

        public void AssertServiceDisconnectCalled()
        {
            sonarQubeServiceMock.Verify(x => x.Disconnect(), Times.Once);
        }

        public void AssertServiceDisconnectNotCalled()
        {
            sonarQubeServiceMock.Verify(x => x.Disconnect(), Times.Never);
        }

        private ConnectionWorkflow SetTestSubjectWithConnectedServer()
        {
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            testSubject.ConnectedServer = connectionInfo;
            return testSubject;
        }

        private void AssertCredentialsStored(ConnectionInformation connectionInfo)
        {
            this.credentialStoreMock.Verify(
                x => x.WriteCredentials(
                    It.Is<TargetUri>(uri => uri == connectionInfo.ServerUri),
                    It.Is<Credential>(credential =>
                        credential.Username == connectionInfo.UserName &&
                        credential.Password == connectionInfo.Password.ToUnsecureString())),
                Times.Once());
        }

        private void AssertCredentialsNotStored()
        {
            this.credentialStoreMock.Verify(
                x => x.WriteCredentials(It.IsAny<TargetUri>(), It.IsAny<Credential>()),
                Times.Never());
        }

        #endregion Helpers
    }
}
