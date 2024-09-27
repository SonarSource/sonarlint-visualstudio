/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Globalization;
using Microsoft.VisualStudio.ComponentModelHost;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Binding;
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
        private Mock<ISolutionInfoProvider> solutionInfoProvider;
        private TestLogger logger;
        private Mock<ISharedBindingConfigProvider> sharedBindingConfigProvider;
        private Mock<ICredentialStoreService> credentialsStore;

        [TestInitialize]
        public void TestInit()
        {
            this.serviceProvider = new ConfigurableServiceProvider();

            logger = new TestLogger();
            serviceProvider.RegisterService(typeof(ILogger), logger);

            this.sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.connectionProvider = new ConfigurableConnectionInformationProvider();
            this.settings = new ConfigurableSonarLintSettings();
            this.solutionInfoProvider = new Mock<ISolutionInfoProvider>();
            this.host = new ConfigurableHost()
            {
                SonarQubeService = this.sonarQubeServiceMock.Object,
                Logger = logger
            };
            this.sharedBindingConfigProvider = new Mock<ISharedBindingConfigProvider>();
            this.credentialsStore = new Mock<ICredentialStoreService>();

            IComponentModel componentModel = ConfigurableComponentModel.CreateWithExports(
                new []
                {
                    MefTestHelpers.CreateExport<ISolutionInfoProvider>(solutionInfoProvider.Object),
                    MefTestHelpers.CreateExport<ISonarLintSettings>(settings)
                });
            this.serviceProvider.RegisterService(typeof(SComponentModel), componentModel);
        }

        #region Tests

        [TestMethod]
        public void ConnectionController_Ctor_ArgumentChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionController(null, Mock.Of<IHost>(), Mock.Of<IAutoBindTrigger>(), sharedBindingConfigProvider.Object, credentialsStore.Object));
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionController(Mock.Of<IServiceProvider>(), null, Mock.Of<IAutoBindTrigger>(), sharedBindingConfigProvider.Object, credentialsStore.Object));
            Exceptions.Expect<ArgumentNullException>(() => new ConnectionController(Mock.Of<IServiceProvider>(), Mock.Of<IHost>(), null, sharedBindingConfigProvider.Object, credentialsStore.Object));
        }

        [TestMethod]
        public void ConnectionController_DefaultState()
        {
            // Arrange
            var testSubject = CreateTestSubject();

            // Assert
            testSubject.RefreshCommand.Should().NotBeNull("Refresh command should not be null");
            testSubject.WorkflowExecutor.Should().NotBeNull("Need to be able to execute the workflow");
            testSubject.IsConnectionInProgress.Should().BeFalse("Connection is not in progress");
        }

        private static Mock<IConnectionWorkflowExecutor> CreateWorkflow()
        {
            var workflow = new Mock<IConnectionWorkflowExecutor>();
            return workflow;
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
            ConnectionController testSubject = new ConnectionController(serviceProvider, host, null, connectionProvider,
                connectionWorkflowMock.Object, sharedBindingConfigProvider.Object, credentialsStore.Object);
            this.connectionProvider.ConnectionInformationToReturn = new ConnectionInformation(new Uri("http://notExpected"));
            var connection = new ConnectionInformation(new Uri("http://Expected"));
            // Sanity
            testSubject.RefreshCommand.CanExecute(connection).Should().BeTrue("Should be possible to execute");

            // Sanity
            testSubject.LastAttemptedConnection.Should().BeNull("No previous attempts to connect");

            // Act
            testSubject.RefreshCommand.Execute(connection);

            // Assert
            connectionWorkflowMock.Verify(x => x.EstablishConnection(It.IsAny<ConnectionInformation>(), null), Times.Once);
            testSubject.LastAttemptedConnection.ServerUri.Should().Be(connection.ServerUri, "Unexpected last attempted connection");
            testSubject.LastAttemptedConnection.Should().NotBe(connection, "LastAttemptedConnection should be a clone");
        }

        [TestMethod]
        public void ConnectionController_SetConnectionInProgress()
        {
            // Arrange
            var connectionWorkflowMock = CreateWorkflow();
            ConnectionController testSubject = new ConnectionController(serviceProvider, host, null, connectionProvider,
                connectionWorkflowMock.Object, sharedBindingConfigProvider.Object, credentialsStore.Object);
            this.solutionInfoProvider.Setup(x => x.IsSolutionFullyOpened()).Returns(true);
            this.connectionProvider.ConnectionInformationToReturn = null;
            var progressEvents = new ConfigurableProgressEvents();
            var connectionInfo = new ConnectionInformation(new Uri("http://refreshConnection"));

            // Sanity
            testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeTrue();

            foreach (var controllerResult in (ProgressControllerResult[])Enum.GetValues(typeof(ProgressControllerResult)))
            {
                logger.Reset();

                // Act - disable
                testSubject.SetConnectionInProgress(progressEvents);

                // Assert
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
                testSubject.RefreshCommand.CanExecute(connectionInfo).Should().BeTrue("Connection is finished with result: {0}", controllerResult);
            }
        }

        #endregion Tests
        
        private ConnectionController CreateTestSubject() =>
            new ConnectionController(serviceProvider, host, Mock.Of<IAutoBindTrigger>(), sharedBindingConfigProvider.Object, credentialsStore.Object);
    }
}
