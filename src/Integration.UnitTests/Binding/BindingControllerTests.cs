/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Windows.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.ProfileConflicts;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.UnitTests.Binding
{
    [TestClass]
    public class BindingControllerTests
    {
        private ConfigurableHost host;
        private Mock<ISonarQubeService> sonarQubeService;
        private ConfigurableVsProjectSystemHelper projectSystemHelper;
        private TestBindingWorkflow workflow;
        private ConfigurableServiceProvider serviceProvider;
        private SolutionMock solutionMock;
        private ConfigurableVsMonitorSelection monitorSelection;
        private DTEMock dteMock;
        private ConfigurableRuleSetConflictsController conflictsController;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableVsOutputWindowPane outputWindowPane;
        private ConfigurableSolutionRuleSetsInformationProvider ruleSetsInformationProvider;

        private readonly BoundSonarQubeProject ValidProject = new BoundSonarQubeProject(new Uri("http://any"), "projectKey", "Project Name");
        private readonly BindCommandArgs ValidBindingArgs = new BindCommandArgs("any key", "any name", new ConnectionInformation(new Uri("http://anyXXX")));

        [TestInitialize]
        public void TestInitialize()
        {
            KnownUIContextsAccessor.Reset();
            this.sonarQubeService = new Mock<ISonarQubeService>();
            this.workflow = new TestBindingWorkflow();
            this.serviceProvider = new ConfigurableServiceProvider();
            this.dteMock = new DTEMock();
            this.serviceProvider.RegisterService(typeof(DTE), this.dteMock);
            this.solutionMock = new SolutionMock();
            this.monitorSelection = KnownUIContextsAccessor.MonitorSelectionService;
            this.projectSystemHelper = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.conflictsController = new ConfigurableRuleSetConflictsController();
            this.configProvider = new ConfigurableConfigurationProvider();
            this.ruleSetsInformationProvider = new ConfigurableSolutionRuleSetsInformationProvider();
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), this.projectSystemHelper);
            this.serviceProvider.RegisterService(typeof(IRuleSetConflictsController), this.conflictsController);
            this.serviceProvider.RegisterService(typeof(IConfigurationProvider), this.configProvider);
            this.serviceProvider.RegisterService(typeof(ISolutionRuleSetsInformationProvider), this.ruleSetsInformationProvider);
            this.serviceProvider.RegisterService(typeof(ISourceControlledFileSystem), new ConfigurableSourceControlledFileSystem(new MockFileSystem()));

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher)
            {
                SonarQubeService = sonarQubeService.Object
            };

            configProvider.FolderPathToReturn = "c:\\test";
        }

        #region Tests

        [TestMethod]
        public void BindingController_Ctor()
        {
            // Arrange
            BindingController testSubject = this.CreateBindingController();

            // Assert
            testSubject.BindCommand.Should().NotBeNull("The Bind command should never be null");
        }

        [TestMethod]
        public void Ctor_WithNullHost_ThrowsArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new BindingController(null);

            // Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("serviceProvider");
        }

        [TestMethod]
        public void BindingController_BindCommand_Status()
        {
            // Arrange
            BindCommandArgs bindingArgs = CreateBindingArguments("key1", "name1", "http://localhost");
            BindingController testSubject = this.PrepareCommandForExecution();

            // Case 1: All the requirements are set
            // Act + Assert
            testSubject.BindCommand.CanExecute(bindingArgs).Should().BeTrue("All the requirement should be satisfied for the command to be enabled");

            // Case 2: project is null
            // Act + Assert
            testSubject.BindCommand.CanExecute(null)
                .Should().BeFalse("Project is null");

            // Case 3: No connection
            this.host.TestStateManager.IsConnected = false;
            // Act + Assert
            testSubject.BindCommand.CanExecute(bindingArgs)
                .Should().BeFalse("No connection");

            // Case 4: busy
            this.host.TestStateManager.IsConnected = true;
            this.host.VisualStateManager.IsBusy = true;
            // Act + Assert
            testSubject.BindCommand.CanExecute(bindingArgs)
                .Should().BeFalse("Connecting");
        }

        [TestMethod]
        public void BindingController_BindCommand_Status_VsState()
        {
            // Arrange
            BindCommandArgs bindArgs = CreateBindingArguments("proj1", "name1", "http://localhost:9000");
            BindingController testSubject = this.PrepareCommandForExecution();
            ProjectMock project1 = this.solutionMock.Projects.Single();

            // Case 1: SolutionExistsAndFullyLoaded is not active
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, false);
            // Act + Assert
            testSubject.BindCommand.CanExecute(bindArgs).Should().BeFalse("No UI context: SolutionExistsAndFullyLoaded");

            // Case 2: SolutionExistsAndNotBuildingAndNotDebugging is not active
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, true);
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, false);
            // Act + Assert
            testSubject.BindCommand.CanExecute(bindArgs).Should().BeFalse("No UI context: SolutionExistsAndNotBuildingAndNotDebugging");

            // Case 3: Non-managed project kind
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, true);
            this.projectSystemHelper.Projects = null;
            // Act + Assert
            testSubject.BindCommand.CanExecute(bindArgs).Should().BeFalse("No managed projects");

            // Case 4: No projects at all
            solutionMock.RemoveProject(project1);
            // Act + Assert
            testSubject.BindCommand.CanExecute(bindArgs).Should().BeFalse("No projects");
        }

        [TestMethod]
        public void BindingController_BindCommand_Execution()
        {
            // Arrange
            BindingController testSubject = this.PrepareCommandForExecution();

            // Act
            BindCommandArgs bindingArgs1 = CreateBindingArguments("1", "name1", "http://localhost");
            testSubject.BindCommand.Execute(bindingArgs1);

            // Assert
            this.workflow.BindingArgs.ProjectKey.Should().Be("1");
            this.workflow.BindingArgs.ProjectName.Should().Be("name1");

            // Act, bind a different project
            BindCommandArgs bingingArgs2 = CreateBindingArguments("2", "name2", "http://localhost");
            testSubject.BindCommand.Execute(bingingArgs2);

            // Assert
            this.workflow.BindingArgs.ProjectKey.Should().Be("2");
            this.workflow.BindingArgs.ProjectName.Should().Be("name2");
        }

        [TestMethod]
        public void BindingController_SetBindingInProgress()
        {
            // Arrange
            BindCommandArgs bindingArgs = CreateBindingArguments("key1", "name1", "http://localhost");
            ConnectionInformation otherConnection = new ConnectionInformation(new Uri("http://otherConnection"));
            BindCommandArgs bindingInProgressArgs = new BindCommandArgs("another.key", "another.name", otherConnection);

            BindingController testSubject = this.PrepareCommandForExecution();
            var progressEvents = new ConfigurableProgressEvents();

            foreach (var controllerResult in (ProgressControllerResult[])Enum.GetValues(typeof(ProgressControllerResult)))
            {
                this.dteMock.ToolWindows.SolutionExplorer.Window.Active = false;

                // Sanity
                testSubject.BindCommand.CanExecute(bindingArgs).Should().BeTrue();

                // Act - disable
                testSubject.SetBindingInProgress(progressEvents, bindingInProgressArgs);

                // Assert
                testSubject.BindCommand.CanExecute(bindingArgs).Should().BeFalse("Binding is in progress so should not be enabled");

                // Act - finish
                progressEvents.SimulateFinished(controllerResult);

                // Assert
                testSubject.BindCommand.CanExecute(bindingArgs).Should().BeTrue("Binding is finished with result: {0}", controllerResult);
                if (controllerResult == ProgressControllerResult.Succeeded)
                {
                    this.dteMock.ToolWindows.SolutionExplorer.Window.Active.Should().BeTrue("SolutionExplorer window supposed to be activated");
                }
                else
                {
                    this.dteMock.ToolWindows.SolutionExplorer.Window.Active.Should().BeFalse("SolutionExplorer window is not supposed to be activated");
                }
            }
        }

        [TestMethod]
        public void BindingController_BindingFinished()
        {
            // Arrange
            var bindingArgs = new BindCommandArgs("key1", "name1", new ConnectionInformation(new Uri("http://localhost")));

            BindingController testSubject = this.PrepareCommandForExecution();
            var progressEvents = new ConfigurableProgressEvents();

            foreach (ProgressControllerResult result in Enum.GetValues(typeof(ProgressControllerResult)).OfType<ProgressControllerResult>())
            {
                // Arrange
                testSubject.SetBindingInProgress(progressEvents, bindingArgs);
                testSubject.IsBindingInProgress.Should().BeTrue();

                // Act
                progressEvents.SimulateFinished(result);

                // Assert
                testSubject.IsBindingInProgress.Should().BeFalse();

                if (result == ProgressControllerResult.Succeeded)
                {
                    this.host.TestStateManager.AssignedProjectKey.Should().Be("key1");
                }
                else
                {
                    this.host.TestStateManager.AssignedProjectKey.Should().BeNull();
                }
            }
        }

        [TestMethod]
        public void BindingController_BindingFinished_Navigation()
        {
            // Arrange
            var bindingArgs = new BindCommandArgs("key2", "", new ConnectionInformation(new Uri("http://myUri")));

            BindingController testSubject = this.PrepareCommandForExecution();
            var progressEvents = new ConfigurableProgressEvents();
            var teController = new ConfigurableTeamExplorerController();

            var mefExports = MefTestHelpers.CreateExport<ITeamExplorerController>(teController);
            var mefModel = ConfigurableComponentModel.CreateWithExports(mefExports);
            serviceProvider.RegisterService(typeof(SComponentModel), mefModel, replaceExisting: true);

            // Case 1: On non-successful binding no navigation will occur
            foreach (ProgressControllerResult nonSuccuess in new[] { ProgressControllerResult.Cancelled, ProgressControllerResult.Failed })
            {
                // Act
                testSubject.SetBindingInProgress(progressEvents, bindingArgs);
                progressEvents.SimulateFinished(nonSuccuess);

                // Assert
                teController.ShowConnectionsPageCallsCount.Should().Be(0);
                this.dteMock.ToolWindows.SolutionExplorer.Window.Active.Should().BeFalse();
            }

            // Case 2: Has conflicts (should navigate to team explorer page)
            this.conflictsController.HasConflicts = true;

            // Act
            testSubject.SetBindingInProgress(progressEvents, bindingArgs);
            progressEvents.SimulateFinished(ProgressControllerResult.Succeeded);

            // Assert
            teController.ShowConnectionsPageCallsCount.Should().Be(1);
            this.dteMock.ToolWindows.SolutionExplorer.Window.Active.Should().BeFalse();

            // Case 3: Has no conflicts (should navigate to solution explorer)
            this.conflictsController.HasConflicts = false;

            // Act
            testSubject.SetBindingInProgress(progressEvents, bindingArgs);
            progressEvents.SimulateFinished(ProgressControllerResult.Succeeded);

            // Assert
            teController.ShowConnectionsPageCallsCount.Should().Be(1);
            this.dteMock.ToolWindows.SolutionExplorer.Window.Active.Should().BeTrue();
        }

        [TestMethod]
        public void BindingController_SetBindingInProgress_Notifications()
        {
            // Arrange
            var bindingArgs = new BindCommandArgs("key2", "", new ConnectionInformation(new Uri("http://myUri")));

            BindingController testSubject = this.PrepareCommandForExecution();
            var section = ConfigurableSectionController.CreateDefault();
            this.host.SetActiveSection(section);
            var progressEvents = new ConfigurableProgressEvents();
            this.host.ActiveSection.UserNotifications.ShowNotificationError("Need to make sure that this is clear once started", NotificationIds.FailedToBindId, new RelayCommand(() => { }));
            ConfigurableUserNotification userNotifications = (ConfigurableUserNotification)section.UserNotifications;

            foreach (ProgressControllerResult result in Enum.GetValues(typeof(ProgressControllerResult)).OfType<ProgressControllerResult>())
            {
                // Act - start
                testSubject.SetBindingInProgress(progressEvents, bindingArgs);

                // Assert
                userNotifications.AssertNoNotification(NotificationIds.FailedToBindId);

                // Act - finish
                progressEvents.SimulateFinished(result);

                // Assert
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
        public void BindingController_BindCommand_OnQueryStatus()
        {
            // Arrange
            BindingController testSubject = this.CreateBindingController();
            bool canExecuteChanged = false;
            testSubject.BindCommand.CanExecuteChanged += (o, e) => canExecuteChanged = true;

            // Act
            Guid notUsed = Guid.Empty;
            ((IOleCommandTarget)testSubject).QueryStatus(ref notUsed, 0, new OLECMD[0], IntPtr.Zero);

            // Assert
            canExecuteChanged.Should().BeTrue("The command needs to invalidate the previous CanExecute state using CanExecuteChanged event");
        }

        [TestMethod]
        public void BindingController_ChooseWorkflow_Legacy_UsesOldWorkflow()
        {
            // Arrange
            configProvider.ModeToReturn = SonarLintMode.LegacyConnected;
            configProvider.ProjectToReturn = ValidProject;
            

            // Act
            var actual = BindingController.CreateBindingProcess(host, ValidBindingArgs);

            // Assert
            actual.Should().BeOfType<BindingProcessImpl>();
            outputWindowPane.AssertOutputStrings(Strings.Bind_UpdatingLegacyBinding);
            ((BindingProcessImpl)actual).InternalState.IsFirstBinding.Should().BeFalse();
        }

        [TestMethod]
        public void BindingController_ChooseWorkflow_Standalone_UsesNewWorkflow()
        {
            // Arrange
            configProvider.ModeToReturn = SonarLintMode.Standalone;
            configProvider.ProjectToReturn = ValidProject;

            // Act
            var actual = BindingController.CreateBindingProcess(host, ValidBindingArgs);

            // Assert
            actual.Should().BeOfType<BindingProcessImpl>();
            var bindingProcessImpl = (BindingProcessImpl)actual;
            bindingProcessImpl.NuGetBindingOperation.Should().BeOfType<NoOpNuGetBindingOperation>();
            outputWindowPane.AssertOutputStrings(Strings.Bind_FirstTimeBinding);
            bindingProcessImpl.InternalState.IsFirstBinding.Should().BeTrue();
        }

        [TestMethod]
        public void BindingController_ChooseWorkflow_Connected_UsesNewWorkflow()
        {
            // Arrange
            configProvider.ModeToReturn = SonarLintMode.Connected;
            configProvider.ProjectToReturn = ValidProject;

            // Act
            var actual = BindingController.CreateBindingProcess(host, ValidBindingArgs);

            // Assert
            actual.Should().BeOfType<BindingProcessImpl>();
            var bindingProcessImpl = (BindingProcessImpl)actual;
            bindingProcessImpl.NuGetBindingOperation.Should().BeOfType<NoOpNuGetBindingOperation>();
            bindingProcessImpl.InternalState.IsFirstBinding.Should().BeFalse();
            outputWindowPane.AssertOutputStrings(Strings.Bind_UpdatingNewStyleBinding);
        }

        #endregion Tests

        #region Helpers

        private static BindCommandArgs CreateBindingArguments(string key, string name, string serverUri)
        {
            return new BindCommandArgs(key, name, new ConnectionInformation(new Uri(serverUri)));
        }

        private static ServerViewModel CreateServerViewModel()
        {
            return new ServerViewModel(new ConnectionInformation(new Uri("http://123")));
        }

        private BindingController PrepareCommandForExecution()
        {
            this.host.TestStateManager.IsConnected = true;
            BindingController testSubject = this.CreateBindingController();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, true);
            this.monitorSelection.SetContext(VSConstants.UICONTEXT.SolutionExistsAndNotBuildingAndNotDebugging_guid, true);
            ProjectMock project1 = this.solutionMock.AddOrGetProject("project1");
            this.projectSystemHelper.Projects = new[] { project1 };

            // Sanity
            testSubject.BindCommand.CanExecute(CreateBindingArguments("project1", "name1", "http://localhost")).Should().BeTrue("All the requirement should be satisfied for the command to be enabled");

            return testSubject;
        }

        private class TestBindingWorkflow : IBindingWorkflowExecutor
        {
            public BindCommandArgs BindingArgs { get; private set; }

            #region IBindingWorkflowExecutor.

            void IBindingWorkflowExecutor.BindProject(BindCommandArgs bindingArgs)
            {
                bindingArgs.Should().NotBeNull();
                this.BindingArgs = bindingArgs;
            }

            #endregion IBindingWorkflowExecutor.
        }

        private BindingController CreateBindingController()
        {
            return new BindingController(this.host, this.workflow);
        }

        #endregion Helpers
    }
}
