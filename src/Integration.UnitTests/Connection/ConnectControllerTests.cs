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
using System.Globalization;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Progress.Controller;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.Connection
{
    [TestClass]
    public class ConnectControllerTests
    {
        private ConfigurableHost host;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableConnectionInformationProvider connectionProvider;
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarLintSettings settings;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private TestLogger logger;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            logger = new TestLogger();
            serviceProvider.RegisterService(typeof(ILogger), logger);

            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.connectionProvider = new ConfigurableConnectionInformationProvider();
            this.settings = new ConfigurableSonarLintSettings();
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.host = new ConfigurableHost()
            {
                SonarQubeService = this.sonarQubeServiceMock.Object,
                Logger = logger
            };

            IComponentModel componentModel = ConfigurableComponentModel.CreateWithExports(
                new []
                {
                    MefTestHelpers.CreateExport<IProjectSystemHelper>(projectSystemHelper),
                    MefTestHelpers.CreateExport<ISonarLintSettings>(settings)
                });
            this.serviceProvider.RegisterService(typeof(SComponentModel), componentModel);
        }

        #region Tests

        [TestMethod]
        public void ConnectionController_Ctor_ArgumentChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionController(null, Mock.Of<IHost>()));
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionController(Mock.Of<IServiceProvider>(), null));
        }

        [TestMethod]
        public void ConnectionController_DefaultState()
        {
            // Arrange
            var testSubject = CreateTestSubject();

            // Assert
            testSubject.ConnectCommand.Should().NotBeNull("Connected command should not be null");
            testSubject.RefreshCommand.Should().NotBeNull("Refresh command should not be null");
            testSubject.WorkflowExecutor.Should().NotBeNull("Need to be able to execute the workflow");
            testSubject.IsConnectionInProgress.Should().BeFalse("Connection is not in progress");
        }

        [TestMethod]
        public void ConnectionController_ConnectCommand_SolutionFully_Open_Status()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            this.projectSystemHelper.SetIsSolutionFullyOpened(true);

            // Case 1: has connection, is busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Connected already and busy");

            // Case 2: has connection, not busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = false;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Connected already");

            // Case 3: no connection, is busy
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Busy");

            // Case 4: no connection, not busy
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = false;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeTrue("No connection and not busy");
        }

        [TestMethod]
        public void ConnectionController_ConnectCommand_SolutionNotFully_Open_Status_AlwaysFalse()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            this.projectSystemHelper.SetIsSolutionFullyOpened(false);

            // Case 1: has connection, is busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Solution not fully open");

            // Case 2: has connection, not busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = false;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Solution not fully open");

            // Case 3: no connection, is busy
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Busy");

            // Case 4: no connection, not busy
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = false;

            // Act + Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Solution not fully open");
        }

        private static Mock<IConnectionWorkflowExecutor> CreateWorkflow()
        {
            var workflow = new Mock<IConnectionWorkflowExecutor>();
            return workflow;
        }

        [TestMethod]
        public void ConnectionController_ConnectCommand_Execution()
        {
            // Arrange
            var connectionWorkflowMock = CreateWorkflow();
            connectionWorkflowMock.Setup(x => x.EstablishConnection(It.IsAny<ConnectionInformation>()));
            ConnectionController testSubject = new ConnectionController(this.serviceProvider, this.host, this.connectionProvider,
                connectionWorkflowMock.Object);
            this.projectSystemHelper.SetIsSolutionFullyOpened(true);

            // Case 1: connection provider return null connection
            this.connectionProvider.ConnectionInformationToReturn = null;

            // Sanity
            testSubject.ConnectCommand.CanExecute().Should().BeTrue("Should be possible to execute");

            // Sanity
            testSubject.LastAttemptedConnection.Should().BeNull("No previous attempts to connect");

            // Act
            testSubject.ConnectCommand.Execute();

            // Assert
            connectionWorkflowMock.Verify(x => x.EstablishConnection(It.IsAny<ConnectionInformation>()), Times.Never);

            // Case 2: connection provider returns a valid connection
            var expectedConnection = new ConnectionInformation(new Uri("https://127.0.0.0"));
            this.connectionProvider.ConnectionInformationToReturn = expectedConnection;
            // Sanity
            testSubject.LastAttemptedConnection.Should().BeNull("Previous attempt returned null");

            // Act
            testSubject.ConnectCommand.Execute();

            // Assert
            connectionWorkflowMock.Verify(x => x.EstablishConnection(It.IsAny<ConnectionInformation>()), Times.Once);

            // Case 3: existing connection, change to a different one
            var existingConnection = expectedConnection;
            this.host.TestStateManager.IsConnected = true;

            // Sanity
            testSubject.LastAttemptedConnection.Should().Be(existingConnection, "Unexpected last attempted connection");

            // Assert
            testSubject.ConnectCommand.CanExecute().Should().BeFalse("Should not be able to connect if an existing connecting is present");
        }

        [TestMethod]
        public void ConnectionController_RefreshCommand_Status()
        {
            // Arrange
            var testSubject = CreateTestSubject();
            var connection = new ConnectionInformation(new Uri("http://connection"));

            // Case 1: Has connection and busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = true;

            // Act + Assert
            testSubject.RefreshCommand.CanExecute(null).Should().BeFalse("Busy");

            // Case 2: no connection, not busy, no connection argument
            this.host.TestStateManager.IsConnected = false;
            this.host.VisualStateManager.IsBusy = false;

            // Act + Assert
            testSubject.RefreshCommand.CanExecute(null).Should().BeFalse("Nothing to refresh");

            // Case 3: no connection, is busy, has connection argument
            this.host.VisualStateManager.IsBusy = true;

            // Act + Assert
            testSubject.RefreshCommand.CanExecute(connection).Should().BeFalse("Busy");

            // Case 4: no connection, not busy, has connection argument
            this.host.VisualStateManager.IsBusy = false;

            // Act + Assert
            testSubject.RefreshCommand.CanExecute(connection).Should().BeTrue("Has connection argument and not busy");

            // Case 5: has connection, not busy, no connection argument
            this.host.TestStateManager.IsConnected = true;
            // Act + Assert
            testSubject.RefreshCommand.CanExecute(null).Should().BeTrue("Has connection and not busy");

            // Case 6: has connection, not busy, connection argument the same as the existing connection
            // Act + Assert
            testSubject.RefreshCommand.CanExecute(connection).Should().BeTrue("Has connection and not busy");
        }

        [TestMethod]
        public void ConnectionController_RefreshCommand_Execution()
        {
            // Arrange
            var connectionWorkflowMock = CreateWorkflow();
            ConnectionController testSubject = new ConnectionController(serviceProvider, host, connectionProvider,
                connectionWorkflowMock.Object);
            this.connectionProvider.ConnectionInformationToReturn = new ConnectionInformation(new Uri("http://notExpected"));
            var connection = new ConnectionInformation(new Uri("http://Expected"));
            // Sanity
            testSubject.RefreshCommand.CanExecute(connection).Should().BeTrue("Should be possible to execute");

            // Sanity
            testSubject.LastAttemptedConnection.Should().BeNull("No previous attempts to connect");

            // Act
            testSubject.RefreshCommand.Execute(connection);

            // Assert
            connectionWorkflowMock.Verify(x => x.EstablishConnection(It.IsAny<ConnectionInformation>()), Times.Once);
            testSubject.LastAttemptedConnection.ServerUri.Should().Be(connection.ServerUri, "Unexpected last attempted connection");
            testSubject.LastAttemptedConnection.Should().NotBe(connection, "LastAttemptedConnection should be a clone");
        }

        [TestMethod]
        public void ConnectionController_SetConnectionInProgress()
        {
            // Arrange
            var connectionWorkflowMock = CreateWorkflow();
            ConnectionController testSubject = new ConnectionController(serviceProvider, host, connectionProvider,
                connectionWorkflowMock.Object);
            this.projectSystemHelper.SetIsSolutionFullyOpened(true);
            this.connectionProvider.ConnectionInformationToReturn = null;
            var progressEvents = new ConfigurableProgressEvents();
            var connectionInfo = new ConnectionInformation(new Uri("http://refreshConnection"));

            // Sanity
            testSubject.ConnectCommand.CanExecute().Should().BeTrue();
            testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeTrue();

            foreach (var controllerResult in (ProgressControllerResult[])Enum.GetValues(typeof(ProgressControllerResult)))
            {
                logger.Reset();

                // Act - disable
                testSubject.SetConnectionInProgress(progressEvents);

                // Assert
                testSubject.ConnectCommand.CanExecute().Should().BeFalse("Connection is in progress so should not be enabled");
                testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeFalse("Connection is in progress so should not be enabled");
                logger.AssertOutputStrings(0);

                // Act - log progress
                string message = controllerResult.ToString();
                progressEvents.SimulateStepExecutionChanged(message, double.NaN);

                // Assert prefix
                logger.AssertOutputStrings(string.Format(CultureInfo.CurrentCulture, Strings.ConnectingToSonarQubePrefixMessageFormat, message));

                // Act - finish
                progressEvents.SimulateFinished(controllerResult);

                // Assert
                testSubject.ConnectCommand.CanExecute().Should().BeTrue("Connection is finished with result: {0}", controllerResult);
                testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeTrue("Connection is finished with result: {0}", controllerResult);
            }
        }

        #endregion Tests

        private ConnectionController CreateTestSubject() =>
            new ConnectionController(serviceProvider, host);
    }
}
