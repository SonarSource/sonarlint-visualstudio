/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Net.Http;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.ConfigurationScope;
using SonarLint.VisualStudio.Core.Initialization;
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
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableActiveSolutionTracker activeSolutionTracker;
        private IReadOnlyCollection<IRequireInitialization> initializationDependencies;
        private IVsMonitorSelection vsMonitorMock;
        private TestLogger testLogger;
        private ISonarQubeService sonarQubeServiceMock;

        private IConfigScopeUpdater configScopeUpdater;
        private IInitializationProcessorFactory initializationProcessorFactory;
        private MockableInitializationProcessor createdInitializationProcessor;
        private NoOpThreadHandler threadHandling;

        private readonly uint boundSolutionUiContextCookie = 999;
        private bool isMockServiceConnected;

        [TestInitialize]
        public void TestInitialize()
        {
            configProvider = Substitute.ForPartsOf<ConfigurableConfigurationProvider>();
            activeSolutionTracker = Substitute.ForPartsOf<ConfigurableActiveSolutionTracker>();
            initializationDependencies = [activeSolutionTracker];
            testLogger = new TestLogger();
            vsMonitorMock = Substitute.For<IVsMonitorSelection>();
            vsMonitorMock.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out Arg.Any<uint>())
                .Returns(info =>
                {
                    info[1] = boundSolutionUiContextCookie;
                    return VSConstants.S_OK;
                });

            serviceProvider = new ConfigurableServiceProvider(false);
            serviceProvider.RegisterService(typeof(SVsShellMonitorSelection), vsMonitorMock, replaceExisting: true);

            sonarQubeServiceMock = Substitute.For<ISonarQubeService>();

            configScopeUpdater = Substitute.For<IConfigScopeUpdater>();
            threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        }

        [TestMethod]
        public void MefCtor_CheckIsExported() =>
            MefTestHelpers.CheckTypeCanBeImported<ActiveSolutionBoundTracker, IActiveSolutionBoundTracker>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(),
                MefTestHelpers.CreateExport<IActiveSolutionTracker>(),
                MefTestHelpers.CreateExport<IConfigScopeUpdater>(),
                MefTestHelpers.CreateExport<ILogger>(),
                MefTestHelpers.CreateExport<IConfigurationProvider>(),
                MefTestHelpers.CreateExport<ISonarQubeService>(),
                MefTestHelpers.CreateExport<IInitializationProcessorFactory>());

        [TestMethod]
        public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<ActiveSolutionBoundTracker>();

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Unbound()
        {
            using var testSubject = CreateAndInitializeTestSubject();

            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Unbound solution should report false activation");

            Received.InOrder(() =>
            {
                initializationProcessorFactory.Create<ActiveSolutionBoundTracker>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(initializationDependencies)),
                    Arg.Any<Func<IThreadHandling, Task>>());
                createdInitializationProcessor.InitializeAsync();
                threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
                vsMonitorMock.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out Arg.Any<uint>());
                _ = sonarQubeServiceMock.IsConnected;
                activeSolutionTracker.ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
                createdInitializationProcessor.InitializeAsync();
            });
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Initialisation_Bound()
        {
            ConfigureService(false);
            ConfigureSolutionBinding(boundSonarQubeProject);

            using var testSubject = CreateAndInitializeTestSubject();

            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Bound solution should report true activation");
            Received.InOrder(() =>
            {
                initializationProcessorFactory.Create<ActiveSolutionBoundTracker>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.SequenceEqual(initializationDependencies)),
                    Arg.Any<Func<IThreadHandling, Task>>());
                createdInitializationProcessor.InitializeAsync();
                threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
                vsMonitorMock.GetCmdUIContextCookie(ref BoundSolutionUIContext.Guid, out Arg.Any<uint>());
                _ = sonarQubeServiceMock.IsConnected;
                sonarQubeServiceMock.ConnectAsync(Arg.Any<ConnectionInformation>(), Arg.Any<CancellationToken>());
                activeSolutionTracker.ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
                createdInitializationProcessor.InitializeAsync();
            });
        }

        [TestMethod]
        public void DisposeBeforeInitialized_DisposeAndInitializeDoNothingWithEvents()
        {
            var testSubject = CreateUninitializedTestSubject(out var barrier);
            testSubject.Dispose();
            barrier.SetResult(1);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

            activeSolutionTracker.DidNotReceive().ActiveSolutionChanged += Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
            activeSolutionTracker.DidNotReceive().ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        }

        [TestMethod]
        public void Dispose_UnsubscribesFromEvents()
        {
            var testSubject = CreateAndInitializeTestSubject();
            testSubject.Dispose();

            activeSolutionTracker.Received().ActiveSolutionChanged -= Arg.Any<EventHandler<ActiveSolutionChangedEventArgs>>();
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_SolutionChanged_EventsAreNotTriggeredBeforeInitializationIsComplete()
        {
            ConfigureService(false);
            using var testSubject = CreateUninitializedTestSubject(out var barrier);
            var bindingChangedHandler = Substitute.For<EventHandler<ActiveSolutionBindingEventArgs>>();
            testSubject.SolutionBindingChanged += bindingChangedHandler;

            ConfigureSolutionBinding(boundSonarQubeProject);
            activeSolutionTracker.SimulateActiveSolutionChanged(false, null);
            activeSolutionTracker.SimulateActiveSolutionChanged(true, "name");

            // state is unmodified and events are not raised until initialized
            bindingChangedHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
            configScopeUpdater.DidNotReceiveWithAnyArgs().UpdateConfigScopeForCurrentSolution(boundSonarQubeProject);
            configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone);

            barrier.SetResult(1);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

            // works as normal after initialization
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected);
            ConfigureSolutionBinding(null);
            activeSolutionTracker.SimulateActiveSolutionChanged(true, "sln");
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone);

            bindingChangedHandler.ReceivedWithAnyArgs().Invoke(default, default);
            configScopeUpdater.ReceivedWithAnyArgs().UpdateConfigScopeForCurrentSolution(default);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_HandleBinding_EventsAreNotTriggeredBeforeInitializationIsComplete()
        {
            ConfigureService(false);
            ConfigureSolutionBinding(boundSonarQubeProject);
            using var testSubject = CreateUninitializedTestSubject(out var barrier);
            var bindingChangedHandler = Substitute.For<EventHandler<ActiveSolutionBindingEventArgs>>();
            testSubject.SolutionBindingChanged += bindingChangedHandler;

            ConfigureSolutionBinding(boundSonarQubeProject);
            testSubject.HandleBindingChange();

            // state is unmodified and events are not raised until initialized
            bindingChangedHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
            configScopeUpdater.DidNotReceiveWithAnyArgs().UpdateConfigScopeForCurrentSolution(boundSonarQubeProject);
            configProvider.DidNotReceiveWithAnyArgs().GetConfiguration();
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone);

            barrier.SetResult(1);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();

            // works as normal after initialization
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected);
            ConfigureSolutionBinding(null);
            testSubject.HandleBindingChange();
            bindingChangedHandler.ReceivedWithAnyArgs().Invoke(default, default);
            configScopeUpdater.ReceivedWithAnyArgs().UpdateConfigScopeForCurrentSolution(default);
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_WhenConnectionCannotBeEstablished_ReportAsStandalone()
        {
            // We want to directly jump to Connect
            sonarQubeServiceMock.IsConnected.Returns(false);
            ConfigureSolutionBinding(boundSonarQubeProject);

            // ConnectAsync should throw
            sonarQubeServiceMock.ConnectAsync(Arg.Any<ConnectionInformation>(), Arg.Any<CancellationToken>())
                .Returns(
                    _ => throw new Exception(),
                    _ => throw new TaskCanceledException(),
                    _ => throw new HttpRequestException("http request", new Exception("something happened")));

            // Arrange
            using var activeSolutionBoundTracker = CreateAndInitializeTestSubject();

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

            // Assert
            activeSolutionBoundTracker.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_When_ConnectAsync_Throws_Write_To_Output()
        {
            // We want to directly jump to Connect
            sonarQubeServiceMock.IsConnected.Returns(false);
            ConfigureSolutionBinding(boundSonarQubeProject);

            // ConnectAsync should throw
            sonarQubeServiceMock.ConnectAsync(Arg.Any<ConnectionInformation>(), Arg.Any<CancellationToken>())
                .Returns(
                    _ => throw new Exception(),
                    _ => throw new TaskCanceledException(),
                    _ => throw new HttpRequestException("http request", new Exception("something happened")));

            // Arrange
            using var activeSolutionBoundTracker = CreateAndInitializeTestSubject();

            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false, null);
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

            // Assert
            testLogger.OutputStrings.Where(x => x.Contains("SonarQube (Server, Cloud) request failed:")).Should().HaveCount(3);
            testLogger.AssertPartialOutputStringExists(CoreStrings.SonarQubeRequestTimeoutOrCancelled);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_BoundProject_PassedToConfigScopeUpdater()
        {
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(boundSonarQubeProject);
            using var testSubject = CreateAndInitializeTestSubject();

            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

            configScopeUpdater.Received(1).UpdateConfigScopeForCurrentSolution(boundSonarQubeProject);
            configScopeUpdater.ReceivedWithAnyArgs(1).UpdateConfigScopeForCurrentSolution(default);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_UnBoundProject_NullPassedToConfigScopeUpdater()
        {
            ConfigureService(isConnected: false);
            using var testSubject = CreateAndInitializeTestSubject();

            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

            configScopeUpdater.Received().UpdateConfigScopeForCurrentSolution(null);
        }

        [TestMethod]
        public void ActiveSolutionBoundTracker_Changes()
        {
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(boundSonarQubeProject);

            var testSubject = CreateAndInitializeTestSubject();
            var eventCounter = new EventCounter(testSubject);

            // Sanity
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Initially bound");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(0, "no events raised during construction");
            eventCounter.SolutionBindingChangedCount.Should().Be(0, "no events raised during construction");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "no events raised during construction");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "no events raised during construction");
            VerifyAndResetBoundSolutionUiContextMock(isActive: true);
            configScopeUpdater.DidNotReceiveWithAnyArgs().UpdateConfigScopeForCurrentSolution(default);

            // Case 1: Clear bound project
            ConfigureSolutionBinding(null);
            activeSolutionTracker.CurrentSolutionName = "solution1";

            // Act
            testSubject.HandleBindingChange();

            // Assert
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Unbound solution should report false activation");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(1, "Unbind should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(1, "Unbind should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Unbind should not trigger change");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Unbind should not trigger change");
            configScopeUpdater.Received(1).UpdateConfigScopeForCurrentSolution(null);
            configScopeUpdater.ReceivedWithAnyArgs(1).UpdateConfigScopeForCurrentSolution(default);
            VerifyAndResetBoundSolutionUiContextMock(isActive: false);

            // initializer connects to the server
            VerifyServiceDisconnect(0);
            VerifyServiceConnect(1);

            // Case 2: Set bound project
            ConfigureSolutionBinding(boundSonarQubeProject);
            // Act
            testSubject.HandleBindingChange();

            // Assert
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Bound solution should report true activation");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(2, "Bind should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(2, "Bind should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            configScopeUpdater.Received(1).UpdateConfigScopeForCurrentSolution(boundSonarQubeProject);
            configScopeUpdater.ReceivedWithAnyArgs(2).UpdateConfigScopeForCurrentSolution(default);
            VerifyAndResetBoundSolutionUiContextMock(isActive: true);

            // Binding controller manages connections in case of new bindings
            VerifyServiceDisconnect(0);
            VerifyServiceConnect(1);

            // Case 3: Bound solution unloaded -> disconnect
            ConfigureSolutionBinding(null);
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false, null);

            // Assert
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Standalone, "Should respond to solution change event and report unbound");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(3, "Solution change should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(3, "Solution change should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
            configScopeUpdater.Received(2).UpdateConfigScopeForCurrentSolution(null);
            configScopeUpdater.ReceivedWithAnyArgs(3).UpdateConfigScopeForCurrentSolution(default);
            VerifyAndResetBoundSolutionUiContextMock(isActive: false);

            // Disconnect on any solution change if was connected
            VerifyServiceDisconnect(1);
            VerifyServiceConnect(1);

            // Case 4: Load a bound solution
            ConfigureSolutionBinding(boundSonarQubeProject);
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution2");

            // Assert
            testSubject.CurrentConfiguration.Mode.Should().Be(SonarLintMode.Connected, "Bound respond to solution change event and report bound");
            eventCounter.PreSolutionBindingChangedCount.Should().Be(4, "Solution change should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(4, "Solution change should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Bind should not trigger update event");
            configScopeUpdater.Received(2).UpdateConfigScopeForCurrentSolution(boundSonarQubeProject);
            configScopeUpdater.ReceivedWithAnyArgs(4).UpdateConfigScopeForCurrentSolution(default);
            VerifyAndResetBoundSolutionUiContextMock(isActive: true);

            // Loading a bound solution should call connect
            VerifyServiceDisconnect(1);
            VerifyServiceConnect(2);

            // Case 5: Close a bound solution
            ConfigureSolutionBinding(null);
            // Act
            activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false, null);

            // Assert
            eventCounter.PreSolutionBindingChangedCount.Should().Be(5, "Solution change should trigger reanalysis");
            eventCounter.SolutionBindingChangedCount.Should().Be(5, "Solution change should trigger reanalysis");
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0, "Solution change should not trigger update event");
            configScopeUpdater.Received(3).UpdateConfigScopeForCurrentSolution(null);
            configScopeUpdater.ReceivedWithAnyArgs(5).UpdateConfigScopeForCurrentSolution(default);
            VerifyAndResetBoundSolutionUiContextMock(isActive: false);

            // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
            VerifyServiceDisconnect(2);
            VerifyServiceConnect(2);

            // Case 6: Dispose and change
            // Act
            testSubject.Dispose();
            ConfigureSolutionBinding(boundSonarQubeProject);
            testSubject.HandleBindingChange();

            // Assert
            eventCounter.PreSolutionBindingChangedCount.Should().Be(5, "Once disposed should stop raising the event");
            eventCounter.SolutionBindingChangedCount.Should().Be(5, "Once disposed should stop raising the event");
            configScopeUpdater.ReceivedWithAnyArgs(5).UpdateConfigScopeForCurrentSolution(default);
            // SonarQubeService.Disconnect should be called since the WPF DisconnectCommand is not available
            VerifyServiceDisconnect(2);
            VerifyServiceConnect(2);
        }

        [TestMethod]
        public void UpdateConnection_WasDisconnected_NewSolutionIsUnbound_NoConnectOrDisconnectCalls()
        {
            // Arrange
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(null);

            using (CreateAndInitializeTestSubject())
            {
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

                // Assert
                VerifyServiceConnect(0);
                VerifyServiceDisconnect(0);
                isMockServiceConnected.Should().Be(false);
            }
        }

        [TestMethod]
        public void UpdateConnection_WasDisconnected_NewSolutionIsBound_ConnectCalled()
        {
            // Arrange
            ConfigureService(isConnected: false);
            ConfigureSolutionBinding(new BoundServerProject("solution", "projectKey", new ServerConnection.SonarQube(new Uri("http://foo"))));

            using (CreateAndInitializeTestSubject())
            {
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

                // Assert
                VerifyServiceConnect(2);
                VerifyServiceDisconnect(1);
                isMockServiceConnected.Should().Be(true);
            }
        }

        [TestMethod]
        public void UpdateConnection_WasConnected_NewSolutionIsUnbound_DisconnectedCalled()
        {
            // Arrange
            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(null);

            using (CreateAndInitializeTestSubject())
            {
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

                // Assert
                VerifyServiceConnect(0);
                VerifyServiceDisconnect(1);
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

            using (CreateAndInitializeTestSubject())
            {
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: true, "solution");

                // Assert
                VerifyServiceConnect(2);
                VerifyServiceDisconnect(2);
                isMockServiceConnected.Should().Be(true);
            }
        }

        [TestMethod]
        public void UpdateConnection_Disconnect_ServiceDisconnectedIsCalled()
        {
            ConfigureService(isConnected: true);
            ConfigureSolutionBinding(null);

            // Arrange
            using (CreateAndInitializeTestSubject())
            {
                // Act
                activeSolutionTracker.SimulateActiveSolutionChanged(isSolutionOpen: false, null);

                // Assert
                VerifyServiceConnect(0);
                VerifyServiceDisconnect(1);
            }
        }

        [TestMethod]
        public void HandleBindingChange_WhenSameProject_NotRaised()
        {
            // Arrange
            using var testSubject = CreateAndInitializeTestSubject();
            var eventCounter = new EventCounter(testSubject);

            // Act
            testSubject.HandleBindingChange();

            // Assert
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0);
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0);
            eventCounter.PreSolutionBindingChangedCount.Should().Be(0);
            eventCounter.SolutionBindingChangedCount.Should().Be(0);
        }

        [TestMethod]
        public void HandleBindingChange_WhenProjectChanged_RaisedChangedEvents()
        {
            // Arrange
            using var testSubject = CreateAndInitializeTestSubject();
            var eventCounter = new EventCounter(testSubject);
            ConfigureSolutionBinding(boundSonarQubeProject);

            // Act
            testSubject.HandleBindingChange();

            // Assert
            eventCounter.PreSolutionBindingUpdatedCount.Should().Be(0);
            eventCounter.SolutionBindingUpdatedCount.Should().Be(0);
            eventCounter.PreSolutionBindingChangedCount.Should().Be(1);
            eventCounter.SolutionBindingChangedCount.Should().Be(1);
        }

        private ActiveSolutionBoundTracker CreateUninitializedTestSubject(out TaskCompletionSource<byte> barrier)
        {
            var tcs = barrier = new();
            initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<ActiveSolutionBoundTracker>(threadHandling, new TestLogger(), processor =>
            {
                createdInitializationProcessor = processor;
                MockableInitializationProcessor.ConfigureWithWait(processor, tcs);
            });
            return new ActiveSolutionBoundTracker(serviceProvider, activeSolutionTracker, configScopeUpdater, testLogger, configProvider, sonarQubeServiceMock,
                initializationProcessorFactory);
        }

        private ActiveSolutionBoundTracker CreateAndInitializeTestSubject()
        {
            initializationProcessorFactory
                = MockableInitializationProcessor.CreateFactory<ActiveSolutionBoundTracker>(threadHandling, new TestLogger(), processor => createdInitializationProcessor = processor);
            var testSubject = new ActiveSolutionBoundTracker(serviceProvider, activeSolutionTracker, configScopeUpdater, testLogger, configProvider, sonarQubeServiceMock,
                initializationProcessorFactory);
            testSubject.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
            return testSubject;
        }

        private void ConfigureService(bool isConnected)
        {
            isMockServiceConnected = isConnected;

            sonarQubeServiceMock.IsConnected.Returns(_ => isMockServiceConnected);
            sonarQubeServiceMock.When(x => x.Disconnect()).Do(_ => isMockServiceConnected = false);
            sonarQubeServiceMock.ConnectAsync(default, default).ReturnsForAnyArgs(_ =>
            {
                isMockServiceConnected = true;
                return Task.CompletedTask;
            });
        }

        private void ConfigureSolutionBinding(BoundServerProject boundProject)
        {
            configProvider.ProjectToReturn = boundProject;
            configProvider.ModeToReturn = boundProject == null ? SonarLintMode.Standalone : SonarLintMode.Connected;
            configProvider.FolderPathToReturn = "c:\\test";
        }

        private void VerifyServiceConnect(int expected)
        {
            sonarQubeServiceMock.ReceivedWithAnyArgs(expected).ConnectAsync(default, default);
        }

        private void VerifyServiceDisconnect(int expected)
        {
            sonarQubeServiceMock.ReceivedWithAnyArgs(expected).Disconnect();
        }

        private void VerifyAndResetBoundSolutionUiContextMock(bool isActive)
        {
            var isActiveInt = isActive ? 1 : 0;
            vsMonitorMock.Received(1).SetCmdUIContext(boundSolutionUiContextCookie, isActiveInt);
            vsMonitorMock.ReceivedWithAnyArgs(1).SetCmdUIContext(default, default);
            vsMonitorMock.ClearReceivedCalls();
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
            }
        }
    }
}
