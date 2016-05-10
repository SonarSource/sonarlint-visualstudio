//-----------------------------------------------------------------------
// <copyright file="TransferableVisualState.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.State
{
    internal class TransferableVisualState : ViewModelBase
    {
        private readonly ObservableCollection<ServerViewModel> connectedServers = new ObservableCollection<ServerViewModel>();
        private ProjectViewModel boundProject;
        private bool isBusy;

        public ObservableCollection<ServerViewModel> ConnectedServers
        {
            get
            {
                Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(ConnectedServers)} should only be accessed from the UI thread");
                return this.connectedServers;
            }
        }

        public bool HasBoundProject
        {
            get
            {
                Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(HasBoundProject)} should only be accessed from the UI thread");
                return this.boundProject != null;
            }
        }

        public bool IsBusy
        {
            get
            {
                Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(IsBusy)} should only be accessed from the UI thread");
                return this.isBusy;
            }
            set
            {
                Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(IsBusy)} should only be set from the UI thread");
                this.SetAndRaisePropertyChanged(ref this.isBusy, value);
            }
        }

        public void SetBoundProject(ProjectViewModel project)
        {
            Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(SetBoundProject)} should only be accessed from the UI thread");
            this.ClearBoundProject();

            this.boundProject = project;
            this.boundProject.IsBound = true;
            this.boundProject.Owner.ShowAllProjects = false;

            this.OnHasBoundProjectChanged();
        }

        public void ClearBoundProject()
        {
            Debug.Assert(ThreadHelper.CheckAccess(), $"{nameof(ClearBoundProject)} should only be accessed from the UI thread");
            if (this.boundProject != null)
            {
                this.boundProject.IsBound = false;
                this.boundProject.Owner.ShowAllProjects = true;
                this.boundProject = null;

                this.OnHasBoundProjectChanged();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
          "S3236:Methods with caller info attributes should not be invoked with explicit arguments",
          Justification = "We actually want to specify a different property to change",
          Scope = "member",
          Target = "~M:SonarLint.VisualStudio.Integration.State.TransferableVisualState.OnHasBoundProjectChanged()")]
        private void OnHasBoundProjectChanged()
        {
            this.RaisePropertyChanged(nameof(HasBoundProject));
        }
    }
}
