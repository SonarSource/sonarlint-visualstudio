//-----------------------------------------------------------------------
// <copyright file="ConnectSectionControllerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class ConnectSectionControllerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableConnectionInformationProvider connectionInformationProvider;
        private ConfigurableConnectionWorkflow workflow;
        private DTEMock dteMock;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.workflow = new ConfigurableConnectionWorkflow(this.sonarQubeService);
            this.connectionInformationProvider = new ConfigurableConnectionInformationProvider();
            this.dteMock = this.RegisterDTEWithSolution();
        }

        #region Tests
        [TestMethod]
        public void ConnectSectionController_Attach()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var section = new ConfigurableConnectSection();
            var vm = section.ViewModel = new ConnectSectionViewModel();
            var view = section.View = new TestableConnectSectionView();

            // Act
            testSubject.Attach(section);

            // Verify
            Assert.AreSame(vm, testSubject.ConnectCommand.UserNotification);
            Assert.AreSame(vm, testSubject.BindCommand.UserNotification);
            Assert.AreSame(view, testSubject.ConnectCommand.ProgressControlHost);
            Assert.AreSame(view, testSubject.BindCommand.ProgressControlHost);
            Assert.AreSame(testSubject.ConnectCommand.WpfCommand, vm.ConnectCommand);
            Assert.AreSame(testSubject.BindCommand.WpfCommand, vm.BindCommand);
        }

        [TestMethod]
        public void ConnectSectionController_Detach()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var section = new ConfigurableConnectSection();
            var vm = section.ViewModel = new ConnectSectionViewModel();
            var view = section.View = new TestableConnectSectionView();
            testSubject.Attach(section);

            // Act
            testSubject.Detach(section);

            // Verify
            Assert.IsNull(testSubject.ConnectCommand.UserNotification);
            Assert.IsNull(testSubject.BindCommand.UserNotification);
            Assert.IsNull(testSubject.ConnectCommand.ProgressControlHost);
            Assert.IsNull(testSubject.BindCommand.ProgressControlHost);
        }

        [TestMethod]
        public void ConnectSectionController_Attach_Detach_Dispose()
        {
            // Setup
            bool firedProjectsChangedEvent = false;
            var testSubject = this.CreateTestSubject(setProjectsAction: () => firedProjectsChangedEvent = true);
            var section = new ConfigurableConnectSection();
            var vm = section.ViewModel = new ConnectSectionViewModel();
            var view = section.View = new TestableConnectSectionView();

            // Act (attach + raise event)
            testSubject.Attach(section);
            firedProjectsChangedEvent = false;
            testSubject.ConnectCommand.ConnectedProjectsChanged(new ConnectionInformation(new Uri("http://www")), new ProjectInformation[0]);

            // Verify
            Assert.IsTrue(firedProjectsChangedEvent, "ProjectsChanged event is expected");
            Assert.AreSame(section, testSubject.AttachedSection, "Unexpected attached section");

            // Act (detach + raise event)
            testSubject.Detach(section);
            firedProjectsChangedEvent = false;
            testSubject.ConnectCommand.ConnectedProjectsChanged(new ConnectionInformation(new Uri("http://www")), new ProjectInformation[0]);

            // Verify
            Assert.IsTrue(firedProjectsChangedEvent, "ProjectsChanged event is expected");
            Assert.IsNull(testSubject.AttachedSection, "Unexpected attached section");

            // Act (dispose)
            testSubject.Dispose();
            firedProjectsChangedEvent = false;
            testSubject.ConnectCommand.ConnectedProjectsChanged(new ConnectionInformation(new Uri("http://www")), new ProjectInformation[0]);

            // Verify
            Assert.IsFalse(firedProjectsChangedEvent, "Expected to unregister from ProjectsChanged event");
        }

        [TestMethod]
        public void ConnectSectionController_StateManagement()
        {
            // Setup
            var testSubject1 = this.CreateTestSubject();
            var server1 = new ServerViewModel(new ConnectionInformation(new Uri("http://www1")));
            var project1 = new ProjectViewModel(server1, new ProjectInformation { Name = "Proj1", Key = "p1" });
            testSubject1.ConnectedServers.Add(server1);
            testSubject1.BoundProjects.Add(project1);
            var server2 = new ServerViewModel(new ConnectionInformation(new Uri("http://www2")));
            var project2 = new ProjectViewModel(server2, new ProjectInformation { Name = "Proj2", Key = "p2" });
            testSubject1.ConnectedServers.Add(server2);
            testSubject1.BoundProjects.Add(project2);
            // Detached sections
            var section1 = CreateConnectSection();
            var section2 = CreateConnectSection();

            // Act (will load the state)
            testSubject1.Attach(section2);

            // Verify
            Assert.AreSame(testSubject1.ConnectedServers, section2.ViewModel.ConnectedServers);
            Assert.AreSame(testSubject1.BoundProjects, section2.ViewModel.BoundProjects);

            // Act (detach)
            testSubject1.Detach(section2);

            // Verify 
            Assert.IsNull(section2.ViewModel.ConnectedServers);
            Assert.IsNull(section2.ViewModel.BoundProjects);

            // Act (attach a different section, will load the state on it)
            testSubject1.Attach(section1);

            // Verify
            Assert.AreSame(testSubject1.ConnectedServers, section1.ViewModel.ConnectedServers);
            Assert.AreSame(testSubject1.BoundProjects, section1.ViewModel.BoundProjects);
        }

        [TestMethod]
        public void ConnectSectionController_RefreshCommand()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            // Case 1: No connection
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.RefreshCommand.CanExecute(null));
            this.sonarQubeService.AssertConnectRequests(0);

            // Case 2: Connected
            this.sonarQubeService.SetConnection(new Uri("http://connected"));
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];

            // Case 3: Connecting
            testSubject.IsConnecting = true;
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.RefreshCommand.CanExecute(null));

            // Case 4: Binding
            testSubject.IsConnecting = false;
            testSubject.IsBinding = true;
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.RefreshCommand.CanExecute(null));

            testSubject.IsBinding = false;

            // Act + Verify CanExecute
            Assert.IsTrue(testSubject.RefreshCommand.CanExecute(null));
            this.sonarQubeService.AssertConnectRequests(0);

            // Act + Verify Execute
            testSubject.RefreshCommand.Execute(null);
            this.sonarQubeService.AssertConnectRequests(1);
        }

        [TestMethod]
        public void ConnectSectionController_DisconnectCommand()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            // Case 1: No connection
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.DisconnectCommand.CanExecute(null));
            this.sonarQubeService.AssertDisconnectRequests(0);

            // Case 2: Connected
            this.sonarQubeService.SetConnection(new Uri("http://connected"));

            // Act + Verify CanExecute
            Assert.IsTrue(testSubject.DisconnectCommand.CanExecute(null));
            this.sonarQubeService.AssertDisconnectRequests(0);

            // Act + Verify Execute
            testSubject.DisconnectCommand.Execute(null);
            this.sonarQubeService.AssertDisconnectRequests(1);
        }

        [TestMethod]
        public void ConnectSectionController_ToggleShowAllProjectsCommand()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var connInfo = new ConnectionInformation(new Uri("http://localhost"));
            var projectInfo = new ProjectInformation { Key = "p1", Name = "proj1" };
            testSubject.SetProjects(null, new ConnectedProjectsEventArgs(connInfo, new[] { projectInfo }));

            ServerViewModel server = testSubject.ConnectedServers.First();
            ProjectViewModel project = server.Projects.Single();
            ContextualCommandViewModel toggleContextCmd = server.Commands.First(x => x.InternalRealCommand == testSubject.ToggleShowAllProjectsCommand);

            // Case 1: No bound projects
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.ToggleShowAllProjectsCommand.CanExecute(server));
            Assert.AreEqual(Strings.HideUnboundProjectsCommandText, toggleContextCmd.DisplayText, "Unexpected disabled context command text");

            // Case 2: Bound
            project.IsBound = true;
            testSubject.BoundProjects.Add(project);

            // Case 2a: Hide --> Show
            // Setup
            server.ShowAllProjects = false;

            // Act + Verify CanExecute
            Assert.IsTrue(testSubject.ToggleShowAllProjectsCommand.CanExecute(server));
            Assert.AreEqual(Strings.ShowAllProjectsCommandText, toggleContextCmd.DisplayText, "Unexpected context command text for hide --> show");
            // Act + Verify Execute
            testSubject.ToggleShowAllProjectsCommand.Execute(server);
            Assert.IsTrue(server.ShowAllProjects);

            // Case 2b: Show --> Hide
            // Setup
            server.ShowAllProjects = true;

            // Act + Verify CanExecute
            Assert.IsTrue(testSubject.ToggleShowAllProjectsCommand.CanExecute(server));
            Assert.AreEqual(Strings.HideUnboundProjectsCommandText, toggleContextCmd.DisplayText, "Unexpected context command text for show --> hide");
            // Act + Verify Execute
            testSubject.ToggleShowAllProjectsCommand.Execute(server);
            Assert.IsFalse(server.ShowAllProjects);
        }

        [TestMethod]
        public void ConnectSectionController_SetProjectsUIThread()
        {
            // Setup
            var connection1 = new ConnectionInformation(new Uri("http://127.0.0.1"));
            var connection2 = new ConnectionInformation(new Uri("http://127.0.0.2"));
            var testSubject = (TestableConnectSectionController)this.CreateTestSubject();
            var section = new ConfigurableConnectSection();
            var vm = section.ViewModel = new ConnectSectionViewModel() { ConnectedServers = testSubject.ConnectedServers };
            section.View = new TestableConnectSectionView();
            testSubject.Attach(section);
            var projects = new ProjectInformation[] { new ProjectInformation(), new ProjectInformation() };
            this.sonarQubeService.SetConnection(connection1);
            ServerViewModel serverVM = null;

            // Act + Verify
            // Case 1 - no active connection (indicated by the null in projects rather that the using the service figure this out)
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ConnectCommand.ConnectedProjectsChanged(connection1, null);

            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            VerifyConnectSectionViewModelIsNotConnected(vm, connection1);
            VerifyConnectSectionViewModelIsNotConnected(vm, connection2);
            VerifyConnectSectionViewModelHasNoBoundProjects(vm);

            // Case 2 - connection1, empty project collection
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ConnectCommand.ConnectedProjectsChanged(connection1, new ProjectInformation[0]);

            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasNoProjects(vm, connection1);
            Assert.IsTrue(serverVM.ShowAllProjects, "Expected show all projects");
            VerifyCommands(testSubject, serverVM);
            VerifyConnectSectionViewModelIsNotConnected(vm, connection2);
            VerifyConnectSectionViewModelHasNoBoundProjects(vm);

            // Case 3 - connection1, non-empty project collection
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ConnectCommand.ConnectedProjectsChanged(connection1, projects);

            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(vm, connection1, projects);
            VerifyCommands(testSubject, serverVM);
            VerifyConnectSectionViewModelIsNotConnected(vm, connection2);
            VerifyConnectSectionViewModelHasNoBoundProjects(vm);
            Assert.IsTrue(serverVM.ShowAllProjects, "Expected show all projects to be true when adding new projects");

            // Case 4 - connection2, change projects
            testSubject.ConnectCommand.ConnectedProjectsChanged(connection1, projects);
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ConnectCommand.ConnectedProjectsChanged(connection2, projects);

            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(vm, connection1, projects);
            VerifyCommands(testSubject, serverVM);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(vm, connection2, projects);
            VerifyCommands(testSubject, serverVM);
            VerifyConnectSectionViewModelHasNoBoundProjects(vm);
            Assert.IsTrue(serverVM.ShowAllProjects, "Expected show all projects to be true when changing projects");

            // Case 5 - connection1 & connection2, once detached (connected or not), are reset, changes still being tracked
            testSubject.Detach(section);
            testSubject.ConnectCommand.ConnectedProjectsChanged(connection1, projects);
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ConnectCommand.ConnectedProjectsChanged(connection2, projects);
            // Sanity
            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            VerifyConnectSectionViewModelIsNotConnected(vm, connection1);
            VerifyConnectSectionViewModelIsNotConnected(vm, connection2);
            // Act
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.Attach(section);
            // Verify
            testSubject.NotificationOverride.AssertNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(vm, connection1, projects);
            VerifyCommands(testSubject, serverVM);
            serverVM = VerifyConnectSectionViewModelIsConnectedAndHasProjects(vm, connection2, projects);
            VerifyCommands(testSubject, serverVM);
            VerifyConnectSectionViewModelHasNoBoundProjects(vm);
        }

        [TestMethod]
        public void ConnectSectionController_ClearBoundProjects()
        {
            // Setup
            var testSubject = (TestableConnectSectionController)this.CreateTestSubject();

            var serverA = new ServerViewModel(new ConnectionInformation(new Uri("http://sonarqube-A")));
            serverA.SetProjects(new[]
            {
                new ProjectInformation { Key = "A_p1", Name = "A_Project1" },
                new ProjectInformation { Key = "A_p2", Name = "A_Project2" }
            });
            IList<ProjectViewModel> projectsA = serverA.Projects.ToList();
            projectsA[0].IsBound = true;

            var serverB = new ServerViewModel(new ConnectionInformation(new Uri("http://sonarqube-B")));
            serverB.SetProjects(new[]
            {
                new ProjectInformation { Key = "B_p1", Name = "B_Project1" },
                new ProjectInformation { Key = "B_p2", Name = "B_Project2" }

            });
            IList<ProjectViewModel> projectsB = serverB.Projects.ToList();
            projectsB[0].IsBound = true;

            testSubject.ConnectedServers.Add(serverA);
            testSubject.ConnectedServers.Add(serverB);
            testSubject.BoundProjects.Add(projectsA[0]);
            testSubject.BoundProjects.Add(projectsB[0]);

            serverA.ShowAllProjects = false;
            serverB.ShowAllProjects = false;
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);

            // Act
            testSubject.ClearBoundProjects(serverA);

            // Verify
            Assert.AreSame(projectsB[0], testSubject.BoundProjects.Single(), "Expected only server B bound project");
            Assert.IsTrue(projectsB[0].IsBound, "Expected server B bound project to remain bound");
            Assert.IsFalse(projectsB[1].IsBound, "Expected server B unbound project remain unbound");
            Assert.IsFalse(serverB.ShowAllProjects, "Server B ShowAllProjects should remain false");

            Assert.IsFalse(projectsA[0].IsBound, "Expected server A bound project to now be set as unbound");
            Assert.IsFalse(projectsA[1].IsBound, "Expected server A unbound project remain unbound");
            Assert.IsTrue(serverA.ShowAllProjects, "Server A ShowAllProjects should be true after clearing it's bound projects");
            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
        }

        [TestMethod]
        public void ConnectSectionController_ClearAllBoundProjects()
        {
            // Setup
            var testSubject = (TestableConnectSectionController)this.CreateTestSubject();
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            testSubject.ConnectedServers.Add(new ServerViewModel(new ConnectionInformation(new Uri("http://zzz1"))));
            testSubject.ConnectedServers.Add(new ServerViewModel(new ConnectionInformation(new Uri("http://zzz2"))));
            testSubject.ConnectedServers.ToList().ForEach(s => s.Projects.Add(new ProjectViewModel(s, new ProjectInformation()) { IsBound = true }));
            var allProjects = testSubject.ConnectedServers.SelectMany(s => s.Projects).ToList();
            allProjects.ForEach(p => testSubject.BoundProjects.Add(p));

            // Act
            testSubject.ClearAllBoundProjects();

            // Verify
            Assert.AreEqual(0, testSubject.BoundProjects.Count, "All the bound projects were supposed to be cleared");
            Assert.IsFalse(allProjects.Any(p => p.IsBound), "Not expecting any bound projects");
            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
        }

        [TestMethod]
        public void ConnectSectionController_SetBoundProject()
        {
            // Setup
            var testSubject = (TestableConnectSectionController)this.CreateTestSubject();
            testSubject.Notification.ShowNotificationError("message", NotificationIds.FailedToFindBoundProjectKeyId, null);
            ServerViewModel serverVM = new ServerViewModel(new ConnectionInformation(new Uri("http://xyz")));
            ProjectViewModel project1 = new ProjectViewModel(serverVM, new ProjectInformation());
            serverVM.Projects.Add(project1);
            ProjectViewModel project2 = new ProjectViewModel(serverVM, new ProjectInformation());
            serverVM.Projects.Add(project2);

            // Act
            testSubject.SetBoundProject(project2);

            // Verify
            testSubject.NotificationOverride.AssertNoNotification(NotificationIds.FailedToFindBoundProjectKeyId);
            Assert.IsTrue(project2.IsBound, "Expected to be bound");
            Assert.IsFalse(project1.IsBound, "Not expected to be bound");
            CollectionAssert.AreEqual(new[] { project2 }, testSubject.BoundProjects.ToArray(), "Unexpected projects in the bound collection");
        }

        [TestMethod]
        [WorkItem(173611)]
        public void ConnectSectionController_UpdateBusyState()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var section = CreateConnectSection();

            // Case 1: not attached -> no-op (i.e. no exception)
            testSubject.IsBinding = true;
            testSubject.IsConnecting = true;
            testSubject.IsBinding = false;
            testSubject.IsConnecting = false;

            // Case 2: controller attached to section
            testSubject.Attach(section);

            // Sanity (!IsBinding & !IsConnecting -> !IsBusy)
            Assert.IsFalse(section.ViewModel.IsBusy);

            // Act (IsBinding -> IsBusy)
            testSubject.IsBinding = true;

            // Verify
            Assert.IsTrue(section.ViewModel.IsBusy);

            // Act (!IsBinding -> !IsBusy)
            testSubject.IsBinding = false;

            // Verify
            Assert.IsFalse(section.ViewModel.IsBusy);

            // Act (IsConnecting -> IsBusy)
            testSubject.IsConnecting = true;

            // Verify
            Assert.IsTrue(section.ViewModel.IsBusy);

            // Act (!IsConnecting -> !IsBusy)
            testSubject.IsConnecting = false;

            // Verify
            Assert.IsFalse(section.ViewModel.IsBusy);

            // Act (IsConnecting && IsBinding -> IsBusy)
            testSubject.IsConnecting = true;
            testSubject.IsBinding = true;

            // Verify
            Assert.IsTrue(section.ViewModel.IsBusy);
        }
        #endregion

        #region Helpers
        private DTEMock RegisterDTEWithSolution()
        {
            DTEMock dte = new DTEMock();
            dte.Solution = new SolutionMock(dte, Path.Combine(this.TestContext.TestDeploymentDir, this.TestContext.TestName, "solution.sln"));
            this.serviceProvider.RegisterService(typeof(DTE), dte);
            return dte;
        }

        private static ConfigurableConnectSection CreateConnectSection()
        {
            var section = new ConfigurableConnectSection();
            section.ViewModel = new ConnectSectionViewModel();
            section.View = new TestableConnectSectionView();

            return section;
        } 

        private ConnectSectionController CreateTestSubject(ConfigurableActiveSolutionTracker tracker)
        {
            var controller = new TestableConnectSectionController(this.serviceProvider, this.sonarQubeService, tracker);
            controller.SetConnectCommand(new ConnectCommand(controller, this.sonarQubeService, null, this.workflow));
            return controller;
        }

        private ConnectSectionController CreateTestSubject(Action setProjectsAction = null)
        {
            var controller = new TestableConnectSectionController(this.serviceProvider, this.sonarQubeService)
            {
                SetProjectsAction = setProjectsAction
            };
            controller.SetConnectCommand(new ConnectCommand(controller, this.sonarQubeService, null, this.workflow));
            return controller;
        }

        private static void VerifyCommands(ConnectSectionController controller, ServerViewModel serverVM)
        {
            AssertExpectedNumberOfCommands(serverVM.Commands, 3);
            VerifyServerViewModelCommand(serverVM, controller.DisconnectCommand, hasIcon: true);
            VerifyServerViewModelCommand(serverVM, controller.RefreshCommand, hasIcon: true);
            VerifyServerViewModelCommand(serverVM, controller.ToggleShowAllProjectsCommand, hasIcon: false);

            foreach (ProjectViewModel project in serverVM.Projects)
            {
                VerifyServerViewModelCommand(project, controller.BindCommand.WpfCommand);
            }
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
            ContextualCommandViewModel[] commandsArr = commands.Where(c => c.InternalRealCommand == realCommand).ToArray();
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
            ServerViewModel serverVM = vm.ConnectedServers?.SingleOrDefault(s => s.Url == connection.ServerUri);
            Assert.IsNull(serverVM, "Should not find server view model for {0}", connection.ServerUri);
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnected(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = vm.ConnectedServers?.SingleOrDefault(s => s.Url == connection.ServerUri);
            Assert.IsNotNull(serverVM, "Could not find server view model for {0}", connection.ServerUri);

            return serverVM;
        }

        private static void VerifyConnectSectionViewModelHasNoBoundProjects(ConnectSectionViewModel vm)
        {
            Assert.IsFalse(vm.BoundProjects.Any(), "View model should not have any bound projects");
        }

        #endregion
    }
}
