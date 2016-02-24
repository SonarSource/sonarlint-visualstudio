//-----------------------------------------------------------------------
// <copyright file="ConnectSectionTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Client.CommandTarget;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel.Design;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    [TestClass]
    public class ConnectSectionTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableConnectionWorkflow workflow;

        [TestInitialize]
        public void TestInitialize()
        {
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.workflow = new ConfigurableConnectionWorkflow(this.sonarQubeService);
        }

        #region Tests
        [TestMethod]
        public void ConnectSection_Initialization()
        {
            // Act 
            var testSubject = this.CreateTestSubject();

            // Constructor time initialization
            Assert.IsNotNull(testSubject.Controller.ConnectCommand, "ConnectCommand is not initialized");
            Assert.IsNotNull(testSubject.Controller.BindCommand, "BindCommand is not initialized");
            Assert.IsNotNull(testSubject.Controller.DisconnectCommand, "DisconnectCommand is not initialized");
            Assert.IsNotNull(testSubject.Controller.RefreshCommand, "RefreshCommand is not initialized");

            // Case 1: first time initialization
            // Verify
            workflow.AssertEstablishConnectionCalled(0);
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");

            // Case 2: re-initialization with connection but no projects;
            var connection = new ConnectionInformation(new Uri("http://localhost"));
            this.sonarQubeService.SetConnection(connection);
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];
            ResetSection(testSubject);

            // Sanity
            workflow.AssertEstablishConnectionCalled(1);

            // Verify
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");
            ConnectSectionViewModel vm = ((IConnectSection)testSubject).ViewModel;
            VerifyConnectSectionViewModelIsConnectedAndHasNoProjects(vm, connection);

            // Case 3: re-initialization with connection and projects;
            var projects = new [] { new ProjectInformation() };
            this.sonarQubeService.ReturnProjectInformation = projects;
            ResetSection(testSubject);

            // Verify
            workflow.AssertEstablishConnectionCalled(2);
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");
            vm = ((IConnectSection)testSubject).ViewModel;
            VerifyConnectSectionViewModelIsConnectedAndHasProjects(vm, connection, projects);

            // Case 4: re-initialization with no connection
            this.sonarQubeService.ClearConnection();
            ResetSection(testSubject);

            // Verify
            workflow.AssertEstablishConnectionCalled(2);
            Assert.IsNotNull(testSubject.View, "Failed to get the View");
            Assert.IsNotNull(testSubject.ViewModel, "Failed to get the ViewModel");
            vm = ((IConnectSection)testSubject).ViewModel;
            VerifyConnectSectionViewModelIsNotConnected(vm, connection);
        }

        [TestMethod]
        public void ConnectionSection_IOleCommandTargetQueryStatus()
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
            Assert.AreEqual(ConnectSection.CommandNotHandled, testSubjectCommandTarget.QueryStatus(ref group, cCmds, prgCmds, pCmdText));
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
        public void ConnectionSection_Refresh()
        {
            // Setup
            var testSubject = this.CreateTestSubject();

            // Case 1: No connection
            // Act
            testSubject.Refresh();

            // Verify
            this.sonarQubeService.AssertConnectRequests(0);

            // Case 2: Connected
            this.sonarQubeService.SetConnection(new Uri("http://connected"));
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];

            // Act
            testSubject.Refresh();

            // Verify
            this.sonarQubeService.AssertConnectRequests(1);
        }
        #endregion 

        #region Helpers
        private static void ResetSection(ConnectSection section)
        {
            section.Controller.Detach(section);
            section.Controller.ConnectedServers.Clear();
            section.Initialize(null, new Microsoft.TeamFoundation.Controls.SectionInitializeEventArgs(new ServiceContainer(), null));
            section.Refresh();
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnectedAndHasNoProjects(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = VerifyConnectSectionViewModelIsConnected(vm, connection);
            Assert.AreEqual(0, serverVM.Projects.Count, "Unexpected number of projects");

            return serverVM;
        }

        private static void VerifyConnectSectionViewModelIsNotConnected(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = vm.ConnectedServers.SingleOrDefault(s => s.Url == connection.ServerUri);
            Assert.IsNull(serverVM, "Should not find server view model for {0}", connection.ServerUri);
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnected(ConnectSectionViewModel vm, ConnectionInformation connection)
        {
            ServerViewModel serverVM = vm.ConnectedServers.SingleOrDefault(s => s.Url == connection.ServerUri);
            Assert.IsNotNull(serverVM, "Could not find server view model for {0}", connection.ServerUri);

            return serverVM;
        }

        private static ServerViewModel VerifyConnectSectionViewModelIsConnectedAndHasProjects(ConnectSectionViewModel vm, ConnectionInformation connection, ProjectInformation[] projects)
        {
            ServerViewModel serverVM = VerifyConnectSectionViewModelIsConnected(vm, connection);
            CollectionAssert.AreEquivalent(projects, serverVM.Projects.Select(p => p.ProjectInformation).ToArray(), "Unexpected projects for server {0}", connection.ServerUri);

            return serverVM;
        }

        private class TestCommandTarget : IOleCommandTarget
        {
            private int queryStatusNumberOfCalls = 0;

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
            } = ConnectSection.CommandNotHandled;

            public void AssertQueryStatusCalled(int expectedNumberOfTimes)
            {
                Assert.AreEqual(expectedNumberOfTimes, this.queryStatusNumberOfCalls, "IOleCommandTarget.QueryStatus is called unexpected number of times");
            }
            #endregion
        }

        private ConnectSection CreateTestSubject()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
            var controller = new TestableConnectSectionController(this.serviceProvider, this.sonarQubeService);
            controller.SetConnectCommand(new ConnectCommand(controller, this.sonarQubeService, null, this.workflow));
            var section = new ConnectSection(controller);
            section.Initialize(null, new Microsoft.TeamFoundation.Controls.SectionInitializeEventArgs(new ServiceContainer(), null));
            return section;
        }
        #endregion
    }
}
