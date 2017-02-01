/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Globalization;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;

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
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableIntegrationSettings settings;

        [TestInitialize]
        public void TestInit()
        {
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.connectionWorkflow = new ConfigurableConnectionWorkflow(this.sonarQubeService);
            this.connectionProvider = new ConfigurableConnectionInformationProvider();
            this.serviceProvider = new ConfigurableServiceProvider();
            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);
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
            testSubject.ConnectCommand.Should().NotBeNull("Connected command should not be null");
            testSubject.DontWarnAgainCommand.Should().NotBeNull("DontWarnAgain command should not be null");
            testSubject.RefreshCommand.Should().NotBeNull("Refresh command should not be null");
            testSubject.WorkflowExecutor.Should().NotBeNull("Need to be able to execute the workflow");
            testSubject.IsConnectionInProgress.Should().BeFalse("Connection is not in progress");
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
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Connected already and busy");

            // Case 2: has connection, not busy
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Connected already");

            // Case 3: no connection, is busy
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Verify
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Busy");

            // Case 4: no connection, not busy
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            testSubject.ConnectCommand.CanExecute().Should().BeTrue("No connection and not busy");
        }

        [TestMethod]
        public void ConnectionController_ConnectCommand_Execution()
        {
            // Setup
            ConnectionController testSubject = new ConnectionController(this.host, this.connectionProvider, this.connectionWorkflow);

            // Case 1: connection provider return null connection
            this.connectionProvider.ConnectionInformationToReturn = null;

            // Sanity
            testSubject.ConnectCommand.CanExecute().Should().BeTrue("Should be possible to execute");

            // Sanity
            testSubject.LastAttemptedConnection.Should().BeNull("No previous attempts to connect");

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
            testSubject.LastAttemptedConnection.Should().BeNull("Previous attempt returned null");

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
            testSubject.LastAttemptedConnection.Should().Be(existingConnection, "Unexpected last attempted connection");

            // Verify
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Should not be able to connect if an existing connecting is present");
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
            testSubject.RefreshCommand.CanExecute(null).Should().BeFalse("Busy");

            // Case 2: no connection, not busy, no connection argument
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            testSubject.RefreshCommand.CanExecute(null).Should().BeFalse("Nothing to refresh");

            // Case 3: no connection, is busy, has connection argument
            this.host.VisualStateManager.IsBusy = true;

            // Act + Verify
            testSubject.RefreshCommand.CanExecute(connection).Should().BeFalse("Busy");

            // Case 4: no connection, not busy, has connection argument
            this.host.VisualStateManager.IsBusy = false;

            // Act + Verify
            testSubject.RefreshCommand.CanExecute(connection).Should().BeTrue("Has connection argument and not busy");

            // Case 5: has connection, not busy, no connection argument
            this.host.TestStateManager.IsConnected = true;
            // Act + Verify
            testSubject.RefreshCommand.CanExecute(null).Should().BeTrue("Has connection and not busy");

            // Case 6: has connection, not busy, connection argument the same as the existing connection
            // Act + Verify
            testSubject.RefreshCommand.CanExecute(connection).Should().BeTrue("Has connection and not busy");
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
            testSubject.RefreshCommand.CanExecute(connection).Should().BeTrue("Should be possible to execute");

            // Sanity
            testSubject.LastAttemptedConnection.Should().BeNull("No previous attempts to connect");

            // Act
            testSubject.RefreshCommand.Execute(connection);

            // Verify
            this.connectionWorkflow.AssertEstablishConnectionCalled(1);
            this.sonarQubeService.AssertConnectRequests(1);
            testSubject.LastAttemptedConnection.ServerUri.Should().Be(connection.ServerUri, "Unexpected last attempted connection");
            testSubject.LastAttemptedConnection.Should().NotBe(connection, "LastAttemptedConnection should be a clone");
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
            testSubject.ConnectCommand.CanExecute().Should().BeTrue();
            testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeTrue();

            foreach (var controllerResult in (ProgressControllerResult[])Enum.GetValues(typeof(ProgressControllerResult)))
            {
                this.outputWindowPane.Reset();

                // Act - disable
                testSubject.SetConnectionInProgress(progressEvents);

                // Verify
                testSubject.ConnectCommand.CanExecute().Should().BeFalse("Connection is in progress so should not be enabled");
                testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeFalse("Connection is in progress so should not be enabled");
                this.outputWindowPane.AssertOutputStrings(0);

                // Act - log progress
                string message = controllerResult.ToString();
                progressEvents.SimulateStepExecutionChanged(message, double.NaN);

                // Verify prefix
                this.outputWindowPane.AssertOutputStrings(string.Format(CultureInfo.CurrentCulture, Strings.ConnectingToSonarQubePrefixMessageFormat, message));

                // Act - finish
                progressEvents.SimulateFinished(controllerResult);

                // Verify
                testSubject.ConnectCommand.CanExecute().Should().BeTrue("Connection is finished with result: {0}", controllerResult);
                testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeTrue("Connection is finished with result: {0}", controllerResult);
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

            // Case 2: show, but canceled
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
            testSubject.DontWarnAgainCommand.CanExecute().Should().BeTrue();

            // Act
            testSubject.DontWarnAgainCommand.Execute();

            // Verify
            this.settings.ShowServerNuGetTrustWarning.Should().BeFalse("Expected show warning settings to be false");
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
            testSubject.DontWarnAgainCommand.CanExecute().Should().BeFalse();
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
            testSubject.DontWarnAgainCommand.CanExecute().Should().BeTrue();
        }

        #endregion
    }
}
