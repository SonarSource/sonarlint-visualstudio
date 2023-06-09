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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.Secrets;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectionWorkflowTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableHost host;
        private ConfigurableSonarLintSettings settings;
        private Mock<ICredentialStoreService> credentialStoreMock;
        private Mock<ITestProjectRegexSetter> testProjectRegexSetter;
        private Mock<IFolderWorkspaceService> folderWorkspaceService;
        private TestLogger logger;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            this.host.SonarQubeService = this.sonarQubeServiceMock.Object;

            this.settings = new ConfigurableSonarLintSettings();

            folderWorkspaceService = new Mock<IFolderWorkspaceService>();
            folderWorkspaceService.Setup(x => x.IsFolderWorkspace()).Returns(false);

            this.credentialStoreMock = new Mock<ICredentialStoreService>();

            var mefModel = ConfigurableComponentModel.CreateWithExports(
                MefTestHelpers.CreateExport<IFolderWorkspaceService>(folderWorkspaceService.Object),
                MefTestHelpers.CreateExport<ISonarLintSettings>(settings),
                MefTestHelpers.CreateExport<IProjectToLanguageMapper>(new ProjectToLanguageMapper(Mock.Of<ICMakeProjectTypeIndicator>(), Mock.Of<IProjectLanguageIndicator>(), Mock.Of<IConnectedModeSecrets>())),
                MefTestHelpers.CreateExport<ICredentialStoreService>(credentialStoreMock.Object));

            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);


            this.testProjectRegexSetter = new Mock<ITestProjectRegexSetter>();
            this.serviceProvider.RegisterService(typeof(ITestProjectRegexSetter), testProjectRegexSetter.Object);

            logger = new TestLogger();
            host.Logger = logger;
            serviceProvider.RegisterService(typeof(ILogger), logger);
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
        public async Task ConnectionWorkflow_ConnectionStep_WhenNoProjectsOnServer_SuccessfulConnection()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"), "user", "pass".ToSecureString());
            var projects = Array.Empty<SonarQubeProject>();
            this.sonarQubeServiceMock.Setup(x => x.ConnectAsync(connectionInfo, It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(0));
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

            // Act
            await testSubject.ConnectionStepAsync(connectionInfo, controller, executionEvents, CancellationToken.None);

            // Assert
            controller.NumberOfAbortRequests.Should().Be(0);
            AssertServiceDisconnectNotCalled();
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.ConnectionStepValidatinCredentials,
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
        public async Task ConnectionWorkflow_ConnectionStep_WhenFolderWorkspace_SuccessfulConnection()
        {
            // Arrange
            folderWorkspaceService.Setup(x => x.IsFolderWorkspace()).Returns(true);

            var connectionInfo = new ConnectionInformation(new Uri("http://server"), "user", "pass".ToSecureString());
            var projects = new List<SonarQubeProject> { new SonarQubeProject("project1", "") };
            this.sonarQubeServiceMock.Setup(x => x.ConnectAsync(connectionInfo, It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(0));
            this.sonarQubeServiceMock.Setup(x => x.GetAllProjectsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(projects);

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
                Strings.ConnectionStepRetrievingProjects,
                Strings.ConnectionResultSuccess);

            sonarQubeServiceMock.Verify(x=> x.GetAllPluginsAsync(CancellationToken.None), Times.Never);

            this.sonarQubeServiceMock.Verify(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(),
                It.IsAny<CancellationToken>()), Times.Once());
            testSubject.ConnectedServer.Should().Be(connectionInfo);
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoShowErrorMessages();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoNotification(NotificationIds.FailedToConnectId);

            AssertCredentialsStored(connectionInfo);
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
                Strings.ConnectionStepRetrievingProjects,
                Strings.ConnectionResultSuccess);
            projectChangedCallbackCalled.Should().BeTrue("ConnectedProjectsCallaback was not called");
            this.sonarQubeServiceMock.Verify(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(),
                It.IsAny<CancellationToken>()), Times.Once());
            testSubject.ConnectedServer.Should().Be(connectionInfo);
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoShowErrorMessages();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoNotification(NotificationIds.FailedToConnectId);

            AssertCredentialsStored(connectionInfo);

            logger.OutputStrings.Where(x => x.Contains("whatever-key"))
                .Should().HaveCountGreaterOrEqualTo(1);
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
            testProjectRegexSetter.Verify(x => x.SetTestRegex(expectedExpression), Times.Once);
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
