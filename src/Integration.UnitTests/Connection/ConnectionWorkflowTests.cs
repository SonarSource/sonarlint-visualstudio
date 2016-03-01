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
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectionWorkflowTests
    {
        private ConfigurableIntegrationSettings settings;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.settings = new ConfigurableIntegrationSettings();

            var mefExports = MefTestHelpers.CreateExport<IIntegrationSettings>(settings);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region Tests

        [TestMethod]
        public void ConnectionWorkflow_ShowNuGetWarning()
        {
            // Setup
            ConnectionController command;
            ConnectedProjectsCallback callback = (c, p) => { };
            ConnectionWorkflow testSubject = this.CreateTestSubject(callback, out command);
            var notifications = new ConfigurableUserNotification();
            command.UserNotification = notifications;

            // Test Case 1: do NOT show
            // Setup
            this.settings.ShowServerNuGetTrustWarning = false;

            // Act
            testSubject.ShowNuGetWarning();

            // Verify
            notifications.AssertNoNotification(NotificationIds.WarnServerTrustId);


            // Test Case 2: show
            // Setup
            this.settings.ShowServerNuGetTrustWarning = true;

            // Act
            testSubject.ShowNuGetWarning();

            // Verify
            notifications.AssertNotification(NotificationIds.WarnServerTrustId, Strings.ServerNuGetTrustWarningMessage);
        }

        [TestMethod]
        public void ConnectionWorkflow_DontWarnAgainExec()
        {
            // Setup
            ConnectionController command;
            ConnectedProjectsCallback callback = (c, p) => { };
            ConnectionWorkflow testSubject = this.CreateTestSubject(callback, out command);
            var notifications = new ConfigurableUserNotification();
            command.UserNotification = notifications;

            this.settings.ShowServerNuGetTrustWarning = true;
            ((IUserNotification)notifications).ShowNotificationWarning("myMessage", NotificationIds.WarnServerTrustId, new RelayCommand(() => { }));

            // Act
            testSubject.DontWarnAgainExec();

            // Verify
            Assert.IsFalse(this.settings.ShowServerNuGetTrustWarning, "Expected show warning settings to be false");
            notifications.AssertNoNotification(NotificationIds.WarnServerTrustId);
        }

        [TestMethod]
        public void ConnectionWorkflow_DontWarnAgainCanExec_HasSettings_IsTrue()
        {
            // Setup
            ConnectionController command;
            ConnectedProjectsCallback callback = (c, p) => { };
            ConnectionWorkflow testSubject = this.CreateTestSubject(callback, out command);

            // Act + Verify
            Assert.IsTrue(testSubject.DontWarnAgainCanExec(), "Expected to be executable when settings are available");
        }

        [TestMethod]
        public void ConnectionWorkflow_DontWarnAgainCanExec_NoSettings_IsFalse()
        {
            // Setup
            var controller = new ConnectSectionController(new ConfigurableServiceProvider(false), new TransferableVisualState(), sonarQubeService, new ConfigurableActiveSolutionTracker(), Dispatcher.CurrentDispatcher);
            var command = new ConnectionController(controller, this.sonarQubeService);
            ConnectedProjectsCallback callback = (c, p) => { };
            var testSubject = new ConnectionWorkflow(command, callback);

            // Act + Verify
            Assert.IsFalse(testSubject.DontWarnAgainCanExec(), "Not expected to be executable when settings are unavailable");
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_SuccessfulConnection()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            var projects = new ProjectInformation[] { new ProjectInformation { Key = "project1" } };
            this.sonarQubeService.ReturnProjectInformation = projects;
            ConnectionController command;
            bool projectChangedCallbackCalled = false;
            ConnectedProjectsCallback projectsChanged = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                CollectionAssert.AreEqual(projects, p.ToArray(), "Unexpected projects");
            };
            ConnectionWorkflow testSubject = this.CreateTestSubject(projectsChanged, out command);
            var controller = new ConfigurableProgressController();
            var notifications = new ConfigurableUserNotification();
            command.UserNotification = notifications;
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();
            this.settings.ShowServerNuGetTrustWarning = true;

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(connectionMessage, Strings.ConnectionResultSuccess);
            Assert.IsTrue(projectChangedCallbackCalled, "ConnectedProjectsCallaback was not called");
            sonarQubeService.AssertConnectRequests(1);
            Assert.AreSame(connectionInfo, ((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            notifications.AssertNoShowErrorMessages();
            notifications.AssertNoNotification(NotificationIds.FailedToConnectId);
        }

        [TestMethod]
        public void ConnectionWorkflow_ConnectionStep_UnsuccessfulConnection()
        {
            // Setup
            var connectionInfo = new ConnectionInformation(new Uri("http://server"));
            ConnectionController command;
            bool projectChangedCallbackCalled = false;
            ConnectedProjectsCallback projectsChanged = (c, p) =>
            {
                projectChangedCallbackCalled = true;
                Assert.AreSame(connectionInfo, c, "Unexpected connection");
                Assert.IsNull(p, "Not expecting any projects");
            };
            ConnectionWorkflow testSubject = this.CreateTestSubject(projectsChanged, out command);
            var controller = new ConfigurableProgressController();
            this.sonarQubeService.AllowConnections = false;
            var notifications = new ConfigurableUserNotification();
            command.UserNotification = notifications;
            var executionEvents = new ConfigurableProgressStepExecutionEvents();
            string connectionMessage = connectionInfo.ServerUri.ToString();

            // Act
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(connectionMessage, Strings.ConnectionResultFailure);
            Assert.IsTrue(projectChangedCallbackCalled, "ConnectedProjectsCallaback was not called");
            this.sonarQubeService.AssertConnectRequests(1);
            Assert.IsNull(((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            notifications.AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

            // Act (reconnect with same bad connection)
            executionEvents.Reset();
            projectChangedCallbackCalled = false;
            testSubject.ConnectionStep(controller, CancellationToken.None, connectionInfo, executionEvents);

            // Verify
            executionEvents.AssertProgressMessages(connectionMessage, Strings.ConnectionResultFailure);
            Assert.IsTrue(projectChangedCallbackCalled, "ConnectedProjectsCallaback was not called");
            this.sonarQubeService.AssertConnectRequests(2);
            Assert.IsNull(((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            notifications.AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);

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
            Assert.IsTrue(projectChangedCallbackCalled, "ConnectedProjectsCallaback was not called");
            this.sonarQubeService.AssertConnectRequests(3);
            Assert.IsNull(((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");
            notifications.AssertNotification(NotificationIds.FailedToConnectId, Strings.ConnectionFailed);
        }
        #endregion

        #region Helpers
        private ConnectionWorkflow CreateTestSubject(ConnectedProjectsCallback projectsChanged, out ConnectionController owningCommand)
        {
            var controller = new ConnectSectionController(this.serviceProvider, new TransferableVisualState(), this.sonarQubeService, new ConfigurableActiveSolutionTracker(), Dispatcher.CurrentDispatcher);
            owningCommand = controller.ConnectCommand;
            return new ConnectionWorkflow(owningCommand, projectsChanged);
        }
        #endregion
    }
}
