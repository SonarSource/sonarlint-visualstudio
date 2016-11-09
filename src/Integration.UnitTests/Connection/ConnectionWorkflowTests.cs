//-----------------------------------------------------------------------
// <copyright file="ConnectionWorkflowTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectionWorkflowTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableHost host;
        private ConfigurableIntegrationSettings settings;
        private ConfigurableProjectSystemFilter filter;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            this.host.SonarQubeService = this.sonarQubeService;
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);

            this.sonarQubeService.RegisterServerPlugin(new ServerPlugin { Key = MinimumSupportedServerPlugin.CSharp.Key, Version = MinimumSupportedServerPlugin.CSharp.MinimumVersion });
            this.sonarQubeService.RegisterServerPlugin(new ServerPlugin { Key = MinimumSupportedServerPlugin.VbNet.Key, Version = MinimumSupportedServerPlugin.VbNet.MinimumVersion });
            this.settings = new ConfigurableIntegrationSettings {AllowNuGetPackageInstall = true};

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
            Assert.Fail("Expected exception of type ArgumentNullException but no exception was thrown.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ConnectionWorkflow_ConnectionStep_WhenGivenANullParentCommand_ThrowsArgumentNullException()
        {
            // Arrange & Act
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, null);

            // Assert
            Assert.Fail("Expected exception of type ArgumentNullException but no exception was thrown.");
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
            Assert.AreEqual(testSubject.ConnectedServer, connectionInfo);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenCSharpPluginAndAnyCSharpProject_SuccessfulConnection()
        {
            ConnectionWorkflow_ConnectionStep_WhenXPluginAndAnyXProject_SuccessfulConnection("foo.csproj", ProjectSystemHelper.CSharpProjectKind);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenVBNetPluginAndAnyVBNetProject_SuccessfulConnection()
        {
            ConnectionWorkflow_ConnectionStep_WhenXPluginAndAnyXProject_SuccessfulConnection("foo.vbproj", ProjectSystemHelper.VbProjectKind);
        }

        private void ConnectionWorkflow_ConnectionStep_WhenXPluginAndAnyXProject_SuccessfulConnection(string projectName, string projectKind)
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new ProjectInformation[] { new ProjectInformation { Key = "project1" } };
            this.sonarQubeService.ReturnProjectInformation = projects;
            this.projectSystemHelper.Projects = new[] { new ProjectMock(projectName) { ProjectKind = projectKind } };
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            controller.AssertNumberOfAbortRequests(0);
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.DetectingServerPlugins,
                Strings.ConnectionResultSuccess);
            Assert.IsTrue(projectChangedCallbackCalled, "ConnectedProjectsCallaback was not called");
            sonarQubeService.AssertConnectRequests(1);
            Assert.AreEqual(connectionInfo, testSubject.ConnectedServer);
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoShowErrorMessages();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoNotification(NotificationIds.FailedToConnectId);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenMissingCSharpPluginAndVBNetPlugin_AbortsWorkflowAndDisconnects()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(() => { }));
            var controller = new ConfigurableProgressController();
            this.sonarQubeService.AllowConnections = true;
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];
            this.sonarQubeService.ClearServerPlugins();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;
            var executionEvents = new ConfigurableProgressStepExecutionEvents();

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            controller.AssertNumberOfAbortRequests(1);
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.DetectingServerPlugins,
                Strings.ConnectionResultFailure);
            notifications.AssertNotification(NotificationIds.BadServerPluginId, Strings.ServerHasNoSupportedPluginVersion);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenPluginOkAndNoProjects_AbortsWorkflowAndDisconnects()
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new ProjectInformation[] { new ProjectInformation { Key = "project1" } };
            this.sonarQubeService.ReturnProjectInformation = projects;
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            controller.AssertNumberOfAbortRequests(1);
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.DetectingServerPlugins,
                Strings.ConnectionResultFailure);
            Assert.IsFalse(projectChangedCallbackCalled, "ConnectedProjectsCallaback was called");
            notifications.AssertNotification(NotificationIds.BadServerPluginId, Strings.SolutionContainsNoSupportedProject);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenCSharpPluginAndNoCSharpProject_AbortsWorkflowAndDisconnects()
        {
            ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.vbproj", ProjectSystemHelper.VbProjectKind, MinimumSupportedServerPlugin.CSharp);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_WhenVBNetPluginAndNoVBNetProject_AbortsWorkflowAndDisconnects()
        {
            ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects("foo.csproj", ProjectSystemHelper.CSharpProjectKind, MinimumSupportedServerPlugin.VbNet);
        }

        private void ConnectionWorkflow_ConnectionStep_WhenXPluginAndNoXProject_AbortsWorkflowAndDisconnects(string projectName, string projectKind, MinimumSupportedServerPlugin minimumSupportedServerPlugin)
        {
            // Arrange
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new ProjectInformation[] { new ProjectInformation { Key = "project1" } };
            this.sonarQubeService.ReturnProjectInformation = projects;
            this.sonarQubeService.ClearServerPlugins();
            this.sonarQubeService.RegisterServerPlugin(new ServerPlugin { Key = minimumSupportedServerPlugin.Key, Version = minimumSupportedServerPlugin.MinimumVersion });
            this.projectSystemHelper.Projects = new[] { new ProjectMock(projectName) { ProjectKind = projectKind } };
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };

            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            controller.AssertNumberOfAbortRequests(1);
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                Strings.DetectingServerPlugins,
                Strings.ConnectionResultFailure);
            Assert.IsFalse(projectChangedCallbackCalled, "ConnectedProjectsCallaback was called");
            notifications.AssertNotification(NotificationIds.BadServerPluginId, string.Format(Strings.OnlySupportedPluginHasNoProjectInSolution, minimumSupportedServerPlugin.Language.Name));
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_UnsuccessfulConnection()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            bool projectChangedCallbackCalled = false;
            this.host.TestStateManager.SetProjectsAction = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                Assert.IsNull(p, "Not expecting any projects");
            };
            this.projectSystemHelper.Projects = new[] { new ProjectMock("foo.csproj") { ProjectKind = ProjectSystemHelper.CSharpProjectKind } };
            var controller = new ConfigurableProgressController();
            this.sonarQubeService.AllowConnections = false;
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.DetectingServerPlugins,
                Strings.ConnectionResultFailure);
            Assert.IsFalse(projectChangedCallbackCalled, "Callback should not have been called");
            this.sonarQubeService.AssertConnectRequests(1);
            Assert.IsFalse(this.host.VisualStateManager.IsConnected);
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

            // Act (reconnect with same bad connection)
            executionEvents.Reset();
            projectChangedCallbackCalled = false;
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.DetectingServerPlugins,
                Strings.ConnectionResultFailure);
            Assert.IsFalse(projectChangedCallbackCalled, "Callback should not have been called");
            this.sonarQubeService.AssertConnectRequests(2);
            Assert.IsFalse(this.host.VisualStateManager.IsConnected);
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

            // Canceled connections
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            executionEvents.Reset();
            projectChangedCallbackCalled = false;
            CancellationToken token = tokenSource.Token;
            tokenSource.Cancel();

            // Act
            testSubject.ConnectionStep(controller, token, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(
                connectionMessage,
                Strings.DetectingServerPlugins,
                Strings.ConnectionResultCancellation);
            Assert.IsFalse(projectChangedCallbackCalled, "Callback should not have been called");
            this.sonarQubeService.AssertConnectRequests(3);
            Assert.IsFalse(this.host.VisualStateManager.IsConnected);
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);
        }

        [TestMethod]
        public void ConnectionWorkflow_DownloadServiceParameters_RegexPropertyNotSet_SetsFilterWithDefaultExpression()
        {
            // Setup
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();
            var expectedExpression = ServerProperty.TestProjectRegexDefaultValue;
            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            // Sanity
            Assert.IsFalse(this.sonarQubeService.ServerProperties.Any(x => x.Key != ServerProperty.TestProjectRegexKey), "Test project regex property should not be set");

            // Act
            testSubject.DownloadServiceParameters(controller, CancellationToken.None, progressEvents);

            // Verify
            filter.AssertTestRegex(expectedExpression, RegexOptions.IgnoreCase);
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
        }

        [TestMethod]
        public void ConnectionWorkflow_DownloadServiceParameters_CustomRegexProperty_SetsFilterWithCorrectExpression()
        {
            // Setup
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            var expectedExpression = ".*spoon.*";
            this.sonarQubeService.RegisterServerProperty(new ServerProperty
            {
                Key = ServerProperty.TestProjectRegexKey,
                Value = expectedExpression
            });

            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            // Act
            testSubject.DownloadServiceParameters(controller, CancellationToken.None, progressEvents);

            // Verify
            filter.AssertTestRegex(expectedExpression, RegexOptions.IgnoreCase);
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
        }

        [TestMethod]
        public void ConnectionWorkflow_DownloadServiceParameters_InvalidRegex_UsesDefault()
        {
            // Setup
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            var badExpression = "*-gf/d*-b/try\\*-/r-*yeb/\\";
            var expectedExpression = ServerProperty.TestProjectRegexDefaultValue;
            this.sonarQubeService.RegisterServerProperty(new ServerProperty
            {
                Key = ServerProperty.TestProjectRegexKey,
                Value = badExpression
            });

            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            // Act
            testSubject.DownloadServiceParameters(controller, CancellationToken.None, progressEvents);

            // Verify
            filter.AssertTestRegex(expectedExpression, RegexOptions.IgnoreCase);
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
            this.outputWindowPane.AssertOutputStrings(string.Format(CultureInfo.CurrentCulture, Strings.InvalidTestProjectRegexPattern, badExpression));
        }

        [TestMethod]
        public void ConnectionWorkflow_DownloadServiceParameters_Cancelled_AbortsWorkflow()
        {
            // Setup
            var controller = new ConfigurableProgressController();
            var progressEvents = new ConfigurableProgressStepExecutionEvents();

            ConnectionWorkflow testSubject = SetTestSubjectWithConnectedServer();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            testSubject.DownloadServiceParameters(controller, cts.Token, progressEvents);

            // Verify
            progressEvents.AssertProgressMessages(Strings.DownloadingServerSettingsProgessMessage);
            controller.AssertNumberOfAbortRequests(1);
        }
        #endregion

        #region Helpers
        private static void AssertIfCalled()
        {
            Assert.Fail("Command not expected to be called");
        }

        private ConnectionWorkflow SetTestSubjectWithConnectedServer()
        {
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled));
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            testSubject.ConnectedServer = connectionInfo;
            return testSubject;
        }
        #endregion
    }
}
