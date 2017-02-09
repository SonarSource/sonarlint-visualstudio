/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Windows.Threading;
using FluentAssertions;
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
        private ConfigurableVsOutputWindowPane outputWindowPane;
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

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.solutionBindingSerializer = new ConfigurableSolutionBindingSerializer();
            this.serviceProvider.RegisterService(typeof(Persistence.ISolutionBindingSerializer), this.solutionBindingSerializer);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.stateManager = (ConfigurableStateManager)this.host.VisualStateManager;
        }

        #endregion Test plumbing

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
            this.ConfigureLoadedSolution(hasUnboundProject: false);
            // Set project system with no filtered project, to quickly stop SonarQubeQualityProfileBackgroundProcessor
            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Verify
            this.outputWindowPane.AssertOutputStrings(2);
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
            this.outputWindowPane.AssertOutputStrings(2);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "unbound.csproj" }, splitter: "\r\n\t ()".ToArray());
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBound_NotFullyLoaded_HasUnboundProjects()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            SetSolutionExistsAndFullyLoadedContextState(isActive: false);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Verify
            this.outputWindowPane.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);

            // Act (simulate solution fully loaded event)
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            // Verify
            this.outputWindowPane.AssertOutputStrings(2);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "unbound.csproj" }, splitter: "\r\n\t ()".ToArray());
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionNotBound()
        {
            // Setup
            this.IsActiveSolutionBound = false;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution(hasUnboundProject: false);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Verify
            this.outputWindowPane.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBecameUnboundAfterRefresh()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution(hasUnboundProject: false);

            // Act
            testSubject.Refresh();
            this.IsActiveSolutionBound = false;
            RunAsyncAction();

            // Verify
            this.outputWindowPane.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_CurrentBackgroundProcessorCancellation()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution(hasUnboundProject: false);
            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);
            var project = new ProjectMock("project.proj");
            project.SetCSProjectKind();
            projectSystem.FilteredProjects = new[] { project };
            this.solutionBindingSerializer.CurrentBinding.Profiles = new Dictionary<Language, Persistence.ApplicableQualityProfile>();
            this.solutionBindingSerializer.CurrentBinding.Profiles[Language.CSharp] = new Persistence.ApplicableQualityProfile
            {
                ProfileKey = "Profile",
                ProfileTimestamp = DateTime.Now
            };
            var sqService = new ConfigurableSonarQubeServiceWrapper();
            this.host.SonarQubeService = sqService;
            sqService.ReturnProfile[Language.CSharp] = new QualityProfile();

            // Act
            testSubject.ProcessSolutionBinding();

            // Verify
            testSubject.CurrentBackgroundProcessor?.BackgroundTask.Should().NotBeNull("Background task is expected");
            testSubject.CurrentBackgroundProcessor.BackgroundTask.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue("Timeout waiting for the background task");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);

            // Act (refresh again and  let the blocked UI thread run to completion)
            testSubject.ProcessSolutionBinding();
            DispatcherHelper.DispatchFrame(DispatcherPriority.Normal);
            this.IsActiveSolutionBound = false;

            // Verify that no info bar was added (due to the last action in which the state will not cause the info bar to appear)
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_RefreshShowInfoBar_ClickClose_UnregisterEvents()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulateClosedEvent();

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
            this.ConfigureLoadedSolution();
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulateButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_NavigatesToSection()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulateButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_SolutionBindingAreDifferentThatTheOnesUsedForTheInfoBar()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Change binding
            this.solutionBindingSerializer.CurrentBinding = new Persistence.BoundSonarQubeProject(new Uri("http://server"), "SomeOtherProjectKey");

            // Act
            infoBar.SimulateButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            this.outputWindowPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasDisconnectedActiveSection()
        {
            // Setup
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            int bindingCalled = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                bindingCalled++;
                vm.Key.Should().Be(this.solutionBindingSerializer.CurrentBinding.ProjectKey);
            });
            int refreshCalled = 0;
            this.ConfigureActiveSectionWithRefreshCommand(connection =>
            {
                refreshCalled++;
                connection.ServerUri.Should().Be(this.solutionBindingSerializer.CurrentBinding.ServerUri);
            });
            int disconnectCalled = 0;
            this.ConfigureActiveSectionWithDisconnectCommand(() =>
            {
                disconnectCalled++;
            });
            this.IsActiveSolutionBound = true;
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (kick off connection)
            infoBar.SimulateButtonClickEvent();

            // Verify
            refreshCalled.Should().Be(1, "Expected to connect once");
            disconnectCalled.Should().Be(0, "Not expected to disconnect");
            bindingCalled.Should().Be(0, "Not expected to bind yet");

            // Act (connected)
            this.ConfigureProjectViewModel(section);
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            refreshCalled.Should().Be(1, "Expected to connect once");
            bindingCalled.Should().Be(1, "Expected to bind once");
            disconnectCalled.Should().Be(0, "Not expected to disconnect");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindowPane.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasDisconnectedActiveSection_ConnectCommandIsBusy()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            int bindingCalled = 0;
            ProjectViewModel project = null;
            this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                bindingCalled++;
                vm.Should().Be(project);
            });
            int refreshCalled = 0;
            this.ConfigureActiveSectionWithRefreshCommand(connection =>
            {
                refreshCalled++;
            }, connection => false);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (kick off connection)
            infoBar.SimulateButtonClickEvent();

            // Verify
            refreshCalled.Should().Be(0, "Expected to connect once");
            bindingCalled.Should().Be(0, "Not expected to bind yet");
            this.outputWindowPane.AssertOutputStrings(1);
            infoBar.VerifyAllEventsRegistered();
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_NotBusy()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            int bindExecuted = 0;
            bool canExecute = false;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                bindExecuted++;
                vm.Should().Be(project);
            }, vm => canExecute);
            this.ConfigureActiveSectionWithRefreshCommand(c =>
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Refresh is not expected to be called");
            });
            this.ConfigureActiveSectionWithDisconnectCommand(() =>
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Disconnect is not expected to be called");
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (command disabled)
            infoBar.SimulateButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);
            bindExecuted.Should().Be(0, "Update was not expected to be executed");
            this.outputWindowPane.AssertOutputStrings(1);

            // Act (command enabled)
            canExecute = true;
            infoBar.SimulateButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(2);
            bindExecuted.Should().Be(1, "Update was expected to be executed");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindowPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_IsBusy()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                executed++;
                vm.Should().Be(project);
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (command enabled)
            infoBar.SimulateButtonClickEvent();

            // Verify
            executed.Should().Be(0, "Busy, should not be executed");

            // Act
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            executed.Should().Be(1, "Update was expected to be executed");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_WasBusyAndInfoBarClosed()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                executed++;
                vm.Should().Be(project);
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (command enabled)
            infoBar.SimulateButtonClickEvent();

            // Verify
            executed.Should().Be(0, "Busy, should not be executed");

            // Act (close the current info bar)
            testSubject.Reset();
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            executed.Should().Be(1, "Once started, the process can only be canceled from team explorer, closing the info bar should not impact the running update execution");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_WasBusyAndSectionClosed()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm =>
            {
                executed++;
                vm.Should().Be(project);
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
            infoBar.SimulateButtonClickEvent();

            // Verify
            executed.Should().Be(0, "Busy, should not be executed");

            // Act (close the current section)
            this.host.ClearActiveSection();
            this.stateManager.SetAndInvokeBusyChanged(false);
            RunAsyncAction();

            // Verify
            executed.Should().Be(0, "Update was not expected to be executed since there is not ActiveSection");
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsRegistered(); // Should be usable
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_ConnectedToADifferentServer()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            int refreshCalled = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithRefreshCommand(c =>
            {
                c.ServerUri.Should().Be(this.solutionBindingSerializer.CurrentBinding.ServerUri);
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
                vm.Key.Should().Be(this.solutionBindingSerializer.CurrentBinding.ProjectKey);
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
            infoBar.SimulateButtonClickEvent();

            // Verify
            disconnectCalled.Should().Be(1, "Should have been disconnected");
            refreshCalled.Should().Be(1, "Also expected to connect to the right server");
            bindCalled.Should().Be(0, "Busy, should not be executed");

            // Simulate that connected to the project that is bound to
            this.ConfigureProjectViewModel(section);

            // Act
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Verify
            bindCalled.Should().Be(1, "Should be bound");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_MoreThanOnce()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
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
            infoBar.SimulateButtonClickEvent();

            // Verify
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(1);

            // Act (click again)
            infoBar.SimulateButtonClickEvent();

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
            bindCommandExecuted.Should().Be(1, "Expecting the command to be executed only once");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Reset()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm => { });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            testSubject.Reset();

            // Verify
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindowPane.AssertOutputStrings(0);
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Dispose()
        {
            // Setup
            this.IsActiveSolutionBound = true;
            var testSubject = new ErrorListInfoBarController(this.host);
            this.ConfigureLoadedSolution();
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(vm => { });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            testSubject.Dispose();

            // Verify
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindowPane.AssertOutputStrings(0);
            this.teamExplorerController.AssertExpectedNumCallsShowConnectionsPage(0);
        }

        #endregion Tests

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
                FluentAssertions.Execution.Execute.Assertion.FailWith("Test setup: the server uri is not valid");
            }

            if (string.IsNullOrWhiteSpace(projectKey))
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Test setup: the project key is not valid");
            }

            section.ViewModel.State.ConnectedServers.Clear();
            var serverVM = new ServerViewModel(new ConnectionInformation(serverUri));
            section.ViewModel.State.ConnectedServers.Add(serverVM);
            var projectVM = new ProjectViewModel(serverVM, new ProjectInformation { Key = projectKey });
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

        private void ConfigureLoadedSolution(bool hasBoundProject = true, bool hasUnboundProject = true)
        {
            if (hasBoundProject)
            {
                this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            }

            if (hasUnboundProject)
            {
                this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            }

            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
        }

        private static void SetSolutionExistsAndFullyLoadedContextState(bool isActive)
        {
            KnownUIContextsAccessor.MonitorSelectionService.SetContext(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, isActive);
        }

        private static void VerifyInfoBar(ConfigurableInfoBar infoBar)
        {
            infoBar.Message.Should().Be(Strings.SonarLintInfoBarUnboundProjectsMessage);
            infoBar.ButtonText.Should().Be(Strings.SonarLintInfoBarUpdateCommandText);
            infoBar.Image.Should().Be(KnownMonikers.RuleWarning);
        }

        #endregion Test helpers
    }
}