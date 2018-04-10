/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.InfoBar;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

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
        private ConfigurableConfigurationProvider configProvider;
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
                    MefTestHelpers.CreateExport<ITelemetryLogger>(new ConfigurableTelemetryLogger())
                });
            this.serviceProvider.RegisterService(typeof(SComponentModel), componentModel);

            this.solutionBindingInformationProvider = new ConfigurableSolutionBindingInformationProvider();

            var outputWindow = new ConfigurableVsOutputWindow();
            this.outputWindowPane = outputWindow.GetOrCreateSonarLintPane();
            this.serviceProvider.RegisterService(typeof(SVsOutputWindow), outputWindow);

            this.configProvider = new ConfigurableConfigurationProvider();
            this.serviceProvider.RegisterService(typeof(IConfigurationProvider), this.configProvider);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.stateManager = (ConfigurableStateManager)this.host.VisualStateManager;
        }

        #endregion Test plumbing

        #region Tests

        [TestMethod]
        public void ErrorListInfoBarController_Ctor_NullHost_Throws()
        {
            // Arrange
            Action act = () => new ErrorListInfoBarController(null, this.solutionBindingInformationProvider);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Ctor_NullBindingProvider_Throws()
        {
            // Arrange
            Action act = () => new ErrorListInfoBarController(this.host, null);

            // Act & Assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingInformationProvider");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBoundAndFullyLoaded_HasNoUnboundProjects()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution(hasUnboundProject: false);
            // Set project system with no filtered project, to quickly stop SonarQubeQualityProfileBackgroundProcessor
            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            this.outputWindowPane.AssertOutputStrings(2);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBoundAndFullyLoaded_HasUnboundProjects()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            this.solutionBindingInformationProvider.BoundProjects = new[] { new ProjectMock("bound.csproj") };
            this.solutionBindingInformationProvider.UnboundProjects = new[] { new ProjectMock("unbound.csproj") };
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            this.outputWindowPane.AssertOutputStrings(2);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "unbound.csproj" }, splitter: "\r\n\t ()".ToArray());
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBound_NotFullyLoaded_HasUnboundProjects()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            SetSolutionExistsAndFullyLoadedContextState(isActive: false);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            this.outputWindowPane.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);

            // Act (simulate solution fully loaded event)
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            // Assert
            this.outputWindowPane.AssertOutputStrings(2);
            this.outputWindowPane.AssertMessageContainsAllWordsCaseSensitive(1, new[] { "unbound.csproj" }, splitter: "\r\n\t ()".ToArray());
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionNotBound()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.Standalone);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution(hasUnboundProject: true);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            this.outputWindowPane.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolution_NewConnectedMode()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.Connected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution(hasUnboundProject: true);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            this.outputWindowPane.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBecameUnboundAfterRefresh()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution(hasUnboundProject: false);

            // Act
            testSubject.Refresh();
            this.SetBindingMode(SonarLintMode.Standalone);
            RunAsyncAction();

            // Assert
            this.outputWindowPane.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_CurrentBackgroundProcessorCancellation()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution(hasUnboundProject: false);
            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);
            var project = new ProjectMock("project.proj");
            project.SetCSProjectKind();
            projectSystem.FilteredProjects = new[] { project };
            this.configProvider.ProjectToReturn.Profiles = new Dictionary<Language, Persistence.ApplicableQualityProfile>();
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new Persistence.ApplicableQualityProfile
            {
                ProfileKey = "Profile",
                ProfileTimestamp = DateTime.Now
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;

            // Act
            testSubject.ProcessSolutionBinding();

            // Assert
            testSubject.CurrentBackgroundProcessor?.BackgroundTask.Should().NotBeNull("Background task is expected");
            testSubject.CurrentBackgroundProcessor.BackgroundTask.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue("Timeout waiting for the background task");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);

            // Act (refresh again and  let the blocked UI thread run to completion)
            testSubject.ProcessSolutionBinding();
            DispatcherHelper.DispatchFrame(DispatcherPriority.Normal);
            this.SetBindingMode(SonarLintMode.Standalone);

            // Assert that no info bar was added (due to the last action in which the state will not cause the info bar to appear)
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_RefreshShowInfoBar_ClickClose_UnregisterEvents()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulateClosedEvent();

            // Assert
            infoBar.VerifyAllEventsUnregistered();
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_NoActiveSection_NavigatesToSection()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_NavigatesToSection()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_SolutionBindingAreDifferentThatTheOnesUsedForTheInfoBar_NoOp()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Change binding
            this.configProvider.ProjectToReturn = new Persistence.BoundSonarQubeProject(new Uri("http://server"), "SomeOtherProjectKey");

            // Act
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            this.outputWindowPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_NoLongerInLegacyConnected_NoOp()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Change binding
            // Note: in practice we can't switch from legacy to new connected mode, but the important
            // thing for this test is that the solution isn't in legacy mode
            this.SetBindingMode(SonarLintMode.Connected);

            // Act
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            this.outputWindowPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasDisconnectedActiveSection()
        {
            // Arrange
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int bindingCalled = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args =>
            {
                bindingCalled++;
                args.ProjectKey.Should().Be(this.configProvider.ProjectToReturn.ProjectKey);
            });
            int refreshCalled = 0;
            this.ConfigureActiveSectionWithRefreshCommand(connection =>
            {
                refreshCalled++;
                connection.ServerUri.Should().Be(this.configProvider.ProjectToReturn.ServerUri);
            });
            int disconnectCalled = 0;
            this.ConfigureActiveSectionWithDisconnectCommand(() =>
            {
                disconnectCalled++;
            });
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (kick off connection)
            infoBar.SimulateButtonClickEvent();

            // Assert
            refreshCalled.Should().Be(1, "Expected to connect once");
            disconnectCalled.Should().Be(0, "Not expected to disconnect");
            bindingCalled.Should().Be(0, "Not expected to bind yet");

            // Act (connected)
            this.ConfigureProjectViewModel(section);
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Assert
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
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int bindingCalled = 0;
            ProjectViewModel project = null;
            this.ConfigureActiveSectionWithBindCommand(args =>
            {
                bindingCalled++;
                args.ProjectKey.Should().Be(project.Key);
                args.ProjectName.Should().Be(project.ProjectName);
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

            // Assert
            refreshCalled.Should().Be(0, "Expected to connect once");
            bindingCalled.Should().Be(0, "Not expected to bind yet");
            this.outputWindowPane.AssertOutputStrings(1);
            infoBar.VerifyAllEventsRegistered();
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_NotBusy()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int bindExecuted = 0;
            bool canExecute = false;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args =>
            {
                bindExecuted++;
                args.ProjectKey.Should().Be(project.Key);
                args.ProjectName.Should().Be(project.ProjectName);
            }, args => canExecute);
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

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(1);
            bindExecuted.Should().Be(0, "Update was not expected to be executed");
            this.outputWindowPane.AssertOutputStrings(1);

            // Act (command enabled)
            canExecute = true;
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(2);
            bindExecuted.Should().Be(1, "Update was expected to be executed");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindowPane.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_IsBusy()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args =>
            {
                executed++;
                args.ProjectKey.Should().Be(project.Key);
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

            // Assert
            executed.Should().Be(0, "Busy, should not be executed");

            // Act
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Assert
            executed.Should().Be(1, "Update was expected to be executed");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_WasBusyAndInfoBarClosed()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args =>
            {
                executed++;
                args.ProjectKey.Should().Be(project.Key);
                args.ProjectName.Should().Be(project.ProjectName);
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

            // Assert
            executed.Should().Be(0, "Busy, should not be executed");

            // Act (close the current info bar)
            testSubject.Reset();
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Assert
            executed.Should().Be(1, "Once started, the process can only be canceled from team explorer, closing the info bar should not impact the running update execution");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasActiveSection_WasBusyAndSectionClosed()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int executed = 0;
            ProjectViewModel project = null;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args =>
            {
                executed++;
            });
            project = this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);

            // Act (command enabled)
            infoBar.SimulateButtonClickEvent();

            // Assert
            executed.Should().Be(0, "Busy, should not be executed");

            // Act (close the current section)
            this.host.ClearActiveSection();
            this.stateManager.SetAndInvokeBusyChanged(false);
            RunAsyncAction();

            // Assert
            executed.Should().Be(0, "Update was not expected to be executed since there is not ActiveSection");
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsRegistered(); // Should be usable
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_ConnectedToADifferentServer()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int refreshCalled = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithRefreshCommand(c =>
            {
                c.ServerUri.Should().Be(this.configProvider.ProjectToReturn.ServerUri);
                refreshCalled++;
            });
            int disconnectCalled = 0;
            this.ConfigureActiveSectionWithDisconnectCommand(() =>
            {
                disconnectCalled++;
            });
            int bindCalled = 0;
            this.ConfigureActiveSectionWithBindCommand(args =>
            {
                args.ProjectKey.Should().Be(this.configProvider.ProjectToReturn.ProjectKey);
                bindCalled++;
            });

            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);

            // Connect to a different server
            this.ConfigureProjectViewModel(section, new Uri("http://SomeOtherServer"), "someOtherProjectKey");

            // Act
            infoBar.SimulateButtonClickEvent();

            // Assert
            disconnectCalled.Should().Be(1, "Should have been disconnected");
            refreshCalled.Should().Be(1, "Also expected to connect to the right server");
            bindCalled.Should().Be(0, "Busy, should not be executed");

            // Simulate that connected to the project that is bound to
            this.ConfigureProjectViewModel(section);

            // Act
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Assert
            bindCalled.Should().Be(1, "Should be bound");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_MoreThanOnce()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            int bindCommandExecuted = 0;
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args => { bindCommandExecuted++; });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.stateManager.SetAndInvokeBusyChanged(true);

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);

            // Act (command enabled)
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(1);

            // Act (click again)
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(1);
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsRegistered(); // Should be usable

            // Act (not busy anymore)
            this.stateManager.SetAndInvokeBusyChanged(false);

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(1);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            bindCommandExecuted.Should().Be(1, "Expecting the command to be executed only once");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Reset()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args => { });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            testSubject.Reset();

            // Assert
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindowPane.AssertOutputStrings(0);
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Dispose()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = new ErrorListInfoBarController(this.host, this.solutionBindingInformationProvider);
            this.ConfigureLoadedSolution();
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args => { });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            this.outputWindowPane.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            testSubject.Dispose();

            // Assert
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            this.outputWindowPane.AssertOutputStrings(0);
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
        }

        #endregion Tests

        #region Test helpers

        private ConfigurableSectionController ConfigureActiveSectionWithBindCommand(Action<BindCommandArgs> commandAction, Predicate<BindCommandArgs> canExecuteCommand = null)
        {
            var section = this.host.ActiveSection as ConfigurableSectionController;
            if (section == null)
            {
                section = ConfigurableSectionController.CreateDefault();
            }
            section.ViewModel.State = this.host.VisualStateManager.ManagedState;
            section.BindCommand = new RelayCommand<BindCommandArgs>(args =>
            {
                commandAction(args);
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
            var vm = this.ConfigureProjectViewModel(section, this.configProvider.ProjectToReturn?.ServerUri, this.configProvider.ProjectToReturn?.ProjectKey);
            if (this.configProvider.ProjectToReturn != null)
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
            var projectVM = new ProjectViewModel(serverVM, new SonarQubeProject(projectKey, ""));
            serverVM.Projects.Add(projectVM);

            return projectVM;
        }

        private void SetBindingMode(SonarLintMode mode)
        {
            this.configProvider.ModeToReturn = mode;
            this.configProvider.ProjectToReturn = mode == SonarLintMode.Standalone ? null : new Persistence.BoundSonarQubeProject(new Uri("http://Server"), "boundProjectKey");
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