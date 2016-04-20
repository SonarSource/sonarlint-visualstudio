//-----------------------------------------------------------------------
// <copyright file="ConnectControllerTests.cs" company="SonarSource SA and Microsoft Corporation">
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
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Globalization;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectControllerTests
    {
        private ConfigurableHost host;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableConnectionWorkflow connectionWorkflow;
        private ConfigurableConnectionInformationProvider connectionProvider;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsGeneralOutputWindowPane outputWindowPane;
        private ConfigurableIntegrationSettings settings;

        [TestInitialize]
        public void TestInit()
        {
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.connectionWorkflow = new ConfigurableConnectionWorkflow(this.sonarQubeService);
            this.connectionProvider = new ConfigurableConnectionInformationProvider();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.outputWindowPane = new ConfigurableVsGeneralOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindowPane);
            this.settings = new ConfigurableIntegrationSettings();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.host.SonarQubeService = this.sonarQubeService;

            var mefExports = MefTestHelpers.CreateExport<IIntegrationSettings>(settings);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel);
        }

        #region  Tests
        [TestMethod]
        public void ConnectionController_Ctor_ArgumentChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionController(null));
        }

        [TestMethod]
        public void ConnectionController_DefaultState()
        {
            // Setup
            var testSubject = new ConnectionController(this.host);

            // Verify
            Assert.IsNotNull(testSubject.ConnectCommand, "Connected command should not be null");
            Assert.IsNotNull(testSubject.DontWarnAgainCommand, "DontWarnAgain command should not be null");
            Assert.IsNotNull(testSubject.RefreshCommand, "Refresh command should not be null");
            Assert.IsNotNull(testSubject.WorkflowExecutor, "Need to be able to execute the workflow");
            Assert.IsFalse(testSubject.IsConnectionInProgress, "Connection is not in progress");
        }

        [TestMethod]
        public void ConnectionController_ConnectCommand_Status()
        {
            // Setup
            var testSubject = new ConnectionController(this.host);

            // Case 1: has connection, is busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Verify
            Assert.IsFalse(testSubject.ConnectCommand.CanExecute(), "Connected already and busy");

            // Case 2: has connection, not busy
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            Assert.IsFalse(testSubject.ConnectCommand.CanExecute(), "Connected already");

            // Case 3: no connection, is busy
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Verify
            Assert.IsFalse(testSubject.ConnectCommand.CanExecute(), "Busy");

            // Case 4: no connection, not busy
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            Assert.IsTrue(testSubject.ConnectCommand.CanExecute(), "No connection and not busy");
        }

        [TestMethod]
        public void ConnectionController_ConnectCommand_Execution()
        {
            // Setup
            ConnectionController testSubject = new ConnectionController(this.host, this.connectionProvider, this.connectionWorkflow);

            // Case 1: connection provider return null connection
            this.connectionProvider.ConnectionInformationToReturn = null;

            // Sanity
            Assert.IsTrue(testSubject.ConnectCommand.CanExecute(), "Should be possible to execute");

            // Sanity
            Assert.IsNull(testSubject.LastAttemptedConnection, "No previous attempts to connect");

            // Act
            testSubject.ConnectCommand.Execute();

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(0);
            this.sonarQubeService.AssertConnectRequests(0);

            // Case 2: connection provider returns a valid connection
            var expectedConnection = new ConnectionInformation(new Uri("https://127.0.0.0"));
            this.connectionProvider.ConnectionInformationToReturn = expectedConnection;
            this.sonarQubeService.ExpectedConnection = expectedConnection;
            // Sanity
            Assert.IsNull(testSubject.LastAttemptedConnection, "Previous attempt returned null");

            // Act
            testSubject.ConnectCommand.Execute();

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(1);
            this.sonarQubeService.AssertConnectRequests(1);

            // Case 3: existing connection, change to a different one
            var existingConnection = expectedConnection;
            this.sonarQubeService.ExpectedConnection = expectedConnection;
            this.host.TestStateManager.IsConnected = true;

            // Sanity
            Assert.AreEqual(existingConnection, testSubject.LastAttemptedConnection, "Unexpected last attempted connection");

            // Verify
            Assert.IsFalse(testSubject.ConnectCommand.CanExecute(), "Should not be able to connect if an existing connecting is present");
        }

        [TestMethod]
        public void ConnectionController_RefreshCommand_Status()
        {
            // Setup
            var testSubject = new ConnectionController(this.host);
            var connection = new ConnectionInformation(new Uri("http://connection"));

            // Case 1: Has connection and busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Verify
            Assert.IsFalse(testSubject.RefreshCommand.CanExecute(null), "Busy");

            // Case 2: no connection, not busy, no connection argument
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            Assert.IsFalse(testSubject.RefreshCommand.CanExecute(null), "Nothing to refresh");

            // Case 3: no connection, is busy, has connection argument
            this.host.VisualStateManager.IsBusy = true;

            // Act + Verify
            Assert.IsFalse(testSubject.RefreshCommand.CanExecute(connection), "Busy");

            // Case 4: no connection, not busy, has connection argument
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            Assert.IsTrue(testSubject.RefreshCommand.CanExecute(connection), "Has connection argument and not busy");

            // Case 5: has connection, not busy, no connection argument
            this.host.TestStateManager.IsConnected = true;
            // Act + Verify
            Assert.IsTrue(testSubject.RefreshCommand.CanExecute(null), "Has connection and not busy");

            // Case 6: has connection, not busy, connection argument the same as the existing connection
            // Act + Verify
            Assert.IsTrue(testSubject.RefreshCommand.CanExecute(connection), "Has connection and not busy");
        }

        [TestMethod]
        public void ConnectionController_RefreshCommand_Execution()
        {
            // Setup
            ConnectionController testSubject = new ConnectionController(this.host, this.connectionProvider, this.connectionWorkflow);
            this.connectionProvider.ConnectionInformationToReturn = new ConnectionInformation(new Uri("http://notExpected"));
            var connection = new ConnectionInformation(new Uri("http://Expected"));
            this.sonarQubeService.ExpectedConnection = connection;
            // Sanity
            Assert.IsTrue(testSubject.RefreshCommand.CanExecute(connection), "Should be possible to execute");

            // Sanity
            Assert.IsNull(testSubject.LastAttemptedConnection, "No previous attempts to connect");

            // Act
            testSubject.RefreshCommand.Execute(connection);

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(1);
            this.sonarQubeService.AssertConnectRequests(1);
            Assert.AreEqual(connection.ServerUri, testSubject.LastAttemptedConnection.ServerUri, "Unexpected last attempted connection");
            Assert.AreNotSame(connection, testSubject.LastAttemptedConnection, "LastAttemptedConnection should be a clone");
        }

        [TestMethod]
        public void ConnectionController_SetConnectionInProgress()
        {
            // Setup
            ConnectionController testSubject = new ConnectionController(this.host, this.connectionProvider, this.connectionWorkflow);
            this.connectionProvider.ConnectionInformationToReturn = null;
            var progressEvents = new ConfigurableProgressEvents();
            var connectionInfo = new ConnectionInformation(new Uri("http://refreshConnection"));

            // Sanity
            Assert.IsTrue(testSubject.ConnectCommand.CanExecute());
            Assert.IsTrue(testSubject.RefreshCommand.CanExecute(connectionInfo));

            foreach (var controllerResult in (ProgressControllerResult[])Enum.GetValues(typeof(ProgressControllerResult)))
            {
                this.outputWindowPane.Reset();

                // Act - disable
                testSubject.SetConnectionInProgress(progressEvents);

                // Verify 
                Assert.IsFalse(testSubject.ConnectCommand.CanExecute(), "Connection is in progress so should not be enabled");
                Assert.IsFalse(testSubject.RefreshCommand.CanExecute(connectionInfo), "Connection is in progress so should not be enabled");
                this.outputWindowPane.AssertOutputStrings(0);

                // Act - log progress
                string message = controllerResult.ToString();
                progressEvents.SimulateStepExecutionChanged(message, double.NaN);

                // Verify prefix
                this.outputWindowPane.AssertOutputStrings(string.Format(CultureInfo.CurrentCulture, Strings.ConnectingToSonarQubePrefixMessageFormat, message));

                // Act - finish
                progressEvents.SimulateFinished(controllerResult);

                // Verify 
                Assert.IsTrue(testSubject.ConnectCommand.CanExecute(), "Connection is finished with result: {0}", controllerResult);
                Assert.IsTrue(testSubject.RefreshCommand.CanExecute(connectionInfo), "Connection is finished with result: {0}", controllerResult);
            }
        }

        [TestMethod]
        public void ConnectionController_ShowNuGetWarning()
        {
            // Setup
            ConnectionController testSubject = new ConnectionController(this.host, this.connectionProvider, this.connectionWorkflow);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)this.host.ActiveSection.UserNotifications;
            this.connectionProvider.ConnectionInformationToReturn = null;
            var progressEvents = new ConfigurableProgressEvents();
            
            // Case 1: do NOT show
            // Setup
            this.settings.ShowServerNuGetTrustWarning = false;

            // Act
            testSubject.SetConnectionInProgress(progressEvents);
            progressEvents.SimulateFinished(ProgressControllerResult.Succeeded);

            // Verify
            notifications.AssertNoNotification(NotificationIds.WarnServerTrustId);

            // Case 2: show, but cancelled
            // Setup
            this.settings.ShowServerNuGetTrustWarning = false;

            // Act
            testSubject.SetConnectionInProgress(progressEvents);
            progressEvents.SimulateFinished(ProgressControllerResult.Cancelled);

            // Verify
            notifications.AssertNoNotification(NotificationIds.WarnServerTrustId);


            // Case 3: show, but failed
            // Setup
            this.settings.ShowServerNuGetTrustWarning = false;

            // Act
            testSubject.SetConnectionInProgress(progressEvents);
            progressEvents.SimulateFinished(ProgressControllerResult.Failed);

            // Verify
            notifications.AssertNoNotification(NotificationIds.WarnServerTrustId);

            // Test Case 4: show, succeeded
            // Setup
            this.settings.ShowServerNuGetTrustWarning = true;

            // Act
            testSubject.SetConnectionInProgress(progressEvents);
            progressEvents.SimulateFinished(ProgressControllerResult.Succeeded);

            // Verify
            notifications.AssertNotification(NotificationIds.WarnServerTrustId, Strings.ServerNuGetTrustWarningMessage);
        }

        [TestMethod]
        public void ConnectionController_DontWarnAgainCommand_Execution()
        {
            // Setup
            var testSubject = new ConnectionController(this.host);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            this.settings.ShowServerNuGetTrustWarning = true;
            this.host.ActiveSection.UserNotifications.ShowNotificationWarning("myMessage", NotificationIds.WarnServerTrustId, new RelayCommand(() => { }));

            // Sanity
            Assert.IsTrue(testSubject.DontWarnAgainCommand.CanExecute());

            // Act
            testSubject.DontWarnAgainCommand.Execute();

            // Verify
            Assert.IsFalse(this.settings.ShowServerNuGetTrustWarning, "Expected show warning settings to be false");
            ((ConfigurableUserNotification)this.host.ActiveSection.UserNotifications).AssertNoNotification(NotificationIds.WarnServerTrustId);
        }

        [TestMethod]
        public void ConnectionController_DontWarnAgainCommand_Status_NoIIntegrationSettings()
        {
            // Setup
            this.serviceProvider.RegisterService(typeof(SComponentModel), new ConfigurableComponentModel(), replaceExisting: true);
            var testSubject = new ConnectionController(this.host);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            this.settings.ShowServerNuGetTrustWarning = true;
            this.host.ActiveSection.UserNotifications.ShowNotificationWarning("myMessage", NotificationIds.WarnServerTrustId, new RelayCommand(() => { }));

            // Act + Verify
            Assert.IsFalse(testSubject.DontWarnAgainCommand.CanExecute());
        }

        [TestMethod]
        public void ConnectionController_DontWarnAgainCommand_Status()
        {
            // Setup
            var testSubject = new ConnectionController(this.host);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            this.settings.ShowServerNuGetTrustWarning = true;
            this.host.ActiveSection.UserNotifications.ShowNotificationWarning("myMessage", NotificationIds.WarnServerTrustId, new RelayCommand(() => { }));

            // Act + Verify
            Assert.IsTrue(testSubject.DontWarnAgainCommand.CanExecute());
        }

        #endregion
    }
}
