/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Persistence;
using SonarQube.Client.Models;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ActiveSolutionBoundTrackerTests
    {
        private readonly Expression<Func<ISonarQubeService, Task>> connectMethod = x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>());
        private readonly Expression<Action<ISonarQubeService>> disconnectMethod = x => x.Disconnect();

        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ConfigurableActiveSolutionTracker activeSolutionTracker;
        private ConfigurableHost host;
        private ConfigurableErrorListInfoBarController errorListController;

        private Mock<ILogger> sonarLintOutputMock;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableConfigurationProvider configProvider;
        private bool isMockServiceConnected;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider(false);
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            var mefExport1 = MefTestHelpers.CreateExport<IHost>(this.host);

            this.activeSolutionTracker = new ConfigurableActiveSolutionTracker();
            var mefExport2 = MefTestHelpers.CreateExport<IActiveSolutionTracker>(this.activeSolutionTracker);

            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExport1, mefExport2);

            this.serviceProvider.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            this.solutionMock = new SolutionMock();
            this.serviceProvider.RegisterService(typeof(SVsSolution), this.solutionMock);

            this.errorListController = new ConfigurableErrorListInfoBarController();
            this.serviceProvider.RegisterService(typeof(IErrorListInfoBarController), this.errorListController);

            this.configProvider = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationProvider), this.configProvider);

            this.sonarLintOutputMock = new Mock<ILogger>();
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_ArgChecks()
        {
            // Arrange
            Exceptions.Expect<ArgumentNullException>(() =>
                new ActiveSolutionBoundTracker(null, new ConfigurableActiveSolutionTracker(), sonarLintOutputMock.Object));
            Exceptions.Expect<ArgumentNullException>(() =>
                new ActiveSolutionBoundTracker(this.host, null, sonarLintOutputMock.Object));
            Exceptions.Expect<ArgumentNullException>(() =>
                new ActiveSolutionBoundTracker(this.host, new ConfigurableActiveSolutionTracker(), null));
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Unbound()
        {
            // Arrange
            host.VisualStateManager.ClearBoundProject();

            // Act
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeFalse("Unbound solution should report false activation");
            this.errorListController.RefreshCalledCount.Should().Be(0);
            this.errorListController.ResetCalledCount.Should().Be(0);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Bound()
        {
            // Arrange
            this.ConfigureSolutionBinding(new BoundSonarQubeProject());

            // Act
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeTrue("Bound solution should report true activation");
            this.errorListController.RefreshCalledCount.Should().Be(0);
            this.errorListController.ResetCalledCount.Should().Be(0);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_When_ConnectAsync_Throws_Write_To_Output()
        {
            // Arrange
            var sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.host.SonarQubeService = sonarQubeServiceMock.Object;

            var activeSolutionBoundTracker = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker,
                this.sonarLintOutputMock.Object);

            // We want to directly jump to Connect
            sonarQubeServiceMock.SetupGet(x => x.IsConnected).Returns(false);
            ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://test"), "projectkey"));

            // ConnectAsync should throw
            sonarQubeServiceMock
                .SetupSequence(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>()))
                .Throws<Exception>()
                .Throws<TaskCanceledException>()
                .Throws(new HttpRequestException("http requst", new Exception("something happened")));

            // Act
            // Throwing errors will put the connection and binding out of sync, which
            // cause a Debug.Assert in the product code that we need to suppress
            using (new AssertIgnoreScope())
            {
                this.activeSolutionTracker.SimulateActiveSolutionChanged();
                this.activeSolutionTracker.SimulateActiveSolutionChanged();
                this.activeSolutionTracker.SimulateActiveSolutionChanged();
            }

            // Assert
            this.sonarLintOutputMock
                .Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("SonarQube request failed"))), Times.Exactly(2));
            this.sonarLintOutputMock
                .Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("SonarQube request timed out or was canceled"))), Times.Once);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Changes()
        {
            var boundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "key");

            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(boundProject);

            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);
            var solutionBindingChangedEventCount = 0;
            testSubject.SolutionBindingChanged += (obj, args) => { solutionBindingChangedEventCount++; };

            // Sanity
            testSubject.IsActiveSolutionBound.Should().BeTrue("Initially bound");
            this.errorListController.RefreshCalledCount.Should().Be(0);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(0, "no events raised during construction");

            // Case 1: Clear bound project
            ConfigureSolutionBinding(null);
            // Act
            host.VisualStateManager.ClearBoundProject();

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeFalse("Unbound solution should report false activation");
            this.errorListController.RefreshCalledCount.Should().Be(0);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(1, "Unbind should trigger reanalysis");

            VerifyServiceDisconnect(Times.Never());
            VerifyServiceConnect(Times.Never());

            // Case 2: Set bound project
            ConfigureSolutionBinding(boundProject);
            // Act
            host.VisualStateManager.SetBoundProject(new ConnectionInformation(new Uri("http://localhost")), new SonarQubeProject("", ""));

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeTrue("Bound solution should report true activation");
            this.errorListController.RefreshCalledCount.Should().Be(0);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(2, "Bind should trigger reanalysis");

            // Notifications from the Team Explorer should not trigger connect/disconnect
            VerifyServiceDisconnect(Times.Never());
            VerifyServiceConnect(Times.Never());

            // Case 3: Bound solution unloaded -> disconnect
            ConfigureSolutionBinding(null);
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeFalse("Should respond to solution change event and report unbound");
            this.errorListController.RefreshCalledCount.Should().Be(1);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(3, "Solution change should trigger reanalysis");

            // Closing an unbound solution should not call disconnect/connect
            VerifyServiceDisconnect(Times.Never());
            VerifyServiceConnect(Times.Never());

            // Case 4: Load a bound solution
            ConfigureSolutionBinding(boundProject);
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            testSubject.IsActiveSolutionBound.Should().BeTrue("Bound respond to solution change event and report bound");
            this.errorListController.RefreshCalledCount.Should().Be(2);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(4, "Solution change should trigger reanalysis");

            // Loading a bound solution should call connect
            VerifyServiceDisconnect(Times.Never());
            VerifyServiceConnect(Times.Once());

            // Case 5: Close a bound solution
            ConfigureSolutionBinding(null);
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(4);
            this.errorListController.ResetCalledCount.Should().Be(0);
            solutionBindingChangedEventCount.Should().Be(5, "Solution change should trigger reanalysis");

            // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
            VerifyServiceDisconnect(Times.Once());
            VerifyServiceConnect(Times.Once());

            // Case 6: Dispose and change
            // Act
            testSubject.Dispose();
            ConfigureSolutionBinding(boundProject);
            host.VisualStateManager.ClearBoundProject();

            // Assert
            solutionBindingChangedEventCount.Should().Be(5, "Once disposed should stop raising the event");
            // TODO: this.errorListController.RefreshCalledCount.Should().Be(3);
            this.errorListController.ResetCalledCount.Should().Be(1);
            // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
            VerifyServiceDisconnect(Times.Once());
            VerifyServiceConnect(Times.Once());
        }

        [TestMethod]
        public void UpdateConnection_WasDisconnected_NewSolutionIsUnbound_NoConnectOrDisconnectCalls()
        {
            // Arrange
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(null);

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            VerifyServiceConnect(Times.Never());
            VerifyServiceDisconnect(Times.Never());
            isMockServiceConnected.Should().Be(false);
        }

        [TestMethod]
        public void UpdateConnection_WasDisconnected_NewSolutionIsBound_ConnectCalled()
        {
            // Arrange
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://foo"), "projectKey"));

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            VerifyServiceConnect(Times.Once());
            VerifyServiceDisconnect(Times.Never());
            isMockServiceConnected.Should().Be(true);
        }

        [TestMethod]
        public void UpdateConnection_WasConnected_NewSolutionIsUnbound_DisconnectedCalled()
        {
            // Arrange
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);
            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(null);

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            VerifyServiceConnect(Times.Never());
            VerifyServiceDisconnect(Times.Once());
            isMockServiceConnected.Should().Be(false);
        }

        [TestMethod]
        public void UpdateConnection_WasConnected_NewSolutionBound_ConnectedCalled()
        {
            // Note: we don't expect this to happen in practice - we should only
            // ever go from connected->disconnected, or disconnected->connected,
            // never connected->connected. However, we should handle it gracefully
            // just in case.
                
            // Arrange
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);
            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://foo"), "projectKey"));

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            VerifyServiceConnect(Times.Once());
            VerifyServiceDisconnect(Times.Once());
            isMockServiceConnected.Should().Be(true);
        }

        [TestMethod]
        public void UpdateConnection_Disconnect_WpfCommandIsAvailableButNotExecutable_ServiceDisconnectedIsCalled()
        {
            // If the WPF command is available then it should be used to disconnect,
            // rather than calling service.Disconnect() directly.
            // Here, the command exists but is not executable.

            // Arrange
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);

            int commandCallCount = 0;
            int commandCanExecuteCallCount = 0;
            var teSection = ConfigurableSectionController.CreateDefault();
            teSection.DisconnectCommand = new Integration.WPF.RelayCommand(
                () => commandCallCount++,
                () => { commandCanExecuteCallCount++; return false; });
            host.SetActiveSection(teSection);

            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(null);

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            VerifyServiceConnect(Times.Never());
            VerifyServiceDisconnect(Times.Once());
            commandCanExecuteCallCount.Should().Be(1);
            commandCallCount.Should().Be(0);
        }

        [TestMethod]
        public void UpdateConnection_Disconnect_WpfCommandIsAvailableAndExecutable_ServiceDisconnectedIsNotCalled()
        {
            // Wpf command is available and can be executed -> should be called instead of service.Disconnect()

            // Arrange
            var testSubject = new ActiveSolutionBoundTracker(this.host, this.activeSolutionTracker, sonarLintOutputMock.Object);

            int commandCallCount = 0;
            int commandCanExecuteCallCount = 0;
            var teSection = ConfigurableSectionController.CreateDefault();
            teSection.DisconnectCommand = new Integration.WPF.RelayCommand(
                () => { commandCallCount++; isMockServiceConnected = false; },
                () => { commandCanExecuteCallCount++; return true; });
            host.SetActiveSection(teSection);

            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(null);

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged();

            // Assert
            VerifyServiceConnect(Times.Never());
            VerifyServiceDisconnect(Times.Never());
            commandCanExecuteCallCount.Should().Be(1);
            commandCallCount.Should().Be(1);
        }

        private void ConfigureService(bool isConnected)
        {
            sonarQubeServiceMock = new Mock<ISonarQubeService>();
            isMockServiceConnected = isConnected;

            sonarQubeServiceMock.SetupGet(x => x.IsConnected).Returns(() => isMockServiceConnected);
            sonarQubeServiceMock.Setup(disconnectMethod)
                .Callback(() => isMockServiceConnected = false)
                .Verifiable();
            sonarQubeServiceMock.Setup(connectMethod)
                .Callback(() => isMockServiceConnected = true )
                .Returns(Task.Delay(0))
                .Verifiable();

            this.host.SonarQubeService = sonarQubeServiceMock.Object;
        }

        private void ConfigureSolutionBinding(BoundSonarQubeProject boundProject)
        {
            this.configProvider.ProjectToReturn = boundProject;
            this.configProvider.ModeToReturn = boundProject == null ? SonarLintMode.Standalone : SonarLintMode.LegacyConnected;
        }

        private void VerifyServiceConnect(Times expected)
        {
            sonarQubeServiceMock.Verify(connectMethod, expected);
        }

        private void VerifyServiceDisconnect(Times expected)
        {
            sonarQubeServiceMock.Verify(disconnectMethod, expected);
        }
    }
}