//-----------------------------------------------------------------------
// <copyright file="SectionControllerTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Client.CommandTarget;
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

        [TestInitialize]
        public void TestInitialize()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
        }

        #region Tests
        [TestMethod]
        public void SectionController_Initialization()
        {
            // Act 
            SectionController testSubject = this.CreateTestSubject();

            // Constructor time initialization
            Assert.IsNotNull(testSubject.ConnectCommand, "ConnectCommand is not initialized");
            Assert.IsNotNull(testSubject.RefreshCommand, "RefreshCommand is not initialized");
            Assert.IsNotNull(testSubject.DisconnectCommand, "DisconnectCommand is not initialized");
            Assert.IsNotNull(testSubject.BindCommand, "BindCommand is not initialized");
            Assert.IsNotNull(testSubject.ToggleShowAllProjectsCommand, "ToggleShowAllProjectsCommand is not initialized");

            // Case 1: first time initialization
            // Verify
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 2: re-initialization with connection but no projects
            var connection = new ConnectionInformation(new Uri("http://localhost"));
            this.sonarQubeService.SetConnection(connection);
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];
            ReInitialize(testSubject);

            // Verify
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 3: re-initialization with connection and projects
            var projects = new [] { new ProjectInformation() };
            this.sonarQubeService.ReturnProjectInformation = projects;
            ReInitialize(testSubject);

            // Verify
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 4: re-initialization with no connection
            this.sonarQubeService.ClearConnection();
            ReInitialize(testSubject);

            // Verify
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");
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

        #endregion 

        #region Helpers
        private static void ReInitialize(SectionController controller)
        {
            controller.Host.ClearActiveSection();
            controller.Host.VisualStateManager.ManagedState.ConnectedServers.Clear();
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

        private SectionController CreateTestSubject()
        {
            var host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            host.SonarQubeService = this.sonarQubeService;
            var controller = new SectionController(host);
            controller.Initialize(null, new Microsoft.TeamFoundation.Controls.SectionInitializeEventArgs(new ServiceContainer(), null));
            return controller;
        }
        #endregion
    }
}
