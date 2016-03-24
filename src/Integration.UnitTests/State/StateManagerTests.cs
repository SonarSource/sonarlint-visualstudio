//-----------------------------------------------------------------------
// <copyright file="StateManagerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

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
            // Setup
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            host.VisualStateManager = testSubject;
            section.ViewModel.State = testSubject.ManagedState;
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var connection2 = new ConnectionInformation(new Uri("http://127.0.0.2"));
            var projects = new ProjectInformation[] { new ProjectInformation(), new ProjectInformation() };
            host.SetActiveSection(section);
            ServerViewModel serverVM;

            // Act + Verify
            // Case 1 - not connected to server (indicated by null)
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.SetProjects(connection1, null);

            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            VerifyConnectSectionViewModelIsNotConnected(section.ViewModel, connection1);
            VerifyConnectSectionViewModelIsNotConnected(section.ViewModel, connection2);
            VerifyConnectSectionViewModelHasNoBoundProjects(section.ViewModel);

            // Case 2 - connection1, empty project collection
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.SetProjects(connection1, new ProjectInformation[0]);

            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasNoProjects(section.ViewModel, connection1);
            Assert.IsTrue(serverVM.ShowAllProjects, "Expected show all projects");
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
            Assert.IsTrue(serverVM.ShowAllProjects, "Expected show all projects to be true when adding new projects");

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
            Assert.IsTrue(serverVM.ShowAllProjects, "Expected show all projects to be true when changing projects");

            // Case 5 - connection1 & connection2, once detached (connected or not), are reset, changes still being tracked
            host.ClearActiveSection();
            testSubject.SetProjects(connection1, projects);
            testSubject.SetProjects(connection2, projects);
            // Act
            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            host.SetActiveSection(section);
            // Verify
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
            // Setup
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host, section);
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var projects = new ProjectInformation[] { new ProjectInformation(), new ProjectInformation() };
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
            // Setup
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host, section);
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var projects = new ProjectInformation[] { new ProjectInformation(), new ProjectInformation() };
            testSubject.SetProjects(connection1, projects);
            ServerViewModel serverVM = testSubject.ManagedState.ConnectedServers.Single();
            host.SetActiveSection(section);
            testSubject.SyncCommandFromActiveSection();
            ContextualCommandViewModel toggleContextCmd = serverVM.Commands.First(x => x.InternalRealCommand.Equals(section.ToggleShowAllProjectsCommand));

            // Case 1: No bound projects
            serverVM.ShowAllProjects = true;
            // Act + Verify
            Assert.AreEqual(Strings.HideUnboundProjectsCommandText, toggleContextCmd.DisplayText, "Unexpected disabled context command text");

            // Case 2: has bound projects
            serverVM.ShowAllProjects = false;

            // Act + Verify
            Assert.AreEqual(Strings.ShowAllProjectsCommandText, toggleContextCmd.DisplayText, "Unexpected context command text");
        }

        [TestMethod]
        public void StateManager_BindCommand_DynamicText()
        {
            // Setup
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host, section);
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var projects = new ProjectInformation[] { new ProjectInformation() };
            testSubject.SetProjects(connection1, projects);
            ProjectViewModel projectVM = testSubject.ManagedState.ConnectedServers.Single().Projects.Single();
            host.SetActiveSection(section);
            testSubject.SyncCommandFromActiveSection();
            ContextualCommandViewModel bindCmd = projectVM.Commands.First(x => x.InternalRealCommand.Equals(section.BindCommand));

            // Case 1: Bound
            projectVM.IsBound = true;
            // Act + Verify
            Assert.AreEqual(Strings.SyncButtonText, bindCmd.DisplayText, "Unexpected disabled context command text");

            // Case 2: Not bound
            projectVM.IsBound = false;

            // Act + Verify
            Assert.AreEqual(Strings.BindButtonText, bindCmd.DisplayText, "Unexpected context command text");
        }

        [TestMethod]
        public void StateManager_ClearBoundProject()
        {
            // Setup
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;
            ConfigurableHost host = new ConfigurableHost();
            host.SetActiveSection(section);
            StateManager testSubject = this.CreateTestSubject(host);

            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ManagedState.ConnectedServers.Add(new ServerViewModel(new ConnectionInformation(new Uri("http://zzz1"))));
            testSubject.ManagedState.ConnectedServers.Add(new ServerViewModel(new ConnectionInformation(new Uri("http://zzz2"))));
            testSubject.ManagedState.ConnectedServers.ToList().ForEach(s => s.Projects.Add(new ProjectViewModel(s, new ProjectInformation())));
            var allProjects = testSubject.ManagedState.ConnectedServers.SelectMany(s => s.Projects).ToList();
            testSubject.SetBoundProject(allProjects.First().ProjectInformation);

            // Sanity
            Assert.IsTrue(testSubject.ManagedState.HasBoundProject);

            // Act
            testSubject.ClearBoundProject();

            // Verify
            Assert.IsFalse(testSubject.ManagedState.HasBoundProject);
            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
        }

        [TestMethod]
        public void StateManager_SetBoundProject()
        {
            // Setup
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableUserNotification notifications = (ConfigurableUserNotification)section.UserNotifications;
            ConfigurableHost host = new ConfigurableHost();
            host.SetActiveSection(section);
            StateManager testSubject = this.CreateTestSubject(host);

            section.UserNotifications.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            var conn = new ConnectionInformation(new Uri("http://xyz"));
            var projects = new[] { new ProjectInformation(), new ProjectInformation() };
            testSubject.SetProjects(conn, projects);
            TransferableVisualState state = testSubject.ManagedState;
            bool hasBoundProjectChanged = false;
            state.PropertyChanged += (o, e) =>
              {
                  Assert.AreEqual(nameof(state.HasBoundProject), e.PropertyName);
                  hasBoundProjectChanged = true;
              };

            // Act
            testSubject.SetBoundProject(projects[1]);

            // Verify
            notifications.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            var serverVM = state.ConnectedServers.Single();
            var project0VM = serverVM.Projects.Single(p => p.ProjectInformation == projects[0]);
            var project1VM = serverVM.Projects.Single(p => p.ProjectInformation == projects[1]);
            Assert.IsTrue(project1VM.IsBound, "Expected to be bound");
            Assert.IsFalse(project0VM.IsBound, "Not expected to be bound");
            Assert.IsTrue(testSubject.ManagedState.HasBoundProject, "Expected a bound project");
            Assert.IsTrue(hasBoundProjectChanged, "HasBoundProject expected to change");
        }

        [TestMethod]
        public void StateManager_IsBusyChanged()
        {
            // Setup
            var section = ConfigurableSectionController.CreateDefault();
            ConfigurableHost host = new ConfigurableHost();
            StateManager testSubject = this.CreateTestSubject(host);
            testSubject.IsBusyChanged += (s, isBusy) => section.ViewModel.IsBusy = isBusy;

            // Case 1: no active section -> no-op (i.e. no exception)
            testSubject.IsBusy = true;
            Assert.IsTrue(testSubject.ManagedState.IsBusy);
            testSubject.IsBusy = false;
            Assert.IsFalse(testSubject.ManagedState.IsBusy);

            // Case 2: has active section
            host.SetActiveSection(section);

            // Sanity (!IsBusy)
            Assert.IsFalse(section.ViewModel.IsBusy);

            // Act (IsBusy -> IsBusy)
            testSubject.IsBusy = true;

            // Verify
            Assert.IsTrue(section.ViewModel.IsBusy);
            Assert.IsTrue(testSubject.ManagedState.IsBusy);

            // Act (!IsBusy -> !IsBusy)
            testSubject.IsBusy = false;

            // Verify
            Assert.IsFalse(section.ViewModel.IsBusy);
            Assert.IsFalse(testSubject.ManagedState.IsBusy);
        }
        #endregion

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
            AssertExpectedNumberOfCommands(serverVM.Commands, 3);
            VerifyServerViewModelCommand(serverVM, section.DisconnectCommand, hasIcon: true);
            VerifyServerViewModelCommand(serverVM, section.RefreshCommand, hasIcon: true);
            VerifyServerViewModelCommand(serverVM, section.ToggleShowAllProjectsCommand, hasIcon: false);

            foreach (ProjectViewModel project in serverVM.Projects)
            {
                VerifyServerViewModelCommand(project, section.BindCommand);
            }
        }

        private static void VerifyNoCommands(ServerViewModel serverVM)
        {
            AssertExpectedNumberOfCommands(serverVM.Commands, 0);
            Assert.AreEqual(0, serverVM.Projects.Sum(p => p.Commands.Count), "Not expecting any project commands");
        }

        private static void VerifyServerViewModelCommand(ServerViewModel serverVM, ICommand internalCommand, bool hasIcon)
        {
            ContextualCommandViewModel commandVM = AssertCommandExists(serverVM.Commands, internalCommand);
            Assert.IsNotNull(commandVM.DisplayText, "DisplayText expected");
            Assert.AreEqual(serverVM, commandVM.InternalFixedContext, "The fixed context is incorrect");
            if (hasIcon)
            {
                Assert.IsNotNull(commandVM.Icon, "Icon expected");
                Assert.IsNotNull(commandVM.Icon.Moniker, "Icon moniker expected");
            }
            else
            {
                Assert.IsNull(commandVM.Icon, "Icon not expected");
            }
        }

        private static void VerifyServerViewModelCommand(ProjectViewModel projectVM, ICommand internalCommand)
        {
            AssertExpectedNumberOfCommands(projectVM.Commands, 1);
            ContextualCommandViewModel bindCommand = projectVM.Commands.Single();
            Assert.AreEqual(projectVM, bindCommand.InternalFixedContext, "BindCommand fixed context is incorrect");
            Assert.IsNotNull(bindCommand.DisplayText, "BindCommand DisplayText expected");
            Assert.IsNotNull(bindCommand.Icon, "Icon expected");
            Assert.IsNotNull(bindCommand.Icon.Moniker, "Icon moniker expected");
            Assert.IsNotNull(bindCommand.Command, "Unexpected command");
            Assert.AreEqual(internalCommand, bindCommand.InternalRealCommand, "Unexpected command");
        }

        private static ContextualCommandViewModel AssertCommandExists(ContextualCommandsCollection commands, ICommand realCommand)
        {
            ContextualCommandViewModel[] commandsArr = commands.Where(c => c.InternalRealCommand.Equals(realCommand)).ToArray();
            AssertExpectedNumberOfCommands(commandsArr, 1);
            return commandsArr[0];
        }

        private static void AssertExpectedNumberOfCommands(IEnumerable<ContextualCommandViewModel> commands, int expectedCount)
        {
            Assert.AreEqual(expectedCount, commands.Count(), "Unexpected commands. All command: {0}", string.Join(", ", commands.Select(c => c.DisplayText)));
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnectedAndHasProjects(ConnectSectionViewModel vm, ConnectionInformation connection, ProjectInformation[] projects)
        {
            ServerViewModel serverVM = VerifyConnectSectionViewModelIsConnected(vm, connection);
            CollectionAssert.AreEquivalent(projects, serverVM.Projects.Select(p => p.ProjectInformation).ToArray(), "Unexpected projects for server {0}", connection.ServerUri);

            return serverVM;
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnectedAndHasNoProjects(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = VerifyConnectSectionViewModelIsConnected(vm, connection);
            Assert.AreEqual(0, serverVM.Projects.Count, "Unexpected number of projects");

            return serverVM;
        }

        private static void VerifyConnectSectionViewModelIsNotConnected(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = vm.State?.ConnectedServers?.SingleOrDefault(s => s.Url == connection.ServerUri);
            Assert.IsNull(serverVM, "Should not find server view model for {0}", connection.ServerUri);
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnected(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = vm.State?.ConnectedServers?.SingleOrDefault(s => s.Url == connection.ServerUri);
            Assert.IsNotNull(serverVM, "Could not find server view model for {0}", connection.ServerUri);

            return serverVM;
        }

        private static void VerifyConnectSectionViewModelHasNoBoundProjects(ConnectSectionViewModel vm)
        {
            Assert.IsFalse(vm.State.HasBoundProject, "View model should not have any bound projects");
        }
        #endregion
    }
}
