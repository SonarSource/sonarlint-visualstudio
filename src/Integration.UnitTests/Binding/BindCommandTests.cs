//-----------------------------------------------------------------------
// <copyright file="BindCommandTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using EnvDTE;
using Microsoft.TeamFoundation.Client.CommandTarget;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class BindCommandTests
    {
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private TestBindingWorkflow workflow;
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ConfigurableVsMonitorSelection monitorSelection;
        private DTEMock dteMock;

        [TestInitialize]
        public void TestInitialize()
        {
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.workflow = new TestBindingWorkflow();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.dteMock = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), this.dteMock);
            this.solutionMock = new SolutionMock();
            this.monitorSelection = KnownUIContextsAccessor.MonitorSelectionService;
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.monitorSelection = null;
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            KnownUIContextsAccessor.Reset();
        }

        #region Tests
        [TestMethod]
        public void BindCommand_Ctor()
        {
            // Setup
            BindCommand testSubject = this.CreateBindCommand();

            // Verify
            Assert.IsNotNull(testSubject.WpfCommand, "The WPF command should never be null");
        }

        [TestMethod]
        public void BindCommand_Ctor_ArgumentChecks()
        {
            BindCommand suppressAnalysisWarning;

            Exceptions.Expect<ArgumentNullException>(() =>
            {
                suppressAnalysisWarning = new BindCommand(null, new ConfigurableSonarQubeServiceWrapper());
            });

            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new ConnectSectionController(new ServiceContainer(), new SonarQubeServiceWrapper(new ServiceContainer()), new ConfigurableActiveSolutionTracker());
            Exceptions.Expect<ArgumentNullException>(() =>
            {
                suppressAnalysisWarning = new BindCommand(controller, null);
            });
        }

        [TestMethod]
        public void BindCommand_CanExecuteBind()
        {
            // Setup
            ProjectViewModel projectVM = CreateProjectViewModel();
            BindCommand testSubject = this.PrepareCommandForExecution(new ConnectionInformation(new Uri("http://127.0.0.0")));

            // Case 1: All the requirements are set
            // Act + Verify
            Assert.IsTrue(testSubject.WpfCommand.CanExecute(projectVM), "All the requirement should be satisfied for the command to be enabled");

            // Case 2: project is null
            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(null), "Project is null");

            // Case 3: No connection
            this.sonarQubeService.ClearConnection();
            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(projectVM), "No connection");

            // Case 4: busy
            this.sonarQubeService.SetConnection(new Uri("http://127.0.0.0"));
            testSubject.Controller.State.IsBusy = true;
            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(projectVM), "Connecting");

            // Case 5: No host
            testSubject.Controller.State.IsBusy = false;
            testSubject.ProgressControlHost = null;
            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(projectVM), "No host");
        }

        [TestMethod]
        public void BindCommand_CanExecuteBind_VsState()
        {
            // Setup
            ProjectViewModel projectVM = CreateProjectViewModel();
            BindCommand testSubject = this.PrepareCommandForExecution(new ConnectionInformation(new Uri("http://127.0.0.0")));
            ProjectMock project1 = this.solutionMock.Projects.Single();

            // Case 1: SolutionExistsAndNotBuildingAndNotDebugging is not active
            testSubject.ProgressControlHost = new ConfigurableProgressControlHost();
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, false);
            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(projectVM), "No UI context");

            // Case 2: Non-managed project kind
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, true);
            this.projectSystemHelper.ManagedProjects = null;
            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(projectVM), "No managed projects");

            // Case 3: No projects at all
            solutionMock.RemoveProject(project1);
            // Act + Verify
            Assert.IsFalse(testSubject.WpfCommand.CanExecute(projectVM), "No projects");
        }

        [TestMethod]
        public void BindCommand_ExecuteBind()
        {
            // Setup
            BindCommand testSubject = this.PrepareCommandForExecution(new ConnectionInformation(new Uri("http://127.0.0.0")));

            // Act
            var projectToBind1 = new ProjectInformation { Key = "1" };
            ProjectViewModel projectVM1 = CreateProjectViewModel(projectToBind1);
            testSubject.WpfCommand.Execute(projectVM1);

            // Verify
            this.workflow.AssertBoundProject(projectVM1);

            // Act, bind a different project
            var projectToBind2 = new ProjectInformation { Key = "2" };
            ProjectViewModel projectVM2 = CreateProjectViewModel(projectToBind2);
            testSubject.WpfCommand.Execute(projectVM2);

            // Verify
            this.workflow.AssertBoundProject(projectVM2);
        }

        [TestMethod]
        public void BindCommand_SetBindingInProgress()
        {
            // Setup
            ProjectViewModel projectVM = CreateProjectViewModel();
            BindCommand testSubject = this.PrepareCommandForExecution(new ConnectionInformation(new Uri("http://127.0.0.0")));
            var progressEvents = new ConfigurableProgressEvents();

            foreach (var controllerResult in (ProgressControllerResult[])Enum.GetValues(typeof(ProgressControllerResult)))
            {
                this.dteMock.ToolWindows.SolutionExplorer.Window.Active = false;

                // Sanity
                Assert.IsTrue(testSubject.WpfCommand.CanExecute(projectVM));

                // Act - disable
                testSubject.SetBindingInProgress(progressEvents, projectVM);

                // Verify 
                Assert.IsFalse(testSubject.WpfCommand.CanExecute(projectVM), "Binding is in progress so should not be enabled");

                // Act - finish
                progressEvents.SimulateFinished(controllerResult);

                // Verify 
                Assert.IsTrue(testSubject.WpfCommand.CanExecute(projectVM), "Binding is finished with result: {0}", controllerResult);
                if (controllerResult == ProgressControllerResult.Succeeded)
                {
                    Assert.IsTrue(this.dteMock.ToolWindows.SolutionExplorer.Window.Active, "SolutionExplorer window supposed to be activated");
                }
                else
                {
                    Assert.IsFalse(this.dteMock.ToolWindows.SolutionExplorer.Window.Active, "SolutionExplorer window is not supposed to be activated");
                }
            }
        }

        [TestMethod]
        public void BindCommand_SetBindingInProgress_ProjectViewModelIsBoundChanges()
        {
            // Setup
            ServerViewModel serverVM = CreateServerViewModel();
            serverVM.SetProjects(new[]
            {
                new ProjectInformation { Key = "key1" },
                new ProjectInformation { Key = "key2" },
            });
            var projects = serverVM.Projects.ToArray();
            projects[0].IsBound = true;
            ProjectViewModel projectVM = projects[1];

            BindCommand testSubject = this.PrepareCommandForExecution(new ConnectionInformation(serverVM.Url));
            var progressEvents = new ConfigurableProgressEvents();
            testSubject.Controller.State.ConnectedServers.Add(serverVM);
            testSubject.Controller.State.SetBoundProject(projects[0]);

            // Sanity
            Assert.IsTrue(testSubject.Controller.State.HasBoundProject);
            Assert.IsTrue(testSubject.WpfCommand.CanExecute(projectVM));

            // Act - start
            testSubject.SetBindingInProgress(progressEvents, projectVM);

            // Verify 
            Assert.IsTrue(projects[0].IsBound, "Expected to remain bound at this point");
            Assert.IsFalse(projects[1].IsBound, "Expected to remain unbound at this point");
            Assert.IsTrue(testSubject.Controller.State.HasBoundProject);

            // Act - finish
            progressEvents.SimulateFinished(ProgressControllerResult.Succeeded);

            // Verify 
            Assert.IsFalse(projects[0].IsBound, "Expected to become unbound at this point");
            Assert.IsTrue(projects[1].IsBound, "Expected to become bound at this point");
            Assert.IsTrue(testSubject.Controller.State.HasBoundProject);
        }

        [TestMethod]
        public void BindCommand_BindingFinished_ServerViewModelShowAllProjects()
        {
            // Setup
            ServerViewModel serverVM = CreateServerViewModel();
            serverVM.SetProjects(new[]
            {
                new ProjectInformation { Key = "key1" }
            });
            var projects = serverVM.Projects.ToArray();
            ProjectViewModel projectVM = projects[0];
            BindCommand testSubject = this.PrepareCommandForExecution(new ConnectionInformation(serverVM.Url));
            var progressEvents = new ConfigurableProgressEvents();
            testSubject.Controller.State.ConnectedServers.Add(serverVM);

            foreach (ProgressControllerResult result in Enum.GetValues(typeof(ProgressControllerResult)).OfType<ProgressControllerResult>())
            {
                // Setup
                testSubject.SetBindingInProgress(progressEvents, projectVM);
                serverVM.ShowAllProjects = true;

                // Act
                progressEvents.SimulateFinished(result);

                // Verify 
                if (result == ProgressControllerResult.Succeeded)
                {
                    Assert.IsFalse(serverVM.ShowAllProjects, "Not expected to show all projects after successful binding");
                }
                else
                {
                    Assert.IsTrue(serverVM.ShowAllProjects, "Expected to still show all projects after failed binding");
                }
            }
        }

        [TestMethod]
        public void BindCommand_SetBindingInProgress_Notifications()
        {
            // Setup
            ServerViewModel serverVM = CreateServerViewModel();
            serverVM.SetProjects(new[]
            {
                new ProjectInformation { Key = "key1" }
            });
            ProjectViewModel projectVM = serverVM.Projects.ToArray()[0];
            ConfigurableUserNotification userNotifications = new ConfigurableUserNotification();
            BindCommand testSubject = this.PrepareCommandForExecution(new ConnectionInformation(serverVM.Url), userNotifications);
            var progressEvents = new ConfigurableProgressEvents();
            testSubject.UserNotification.ShowNotificationError("Need to make sure that this is clear once started", NotificationIds.FailedToBindId, new RelayCommand(() => { }));

            foreach (ProgressControllerResult result in Enum.GetValues(typeof(ProgressControllerResult)).OfType<ProgressControllerResult>())
            {
                // Act - start
                testSubject.SetBindingInProgress(progressEvents, projectVM);

                // Verify 
                userNotifications.AssertNoNotification(NotificationIds.FailedToBindId);

                // Act - finish
                progressEvents.SimulateFinished(result);

                // Verify 
                if (result == ProgressControllerResult.Succeeded)
                {
                    userNotifications.AssertNoNotification(NotificationIds.FailedToBindId);
                }
                else
                {
                    userNotifications.AssertNotification(NotificationIds.FailedToBindId, Strings.FailedToToBindSolution);
                }
            }
        }

        [TestMethod]
        public void BindCommand_OnQueryStatus()
        {
            // Setup
            BindCommand testSubject = this.CreateBindCommand();
            bool canExecuteChanged = false;
            testSubject.WpfCommand.CanExecuteChanged += (o, e) => canExecuteChanged = true;

            // Act
            Guid notUsed = Guid.Empty;
            ((IOleCommandTarget)testSubject).QueryStatus(ref notUsed, 0, new OLECMD[0], IntPtr.Zero);

            // Verify
            Assert.IsTrue(canExecuteChanged, "The command needs to invalidate the previous CanExecute state using CanExecuteChanged event");
        }

        #endregion

        #region Helpers
        private static ProjectViewModel CreateProjectViewModel(ProjectInformation projectInfo = null)
        {
            return new ProjectViewModel(CreateServerViewModel(), projectInfo ?? new ProjectInformation());
        }

        private static ServerViewModel CreateServerViewModel()
        {
            return new ServerViewModel(new ConnectionInformation(new Uri("http://123")));
        }

        private BindCommand PrepareCommandForExecution(ConnectionInformation connection, ConfigurableUserNotification notifications = null)
        {
            BindCommand testSubject = this.CreateBindCommand();
            testSubject.UserNotification = notifications;
            this.sonarQubeService.SetConnection(connection);
            testSubject.ProgressControlHost = new ConfigurableProgressControlHost();
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, true);
            ProjectMock project1 = this.solutionMock.AddOrGetProject("project1");
            this.projectSystemHelper.ManagedProjects = new[] { project1 };

            // Sanity
            Assert.IsTrue(testSubject.WpfCommand.CanExecute(CreateProjectViewModel()), "All the requirement should be satisfied for the command to be enabled");

            return testSubject;
        }

        private class TestBindingWorkflow : IBindingWorkflowExecutor
        {
            public ProjectViewModel BoundProject { get; private set; }

            #region IBindingWorkflow
            void IBindingWorkflowExecutor.BindProject(ProjectViewModel projectVM)
            {
                this.BoundProject = projectVM;
            }
            #endregion

            public void AssertBoundProject(ProjectViewModel expected)
            {
                Assert.AreSame(expected, this.BoundProject, "Unexpected project binding");
            }
        }
        private BindCommand CreateBindCommand()
        {
            var controller = new ConnectSectionController(this.serviceProvider, new TransferableVisualState(), this.sonarQubeService, new ConfigurableActiveSolutionTracker(), Dispatcher.CurrentDispatcher);
            return new BindCommand(controller, this.sonarQubeService, this.workflow, this.projectSystemHelper);
        }

        #endregion
    }
}
