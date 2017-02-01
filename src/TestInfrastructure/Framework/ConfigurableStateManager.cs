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

using Microsoft.VisualStudio.TestTools.UnitTesting; using FluentAssertions;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.State;
using System;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableStateManager : IStateManager
    {
        private ProjectInformation boundProject;

        public ConfigurableStateManager()
        {
            this.ManagedState = new TransferableVisualState();
        }

        #region IStateManager
        public event EventHandler<bool> IsBusyChanged;
        public event EventHandler BindingStateChanged;

        public string BoundProjectKey
        {
            get;
            set;
        }

        public bool IsBusy
        {
            get;
            set;
        }

        public bool HasBoundProject
        {
            get
            {
                return this.boundProject != null;
            }
        }

        public void ClearBoundProject()
        {
            this.VerifyActiveSection();

            this.boundProject = null;

            this.BindingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetBoundProject(ProjectInformation project)
        {
            project.Should().NotBeNull();

            this.VerifyActiveSection();

            this.boundProject = project;

            this.BindingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetProjects(ConnectionInformation connection, IEnumerable<ProjectInformation> projects)
        {
            this.VerifyActiveSection();
            this.SetProjectsAction?.Invoke(connection, projects);
        }

        public void SyncCommandFromActiveSection()
        {
            this.VerifyActiveSection();
            this.SyncCommandFromActiveSectionAction?.Invoke();
        }


        public bool IsConnected { get; set; }

        public IEnumerable<ConnectionInformation> GetConnectedServers()
        {
            return this.ConnectedServers;
        }

        public ConnectionInformation GetConnectedServer(ProjectInformation project)
        {
            ConnectionInformation conn;
            if (!this.ProjectServerMap.TryGetValue(project, out conn))
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Test setup: project-server mapping is not available for the specified project");
            }

            return conn;
        }
        #endregion

        #region Test helpers
        public IHost Host { get; set; }

        public HashSet<ConnectionInformation> ConnectedServers { get; } = new HashSet<ConnectionInformation>();

        public Dictionary<ProjectInformation, ConnectionInformation> ProjectServerMap { get; } = new Dictionary<ProjectInformation, ConnectionInformation>();

        public TransferableVisualState ManagedState { get; set; }

        public int SyncCommandFromActiveSectionCalled { get; private set; }

        public bool? ExpectActiveSection { get; set; }

        public Action<ConnectionInformation, IEnumerable<ProjectInformation>> SetProjectsAction { get; set; }

        public Action SyncCommandFromActiveSectionAction { get; set; }

        public void AssertBoundProject(ProjectInformation expected)
        {
            this.boundProject.Should().Be(expected, "Unexpected bound project");
        }

        public void AssertNoBoundProject()
        {
            this.boundProject.Should().BeNull("Unexpected bound project");
        }

        private void VerifyActiveSection()
        {
            if (!this.ExpectActiveSection.HasValue)
            {
                return;
            }

            if (this.Host == null)
            {
                FluentAssertions.Execution.Execute.Assertion.FailWith("Test setup issue: the Host needs to be set");
            }

            if (this.ExpectActiveSection.Value)
            {
                this.Host.ActiveSection.Should().NotBeNull("ActiveSection is null");
            }
            else
            {
                this.Host.ActiveSection.Should().BeNull("ActiveSection is not null");
            }
        }

        public void SetAndInvokeBusyChanged(bool value)
        {
            this.IsBusy = value;
            this.IsBusyChanged?.Invoke(this, value);
        }
        #endregion
    }
}
