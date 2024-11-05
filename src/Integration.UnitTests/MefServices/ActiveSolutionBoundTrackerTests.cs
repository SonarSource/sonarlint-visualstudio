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

using System.Linq.Expressions;
using System.Net.Http;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
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
        private readonly BoundServerProject boundSonarQubeProject = new("solution", "key", new ServerConnection.SonarQube(new Uri("http://localhost:9000")));
        private readonly Expression<Func<ISonarQubeService, Task>> connectMethod = x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>());
        private readonly Expression<Action<ISonarQubeService>> disconnectMethod = x => x.Disconnect();

        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableActiveSolutionTracker activeSolutionTracker;
        private SolutionMock solutionMock;
        private Mock<IVsMonitorSelection> vsMonitorMock;
        private Mock<ILogger> loggerMock;
        private Mock<ISonarQubeService> sonarQubeServiceMock;
        
        private uint boundSolutionUiContextCookie = 999;
        private bool isMockServiceConnected;

        [TestInitialize]
        public void TestInitialize()
        {
            configProvider = new ConfigurableConfigurationProvider();
            activeSolutionTracker = new ConfigurableActiveSolutionTracker();
            solutionMock = new SolutionMock();
            loggerMock = new Mock<ILogger>();
            vsMonitorMock = new Mock<IVsMonitorSelection>();
            vsMonitorMock
                .Setup(x => x.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out boundSolutionUiContextCookie))
                .Returns(VSConstants.S_OK);
            
            serviceProvider = new ConfigurableServiceProvider(false);
            serviceProvider.RegisterService(typeof(SVsSolution), solutionMock);
            serviceProvider.RegisterService(typeof(SVsShellMonitorSelection), vsMonitorMock.Object, replaceExisting: true);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Unbound()
        {
            using var testSubject = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object);
            
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Unbound solution should report false activation");
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Bound()
        {
            ConfigureSolutionBinding(boundSonarQubeProject);

            using var testSubject = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object);
            
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Bound solution should report true activation");
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_When_ConnectAsync_Throws_Write_To_Output()
        {
            sonarQubeServiceMock = new Mock<ISonarQubeService>();
            // We want to directly jump to Connect
            sonarQubeServiceMock.SetupGet(x => x.IsConnected).Returns(false);
            ConfigureSolutionBinding(boundSonarQubeProject);

            // ConnectAsync should throw
            sonarQubeServiceMock
                .SetupSequence(x => x.ConnectAsync(It.IsAny<ConnectionInformation>(), It.IsAny<CancellationToken>()))
                .Throws<Exception>()
                .Throws<TaskCanceledException>()
                .Throws(new HttpRequestException("http request", new Exception("something happened")));
            
            // Arrange
            using var activeSolutionBoundTracker = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, sonarQubeService: sonarQubeServiceMock.Object);
            
            // Act
            // Throwing errors will put the connection and binding out of sync, which
            // cause a Debug.Assert in the product code that we need to suppress
            using (new AssertIgnoreScope())
            {
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
            }

            // Assert
            loggerMock
                .Verify(x => x.WriteLine(It.Is<string>(s => s.StartsWith("SonarQube (Cloud, Server) request failed:")), It.IsAny<object[]>()), Times.Exactly(2));
            loggerMock
                .Verify(x => x.WriteLine(It.Is<string>(s => s == CoreStrings.SonarQubeRequestTimeoutOrCancelled)), Times.Once);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_BoundProject_PassedToConfigScopeUpdater()
        {
            var configScopeUpdaterMock = new Mock<IConfigScopeUpdater>();
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(boundSonarQubeProject);
            using var testSubject = CreateTestSubject(
                activeSolutionTracker, 
                configProvider,
                loggerMock.Object, 
                configScopeUpdater: configScopeUpdaterMock.Object, 
                sonarQubeService: sonarQubeServiceMock.Object);
            
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
                
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.Is<BoundServerProject>(s =>
                s.ServerProjectKey == boundSonarQubeProject.ServerProjectKey
                && s.ServerConnection.ServerUri == boundSonarQubeProject.ServerConnection.ServerUri)));
            configScopeUpdaterMock.VerifyNoOtherCalls();
        }
        
        [TestMethod]
        public void ActiveSolutionBoundTracker_UnBoundProject_NullPassedToConfigScopeUpdater()
        {
            var configScopeUpdaterMock = new Mock<IConfigScopeUpdater>();
            ConfigureService(isConnected: false);
            using var testSubject = CreateTestSubject(
                activeSolutionTracker, 
                configProvider,
                loggerMock.Object, 
                configScopeUpdater: configScopeUpdaterMock.Object,
                sonarQubeService: sonarQubeServiceMock.Object);
            
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);
                
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(null));
            configScopeUpdaterMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Changes()
        {
            var configScopeUpdaterMock = new Mock<IConfigScopeUpdater>();

            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(boundSonarQubeProject);

            var testSubject = CreateTestSubject(
                activeSolutionTracker,
                configProvider,
                loggerMock.Object,
                configScopeUpdater: configScopeUpdaterMock.Object,
                sonarQubeService: sonarQubeServiceMock.Object);
            var eventCounter = new EventCounter(testSubject);

            // Sanity
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Initially bound");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(0, "no events raised during construction");
            eventCounter.SolutionBindingChangedCount.Should().Be(0, "no events raised during construction");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "no events raised during construction");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "no events raised during construction");
            VerifyAndResetBoundSolutionUiContextMock(isActive: true);

            // Case 1: Clear bound project
            ConfigureSolutionBinding(null);

            // Act
            testSubject.HandleBindingChange(true);

            // Assert
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Unbound solution should report false activation");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(1, "Unbind should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(1, "Unbind should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Unbind should not trigger change");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Unbind should not trigger change");
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundServerProject>()), Times.Exactly(1));
            VerifyAndResetBoundSolutionUiContextMock(isActive: false);

            VerifyServiceDisconnect(Times.Never());
            VerifyServiceConnect(Times.Never());

            // Case 2: Set bound project
            ConfigureSolutionBinding(boundSonarQubeProject);
            // Act
            testSubject.HandleBindingChange(false);

            // Assert
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Bound solution should report true activation");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(2, "Bind should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(2, "Bind should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundServerProject>()), Times.Exactly(2));
            VerifyAndResetBoundSolutionUiContextMock(isActive: true);

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
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundServerProject>()), Times.Exactly(3));
            VerifyAndResetBoundSolutionUiContextMock(isActive: false);

            // Closing an unbound solution should not call disconnect/connect
            VerifyServiceDisconnect(Times.Never());
            VerifyServiceConnect(Times.Never());

            // Case 4: Load a bound solution
            ConfigureSolutionBinding(boundSonarQubeProject);
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

            // Assert
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Bound respond to solution change event and report bound");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(4, "Solution change should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(4, "Solution change should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundServerProject>()), Times.Exactly(4));
            VerifyAndResetBoundSolutionUiContextMock(isActive: true);

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
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundServerProject>()), Times.Exactly(5));
            VerifyAndResetBoundSolutionUiContextMock(isActive: false);

            // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
            VerifyServiceDisconnect(Times.Once());
            VerifyServiceConnect(Times.Once());

            // Case 6: Dispose and change
            // Act
            testSubject.Dispose();
            ConfigureSolutionBinding(boundSonarQubeProject);
            testSubject.HandleBindingChange(true);

            // Assert
            eventCounter.PreSolutionBindingChangedCount.Should().Be(5, "Once disposed should stop raising the event");
            eventCounter.SolutionBindingChangedCount.Should().Be(5, "Once disposed should stop raising the event");
            configScopeUpdaterMock.Verify(x => x.UpdateConfigScopeForCurrentSolution(It.IsAny<BoundServerProject>()), Times.Exactly(5));
            // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
            VerifyServiceDisconnect(Times.Once());
            VerifyServiceConnect(Times.Once());
        }

        [TestMethod]
        public void UpdateConnection_WasDisconnected_NewSolutionIsUnbound_NoConnectOrDisconnectCalls()
        {
            // Arrange
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(null);
            
            using (CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, sonarQubeService: sonarQubeServiceMock.Object))
            {
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
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(new BoundServerProject("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://foo"))));
            
            using (CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, sonarQubeService: sonarQubeServiceMock.Object))
            {
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
            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(null);
            
            using (CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, sonarQubeService: sonarQubeServiceMock.Object))
            {
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
            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(new BoundServerProject("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://foo"))));
            
            using (CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, sonarQubeService: sonarQubeServiceMock.Object))
            {
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true);

                // Assert
                VerifyServiceConnect(Times.Once());
                VerifyServiceDisconnect(Times.Once());
                isMockServiceConnected.Should().Be(true);
            }
        }

        [TestMethod]
        public void UpdateConnection_Disconnect_ServiceDisconnectedIsCalled()
        {
            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(null);
            
            // Arrange
            using (CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, sonarQubeService: sonarQubeServiceMock.Object))
            {
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false);

                // Assert
                VerifyServiceConnect(Times.Never());
                VerifyServiceDisconnect(Times.Once());
            }
        }
        
        [TestMethod]
        public void HandleBindingChange_WhenClearBoundProject_NotRaised()
        {
            // Arrange
            using var testSubject = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object);
            var eventCounter = new EventCounter(testSubject);

            // Act
            testSubject.HandleBindingChange(true);

            // Assert
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0);
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0);
            eventCounter.PreSolutionBindingChangedCount.Should().Be(0);
            eventCounter.SolutionBindingChangedCount.Should().Be(0);
        }
        
        [TestMethod]
        public void HandleBindingChange_WhenSetBoundProject_EventsRaisedInExpectedOrder()
        {
            // Arrange
            using var testSubject = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object);
            var eventCounter = new EventCounter(testSubject);

            // Act
            testSubject.HandleBindingChange(false);
                
            // Assert
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(1);
            eventCounter.SolutionBindingUpdatedCount.Should().Be(1);
            eventCounter.PreSolutionBindingChangedCount.Should().Be(0);
            eventCounter.SolutionBindingChangedCount.Should().Be(0);

            eventCounter.RaisedEventNames.Should().HaveCount(2);
            eventCounter.RaisedEventNames[0].Should().Be(nameof(testSubject.PreSolutionBindingUpdated));
            eventCounter.RaisedEventNames[1].Should().Be(nameof(testSubject.SolutionBindingUpdated));
        }

        [TestMethod]
        public void GitRepoUpdated_SolutionBindingUpdatedEventsRaised()
        {
            var gitEventsMonitor = new Mock<IBoundSolutionGitMonitor>();

            // Arrange
            using var testSubject = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, gitEventsMonitor.Object);
            var eventCounter = new EventCounter(testSubject);

            // Act
            ConfigureSolutionBinding(new BoundServerProject("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://foo"))));
            gitEventsMonitor.Raise(x => x.HeadChanged += null, null, null);

            // Assert
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(1);
            eventCounter.SolutionBindingUpdatedCount.Should().Be(1);
            eventCounter.PreSolutionBindingChangedCount.Should().Be(0);
            eventCounter.SolutionBindingChangedCount.Should().Be(0);

            eventCounter.RaisedEventNames.Should().HaveCount(2);
            eventCounter.RaisedEventNames[0].Should().Be(nameof(testSubject.PreSolutionBindingUpdated));
            eventCounter.RaisedEventNames[1].Should().Be(nameof(testSubject.SolutionBindingUpdated));
        }

        [TestMethod]
        public void GitRepoUpdated_UnBoundProject_SolutionBingingUpdatedNotInvoked()
        {
            // Arrange
            var gitEventsMonitor = new Mock<IBoundSolutionGitMonitor>();
            using var testSubject = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, gitEventsMonitor.Object);
            var eventCounter = new EventCounter(testSubject);

            // Act
            ConfigureSolutionBinding(null);
            gitEventsMonitor.Raise(x => x.HeadChanged += null, null, null);

            // Assert
            eventCounter.RaisedEventNames.Should().HaveCount(0);
        }

        [TestMethod]
        public void ActiveSolutionChanged_GitEventsMonitorRefreshInvoked()
        {
            var gitEventsMonitor = new Mock<IBoundSolutionGitMonitor>();
            ConfigureService(true);
            ConfigureSolutionBinding(new BoundServerProject("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://foo"))));

            // Arrange
            using var testSubject = CreateTestSubject(activeSolutionTracker, configProvider, loggerMock.Object, gitEventsMonitor.Object, sonarQubeService: sonarQubeServiceMock.Object);

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged(true);

            // Assert
            gitEventsMonitor.Verify(x => x.Refresh(), Times.Once);
        }

        private ActiveSolutionBoundTracker CreateTestSubject(
            IActiveSolutionTracker solutionTracker,
            IConfigurationProvider configurationProvider,
            ILogger logger = null,
            IBoundSolutionGitMonitor gitEvents = null,
            IConfigScopeUpdater configScopeUpdater = null,
            ISonarQubeService sonarQubeService = null)
        {
            configScopeUpdater ??= Mock.Of<IConfigScopeUpdater>();
            logger ??= new TestLogger(logToConsole: true);
            gitEvents ??= Mock.Of<IBoundSolutionGitMonitor>();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();
            return new ActiveSolutionBoundTracker(serviceProvider, solutionTracker, configScopeUpdater, logger, gitEvents, configurationProvider, sonarQubeService);
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
        }

        private void ConfigureSolutionBinding(BoundServerProject boundProject)
        {
            configProvider.ProjectToReturn = boundProject;
            configProvider.ModeToReturn = boundProject == null ? SonarLintMode.Standalone : SonarLintMode.Connected;
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

        private void VerifyAndResetBoundSolutionUiContextMock(bool isActive)
        {
            var isActiveInt = isActive ? 1 : 0;
            vsMonitorMock.Verify(x => x.SetCmdUIContext(boundSolutionUiContextCookie, isActiveInt), Times.Once);
            vsMonitorMock.Verify(x => x.SetCmdUIContext(It.IsAny<uint>(), It.IsAny<int>()), Times.Once);
            vsMonitorMock.Invocations.Clear();
        }

        private class EventCounter
        {
            private readonly List<string> raisedEventNames = [];

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
