//-----------------------------------------------------------------------
// <copyright file="SectionControllerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Client.CommandTarget;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.ComponentModel.Design;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class SectionControllerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableHost host;

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.host.SonarQubeService = this.sonarQubeService;
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), new ConfigurableVsProjectSystemHelper(this.serviceProvider));
        }

        #region Tests
        [TestMethod]
        public void SectionController_Initialization()
        {
            // Act 
            SectionController testSubject = this.CreateTestSubject();

            // Constructor time initialization
            Assert.IsNotNull(testSubject.ConnectCommand, "ConnectCommand is not initialized");
            Assert.IsNotNull(testSubject.BindCommand, "BindCommand is not initialized");
            Assert.IsNotNull(testSubject.BrowseToUrlCommand, "BrowseToUrlCommand is not initialized");
            Assert.IsNotNull(testSubject.BrowseToProjectDashboardCommand, "BrowseToProjectDashboardCommand is not initialized");
            Assert.IsNotNull(testSubject.DisconnectCommand, "DisconnectCommand is not initialized");
            Assert.IsNotNull(testSubject.RefreshCommand, "RefreshCommand is not initialized");
            Assert.IsNotNull(testSubject.ToggleShowAllProjectsCommand, "ToggleShowAllProjectsCommand is not initialized");

            // Case 1: first time initialization
            // Verify
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 2: re-initialization with connection but no projects
            var connection = new ConnectionInformation(new Uri("http://localhost"));
            this.sonarQubeService.SetConnection(connection);
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];
            ReInitialize(testSubject, this.host);

            // Verify
            AssertCommandsInSync(testSubject);
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 3: re-initialization with connection and projects
            var projects = new[] { new ProjectInformation() };
            this.sonarQubeService.ReturnProjectInformation = projects;
            ReInitialize(testSubject, this.host);

            // Verify
            AssertCommandsInSync(testSubject);
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 4: re-initialization with no connection
            this.sonarQubeService.ClearConnection();
            ReInitialize(testSubject, this.host);

            // Verify
            AssertCommandsInSync(testSubject);
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 5: Dispose
            testSubject.Dispose();

            // Verify
            Assert.IsNull(testSubject.ConnectCommand, "ConnectCommand is not cleared");
            Assert.IsNull(testSubject.RefreshCommand, "RefreshCommand is not cleared");
            Assert.IsNull(testSubject.DisconnectCommand, "DisconnectCommand is not cleared");
            Assert.IsNull(testSubject.BindCommand, "BindCommand is not ;");
            Assert.IsNull(testSubject.ToggleShowAllProjectsCommand, "ToggleShowAllProjectsCommand is not cleared");
            Assert.IsNull(testSubject.BrowseToUrlCommand, "BrowseToUrlCommand is not cleared");
            Assert.IsNull(testSubject.BrowseToProjectDashboardCommand, "BrowseToProjectDashboardCommand is not cleared");

        }

        [TestMethod]
        public void SectionController_RespondsToIsBusyChanged()
        {
            // Setup
            SectionController testSubject = this.CreateTestSubject();
            ConfigurableStateManager stateManager = (ConfigurableStateManager)this.host.VisualStateManager;
            ITeamExplorerSection viewModel = testSubject.ViewModel;

            // Act
            stateManager.InvokeBusyChanged(true);

            // Verify
            Assert.IsTrue(viewModel.IsBusy);

            // Act again (different value)
            stateManager.InvokeBusyChanged(false);

            // Verify (should change)
            Assert.IsFalse(viewModel.IsBusy);

            // Dispose
            testSubject.Dispose();

            // Act again(different value)
            stateManager.InvokeBusyChanged(true);

            // Verify (should remain the same)
            Assert.IsFalse(viewModel.IsBusy);
        }

        [TestMethod]
        public void SectionController_IOleCommandTargetQueryStatus()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            IOleCommandTarget testSubjectCommandTarget = testSubject;
            testSubject.CommandTargets.Clear();
            var command1 = new TestCommandTarget();
            var command2 = new TestCommandTarget();
            var command3 = new TestCommandTarget();
            testSubject.CommandTargets.Add(command1);
            testSubject.CommandTargets.Add(command2);
            testSubject.CommandTargets.Add(command3);
            Guid group = Guid.Empty;
            uint cCmds = 0;
            OLECMD[] prgCmds = new OLECMD[0];
            IntPtr pCmdText = IntPtr.Zero;

            // Case 1 : no commands handling the request
            // Act+Verify
            Assert.AreEqual(SectionController.CommandNotHandled, testSubjectCommandTarget.QueryStatus(ref group, cCmds, prgCmds, pCmdText));
            command1.AssertQueryStatusCalled(1);
            command2.AssertQueryStatusCalled(1);
            command3.AssertQueryStatusCalled(1);

            // Case 2 : the last command is handling the request
            command3.QueryStatusReturnsResult = (int)OleConstants.OLECMDERR_E_CANCELED;
            // Act+Verify
            Assert.AreEqual((int)OleConstants.OLECMDERR_E_CANCELED, testSubjectCommandTarget.QueryStatus(ref group, cCmds, prgCmds, pCmdText));
            command1.AssertQueryStatusCalled(2);
            command2.AssertQueryStatusCalled(2);
            command3.AssertQueryStatusCalled(2);

            // Case 3 : the first command is handling the request
            command1.QueryStatusReturnsResult = (int)OleConstants.OLECMDERR_E_DISABLED;
            // Act+Verify
            Assert.AreEqual((int)OleConstants.OLECMDERR_E_DISABLED, testSubjectCommandTarget.QueryStatus(ref group, cCmds, prgCmds, pCmdText));
            command1.AssertQueryStatusCalled(3);
            command2.AssertQueryStatusCalled(2);
            command3.AssertQueryStatusCalled(2);
        }

        [TestMethod]
        public void SectionController_DisconnectCommand()
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
        public void SectionController_ToggleShowAllProjectsCommand()
        {
            // Setup
            var testSubject = this.CreateTestSubject();
            var connInfo = new ConnectionInformation(new Uri("http://localhost"));
            var projectInfo = new ProjectInformation { Key = "p1", Name = "proj1" };
            var server = new ServerViewModel(connInfo);
            var project = new ProjectViewModel(server, projectInfo);
            server.Projects.Add(project);

            // Case 1: No bound projects
            project.IsBound = false;

            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.ToggleShowAllProjectsCommand.CanExecute(server));

            // Case 2: Bound
            project.IsBound = true;

            // Act + Verify
            Assert.IsTrue(testSubject.ToggleShowAllProjectsCommand.CanExecute(server));

            // Verify execution
            bool original = server.ShowAllProjects;

            // Act
            testSubject.ToggleShowAllProjectsCommand.Execute(server);

            // Verify
            Assert.AreEqual(!original, server.ShowAllProjects);

            // Act
            testSubject.ToggleShowAllProjectsCommand.Execute(server);

            // Verify
            Assert.AreEqual(original, server.ShowAllProjects);
        }

        [TestMethod]
        public void SectionController_BrowseToUrlCommand()
        {
            // Setup
            var webBrowser = new ConfigurableWebBrowser();
            var testSubject = this.CreateTestSubject(webBrowser);

            // Case 1: Empty URL
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.BrowseToUrlCommand.CanExecute(null));

            // Case 2: Bad URL
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.BrowseToUrlCommand.CanExecute("not a Uri"));

            // Case 3: Good URL
            const string goodUrl = "http://localhost";

            // Act + Verify CanExecute
            Assert.IsTrue(testSubject.BrowseToUrlCommand.CanExecute(goodUrl));

            // Act + Verify Execute
            testSubject.BrowseToUrlCommand.Execute(goodUrl);
            webBrowser.AssertNavigateToCalls(1);
            webBrowser.AssertRequestToNavigateTo(goodUrl);
        }

        [TestMethod]
        public void SectionController_BrowseToProjectDashboardCommand()
        {
            // Setup
            var webBrowser = new ConfigurableWebBrowser();
            var testSubject = this.CreateTestSubject(webBrowser);
            var serverUrl = new Uri("http://my-sonar-server:5555");
            var connectionInfo = new ConnectionInformation(serverUrl);
            var projectInfo = new ProjectInformation { Key = "p1" };

            Uri expectedUrl = new Uri(serverUrl, string.Format(SonarQubeServiceWrapper.ProjectDashboardRelativeUrl, projectInfo.Key));
            this.sonarQubeService.RegisterProjectDashboardUrl(connectionInfo, projectInfo, expectedUrl);

            // Case 1: Null parameter
            // Act + Verify CanExecute
            Assert.IsFalse(testSubject.BrowseToProjectDashboardCommand.CanExecute(null));

            // Case 2: Project VM
            var serverViewModel = new ServerViewModel(connectionInfo);
            var projectViewModel = new ProjectViewModel(serverViewModel, projectInfo);

            // Act + Verify CanExecute
            Assert.IsTrue(testSubject.BrowseToProjectDashboardCommand.CanExecute(projectViewModel));

            // Act + Verify Execute
            testSubject.BrowseToProjectDashboardCommand.Execute(projectViewModel);
            webBrowser.AssertNavigateToCalls(1);
            webBrowser.AssertRequestToNavigateTo(expectedUrl.ToString());
        }

        #endregion

        #region Helpers
        private static void ReInitialize(SectionController controller, IHost host)
        {
            host.ClearActiveSection();
            host.VisualStateManager.ManagedState.ConnectedServers.Clear();
            controller.Initialize(null, new Microsoft.TeamFoundation.Controls.SectionInitializeEventArgs(new ServiceContainer(), null));
            bool refreshCalled = false;
            controller.RefreshCommand = new RelayCommand(() => refreshCalled = true);
            controller.Refresh();
            Assert.IsTrue(refreshCalled, "Refresh command execution was expected");
        }

        private class TestCommandTarget : IOleCommandTarget
        {
            private int queryStatusNumberOfCalls;

            #region IOleCommandTarget
            int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                throw new NotImplementedException();
            }

            int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                this.queryStatusNumberOfCalls++;
                return this.QueryStatusReturnsResult;
            }
            #endregion

            #region  Test helpers
            public int QueryStatusReturnsResult
            {
                get;
                set;
            } = SectionController.CommandNotHandled;

            public void AssertQueryStatusCalled(int expectedNumberOfTimes)
            {
                Assert.AreEqual(expectedNumberOfTimes, this.queryStatusNumberOfCalls, "IOleCommandTarget.QueryStatus is called unexpected number of times");
            }
            #endregion
        }

        private SectionController CreateTestSubject(IWebBrowser webBrowser = null)
        {
            var controller = new SectionController(host, webBrowser ?? new ConfigurableWebBrowser());
            controller.Initialize(null, new Microsoft.TeamFoundation.Controls.SectionInitializeEventArgs(new ServiceContainer(), null));
            return controller;
        }

        private void AssertCommandsInSync(SectionController section)
        {
            ConnectSectionViewModel viewModel = (ConnectSectionViewModel)section.ViewModel;

            Assert.AreSame(section.ConnectCommand, viewModel.ConnectCommand, "ConnectCommand is not initialized");
            Assert.AreSame(section.BindCommand, viewModel.BindCommand, "BindCommand is not initialized");
            Assert.AreSame(section.BrowseToUrlCommand, viewModel.BrowseToUrlCommand, "BrowseToUrlCommand is not initialized");
        }
        #endregion
    }
}
