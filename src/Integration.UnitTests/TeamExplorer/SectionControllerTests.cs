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

using FluentAssertions;
using Microsoft.TeamFoundation.Client.CommandTarget;
using Microsoft.TeamFoundation.Controls;
 using Xunit;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System;
using System.ComponentModel.Design;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    public class SectionControllerTests
    {
        private ConfigurableServiceProvider serviceProvider;
        private ConfigurableSonarQubeServiceWrapper sonarQubeService;
        private ConfigurableHost host;

        public SectionControllerTests()
        {
            ThreadHelper.SetCurrentThreadAsUIThread();
            this.serviceProvider = new ConfigurableServiceProvider(assertOnUnexpectedServiceRequest: false);
            this.sonarQubeService = new ConfigurableSonarQubeServiceWrapper();
            this.host = new ConfigurableHost(this.serviceProvider, Dispatcher.CurrentDispatcher);
            this.host.SonarQubeService = this.sonarQubeService;
            this.serviceProvider.RegisterService(typeof(IProjectSystemHelper), new ConfigurableVsProjectSystemHelper(this.serviceProvider));
        }

        #region Tests

        [Fact]
        public void Ctor_WithNullHost_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new SectionController(null, new WebBrowser());

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void Ctor_WithNullWebBrowser_ThrowsArgumentNullException()
        {
            // Arrange + Act
            Action act = () => new SectionController(this.host, null);

            // Assert
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public void SectionController_Initialization()
        {
            // Act
            SectionController testSubject = this.CreateTestSubject();

            // Constructor time initialization
            testSubject.ConnectCommand.Should().NotBeNull( "ConnectCommand is not initialized");
            testSubject.BindCommand.Should().NotBeNull( "BindCommand is not initialized");
            testSubject.BrowseToUrlCommand.Should().NotBeNull( "BrowseToUrlCommand is not initialized");
            testSubject.BrowseToProjectDashboardCommand.Should().NotBeNull( "BrowseToProjectDashboardCommand is not initialized");
            testSubject.DisconnectCommand.Should().NotBeNull( "DisconnectCommand is not initialized");
            testSubject.RefreshCommand.Should().NotBeNull( "RefreshCommand is not initialized");
            testSubject.ToggleShowAllProjectsCommand.Should().NotBeNull( "ToggleShowAllProjectsCommand is not initialized");

            // Case 1: first time initialization
            // Assert
            testSubject.View.Should().NotBeNull( "Failed to get the View");
            ((ISectionController)testSubject).View.Should().NotBeNull("Failed to get the View as ConnectSectionView");
            testSubject.ViewModel.Should().NotBeNull( "Failed to get the ViewModel");

            // Case 2: re-initialization with connection but no projects
            this.host.TestStateManager.IsConnected = true;
            this.sonarQubeService.ReturnProjectInformation = new ProjectInformation[0];
            ReInitialize(testSubject, this.host);

            // Assert
            AssertCommandsInSync(testSubject);
            testSubject.View.Should().NotBeNull( "Failed to get the View");
            testSubject.ViewModel.Should().NotBeNull( "Failed to get the ViewModel");

            // Case 3: re-initialization with connection and projects
            var projects = new[] { new ProjectInformation() };
            this.sonarQubeService.ReturnProjectInformation = projects;
            ReInitialize(testSubject, this.host);

            // Assert
            AssertCommandsInSync(testSubject);
            testSubject.View.Should().NotBeNull( "Failed to get the View");
            testSubject.ViewModel.Should().NotBeNull( "Failed to get the ViewModel");

            // Case 4: re-initialization with no connection
            this.host.TestStateManager.IsConnected = false;
            ReInitialize(testSubject, this.host);

            // Assert
            AssertCommandsInSync(testSubject);
            testSubject.View.Should().NotBeNull( "Failed to get the View");
            testSubject.ViewModel.Should().NotBeNull( "Failed to get the ViewModel");

            // Case 5: Dispose
            testSubject.Dispose();

            // Assert
            testSubject.ConnectCommand.Should().BeNull( "ConnectCommand is not cleared");
            testSubject.RefreshCommand.Should().BeNull( "RefreshCommand is not cleared");
            testSubject.DisconnectCommand.Should().BeNull( "DisconnectCommand is not cleared");
            testSubject.BindCommand.Should().BeNull( "BindCommand is not ;");
            testSubject.ToggleShowAllProjectsCommand.Should().BeNull( "ToggleShowAllProjectsCommand is not cleared");
            testSubject.BrowseToUrlCommand.Should().BeNull( "BrowseToUrlCommand is not cleared");
            testSubject.BrowseToProjectDashboardCommand.Should().BeNull( "BrowseToProjectDashboardCommand is not cleared");

        }

        [Fact]
        public void SectionController_RespondsToIsBusyChanged()
        {
            // Arrange
            SectionController testSubject = this.CreateTestSubject();
            ITeamExplorerSection viewModel = testSubject.ViewModel;

            // Act
            this.host.TestStateManager.SetAndInvokeBusyChanged(true);

            // Assert
            viewModel.IsBusy.Should().BeTrue();

            // Act again (different value)
            this.host.TestStateManager.SetAndInvokeBusyChanged(false);

            // Assert (should change)
            viewModel.IsBusy.Should().BeFalse();

            // Dispose
            testSubject.Dispose();

            // Act again(different value)
            this.host.TestStateManager.SetAndInvokeBusyChanged(true);

            // Assert (should remain the same)
            viewModel.IsBusy.Should().BeFalse();
        }

        [Fact]
        public void SectionController_IOleCommandTargetQueryStatus()
        {
            // Arrange
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
            SectionController.CommandNotHandled.Should().Be( testSubjectCommandTarget.QueryStatus(ref group, cCmds, prgCmds, pCmdText));
            command1.AssertQueryStatusCalled(1);
            command2.AssertQueryStatusCalled(1);
            command3.AssertQueryStatusCalled(1);

            // Case 2 : the last command is handling the request
            command3.QueryStatusReturnsResult = (int)OleConstants.OLECMDERR_E_CANCELED;
            // Act+Verify
            testSubjectCommandTarget.QueryStatus(ref group, cCmds, prgCmds, pCmdText).Should()
                .Be((int)OleConstants.OLECMDERR_E_CANCELED);
            command1.AssertQueryStatusCalled(2);
            command2.AssertQueryStatusCalled(2);
            command3.AssertQueryStatusCalled(2);

            // Case 3 : the first command is handling the request
            command1.QueryStatusReturnsResult = (int)OleConstants.OLECMDERR_E_DISABLED;
            // Act+Verify
            testSubjectCommandTarget.QueryStatus(ref group, cCmds, prgCmds, pCmdText).Should()
                .Be((int)OleConstants.OLECMDERR_E_DISABLED);
            command1.AssertQueryStatusCalled(3);
            command2.AssertQueryStatusCalled(2);
            command3.AssertQueryStatusCalled(2);
        }

        [Fact]
        public void SectionController_DisconnectCommand()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var connection = new ConnectionInformation(new Uri("http://connected"));
            int setProjectsCalled = 0;
            this.host.TestStateManager.SetProjectsAction = (conn, projects) =>
            {
                setProjectsCalled++;
                conn.Should().Be(connection);
                projects.Should().BeNull( "Expecting the project to be reset to null");
            };

            // Case 1: No connection
            // Act + Assert CanExecute
            testSubject.DisconnectCommand.CanExecute(null)
                .Should().BeFalse();
            setProjectsCalled.Should().Be(0);

            // Case 2: Connected
            this.host.TestStateManager.ConnectedServers.Add(connection);

            // Act + Assert CanExecute
            testSubject.DisconnectCommand.CanExecute(null).Should().BeTrue();
            setProjectsCalled.Should().Be(0);

            // Act + Assert Execute
            testSubject.DisconnectCommand.Execute(null);
            setProjectsCalled.Should().Be(1);
        }

        [Fact]
        public void SectionController_ToggleShowAllProjectsCommand()
        {
            // Arrange
            var testSubject = this.CreateTestSubject();
            var connInfo = new ConnectionInformation(new Uri("http://localhost"));
            var projectInfo = new ProjectInformation { Key = "p1", Name = "proj1" };
            var server = new ServerViewModel(connInfo);
            var project = new ProjectViewModel(server, projectInfo);
            server.Projects.Add(project);

            // Case 1: No bound projects
            project.IsBound = false;

            // Act + Assert CanExecute
            testSubject.ToggleShowAllProjectsCommand.CanExecute(server)
                .Should().BeFalse();

            // Case 2: Bound
            project.IsBound = true;

            // Act + Assert
            testSubject.ToggleShowAllProjectsCommand.CanExecute(server).Should().BeTrue();

            // Assert execution
            bool original = server.ShowAllProjects;

            // Act
            testSubject.ToggleShowAllProjectsCommand.Execute(server);

            // Assert
            server.ShowAllProjects.Should().Be(!original);

            // Act
            testSubject.ToggleShowAllProjectsCommand.Execute(server);

            // Assert
            original.Should().Be( server.ShowAllProjects);
        }

        [Fact]
        public void SectionController_BrowseToUrlCommand()
        {
            // Arrange
            var webBrowser = new ConfigurableWebBrowser();
            var testSubject = this.CreateTestSubject(webBrowser);

            // Case 1: Empty URL
            // Act + Assert CanExecute
            testSubject.BrowseToUrlCommand.CanExecute(null)
                .Should().BeFalse();

            // Case 2: Bad URL
            // Act + Assert CanExecute
            testSubject.BrowseToUrlCommand.CanExecute("not a Uri")
                .Should().BeFalse();

            // Case 3: Good URL
            const string goodUrl = "http://localhost";

            // Act + Assert CanExecute
            testSubject.BrowseToUrlCommand.CanExecute(goodUrl)
                .Should().BeTrue();

            // Act + Assert Execute
            testSubject.BrowseToUrlCommand.Execute(goodUrl);
            webBrowser.AssertNavigateToCalls(1);
            webBrowser.AssertRequestToNavigateTo(goodUrl);
        }

        [Fact]
        public void SectionController_BrowseToProjectDashboardCommand()
        {
            // Arrange
            var webBrowser = new ConfigurableWebBrowser();
            var testSubject = this.CreateTestSubject(webBrowser);
            var serverUrl = new Uri("http://my-sonar-server:5555");
            var connectionInfo = new ConnectionInformation(serverUrl);
            var projectInfo = new ProjectInformation { Key = "p1" };

            Uri expectedUrl = new Uri(serverUrl, string.Format(SonarQubeServiceWrapper.ProjectDashboardRelativeUrl, projectInfo.Key));
            this.sonarQubeService.RegisterProjectDashboardUrl(connectionInfo, projectInfo, expectedUrl);

            // Case 1: Null parameter
            // Act + Assert CanExecute
            testSubject.BrowseToProjectDashboardCommand.CanExecute(null)
                .Should().BeFalse();

            // Case 2: Project VM
            var serverViewModel = new ServerViewModel(connectionInfo);
            var projectViewModel = new ProjectViewModel(serverViewModel, projectInfo);

            // Act + Assert CanExecute
            testSubject.BrowseToProjectDashboardCommand.CanExecute(projectViewModel)
                .Should().BeTrue();

            // Act + Assert Execute
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
            refreshCalled.Should().BeTrue( "Refresh command execution was expected");
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
                expectedNumberOfTimes.Should().Be( this.queryStatusNumberOfCalls, "IOleCommandTarget.QueryStatus is called unexpected number of times");
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

            viewModel.ConnectCommand.Should().Be(section.ConnectCommand, "ConnectCommand is not initialized");
            viewModel.BindCommand.Should().Be(section.BindCommand, "BindCommand is not initialized");
            viewModel.BrowseToUrlCommand.Should().Be(section.BrowseToUrlCommand, "BrowseToUrlCommand is not initialized");
        }
        #endregion
    }
}
