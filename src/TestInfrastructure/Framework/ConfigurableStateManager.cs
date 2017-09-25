/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using FluentAssertions;
using SonarLint.VisualStudio.Integration.State;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    internal class ConfigurableStateManager : IStateManager
    {
        internal SonarQubeProject BoundProject { get; private set; }

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
                return this.BoundProject != null;
            }
        }

        public void ClearBoundProject()
        {
            this.VerifyActiveSection();

            this.BoundProject = null;

            this.BindingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetBoundProject(SonarQubeProject project)
        {
            project.Should().NotBeNull();

            this.VerifyActiveSection();

            this.BoundProject = project;

            this.BindingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetProjects(ConnectionInformation connection, IEnumerable<SonarQubeProject> projects)
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

        public ConnectionInformation GetConnectedServer(SonarQubeProject project)
        {
            ConnectionInformation conn;
            var isFound = this.ProjectServerMap.TryGetValue(project, out conn);

            isFound.Should().BeTrue("Test setup: project-server mapping is not available for the specified project");

            return conn;
        }

        #endregion IStateManager

        #region Test helpers

        public IHost Host { get; set; }

        public HashSet<ConnectionInformation> ConnectedServers { get; } = new HashSet<ConnectionInformation>();

        public Dictionary<SonarQubeProject, ConnectionInformation> ProjectServerMap { get; } = new Dictionary<SonarQubeProject, ConnectionInformation>();

        public TransferableVisualState ManagedState { get; set; }

        public int SyncCommandFromActiveSectionCalled { get; private set; }

        public bool? ExpectActiveSection { get; set; }

        public Action<ConnectionInformation, IEnumerable<SonarQubeProject>> SetProjectsAction { get; set; }

        public Action SyncCommandFromActiveSectionAction { get; set; }

        private void VerifyActiveSection()
        {
            if (!this.ExpectActiveSection.HasValue)
            {
                return;
            }

            this.Host.Should().NotBeNull("Test setup issue: the Host needs to be set");

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

        #endregion Test helpers
    }
}