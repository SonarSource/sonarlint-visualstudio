//-----------------------------------------------------------------------
// <copyright file="ConnectionWorkflowTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.ComponentModelHost;
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

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.host.SetActiveSection(ConfigurableConnectSection.CreateDefault());
            this.host.SonarQubeService = this.sonarQubeService;

            this.sonarQubeService.RegisterServerPlugin(new ServerPlugin { Key = ServerPlugin.CSharpPluginKey, Version = ServerPlugin.CSharpPluginMinimumVersion });
            this.settings = new ConfigurableIntegrationSettings();

            var mefExports = MefTestHelpers.CreateExport<IIntegrationSettings>(settings);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region Tests
        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_SuccessfulConnection()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new ProjectInformation[] { new ProjectInformation { Key = "project1" } };
            this.sonarQubeService.ReturnProjectInformation = projects;
            bool projectChangedCallbackCalled = false;
            ConnectedProjectsCallback projectsChanged = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };
            
            var controller = new ConfigurableProgressController();
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject= new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled), projectsChanged);

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(connectionMessage, Strings.ConnectionResultSuccess);
            Assert.IsTrue(projectChangedCallbackCalled, "ConnectedProjectsCallaback was not called");
            sonarQubeService.AssertConnectRequests(1);
            Assert.AreSame(connectionInfo, ((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoShowErrorMessages();
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoNotification(NotificationIds.FailedToConnectId);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_UnsuccessfulConnection()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            bool projectChangedCallbackCalled = false;
            ConnectedProjectsCallback projectsChanged = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                Assert.IsNull(p, "Not expecting any projects");
            };
            var controller = new ConfigurableProgressController();
            this.sonarQubeService.AllowConnections = false;
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            var testSubject = new ConnectionWorkflow(this.host, new RelayCommand(AssertIfCalled), projectsChanged);

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(connectionMessage, Strings.ConnectionResultFailure);
            Assert.IsFalse(projectChangedCallbackCalled, "Callback should not have been called");
            this.sonarQubeService.AssertConnectRequests(1);
            Assert.IsNull(((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

            // Act (reconnect with same bad connection)
            executionEvents.Reset();
            projectChangedCallbackCalled = false;
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(connectionMessage, Strings.ConnectionResultFailure);
            Assert.IsFalse(projectChangedCallbackCalled, "Callback should not have been called");
            this.sonarQubeService.AssertConnectRequests(2);
            Assert.IsNull(((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

            // Cancelled connections
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            executionEvents.Reset();
            projectChangedCallbackCalled = false;
            CancellationToken token = tokenSource.Token;
            tokenSource.Cancel();

            // Act
            testSubject.ConnectionStep(controller, token, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(connectionMessage, Strings.ConnectionResultCancellation);
            Assert.IsFalse(projectChangedCallbackCalled, "Callback should not have been called");
            this.sonarQubeService.AssertConnectRequests(3);
            Assert.IsNull(((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_MissingCSharpPlugin_AbortsWorkflowAndDisconnects()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            ConnectedProjectsCallback projectsChanged = (c, p) => { };
            ConnectionWorkflow testSubject = new ConnectionWorkflow(this.host, new RelayCommand(() => { }), projectsChanged);
            var controller = new ConfigurableProgressController();
            this.sonarQubeService.AllowConnections = true;
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];
            this.sonarQubeService.ClearServerPlugins();
            this.host.SetActiveSection(ConfigurableConnectSection.CreateDefault());
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;
            var executionEvents = new ConfigurableProgressStepExecutionEvents();

            string expectedErrorMsg = string.Format(CultureInfo.CurrentCulture, Strings.ServerDoesNotHaveCorrectVersionOfCSharpPlugin, ServerPlugin.CSharpPluginMinimumVersion);

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            controller.AssertNumberOfAbortRequests(1);
            executionEvents.AssertProgressMessages(
                connectionInfo.ServerUri.ToString(),
                expectedErrorMsg,
                Strings.ConnectionResultFailure);
            notifications.AssertNotification(NotificationIds.BadServerPluginId, expectedErrorMsg);
        }

        #endregion

        #region Helpers
        private static void AssertIfCalled()
        {
            Assert.Fail("Command not expected to be called");
        }
        #endregion
    }
}
