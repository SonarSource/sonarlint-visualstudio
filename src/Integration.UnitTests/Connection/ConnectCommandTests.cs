//-----------------------------------------------------------------------
// <copyright file="ConnectCommandTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.ComponentModel.Design;
using System.Globalization;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectCommandTests
    {
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableConnectionWorkflow connectionWorkflow;
        private ConfigurableConnectionInformationProvider connectionProvider;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableVsGeneralOutputWindowPane outputWindowPane;

        [TestInitialize]
        public void TestInit()
        {
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.connectionWorkflow = new ConfigurableConnectionWorkflow(this.sonarQubeService);
            this.connectionProvider = new ConfigurableConnectionInformationProvider();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.outputWindowPane = new ConfigurableVsGeneralOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindowPane);
        }

        #region  Tests
        [TestMethod]
        public void ConnectCommand_Ctor_ArgumentChecks()
        {
            ConnectCommand suppressAnalysisIssue;
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                suppressAnalysisIssue = new ConnectCommand(null, new ConfigurableSonarQubeServiceWrapper());
            });

            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConnectSectionController(new ServiceContainer(), new SonarQubeServiceWrapper(new ServiceContainer()), new ConfigurableActiveSolutionTracker(), new ConfigurableWebBrowser());
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                suppressAnalysisIssue = new ConnectCommand(controller, null);
            });
        }

        [TestMethod]
        public void ConnectCommand_Ctor_DefaultState()
        {
            // Setup
            ConnectCommand testSubject = this.CreateTestSubject();

            // Verify
            Assert.IsNull(testSubject.ProgressControlHost, "Host is expected to be null initially");
            Assert.IsNotNull(testSubject.WpfCommand, "The WPF command should never be null");
        }

        [TestMethod]
        public void ConnectCommand_WpfCommandStatus()
        {
            // Setup case 1: No host, has connection
            ConnectCommand testSubject = this.CreateTestSubject();
            this.sonarQubeService.SetConnection(new Uri("http://www.dummy.com"));

            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(), "Missing host and has connection already");

            // Setup case 2: Has host, has connection
            testSubject.ProgressControlHost = new ConfigurableProgressControlHost();

            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(), "Has connection already");

            // Setup case 4: Has no host, has no connection
            testSubject.ProgressControlHost = null;
            this.sonarQubeService.ClearConnection();

            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(), "Missing host");

            // Setup case 5: Has host, no connection and connection is in progress
            testSubject.ProgressControlHost = new ConfigurableProgressControlHost();
            testSubject.IsConnectionInProgress = true;

            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(), "Connection is in progress");

            // Set case 6: Has host and connection, no connection in progress
            testSubject.IsConnectionInProgress = false;

            // Act + Verify
            Assert.IsTrue(testSubject.WpfCommand.CanExecute(), "Has host and no connection and none being established");
        }

        [TestMethod]
        public void ConnectCommand_WpfCommandExecution()
        {
            // Setup case 1: connection provider return null connection
            ConnectCommand testSubject = this.CreateTestSubject();
            testSubject.ProgressControlHost = new ConfigurableProgressControlHost();
            this.connectionProvider.ConnectionInformationToReturn = null;

            // Sanity
            Assert.IsTrue(testSubject.WpfCommand.CanExecute(), "Should be possible to execute");

            // Sanity
            Assert.IsNull(testSubject.LastAttemptedConnection, "No previous attempts to connect");

            // Act
            testSubject.WpfCommand.Execute();

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(0);
            this.sonarQubeService.AssertConnectRequests(0);

            // Setup case 2: connection provider returns a valid connection
            var expectedConnection = new ConnectionInformation(new Uri("https://127.0.0.0"));
            this.connectionProvider.ConnectionInformationToReturn = expectedConnection;

            // Sanity
            Assert.IsNull(testSubject.LastAttemptedConnection, "Previous attempt returned null");

            // Act
            testSubject.WpfCommand.Execute();

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(1);
            this.sonarQubeService.AssertConnectRequests(1);
            Assert.AreSame(expectedConnection, ((ISonarQubeServiceWrapper)this.sonarQubeService).CurrentConnection, "Unexpected connection");

            // Setup case 3: existing connection, change to a different one
            var existingConnection = expectedConnection;
            expectedConnection = new ConnectionInformation(new Uri("https://128.0.0.0"));
            this.connectionProvider.ConnectionInformationToReturn = expectedConnection;
            this.connectionProvider.ExpectExistingConnection = true;

            // Sanity
            Assert.AreEqual(existingConnection, testSubject.LastAttemptedConnection, "Unexpected last attempted connection");

            // Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(), "Should not be able to connect if an existing connecting is present");
        }

        [TestMethod]
        public void ConnectCommand_EstablishConnection_ArgChecks()
        {
            // Setup
            ConnectCommand testSubject = this.CreateTestSubject();

            Exceptions.Expect<ArgumentNullException>(() => testSubject.EstablishConnection(null));
        }

        [TestMethod]
        public void ConnectCommand_EstablishConnection()
        {
            // Setup 
            var connect1 = new ConnectionInformation(new Uri("https://127.0.0.1"));
            var connect2 = new ConnectionInformation(new Uri("https://127.0.0.2"));
            ConnectCommand testSubject = this.CreateTestSubject();

            // Sanity
            Assert.IsNull(testSubject.LastAttemptedConnection, "No previous attempts to connect");

            // Case 1: Invalid connection
            this.sonarQubeService.AllowConnections = false;

            // Act
            testSubject.EstablishConnection(connect1);

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(1);
            this.sonarQubeService.AssertConnectRequests(1);
            Assert.AreSame(connect1, testSubject.LastAttemptedConnection);

            // Case 2: Valid connection
            this.sonarQubeService.AllowConnections = true;

            // Act
            testSubject.EstablishConnection(connect2);

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(2);
            this.sonarQubeService.AssertConnectRequests(2);
            Assert.AreSame(connect2, testSubject.LastAttemptedConnection);
        }

        [TestMethod]
        public void ConnectCommand_SetConnectionInProgress()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            testSubject.ProgressControlHost = new ConfigurableProgressControlHost();
            this.connectionProvider.ConnectionInformationToReturn = null;
            var progressEvents = new ConfigurableProgressEvents();

            // Sanity
            Assert.IsTrue(testSubject.WpfCommand.CanExecute());

            foreach (var controllerResult in (ProgressControllerResult[])Enum.GetValues(typeof(ProgressControllerResult)))
            {
                this.outputWindowPane.Reset();

                // Act - disable
                testSubject.SetConnectionInProgress(progressEvents);

                // Verify 
                Assert.IsFalse(testSubject.WpfCommand.CanExecute(), "Connection is in progress so should not be enabled");
                this.outputWindowPane.AssertOutputStrings(0);

                // Act - log progress
                string message = controllerResult.ToString();
                progressEvents.SimulateStepExecutionChanged(message, double.NaN);

                // Verify prefix
                this.outputWindowPane.AssertOutputStrings(string.Format(CultureInfo.CurrentCulture, Strings.ConnectingToSonarQubePrefixMessageFormat, message));

                // Act - finish
                progressEvents.SimulateFinished(controllerResult);

                // Verify 
                Assert.IsTrue(testSubject.WpfCommand.CanExecute(), "Connection is finished with result: {0}", controllerResult);
            }
        }
        #endregion

        #region Helpers
        private ConnectCommand CreateTestSubject()
        {
            var controller = new ConnectSectionController(this.serviceProvider, new TransferableVisualState(), this.sonarQubeService, new ConfigurableActiveSolutionTracker(), new ConfigurableWebBrowser(), Dispatcher.CurrentDispatcher);
            return new ConnectCommand(controller, this.sonarQubeService, this.connectionProvider, this.connectionWorkflow);
        }
        #endregion
    }
}
