/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.CFamily;
using SonarLint.VisualStudio.Core.InfoBar;
using SonarLint.VisualStudio.Core.Secrets;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class ErrorListInfoBarControllerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableHost host;
        private ConfigurableTeamExplorerController teamExplorerController;
        private ConfigurableInfoBarManager infoBarManager;
        private Mock<IBindingChecker> bindingRequiredIndicator;
        private ConfigurableConfigurationProvider configProvider;
        private ConfigurableStateManager stateManager;
        private Mock<IKnownUIContexts> knownUIContexts;
        private TestLogger logger;

        #region Test plumbing

        [TestInitialize]
        public void TestInit()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();

            this.serviceProvider = new ConfigurableServiceProvider();

            this.teamExplorerController = new ConfigurableTeamExplorerController();
            this.infoBarManager = new ConfigurableInfoBarManager();

            IComponentModel componentModel = ConfigurableComponentModel.CreateWithExports(
                new Export[]
                {
                    MefTestHelpers.CreateExport<ITeamExplorerController>(this.teamExplorerController),
                    MefTestHelpers.CreateExport<IInfoBarManager>(this.infoBarManager),
                    MefTestHelpers.CreateExport<IProjectToLanguageMapper>(new ProjectToLanguageMapper(Mock.Of<ICMakeProjectTypeIndicator>(), Mock.Of<IProjectLanguageIndicator>(), Mock.Of<IConnectedModeSecrets>()))
                });
            this.serviceProvider.RegisterService(typeof(SComponentModel), componentModel);

            this.bindingRequiredIndicator = new Mock<IBindingChecker>();

            this.configProvider = new ConfigurableConfigurationProvider {FolderPathToReturn = "c:\\test"};

            this.serviceProvider.RegisterService(typeof(IConfigurationProviderService), this.configProvider);

            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.stateManager = (ConfigurableStateManager)this.host.VisualStateManager;
            this.logger = new TestLogger();
            host.Logger = logger;
        
            this.knownUIContexts = new Mock<IKnownUIContexts>();
        }

        #endregion Test plumbing

        #region Tests

        [TestMethod]
        public void ErrorListInfoBarController_Ctor_NullHost_Throws()
        {
            Action act = () => new ErrorListInfoBarController(null, bindingRequiredIndicator.Object, this.logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("host");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Ctor_NullBindingProvider_Throws()
        {
            Action act = () => new ErrorListInfoBarController(this.host, null, this.logger);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bindingChecker");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Ctor_NullLogger_Throws()
        {
            Action act = () => new ErrorListInfoBarController(this.host, bindingRequiredIndicator.Object, null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBoundAndFullyLoaded_HasNoUnboundProjects()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution(hasUnboundProject: false);
            // Set project system with no filtered project, to quickly stop SonarQubeQualityProfileBackgroundProcessor
            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            logger.AssertOutputStrings(2);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_Legacy_ActiveSolutionBoundAndFullyLoaded_HasUnboundProjects()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            bindingRequiredIndicator.Setup(x => x.IsBindingUpdateRequired(It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var testSubject = CreateTestSubject();

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            logger.AssertOutputStrings(1);
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_Connected_ActiveSolutionBoundAndFullyLoaded_HasUnboundProjects()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.Connected);
            SetSolutionExistsAndFullyLoadedContextState(isActive: true);

            bindingRequiredIndicator.Setup(x => x.IsBindingUpdateRequired(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            
            var testSubject = CreateTestSubject();

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            logger.AssertOutputStrings(1);
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public async Task ErrorListInfoBarController_Refresh_ActiveSolutionBound_NotFullyLoaded_HasUnboundProjects()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution();

            var uiContext = SetSolutionExistsAndFullyLoadedContextState(isActive: false);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            // Action should have been queued because the context is not active
            uiContext.Verify(x => x.WhenActivated(It.IsAny<Action>()), Times.Once());

            logger.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);

            // Act (simulate solution fully loaded event)
            uiContext = SetSolutionExistsAndFullyLoadedContextState(isActive: true);
            await SimulateUIContextIsActivated(testSubject);

            // Assert
            // Action should not have been queued because the context is active
            uiContext.Verify(x => x.WhenActivated(It.IsAny<Action>()), Times.Never);

            logger.AssertOutputStrings(1);
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionNotBound()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.Standalone);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution(hasUnboundProject: true);

            // Act
            testSubject.Refresh();
            RunAsyncAction();

            // Assert
            logger.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Refresh_ActiveSolutionBecameUnboundAfterRefresh()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution(hasUnboundProject: false);

            // Act
            testSubject.Refresh();
            this.SetBindingMode(SonarLintMode.Standalone);
            RunAsyncAction();

            // Assert
            logger.AssertOutputStrings(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public async Task ErrorListInfoBarController_CurrentBackgroundProcessorCancellation()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution(hasUnboundProject: false);
            var projectSystem = new ConfigurableVsProjectSystemHelper(this.serviceProvider);
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), projectSystem);
            var project = new ProjectMock("project.proj");
            project.SetCSProjectKind();
            projectSystem.FilteredProjects = new[] { project };
            this.configProvider.ProjectToReturn.Profiles = new Dictionary<Language, ApplicableQualityProfile>();
            this.configProvider.ProjectToReturn.Profiles[Language.CSharp] = new ApplicableQualityProfile
            {
                ProfileKey = "Profile",
                ProfileTimestamp = DateTime.Now
            };
            this.configProvider.ModeToReturn = SonarLintMode.LegacyConnected;

            // Act
            await testSubject.ProcessSolutionBindingAsync();

            // Assert
            testSubject.CurrentBackgroundProcessor?.BackgroundTask.Should().NotBeNull("Background task is expected");
            testSubject.CurrentBackgroundProcessor.BackgroundTask.Wait(TimeSpan.FromSeconds(2)).Should().BeTrue("Timeout waiting for the background task");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);

            // Act (refresh again and let the blocked UI thread run to completion)
            await testSubject.ProcessSolutionBindingAsync();
            DispatcherHelper.DispatchFrame(DispatcherPriority.Normal);
            this.SetBindingMode(SonarLintMode.Standalone);

            // Assert that no info bar was added (due to the last action in which the state will not cause the info bar to appear)
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public async Task ErrorListInfoBarController_Refresh_NonCriticalExceptionIsSuppressed()
        {
            var host = CreateHostWithThrowingConfigProvider(new COMException("thrown by test code"));
            var testSubject = CreateTestSubject(host);

            // Act - should not throw
            await testSubject.ProcessSolutionBindingAsync();

            logger.AssertPartialOutputStringExists("thrown by test code");
        }

        [TestMethod]
        public async Task ErrorListInfoBarController_Refresh_CriticalExceptionIsNotSuppressed()
        {
            var host = CreateHostWithThrowingConfigProvider(new StackOverflowException("thrown by test code"));
            var testSubject = CreateTestSubject(host);

            // Act
            Func<Task> act = async () => await testSubject.ProcessSolutionBindingAsync();
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Contain("thrown by test code");
        }

        [TestMethod]
        public void ErrorListInfoBarController_RefreshShowInfoBar_ClickClose_UnregisterEvents()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();
            logger.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Change binding
            this.configProvider.ProjectToReturn = new BoundSonarQubeProject(new Uri("http://server"), "SomeOtherProjectKey", "projectName");

            // Act
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            logger.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_NoLongerInConnected_NoOp()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution();
            this.host.SetActiveSection(ConfigurableSectionController.CreateDefault());
            testSubject.Refresh();
            RunAsyncAction();
            logger.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Change binding
            this.SetBindingMode(SonarLintMode.Standalone);

            // Act
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            logger.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasDisconnectedActiveSection()
        {
            // Arrange
            var testSubject = CreateTestSubject();
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
            logger.Reset();

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
            logger.AssertOutputStrings(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasDisconnectedActiveSection_ConnectCommandIsBusy()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
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
            logger.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (kick off connection)
            infoBar.SimulateButtonClickEvent();

            // Assert
            refreshCalled.Should().Be(0, "Expected to connect once");
            bindingCalled.Should().Be(0, "Not expected to bind yet");
            logger.AssertOutputStrings(1);
            infoBar.VerifyAllEventsRegistered();
            this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_NotBusy()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
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
            logger.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act (command disabled)
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(1);
            bindExecuted.Should().Be(0, "Update was not expected to be executed");
            logger.AssertOutputStrings(1);

            // Act (command enabled)
            canExecute = true;
            infoBar.SimulateButtonClickEvent();

            // Assert
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(2);
            bindExecuted.Should().Be(1, "Update was expected to be executed");
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            logger.AssertOutputStrings(1);
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_HasConnectedActiveSection_IsBusy()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();
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
        public void ErrorListInfoBarController_InfoBar_ClickButton_NonCriticalExceptionIsSuppressed()
        {
            var host = CreateHostWithThrowingConfigProvider(new InvalidOperationException("thrown by test code"));
            var testSubject = CreateTestSubject(host);

            // Act - should not throw
            testSubject.CurrentErrorWindowInfoBar_ButtonClick(this, EventArgs.Empty);

            logger.AssertPartialOutputStringExists("thrown by test code");
        }

        [TestMethod]
        public void ErrorListInfoBarController_InfoBar_ClickButton_CriticalExceptionIsNotSuppressed()
        {
            var host = CreateHostWithThrowingConfigProvider(new StackOverflowException("thrown by test code"));
            var testSubject = CreateTestSubject(host);

            // Act
            Action act = () => testSubject.CurrentErrorWindowInfoBar_ButtonClick(this, EventArgs.Empty);
            act.Should().ThrowExactly<StackOverflowException>().And.Message.Should().Contain("thrown by test code");
        }

        [TestMethod]
        public void ErrorListInfoBarController_Reset()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution();
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args => { });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            logger.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            testSubject.Reset();

            // Assert
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            logger.AssertOutputStrings(0);
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
        }

        [TestMethod]
        public void ErrorListInfoBarController_Dispose()
        {
            // Arrange
            this.SetBindingMode(SonarLintMode.LegacyConnected);
            var testSubject = CreateTestSubject();
            this.ConfigureLoadedSolution();
            ConfigurableSectionController section = this.ConfigureActiveSectionWithBindCommand(args => { });
            this.ConfigureProjectViewModel(section);
            testSubject.Refresh();
            RunAsyncAction();
            logger.Reset();

            // Sanity
            ConfigurableInfoBar infoBar = this.infoBarManager.AssertHasAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            VerifyInfoBar(infoBar);

            // Act
            testSubject.Dispose();

            // Assert
            this.infoBarManager.AssertHasNoAttachedInfoBar(ErrorListInfoBarController.ErrorListToolWindowGuid);
            infoBar.VerifyAllEventsUnregistered();
            logger.AssertOutputStrings(0);
            this.teamExplorerController.ShowConnectionsPageCallsCount.Should().Be(0);
        }

        #endregion Tests

        #region Test helpers

        private ErrorListInfoBarController CreateTestSubject(IHost host = null)
        {
            host ??= this.host;
            var testSubject = new ErrorListInfoBarController(host, bindingRequiredIndicator.Object, logger, knownUIContexts.Object, new NoOpThreadHandler());
            return testSubject;
        }

        private static IHost CreateHostWithThrowingConfigProvider(Exception exceptionToThrow)
        {
            var configProvider = new Mock<IConfigurationProviderService>();
            configProvider.Setup(x => x.GetConfiguration()).Throws(exceptionToThrow);

            var host = new Mock<IHost>();
            host.As<IServiceProvider>().Setup(x => x.GetService(typeof(IConfigurationProviderService))).Returns(configProvider.Object);

            return host.Object;
        }

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
            this.configProvider.ProjectToReturn = mode == SonarLintMode.Standalone ? null : new BoundSonarQubeProject(new Uri("http://Server"), "boundProjectKey", "projectName");
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

        private void ConfigureLoadedSolution(bool hasUnboundProject = true)
        {
            bindingRequiredIndicator.Setup(x => x.IsBindingUpdateRequired(It.IsAny<CancellationToken>())).ReturnsAsync(hasUnboundProject);

            SetSolutionExistsAndFullyLoadedContextState(isActive: true);
        }

        private Mock<IUIContext> SetSolutionExistsAndFullyLoadedContextState(bool isActive, Action whenChangedCallback = null)
        {
            knownUIContexts.Reset();

            var context = new Mock<IUIContext>();
            context.Setup(x => x.IsActive).Returns(isActive);

            knownUIContexts.SetupGet(x => x.SolutionExistsAndFullyLoadedContext).Returns(context.Object);
            return context;
        }

        private async Task SimulateUIContextIsActivated(ErrorListInfoBarController testSubject) =>
            await testSubject.ProcessSolutionBindingAsync();

        private static void VerifyInfoBar(ConfigurableInfoBar infoBar)
        {
            infoBar.Message.Should().Be(Strings.SonarLintInfoBarUnboundProjectsMessage);
            infoBar.ButtonText.Should().Be(Strings.SonarLintInfoBarUpdateCommandText);
            infoBar.Image.Should().BeEquivalentTo(new SonarLintImageMoniker(KnownMonikers.RuleWarning.Guid, KnownMonikers.RuleWarning.Id));
        }

        #endregion Test helpers
    }
}
