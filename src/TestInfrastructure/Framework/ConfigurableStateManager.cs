//-----------------------------------------------------------------------
// <copyright file="ConfigurableStateManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            Assert.IsNotNull(project);

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
        #endregion

        #region Test helpers
        public IHost Host { get; set; }

        public TransferableVisualState ManagedState { get; set; }

        public int SyncCommandFromActiveSectionCalled { get; private set; }

        public bool? ExpectActiveSection { get; set; }

        public Action<ConnectionInformation, IEnumerable<ProjectInformation>> SetProjectsAction { get; set; }

        public Action SyncCommandFromActiveSectionAction { get; set; }

        public void AssertBoundProject(ProjectInformation expected)
        {
            Assert.AreEqual(expected, this.boundProject, "Unexpected bound project");
        }

        public void AssertNoBoundProject()
        {
            Assert.IsNull(this.boundProject, "Unexpected bound project");
        }

        private void VerifyActiveSection()
        {
            if (!this.ExpectActiveSection.HasValue)
            {
                return;
            }

            if (this.Host == null)
            {
                Assert.Inconclusive("Test setup issue: the Host needs to be set");
            }

            if (this.ExpectActiveSection.Value)
            {
                Assert.IsNotNull(this.Host.ActiveSection, "ActiveSection is null");
            }
            else
            {
                Assert.IsNull(this.Host.ActiveSection, "ActiveSection is not null");
            }
        }

        public void InvokeBusyChanged(bool value)
        {
            this.IsBusyChanged?.Invoke(this, value);
        }
        #endregion
    }
}
