/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using System.Linq;
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.State
{
    [TestClass]
    public class StateManagerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        #region Tests

        [TestMethod]
        public void StateManager_ArgsCheck()
        {
            Exceptions.Expect<ArgumentNullException>(() => new StateManager(null, new TransferableVisualState()));
            Exceptions.Expect<ArgumentNullException>(() => new StateManager(new ConfigurableHost(), null));
        }

        [TestMethod]
        public void StateManager_SetProjectsUIThread()
        {
            // Arrange
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            host.VisualStateManager = testSubject;
            section.ViewModel.State = testSubject.ManagedState;
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var connection2 = new ConnectionInformation(new Uri("http://127.0.0.2"));
            var projects = new[] { new SonarQubeProject("", ""), new SonarQubeProject("", "") };
            host.SetActiveSection(section);
            ServerViewModel serverVM;

            // Act + Assert
            // Case 1 - not connected to server (indicated by null)
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.SetProjects(connection1, null);

            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            VerifyConnectSectionViewModelIsNotConnected(section.ViewModel, connection1);
            VerifyConnectSectionViewModelIsNotConnected(section.ViewModel, connection2);
            VerifyConnectSectionViewModelHasNoBoundProjects(section.ViewModel);

            // Case 2 - connection1, empty project collection
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.SetProjects(connection1, new SonarQubeProject[0]);

            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasNoProjects(section.ViewModel, connection1);
            serverVM.ShowAllProjects.Should().BeTrue("Expected show all projects");
            VerifySectionCommands(section, serverVM);
            VerifyConnectSectionViewModelIsNotConnected(section.ViewModel, connection2);
            VerifyConnectSectionViewModelHasNoBoundProjects(section.ViewModel);

            // Case 3 - connection1, non-empty project collection
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.SetProjects(connection1, projects);

            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(section.ViewModel, connection1, projects);
            VerifySectionCommands(section, serverVM);
            VerifyConnectSectionViewModelIsNotConnected(section.ViewModel, connection2);
            VerifyConnectSectionViewModelHasNoBoundProjects(section.ViewModel);
            serverVM.ShowAllProjects.Should().BeTrue("Expected show all projects to be true when adding new projects");

            // Case 4 - connection2, change projects
            testSubject.SetProjects(connection1, projects);
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.SetProjects(connection2, projects);

            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(section.ViewModel, connection1, projects);
            VerifySectionCommands(section, serverVM);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(section.ViewModel, connection2, projects);
            VerifySectionCommands(section, serverVM);
            VerifyConnectSectionViewModelHasNoBoundProjects(section.ViewModel);
            serverVM.ShowAllProjects.Should().BeTrue("Expected show all projects to be true when changing projects");

            // Case 5 - connection1 & connection2, once detached (connected or not), are reset, changes still being tracked
            host.ClearActiveSection();
            testSubject.SetProjects(connection1, projects);
            testSubject.SetProjects(connection2, projects);
            // Act
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            host.SetActiveSection(section);
            // Assert
            notifications.AssertNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(section.ViewModel, connection1, projects);
            VerifySectionCommands(section, serverVM);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(section.ViewModel, connection2, projects);
            VerifySectionCommands(section, serverVM);
            VerifyConnectSectionViewModelHasNoBoundProjects(section.ViewModel);
        }

        [TestMethod]
        public void StateManager_SyncCommandFromActiveSection()
        {
            // Arrange
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host, section);
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var projects = new SonarQubeProject[] { new SonarQubeProject("", ""), new SonarQubeProject("", "") };
            testSubject.SetProjects(connection1, projects);
            ServerViewModel serverVM = testSubject.ManagedState.ConnectedServers.Single();

            // Case 1: has active section
            host.SetActiveSection(section);

            // Act
            testSubject.SyncCommandFromActiveSection();
            VerifySectionCommands(section, serverVM);

            // Case 2: has no active section
            host.ClearActiveSection();

            // Act
            testSubject.SyncCommandFromActiveSection();
            VerifyNoCommands(serverVM);

            // Case 3: re-active
            host.SetActiveSection(section);

            // Act
            testSubject.SyncCommandFromActiveSection();
            VerifySectionCommands(section, serverVM);
        }

        [TestMethod]
        public void StateManager_ToggleShowAllProjectsCommand_DynamicText()
        {
            // Arrange
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host, section);
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var projects = new SonarQubeProject[] { new SonarQubeProject("", ""), new SonarQubeProject("", "") };
            testSubject.SetProjects(connection1, projects);
            ServerViewModel serverVM = testSubject.ManagedState.ConnectedServers.Single();
            host.SetActiveSection(section);
            testSubject.SyncCommandFromActiveSection();
            ContextualCommandViewModel toggleContextCmd = serverVM.Commands.First(x => x.InternalRealCommand.Equals(section.ToggleShowAllProjectsCommand));

            // Case 1: No bound projects
            serverVM.ShowAllProjects = true;
            // Act + Assert
            toggleContextCmd.DisplayText.Should().Be(Strings.HideUnboundProjectsCommandText, "Unexpected disabled context command text");

            // Case 2: has bound projects
            serverVM.ShowAllProjects = false;

            // Act + Assert
            toggleContextCmd.DisplayText.Should().Be(Strings.ShowAllProjectsCommandText, "Unexpected context command text");
        }

        [TestMethod]
        public void StateManager_BindCommand_DynamicText()
        {
            // Arrange
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host, section);
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var projects = new SonarQubeProject[] { new SonarQubeProject("", "") };
            testSubject.SetProjects(connection1, projects);
            ProjectViewModel projectVM = testSubject.ManagedState.ConnectedServers.Single().Projects.Single();
            host.SetActiveSection(section);
            testSubject.SyncCommandFromActiveSection();
            ContextualCommandViewModel bindCmd = projectVM.Commands.First(x => x.InternalRealCommand.Equals(section.BindCommand));

            // Case 1: Bound
            projectVM.IsBound = true;
            // Act + Assert
            bindCmd.DisplayText.Should().Be(Strings.SyncButtonText, "Unexpected disabled context command text");

            // Case 2: Not bound
            projectVM.IsBound = false;

            // Act + Assert
            bindCmd.DisplayText.Should().Be(Strings.BindButtonText, "Unexpected context command text");
        }

        [TestMethod]
        public void StateManager_ClearBoundProject()
        {
            // Arrange
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;
            ConfigurableHost host = new ConfigurableHost();
            host.SetActiveSection(section);
            StateManager testSubject = this.CreateTestSubject(host);

            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ManagedState.ConnectedServers.Add(new ServerViewModel(new ConnectionInformation(new Uri("http://zzz1"))));
            testSubject.ManagedState.ConnectedServers.Add(new ServerViewModel(new ConnectionInformation(new Uri("http://zzz2"))));
            testSubject.ManagedState.ConnectedServers.ToList().ForEach(s => s.Projects.Add(new ProjectViewModel(s, new SonarQubeProject("", ""))));
            var allProjects = testSubject.ManagedState.ConnectedServers.SelectMany(s => s.Projects).ToList();
            testSubject.SetBoundProject(allProjects.First().SonarQubeProject);

            // Sanity
            testSubject.ManagedState.HasBoundProject.Should().BeTrue();

            // Act
            testSubject.ClearBoundProject();

            // Assert
            testSubject.ManagedState.HasBoundProject.Should().BeFalse();
            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
        }

        [TestMethod]
        public void StateManager_SetBoundProject()
        {
            // Arrange
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;
            ConfigurableHost host = new ConfigurableHost();
            host.SetActiveSection(section);
            StateManager testSubject = this.CreateTestSubject(host);

            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            var conn = new ConnectionInformation(new Uri("http://xyz"));
            var projects = new[] { new SonarQubeProject("", ""), new SonarQubeProject("", "") };
            testSubject.SetProjects(conn, projects);
            TransferableVisualState state = testSubject.ManagedState;
            bool hasBoundProjectChanged = false;
            state.PropertyChanged += (o, e) =>
              {
                  e.PropertyName.Should().Be(nameof(state.HasBoundProject));
                  hasBoundProjectChanged = true;
              };

            // Act
            testSubject.SetBoundProject(projects[1]);

            // Assert
            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            var serverVM = state.ConnectedServers.Single();
            var project0VM = serverVM.Projects.Single(p => p.SonarQubeProject == projects[0]);
            var project1VM = serverVM.Projects.Single(p => p.SonarQubeProject == projects[1]);
            project1VM.IsBound.Should().BeTrue();
            project0VM.IsBound.Should().BeFalse();
            testSubject.ManagedState.HasBoundProject.Should().BeTrue("Expected a bound project");
            hasBoundProjectChanged.Should().BeTrue("HasBoundProject expected to change");
        }

        [TestMethod]
        public void StateManager_IsBusyChanged()
        {
            // Arrange
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            testSubject.IsBusyChanged += (s, isBusy) => section.ViewModel.IsBusy = isBusy;

            // Case 1: no active section -> no-op (i.e. no exception)
            testSubject.IsBusy = true;
            testSubject.ManagedState.IsBusy.Should().BeTrue();
            testSubject.IsBusy = false;
            testSubject.ManagedState.IsBusy.Should().BeFalse();

            // Case 2: has active section
            host.SetActiveSection(section);

            // Sanity (!IsBusy)
            section.ViewModel.IsBusy.Should().BeFalse();

            // Act (IsBusy -> IsBusy)
            testSubject.IsBusy = true;

            // Assert
            section.ViewModel.IsBusy.Should().BeTrue();
            testSubject.ManagedState.IsBusy.Should().BeTrue();

            // Act (!IsBusy -> !IsBusy)
            testSubject.IsBusy = false;

            // Assert
            section.ViewModel.IsBusy.Should().BeFalse();
            testSubject.ManagedState.IsBusy.Should().BeFalse();

            // Dispose (should stop updated the view model)
            testSubject.Dispose();
            testSubject.IsBusy = true;

            // Assert
            section.ViewModel.IsBusy.Should().BeFalse();
            testSubject.ManagedState.IsBusy.Should().BeTrue();
        }

        [TestMethod]
        public void StateManager_BindingStateChanged()
        {
            // Arrange
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            var countOnBindingStateChangeFired = 0;
            testSubject.BindingStateChanged += (sender, e) => countOnBindingStateChangeFired++;

            testSubject.ManagedState.ConnectedServers.Add(new ServerViewModel(new ConnectionInformation(new Uri("http://zzz1"))));
            testSubject.ManagedState.ConnectedServers.ToList().ForEach(s => s.Projects.Add(new ProjectViewModel(s, new SonarQubeProject("", ""))));
            var allProjects = testSubject.ManagedState.ConnectedServers.SelectMany(s => s.Projects).ToList();

            // Sanity
            countOnBindingStateChangeFired.Should().Be(0);

            // Act
            testSubject.SetBoundProject(allProjects.First().SonarQubeProject);

            // Assert
            countOnBindingStateChangeFired.Should().Be(1);

            // Act
            testSubject.ClearBoundProject();

            // Assert
            countOnBindingStateChangeFired.Should().Be(2);
        }

        [TestMethod]
        public void StateManager_IsConnected()
        {
            // Arrange
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);

            // Sanity
            testSubject.IsConnected.Should().BeFalse();

            // Act (connect)
            testSubject.SetProjects(new ConnectionInformation(new Uri("http://qwerty")), new SonarQubeProject[0]);

            // Assert
            testSubject.IsConnected.Should().BeTrue();

            // Act (disconnect)
            testSubject.SetProjects(new ConnectionInformation(new Uri("http://qwerty")), null);

            // Assert
            testSubject.IsConnected.Should().BeFalse();
        }

        [TestMethod]
        public void StateManager_GetConnectedServers()
        {
            // Arrange
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            var connection1 = new ConnectionInformation(new Uri("http://conn1"));
            var connection2 = new ConnectionInformation(new Uri("http://conn2"));

            // Sanity
            testSubject.GetConnectedServers().Should().BeEmpty();

            // Act (connect)
            testSubject.SetProjects(connection1, new SonarQubeProject[0]);

            // Assert
            CollectionAssert.AreEquivalent(new[] { connection1 } , testSubject.GetConnectedServers().ToArray());

            // Act (connect another one)
            testSubject.SetProjects(connection2, new SonarQubeProject[0]);

            // Assert
            CollectionAssert.AreEquivalent(new[] { connection1, connection2 }, testSubject.GetConnectedServers().ToArray());

            // Act (disconnect)
            testSubject.SetProjects(connection1, null);
            testSubject.SetProjects(connection2, null);

            // Assert
            testSubject.GetConnectedServers().Should().BeEmpty();
            connection1.IsDisposed.Should().BeTrue("Leaking connections?");
            connection2.IsDisposed.Should().BeTrue("Leaking connections?");
        }

        [TestMethod]
        public void StateManager_Dispose()
        {
            // Arrange
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            var connection1 = new ConnectionInformation(new Uri("http://conn1"));
            testSubject.SetProjects(connection1, new SonarQubeProject[0]);

            // Act
            testSubject.Dispose();

            // Assert
            connection1.IsDisposed.Should().BeTrue("Leaking connections?");
        }

        [TestMethod]
        public void StateManager_GetConnectedServer()
        {
            // Arrange
            const string SharedKey = "Key"; // The key is the same for all projects on purpose
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            var connection1 = new ConnectionInformation(new Uri("http://conn1"));
            var project1 = new SonarQubeProject(SharedKey, "");
            var connection2 = new ConnectionInformation(new Uri("http://conn2"));
            var project2 = new SonarQubeProject(SharedKey, "");
            testSubject.SetProjects(connection1, new SonarQubeProject[] { project1 });
            testSubject.SetProjects(connection2, new SonarQubeProject[] { project2 });

            // Case 1: Exists
            // Act+Verify
            testSubject.GetConnectedServer(project1).Should().Be(connection1);
            testSubject.GetConnectedServer(project2).Should().Be(connection2);

            // Case 2: Doesn't exist
            // Act+Verify
            testSubject.GetConnectedServer(new SonarQubeProject(SharedKey, "")).Should().BeNull();
        }

        #endregion Tests

        #region Helpers

        private StateManager CreateTestSubject(ConfigurableHost host, ConfigurableSectionController section = null)
        {
            var testSubject = new StateManager(host, new TransferableVisualState());

            if (section != null)
            {
                section.ViewModel.State = testSubject.ManagedState;
            }

            return testSubject;
        }

        private static void VerifySectionCommands(ISectionController section, ServerViewModel serverVM)
        {
            AssertExpectedNumberOfCommands(serverVM.Commands, 4);
            VerifyServerViewModelCommand(serverVM, section.DisconnectCommand, fixedContext: serverVM, hasIcon: true);
            VerifyServerViewModelCommand(serverVM, section.RefreshCommand, fixedContext: serverVM, hasIcon: true);
            VerifyServerViewModelCommand(serverVM, section.BrowseToUrlCommand, fixedContext: serverVM.ConnectionInformation.ServerUri.ToString(), hasIcon: true);
            VerifyServerViewModelCommand(serverVM, section.ToggleShowAllProjectsCommand, fixedContext: serverVM, hasIcon: false);

            foreach (ProjectViewModel project in serverVM.Projects)
            {
                AssertExpectedNumberOfCommands(project.Commands, 2);
                VerifyProjectViewModelCommand(project, section.BindCommand, fixedContext: project, hasIcon: true);
                VerifyProjectViewModelCommand(project, section.BrowseToProjectDashboardCommand, fixedContext: project, hasIcon: true);
            }
        }

        private static void VerifyNoCommands(ServerViewModel serverVM)
        {
            AssertExpectedNumberOfCommands(serverVM.Commands, 0);
            serverVM.Projects.Sum(p => p.Commands.Count).Should().Be(0, "Not expecting any project commands");
        }

        private static void VerifyServerViewModelCommand(ServerViewModel serverVM, ICommand internalCommand, object fixedContext, bool hasIcon)
        {
            ContextualCommandViewModel commandVM = AssertCommandExists(serverVM.Commands, internalCommand);
            commandVM.DisplayText.Should().NotBeNull("DisplayText expected");
            commandVM.InternalFixedContext.Should().Be(fixedContext, "The fixed context is incorrect");
            if (hasIcon)
            {
                commandVM.Icon.Should().NotBeNull("Icon expected");
                commandVM.Icon.Moniker.Should().NotBeNull("Icon moniker expected");
            }
            else
            {
                commandVM.Icon.Should().BeNull("Icon not expected");
            }
        }

        private static void VerifyProjectViewModelCommand(ProjectViewModel projectVM, ICommand internalCommand, object fixedContext, bool hasIcon)
        {
            ContextualCommandViewModel commandVM = AssertCommandExists(projectVM.Commands, internalCommand);
            commandVM.DisplayText.Should().NotBeNull("DisplayText expected");
            commandVM.InternalFixedContext.Should().Be(fixedContext, "The fixed context is incorrect");
            commandVM.InternalRealCommand.Should().Be(internalCommand, "Unexpected command");
            if (hasIcon)
            {
                commandVM.Icon.Should().NotBeNull("Icon expected");
                commandVM.Icon.Moniker.Should().NotBeNull("Icon moniker expected");
            }
            else
            {
                commandVM.Icon.Should().BeNull("Icon not expected");
            }
        }

        private static ContextualCommandViewModel AssertCommandExists(ContextualCommandsCollection commands, ICommand realCommand)
        {
            ContextualCommandViewModel[] commandsArr = commands.Where(c => c.InternalRealCommand.Equals(realCommand)).ToArray();
            AssertExpectedNumberOfCommands(commandsArr, 1);
            return commandsArr[0];
        }

        private static void AssertExpectedNumberOfCommands(IEnumerable<ContextualCommandViewModel> commands, int expectedCount)
        {
            commands.Should().HaveCount(expectedCount, "Unexpected commands. All command: {0}", string.Join(", ", commands.Select(c => c.DisplayText)));
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnectedAndHasProjects(ConnectSectionViewModel vm, ConnectionInformation connection, SonarQubeProject[] projects)
        {
            ServerViewModel serverVM = VerifyConnectSectionViewModelIsConnected(vm, connection);
            CollectionAssert.AreEquivalent(projects, serverVM.Projects.Select(p => p.SonarQubeProject).ToArray(), "Unexpected projects for server {0}", connection.ServerUri);

            return serverVM;
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnectedAndHasNoProjects(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = VerifyConnectSectionViewModelIsConnected(vm, connection);
            serverVM.Projects.Should().BeEmpty("Unexpected number of projects");

            return serverVM;
        }

        private static void VerifyConnectSectionViewModelIsNotConnected(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = vm.State?.ConnectedServers?.SingleOrDefault(s => s.Url == connection.ServerUri);
            serverVM.Should().BeNull("Should not find server view model for {0}", connection.ServerUri);
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnected(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = vm.State?.ConnectedServers?.SingleOrDefault(s => s.Url == connection.ServerUri);
            serverVM.Should().NotBeNull("Could not find server view model for {0}", connection.ServerUri);

            return serverVM;
        }

        private static void VerifyConnectSectionViewModelHasNoBoundProjects(ConnectSectionViewModel vm)
        {
            vm.State.HasBoundProject.Should().BeFalse("View model should not have any bound projects");
        }

        #endregion Helpers
    }
}