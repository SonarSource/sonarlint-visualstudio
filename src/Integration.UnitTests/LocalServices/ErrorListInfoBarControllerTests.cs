//-----------------------------------------------------------------------
// <copyright file="ErrorListInfoBarControllerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.InfoBar;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ErrorListInfoBarControllerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableHost host;
        private ConfigurableTeamExplorerController teamExplorerController;
        private ConfigurableInfoBarManager infoBarManager;
        private ConfigurableSolutionBindingInformationProvider solutionBindingInformationProvider;
        private ConfigurableVsGeneralOutputWindowPane outputWindow;
        private ConfigurableSolutionBindingSerializer solutionBindingSerializer;
        private ConfigurableStateManager stateManager;

        #region Test plumbing
        [TestInitialize]
        public void TestInit()
        {
            KnownUIContextsAccessor.Reset();
            this.serviceProvider = new ConfigurableServiceProvider();

            this.teamExplorerController = new ConfigurableTeamExplorerController();
            this.infoBarManager = new ConfigurableInfoBarManager();

            IComponentModel componentModel = ConfigurableComponentModel.CreateWithExports(
                new Export[]
                {
                    MefTestHelpers.CreateExport<ITeamExplorerController>(this.teamExplorerController),
                    MefTestHelpers.CreateExport<IInfoBarManager>(this.infoBarManager),
                });
            this.serviceProvider.RegisterService(typeof(SComponentModel), componentModel);

            this.solutionBindingInformationProvider = new ConfigurableSolutionBindingInformationProvider();
            this.serviceProvider.RegisterService(typeof(ISolutionBindingInformationProvider), this.solutionBindingInformationProvider);

            this.outputWindow = new ConfigurableVsGeneralOutputWindowPane();
            this.serviceProvider.RegisterService(typeof(SVsGeneralOutputWindowPane), this.outputWindow);

            this.solutionBindingSerializer = new ConfigurableSolutionBindingSerializer();
            this.serviceProvider.RegisterService(typeof(Persistence.ISolutionBindingSerializer), this.solutionBindingSerializer);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.stateManager = (ConfigurableStateManager)this.host.VisualStateManager;
        }

        #endregion

        #region Tests
        [TestMethod]
        public void ErrorListInfoBarController_ArgChecks()
        {
            Exceptions.Expect<ArgumentNullException>(() => new ErrorListInfoBarController(null));
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBoundAndFullyLoaded_HasNoUnboundProjects()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Verify
            this.outputWindow.AssertOutputStrings(2);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBoundAndFullyLoaded_HasUnboundProjects()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            var testSubject = new ErrorListInfoBarController(this.host);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Verify
            this.outputWindow.AssertOutputStrings(2);
            this.outputWindow.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "unbound.csproj" }, splitter: "\r\n\t ()".ToArray());
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBound_NotFullyLoaded_HasUnboundProjects()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: false);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Verify
            this.outputWindow.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);

            // Act (simulate solution fully loaded event)
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            // Verify
            this.outputWindow.AssertOutputStrings(2);
            this.outputWindow.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "unbound.csproj" }, splitter: "\r\n\t ()".ToArray());
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionNotBound()
        {
            // Setup
            this.IsActiveSolutionBound = false;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Verify
            this.outputWindow.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBecameUnboundAfterRefresh()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            // Act
            testSubject.Refresh();
            this.IsActiveSolutionBound = false;
            RunAsyncAction();

            // Verify
            this.outputWindow.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_RefreshShowInfoBar_ClickClose_UnregisterEvents()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulatClosedEvent();

            // Verify
            infoBar.VerifyAllEventsUnregistered();
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_NoActiveSection_NavigatesToSection()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulatButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_NavigatesToSection()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulatButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_SolutionBindingAreDifferentThatTheOnesUsedForTheInfoBar()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindow.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Change binding 
            this.solutionBindingSerializer.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://server"), "SomeOtherProjectKey");

            // Act
            infoBar.SimulatButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            this.outputWindow.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasDisconnectedActiveSection()
        {
            // Setup
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int bindingCalled = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                bindingCalled++;
                Assert.AreEqual(this.solutionBindingSerializer.CurrentBinding.ProjectKey, vm.Key);
            });
            int refreshCalled = 0;
            this.ConfigureActiveSectionWithRefreshCommand(connection =>
            {
                refreshCalled++;
                Assert.AreEqual(this.solutionBindingSerializer.CurrentBinding.ServerUri, connection.ServerUri);
            });
            int disconnectCalled = 0;
            this.ConfigureActiveSectionWithDisconnectCommand(() =>
            {
                disconnectCalled++;
            });
            this.IsActiveSolutionBound = true;
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindow.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (kick off connection)
            infoBar.SimulatButtonClickEvent();

            // Verify
            Assert.AreEqual(1, refreshCalled, "Expected to connect once");
            Assert.AreEqual(0, disconnectCalled, "Not expected to disconnect");
            Assert.AreEqual(0, bindingCalled, "Not expected to bind yet");

            // Act (connected)
            this.ConfigureProjectViewModel(section);
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            Assert.AreEqual(1, refreshCalled, "Expected to connect once");
            Assert.AreEqual(1, bindingCalled, "Expected to bind once");
            Assert.AreEqual(0, disconnectCalled, "Not expected to disconnect");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindow.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasDisconnectedActiveSection_ConnectCommandIsBusy()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int bindingCalled = 0;
            ProjectViewModel project = null;
            this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                bindingCalled++;
                Assert.AreSame(project, vm);
            });
            int refreshCalled = 0;
            this.ConfigureActiveSectionWithRefreshCommand(connection =>
            {
                refreshCalled++;
            }, connection => false);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindow.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (kick off connection)
            infoBar.SimulatButtonClickEvent();

            // Verify
            Assert.AreEqual(0, refreshCalled, "Expected to connect once");
            Assert.AreEqual(0, bindingCalled, "Not expected to bind yet");
            this.outputWindow.AssertOutputStrings(1);
            infoBar.VerifyAllEventsRegistered();
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_NotBusy()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int bindExecuted = 0;
            bool canExecute = false;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                bindExecuted++;
                Assert.AreSame(project, vm);
            }, vm => canExecute);
            this.ConfigureActiveSectionWithRefreshCommand(c =>
            {
                Assert.Fail("Refresh is not expected to be called");
            });
            this.ConfigureActiveSectionWithDisconnectCommand(() =>
            {
                Assert.Fail("Disconnect is not expected to be called");
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindow.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (command disabled)
            infoBar.SimulatButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
            Assert.AreEqual(0, bindExecuted, "Update was not expected to be executed");
            this.outputWindow.AssertOutputStrings(1);

            // Act (command enabled)
            canExecute = true;
            infoBar.SimulatButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(2);
            Assert.AreEqual(1, bindExecuted, "Update was expected to be executed");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindow.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_IsBusy()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                executed++;
                Assert.AreSame(project, vm);
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (command enabled)
            infoBar.SimulatButtonClickEvent();

            // Verify
            Assert.AreEqual(0, executed, "Busy, should not be executed");

            // Act
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            Assert.AreEqual(1, executed, "Update was expected to be executed");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_WasBusyAndInfoBarClosed()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                executed++;
                Assert.AreSame(project, vm);
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (command enabled)
            infoBar.SimulatButtonClickEvent();

            // Verify
            Assert.AreEqual(0, executed, "Busy, should not be executed");

            // Act (close the current info bar)
            testSubject.Reset();
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            Assert.AreEqual(1, executed, "Once started, the process can only be cancelled from team explorer, closing the info bar should not impact the running update execution");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_WasBusyAndSectionClosed()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                executed++;
                Assert.AreSame(project, vm);
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);

            // Act (command enabled)
            infoBar.SimulatButtonClickEvent();

            // Verify
            Assert.AreEqual(0, executed, "Busy, should not be executed");

            // Act (close the current section)
            this.host.ClearActiveSection();
            this.stateManager.SetAndInvokeBusyChanged(false);
            RunAsyncAction();

            // Verify
            Assert.AreEqual(0, executed, "Update was not expected to be executed since there is not ActiveSection");
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsRegistered(); // Should be usable
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_ConnectedToADifferentServer()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int refreshCalled = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithRefreshCommand(c =>
            {
                Assert.AreEqual(this.solutionBindingSerializer.CurrentBinding.ServerUri, c.ServerUri);
                refreshCalled++;
            });
            int disconnectCalled = 0;
            this.ConfigureActiveSectionWithDisconnectCommand(()=>
            {
                disconnectCalled++;
            });
            int bindCalled = 0;
            this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                Assert.AreEqual(this.solutionBindingSerializer.CurrentBinding.ProjectKey, vm.Key);
                bindCalled++;
            });

            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);

            // Connect to a different server
            this.ConfigureProjectViewModel(section, new Uri("http://SomeOtherServer"), "someOtherProjectKey");

            // Act 
            infoBar.SimulatButtonClickEvent();

            // Verify
            Assert.AreEqual(1, disconnectCalled, "Should have been disconnected");
            Assert.AreEqual(1, refreshCalled, "Also expected to connect to the right server");
            Assert.AreEqual(0, bindCalled, "Busy, should not be executed");

            // Simulate that connected to the project that is bound to
            this.ConfigureProjectViewModel(section);

            // Act
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            Assert.AreEqual(1, bindCalled, "Should be bound");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_MoreThanOnce()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            int bindCommandExecuted = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm => { bindCommandExecuted++; });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);

            // Act (command enabled)
            infoBar.SimulatButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);

            // Act (click again)
            infoBar.SimulatButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsRegistered(); // Should be usable

            // Act (not busy anymore)
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            Assert.AreEqual(1, bindCommandExecuted, "Expecting the command to be executed only once");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Reset()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm => { });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindow.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act 
            testSubject.Reset();

            // Verify
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindow.AssertOutputStrings(0);
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);
        }

        #endregion

        #region Test helpers
        private ConfigurableSectionController ConfigureActiveSectionWithBindCommand(Action<ProjectViewModel> commandAction, Predicate<ProjectViewModel> canExecuteCommand = null)
        {
            var section = this.host.ActiveSection as ConfigurableSectionController;
            if (section == null)
            {
                section = ConfigurableSectionController.CreateDefault();
            }
            section.ViewModel.State = this.host.VisualStateManager.ManagedState;
            section.BindCommand = new RelayCommand<ProjectViewModel>(pvm=>
            {
                commandAction(pvm);
                this.stateManager.SetAndInvokeBusyChanged(true);// Simulate product
            }, canExecuteCommand);
            this.host.SetActiveSection(section);

            return section;
        }

        private ConfigurableSectionController ConfigureActiveSectionWithRefreshCommand(Action<ConnectionInformation> commandAction, Predicate<ConnectionInformation> canExecuteCommand = null)
        {
            var section = this.host.ActiveSection as ConfigurableSectionController;
            if (section == null)
            {
                section = ConfigurableSectionController.CreateDefault();
            }
            section.ViewModel.State = this.host.VisualStateManager.ManagedState;
            section.RefreshCommand = new RelayCommand<ConnectionInformation>(ci =>
            {
                commandAction(ci);
                this.stateManager.SetAndInvokeBusyChanged(true);// Simulate product
            }, canExecuteCommand);
            this.host.SetActiveSection(section);

            return section;
        }

        private ConfigurableSectionController ConfigureActiveSectionWithDisconnectCommand(Action commandAction)
        {
            var section = this.host.ActiveSection as ConfigurableSectionController;
            if (section == null)
            {
                section = ConfigurableSectionController.CreateDefault();
            }
            section.ViewModel.State = this.host.VisualStateManager.ManagedState;
            section.DisconnectCommand = new RelayCommand(commandAction);
            this.host.SetActiveSection(section);

            return section;
        }


        private ProjectViewModel ConfigureProjectViewModel(ConfigurableSectionController section)
        {
            var vm = this.ConfigureProjectViewModel(section, this.solutionBindingSerializer.CurrentBinding?.ServerUri, this.solutionBindingSerializer.CurrentBinding?.ProjectKey);
            if (this.solutionBindingSerializer.CurrentBinding != null)
            {
                vm.IsBound = true;
            }
            return vm;
        }

        private ProjectViewModel ConfigureProjectViewModel(ConfigurableSectionController section, Uri serverUri, string projectKey)
        {
            if (serverUri == null)
            {
                Assert.Inconclusive("Test setup: the server uri is not valid");
            }

            if (string.IsNullOrWhiteSpace(projectKey))
            {
                Assert.Inconclusive("Test setup: the project key is not valid");
            }

            section.ViewModel.State.ConnectedServers.Clear();
            var serverVM = new ServerViewModel(new Integration.Service.ConnectionInformation(serverUri));
            section.ViewModel.State.ConnectedServers.Add(serverVM);
            var projectVM = new ProjectViewModel(serverVM, new Integration.Service.ProjectInformation { Key = projectKey });
            serverVM.Projects.Add(projectVM);

            return projectVM;
        }

        private bool IsActiveSolutionBound
        {
            get
            {
                return this.solutionBindingInformationProvider.SolutionBound;
            }
            set
            {
                this.solutionBindingInformationProvider.SolutionBound = value;
                this.solutionBindingSerializer.CurrentBinding = value ? new Persistence.BoundSonarQubeProject(new Uri("http://Server"), "boundProjectKey") : null;
            }
        }

        /// <summary>
        /// Runs a single queued action that was scheduled on the current dispatcher using <see cref="Dispatcher.BeginInvoke(Delegate, DispatcherPriority, object[])"/>
        /// </summary>
        /// <param name="priority">The priority in which the action was scheduled</param>
        private static void RunAsyncAction(DispatcherPriority priority = DispatcherPriority.ContextIdle)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(priority,
                new DispatcherOperationCallback(f =>
                {
                    ((DispatcherFrame)f).Continue = false;
                    return null;
                }), frame);
            Dispatcher.PushFrame(frame);
        }

        private static void SetSolutionExistsAndFullyLoadedContextState(bool isActive)
        {
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, isActive);
        }

        private static void VerifyInfoBar(ConfigurableInfoBar infoBar)
        {
            Assert.AreEqual(Strings.SonarLintInfoBarUnboundProjectsMessage, infoBar.Message);
            Assert.AreEqual(Strings.SonarLintInfoBarUpdateCommandText, infoBar.ButtonText);
            Assert.AreEqual(KnownMonikers.RuleWarning, infoBar.Image);
        }
        #endregion
    }
}
