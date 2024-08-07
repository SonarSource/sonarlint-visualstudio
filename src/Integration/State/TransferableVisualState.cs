/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.TeamExplorer;

namespace SonarLint.VisualStudio.Integration.State
{
    internal class TransferableVisualState : ViewModelBase
    {
        private readonly ObservableCollection<ServerViewModel> connectedServers = new ObservableCollection<ServerViewModel>();
        private readonly IThreadHandling threadHandling;
        private ProjectViewModel boundProject;
        private bool isBusy;
        private ConnectConfiguration connectConfiguration = new ConnectConfiguration();
        private bool hasSharedBinding;

        public TransferableVisualState()
            : this(ThreadHandling.Instance)
        { }

        internal /* for testing */ TransferableVisualState(IThreadHandling threadHandling)
        {
            this.threadHandling = threadHandling;
        }

        public ObservableCollection<ServerViewModel> ConnectedServers
        {
            get
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(ConnectedServers)} should only be accessed from the UI thread");
                return this.connectedServers;
            }
        }

        public ConnectConfiguration ConnectConfiguration
        {
            get
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(ConnectConfiguration)} should only be accessed from the UI thread");
                return connectConfiguration;
            }
            set
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(ConnectConfiguration)} should only be set from the UI thread");
                SetAndRaisePropertyChanged(ref connectConfiguration, value);
            }
        }

        public bool HasSharedBinding
        {
            get
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(HasSharedBinding)} should only be accessed from the UI thread");
                return hasSharedBinding;
            }
            set
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(HasSharedBinding)} should only be set from the UI thread");
                SetAndRaisePropertyChanged(ref hasSharedBinding, value);
            }
        }

        public bool HasBoundProject
        {
            get
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(HasBoundProject)} should only be accessed from the UI thread");
                return this.boundProject != null;
            }
        }

        public bool IsBusy
        {
            get
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(IsBusy)} should only be accessed from the UI thread");
                return this.isBusy;
            }
            set
            {
                Debug.Assert(threadHandling.CheckAccess(), $"{nameof(IsBusy)} should only be set from the UI thread");
                this.SetAndRaisePropertyChanged(ref this.isBusy, value);
            }
        }

        public void SetBoundProject(ProjectViewModel project)
        {
            Debug.Assert(threadHandling.CheckAccess(), $"{nameof(SetBoundProject)} should only be accessed from the UI thread");
            this.ClearBoundProject();

            this.boundProject = project;
            this.boundProject.IsBound = true;
            this.boundProject.Owner.ShowAllProjects = false;

            this.OnHasBoundProjectChanged();
        }

        public void ClearBoundProject()
        {
            Debug.Assert(threadHandling.CheckAccess(), $"{nameof(ClearBoundProject)} should only be accessed from the UI thread");
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
