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

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client;
using SonarQube.Client.Models;
using IVsMonitorSelection = Microsoft.VisualStudio.Shell.Interop.IVsMonitorSelection;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ActiveSolutionBoundTrackerTests
    {
        private uint boundSolutionUIContextCookie = 999;

        private readonly Expression<Func<ISonarQubeService, Task>> connectMethod = x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>());
        private readonly Expression<Action<ISonarQubeService>> disconnectMethod = x => x.Disconnect();

        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ConfigurableActiveSolutionTracker activeSolutionTracker;
        private ConfigurableHost host;
        private Mock<IVsMonitorSelection> vsMonitorMock;
        private Mock<ILogger> loggerMock;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        private ConfigurableConfigurationProvider configProvider;
        private bool isMockServiceConnected;

        [TestInitialize]
        public void TestInitialize()
        {
            serviceProvider = new ConfigurableServiceProvider(false);
            
            host = new ConfigurableHost();
            var mefHost = MefTestHelpers.CreateExport<IHost>(host);

            activeSolutionTracker = new ConfigurableActiveSolutionTracker();
            var mefAST = MefTestHelpers.CreateExport<IActiveSolutionTracker>(activeSolutionTracker);

            var mefModel = ConfigurableComponentModel.CreateWithExports(mefHost, mefAST);
            serviceProvider.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            solutionMock = new SolutionMock();
            serviceProvider.RegisterService(typeof(SVsSolution), solutionMock);

            configProvider = new ConfigurableConfigurationProvider();
            
            loggerMock = new Mock<ILogger>();


            vsMonitorMock = new Mock<IVsMonitorSelection>();
            vsMonitorMock
                .Setup(x => x.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out boundSolutionUIContextCookie))
                .Returns(VSConstants.S_OK);

            serviceProvider.RegisterService(typeof(SVsShellMonitorSelection), vsMonitorMock.Object, replaceExisting: true);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Unbound()
        {
            // Arrange
            host.VisualStateManager.ClearBoundProject();

            // Act
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                // Assert
                testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Unbound solution should report false activation");
            }
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Bound()
        {
            // Arrange
            this.ConfigureSolutionBinding(new BoundSonarQubeProject());

            // Act
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                // Assert
                testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.LegacyConnected, "Bound solution should report true activation");
            }
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_When_ConnectAsync_Throws_Write_To_Output()
        {
            // Arrange
            var sonarQubeServiceMock = new Mock<ISonarQubeService>();
            this.host.SonarQubeService = sonarQubeServiceMock.Object;

            using (var activeSolutionBoundTracker = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, this.loggerMock.Object))
            {
                // We want to directly jump to Connect
                sonarQubeServiceMock.SetupGet(x => x.IsConnected).Returns(false);
                ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://test"), "projectkey", "projectName"));

                // ConnectAsync should throw
                sonarQubeServiceMock
                    .SetupSequence(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>()))
                    .Throws<Exception>()
                    .Throws<TaskCanceledException>()
                    .Throws(new HttpRequestException("http request", new Exception("something happened")));

                // Act
                // Throwing errors will put the connection and binding out of sync, which
                // cause a Debug.Assert in the product code that we need to suppress
                using (new AssertIgnoreScope())
                {
                    this.activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
                    this.activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);
                    this.activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
                }

                // Assert
                this.loggerMock
                    .Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("SonarQube request failed:")), It.IsAny<object[]>()), Times.Exactly(2));
                this.loggerMock
                    .Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("SonarQube request timed out or was canceled"))), Times.Once);
            }
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_BoundProject_PassedToConfigScopeUpdater()
        {
            var boundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "key", "projectName");

            var configScopeUpdaterMock = new Mock<IConfigScopeUpdater>();

            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(boundProject);

            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider,
                       loggerMock.Object, configScopeUpdater: configScopeUpdaterMock.Object))
            {
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
                
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(boundProject));
                configScopeUpdaterMock.VerifyNoOtherCalls();
            }
        }
        
        [TestMethod]
        public void ActiveSolutionBoundTracker_UnBoundProject_NullPassedToConfigScopeUpdater()
        {
            var configScopeUpdaterMock = new Mock<IConfigScopeUpdater>();

            ConfigureService(isConnected: false);

            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider,
                       loggerMock.Object, configScopeUpdater: configScopeUpdaterMock.Object))
            {
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
                
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(null));
                configScopeUpdaterMock.VerifyNoOtherCalls();
            }
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Changes()
        {
            var boundProject = new BoundSonarQubeProject(new Uri("http://localhost:9000"), "key", "projectName");

            var configScopeUpdaterMock = new Mock<IConfigScopeUpdater>();

            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(boundProject);

            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object, configScopeUpdater: configScopeUpdaterMock.Object))
            {
                var eventCounter = new EventCounter(testSubject);

                // Sanity
                testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.LegacyConnected, "Initially bound");
                eventCounter.PreSolutionBindingChangedCount.Should().Be(0, "no events raised during construction");
                eventCounter.SolutionBindingChangedCount.Should().Be(0, "no events raised during construction");
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "no events raised during construction");
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "no events raised during construction");
                VerifyAndResetBoundSolutionUIContextMock(isActive: true);

                // Case 1: Clear bound project
                ConfigureSolutionBinding(null);

                // Act
                host.VisualStateManager.ClearBoundProject();

                // Assert
                testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Unbound solution should report false activation");
                eventCounter.PreSolutionBindingChangedCount.Should().Be(1, "Unbind should trigger reanalysis");
                eventCounter.SolutionBindingChangedCount.Should().Be(1, "Unbind should trigger reanalysis");
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Unbind should not trigger change");
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Unbind should not trigger change");
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundSonarQubeProject>()), Times.Exactly(1));
                VerifyAndResetBoundSolutionUIContextMock(isActive: false);

                VerifyServiceDisconnect(Times.Never());
                VerifyServiceConnect(Times.Never());

                // Case 2: Set bound project
                ConfigureSolutionBinding(boundProject);
                // Act
                host.VisualStateManager.SetBoundProject(new Uri("http://localhost"), null, "project123");

                // Assert
                testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.LegacyConnected, "Bound solution should report true activation");
                eventCounter.PreSolutionBindingChangedCount.Should().Be(2, "Bind should trigger reanalysis");
                eventCounter.SolutionBindingChangedCount.Should().Be(2, "Bind should trigger reanalysis");
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundSonarQubeProject>()), Times.Exactly(2));
                VerifyAndResetBoundSolutionUIContextMock(isActive: true);

                // Notifications from the Team Explorer should not trigger connect/disconnect
                VerifyServiceDisconnect(Times.Never());
                VerifyServiceConnect(Times.Never());

                // Case 3: Bound solution unloaded -> disconnect
                ConfigureSolutionBinding(null);
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);

                // Assert
                testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Should respond to solution change event and report unbound");
                eventCounter.PreSolutionBindingChangedCount.Should().Be(3, "Solution change should trigger reanalysis");
                eventCounter.SolutionBindingChangedCount.Should().Be(3, "Solution change should trigger reanalysis");
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundSonarQubeProject>()), Times.Exactly(3));
                VerifyAndResetBoundSolutionUIContextMock(isActive: false);

                // Closing an unbound solution should not call disconnect/connect
                VerifyServiceDisconnect(Times.Never());
                VerifyServiceConnect(Times.Never());

                // Case 4: Load a bound solution
                ConfigureSolutionBinding(boundProject);
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

                // Assert
                testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.LegacyConnected, "Bound respond to solution change event and report bound");
                eventCounter.PreSolutionBindingChangedCount.Should().Be(4, "Solution change should trigger reanalysis");
                eventCounter.SolutionBindingChangedCount.Should().Be(4, "Solution change should trigger reanalysis");
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundSonarQubeProject>()), Times.Exactly(4));
                VerifyAndResetBoundSolutionUIContextMock(isActive: true);

                // Loading a bound solution should call connect
                VerifyServiceDisconnect(Times.Never());
                VerifyServiceConnect(Times.Once());

                // Case 5: Close a bound solution
                ConfigureSolutionBinding(null);
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);

                // Assert
                eventCounter.PreSolutionBindingChangedCount.Should().Be(5, "Solution change should trigger reanalysis");
                eventCounter.SolutionBindingChangedCount.Should().Be(5, "Solution change should trigger reanalysis");
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundSonarQubeProject>()), Times.Exactly(5));
                VerifyAndResetBoundSolutionUIContextMock(isActive: false);

                // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
                VerifyServiceDisconnect(Times.Once());
                VerifyServiceConnect(Times.Once());

                // Case 6: Dispose and change
                // Act
                testSubject.Dispose();
                ConfigureSolutionBinding(boundProject);
                host.VisualStateManager.ClearBoundProject();

                // Assert
                eventCounter.PreSolutionBindingChangedCount.Should().Be(5, "Once disposed should stop raising the event");
                eventCounter.SolutionBindingChangedCount.Should().Be(5, "Once disposed should stop raising the event");
                configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundSonarQubeProject>()), Times.Exactly(5));
                // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
                VerifyServiceDisconnect(Times.Once());
                VerifyServiceConnect(Times.Once());
            }
        }

        [TestMethod]
        public void OnBindingStateChanged_NewConfiguration_EventsRaisedInCorrectOrder()
        {
            // Arrange
            var initialProject = new BoundSonarQubeProject(
                new Uri("http://localhost:9000"),
                "projectKey", "projectName",
                organization: new SonarQubeOrganization("myOrgKey", "myOrgName"));

            // Set the current configuration used by the tracker
            ConfigureSolutionBinding(initialProject);
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                var eventCounter = new EventCounter(testSubject);

                // Now configure the provider to return a different configuration
                var newProject = new BoundSonarQubeProject(
                    new Uri("http://localhost:9000"),
                    "projectKey", "projectName",
                    organization: new SonarQubeOrganization("myOrgKey_DIFFERENT", "myOrgName"));
                ConfigureSolutionBinding(newProject);

                // Act - simulate the binding state changing in the Team explorer section.
                // The project configuration hasn't changed (it doesn't matter what properties
                // we pass here; they aren't used when raising the event.)
                host.VisualStateManager.SetBoundProject(new Uri("http://junk"), "any", "any");

                // Assert
                // Different config so event should be raised
                eventCounter.PreSolutionBindingChangedCount.Should().Be(1);
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0);
                eventCounter.PreSolutionBindingChangedCount.Should().Be(1);
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0);

                eventCounter.RaisedEventNames.Should().HaveCount(2);
                eventCounter.RaisedEventNames[0].Should().Be("PreSolutionBindingChanged");
                eventCounter.RaisedEventNames[1].Should().Be("SolutionBindingChanged");                    
            }
        }

        [TestMethod]
        public void UpdateConnection_WasDisconnected_NewSolutionIsUnbound_NoConnectOrDisconnectCalls()
        {
            // Arrange
            using (CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                ConfigureService(isConnected: false);
                ConfigureSolutionBinding(null);

                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

                // Assert
                VerifyServiceConnect(Times.Never());
                VerifyServiceDisconnect(Times.Never());
                isMockServiceConnected.Should().Be(false);
            }
        }

        [TestMethod]
        public void UpdateConnection_WasDisconnected_NewSolutionIsBound_ConnectCalled()
        {
            // Arrange
            using (CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                ConfigureService(isConnected: false);
                ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://foo"), "projectKey", "projectName"));

                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

                // Assert
                VerifyServiceConnect(Times.Once());
                VerifyServiceDisconnect(Times.Never());
                isMockServiceConnected.Should().Be(true);
            }
        }

        [TestMethod]
        public void UpdateConnection_WasConnected_NewSolutionIsUnbound_DisconnectedCalled()
        {
            // Arrange
            using (CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                ConfigureService(isConnected: true);
                ConfigureSolutionBinding(null);

                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

                // Assert
                VerifyServiceConnect(Times.Never());
                VerifyServiceDisconnect(Times.Once());
                isMockServiceConnected.Should().Be(false);
            }
        }

        [TestMethod]
        public void UpdateConnection_WasConnected_NewSolutionBound_ConnectedCalled()
        {
            // Note: we don't expect this to happen in practice - we should only
            // ever go from connected->disconnected, or disconnected->connected,
            // never connected->connected. However, we should handle it gracefully
            // just in case.

            // Arrange
            using (CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                ConfigureService(isConnected: true);
                ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://foo"), "projectKey", "projectName"));

                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

                // Assert
                VerifyServiceConnect(Times.Once());
                VerifyServiceDisconnect(Times.Once());
                isMockServiceConnected.Should().Be(true);
            }
        }

        [TestMethod]
        public void UpdateConnection_Disconnect_WpfCommandIsAvailableButNotExecutable_ServiceDisconnectedIsCalled()
        {
            // If the WPF command is available then it should be used to disconnect,
            // rather than calling service.Disconnect() directly.
            // Here, the command exists but is not executable.

            // Arrange
            using (CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {

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
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);

                // Assert
                VerifyServiceConnect(Times.Never());
                VerifyServiceDisconnect(Times.Once());
                commandCanExecuteCallCount.Should().Be(1);
                commandCallCount.Should().Be(0);
            }
        }

        [TestMethod]
        public void UpdateConnection_Disconnect_WpfCommandIsAvailableAndExecutable_ServiceDisconnectedIsNotCalled()
        {
            // Wpf command is available and can be executed -> should be called instead of service.Disconnect()

            // Arrange
            using (CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
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
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);

                // Assert
                VerifyServiceConnect(Times.Never());
                VerifyServiceDisconnect(Times.Never());
                commandCanExecuteCallCount.Should().Be(1);
                commandCallCount.Should().Be(1);
            }
        }

        [TestMethod]
        public void SolutionBindingUpdated_WhenClearBoundProject_NotRaised()
        {
            // Arrange
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                var eventCounter = new EventCounter(testSubject);

                // Act
                host.VisualStateManager.ClearBoundProject();

                // Assert
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0);
                eventCounter.SolutionBindingUpdatedCount.Should().Be(0);
                eventCounter.PreSolutionBindingChangedCount.Should().Be(0);
                eventCounter.SolutionBindingChangedCount.Should().Be(0);
            }
        }

        [TestMethod]
        public void SolutionBindingUpdated_WhenSetBoundProject_EventsRaisedInExpectedOrder()
        {
            // Arrange
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object))
            {
                var eventCounter = new EventCounter(testSubject);

                // Act
                host.VisualStateManager.SetBoundProject(new Uri("http://localhost"), null, "project123");

                // Assert
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(1);
                eventCounter.SolutionBindingUpdatedCount.Should().Be(1);
                eventCounter.PreSolutionBindingChangedCount.Should().Be(0);
                eventCounter.SolutionBindingChangedCount.Should().Be(0);

                eventCounter.RaisedEventNames.Should().HaveCount(2);
                eventCounter.RaisedEventNames[0].Should().Be("PreSolutionBindingUpdated");
                eventCounter.RaisedEventNames[1].Should().Be("SolutionBindingUpdated");
            }
        }

        [TestMethod]
        public void GitRepoUpdated_SolutionBindingUpdatedEventsRaised()
        {
            var gitEventsMonitor = new Mock<IBoundSolutionGitMonitor>();

            // Arrange
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object, gitEventsMonitor.Object))
            {
                var eventCounter = new EventCounter(testSubject);

                // Act
                ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://foo"), "projectKey", "projectName"));
                gitEventsMonitor.Raise(x => x.HeadChanged += null, null, null);

                // Assert
                eventCounter.PreSolutionBindingUpdatedCount.Should().Be(1);
                eventCounter.SolutionBindingUpdatedCount.Should().Be(1);
                eventCounter.PreSolutionBindingChangedCount.Should().Be(0);
                eventCounter.SolutionBindingChangedCount.Should().Be(0);

                eventCounter.RaisedEventNames.Should().HaveCount(2);
                eventCounter.RaisedEventNames[0].Should().Be("PreSolutionBindingUpdated");
                eventCounter.RaisedEventNames[1].Should().Be("SolutionBindingUpdated");
            }
        }

        [TestMethod]
        public void GitRepoUpdated_UnBoundProject_SolutionBingingUpdatedNotInvoked()
        {
            var gitEventsMonitor = new Mock<IBoundSolutionGitMonitor>();

            // Arrange
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object, gitEventsMonitor.Object))
            {
                var eventCounter = new EventCounter(testSubject);

                // Act
                ConfigureSolutionBinding(null);
                gitEventsMonitor.Raise(x => x.HeadChanged += null, null, null);

                // Assert
                eventCounter.RaisedEventNames.Should().HaveCount(0);
            }
        }

        [TestMethod]
        public void ActiveSolutionChanged_GitEventsMonitorRefreshInvoked()
        {
            var gitEventsMonitor = new Mock<IBoundSolutionGitMonitor>();

            // Arrange
            using (var testSubject = CreateTestSubject(this.host, this.activeSolutionTracker, this.configProvider, loggerMock.Object, gitEventsMonitor.Object))
            {
                var eventCounter = new EventCounter(testSubject);

                // Act
                ConfigureService(true);
                ConfigureSolutionBinding(new BoundSonarQubeProject(new Uri("http://foo"), "projectKey", "projectName"));
                this.activeSolutionTracker.SimulateActiveSolutionChanged(true);

                // Assert
                gitEventsMonitor.Verify(x => x.Refresh(), Times.Once);
            }
        }

        private ActiveSolutionBoundTracker CreateTestSubject(IHost host,
            IActiveSolutionTracker solutionTracker,
            IConfigurationProvider configurationProvider,
            ILogger logger = null,
            IBoundSolutionGitMonitor gitEvents = null,
            IServiceProvider serviceProvider = null,
            IConfigScopeUpdater configScopeUpdater = null)
        {
            configScopeUpdater ??= Mock.Of<IConfigScopeUpdater>();
            logger ??= new TestLogger(logToConsole: true);
            gitEvents ??= Mock.Of<IBoundSolutionGitMonitor>();
            serviceProvider ??= this.serviceProvider;
            return new ActiveSolutionBoundTracker(serviceProvider, host, solutionTracker, configScopeUpdater, logger, gitEvents, configurationProvider);
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
            configProvider.ProjectToReturn = boundProject;
            configProvider.ModeToReturn = boundProject == null ? SonarLintMode.Standalone : SonarLintMode.LegacyConnected;
            configProvider.FolderPathToReturn = "c:\\test";
        }

        private void VerifyServiceConnect(Times expected)
        {
            sonarQubeServiceMock.Verify(connectMethod, expected);
        }

        private void VerifyServiceDisconnect(Times expected)
        {
            sonarQubeServiceMock.Verify(disconnectMethod, expected);
        }

        private void VerifyAndResetBoundSolutionUIContextMock(bool isActive)
        {
            var isActiveInt = isActive ? 1 : 0;
            vsMonitorMock.Verify(x => x.SetCmdUIContext(boundSolutionUIContextCookie, isActiveInt), Times.Once);
            vsMonitorMock.Verify(x => x.SetCmdUIContext(It.IsAny<uint>(), It.IsAny<int>()), Times.Once);
            vsMonitorMock.Invocations.Clear();
        }

        private class EventCounter
        {
            private readonly List<string> raisedEventNames = new List<string>();

            public int PreSolutionBindingChangedCount { get; private set; }
            public int SolutionBindingChangedCount { get; private set; }
            public int PreSolutionBindingUpdatedCount { get; private set; }
            public int SolutionBindingUpdatedCount { get; private set; }

            public IReadOnlyList<string> RaisedEventNames => raisedEventNames;

            public EventCounter(IActiveSolutionBoundTracker tracker)
            {
                tracker.PreSolutionBindingChanged += (_, _) =>
                {
                    PreSolutionBindingChangedCount++;
                    raisedEventNames.Add(nameof(IActiveSolutionBoundTracker.PreSolutionBindingChanged));
                };

                tracker.SolutionBindingChanged += (_, _) =>
                {
                    SolutionBindingChangedCount++;
                    raisedEventNames.Add(nameof(IActiveSolutionBoundTracker.SolutionBindingChanged));
                };

                tracker.PreSolutionBindingUpdated += (_, _) =>
                {
                    PreSolutionBindingUpdatedCount++;
                    raisedEventNames.Add(nameof(IActiveSolutionBoundTracker.PreSolutionBindingUpdated));
                };

                tracker.SolutionBindingUpdated += (_, _) =>
                {
                    SolutionBindingUpdatedCount++;
                    raisedEventNames.Add(nameof(IActiveSolutionBoundTracker.SolutionBindingUpdated));
                };
            }
        }
    }
}
