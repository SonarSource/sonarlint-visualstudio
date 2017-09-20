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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.State
{
    /// <summary>
    /// Implementation of <see cref="IStateManager"/>
    /// </summary>
    internal sealed class StateManager : IStateManager, IDisposable
    {
        private bool isDisposed;

        public StateManager(IHost host, TransferableVisualState state)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            this.Host = host;
            this.ManagedState = state;
            this.ManagedState.PropertyChanged += this.OnStatePropertyChanged;
        }

        #region IStateManager
        public event EventHandler<bool> IsBusyChanged;

        public event EventHandler BindingStateChanged;

        public TransferableVisualState ManagedState
        {
            get;
        }

        public IHost Host
        {
            get;
        }

        public bool IsBusy
        {
            get
            {
                return this.ManagedState.IsBusy;
            }
            set
            {
                this.ManagedState.IsBusy = value;
            }
        }

        public bool HasBoundProject
        {
            get
            {
                return this.ManagedState.HasBoundProject;
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.GetConnectedServers().Any();
            }
        }

        public IEnumerable<ConnectionInformation> GetConnectedServers()
        {
            return this.ManagedState.ConnectedServers.Select(s => s.ConnectionInformation);
        }

        public ConnectionInformation GetConnectedServer(SonarQubeProject project)
        {
            return this.ManagedState.ConnectedServers
                .SingleOrDefault(s => s.Projects.Any(p => p.SonarQubeProject == project))?.ConnectionInformation;
        }

        public string BoundProjectKey { get; set; }

        public void SetProjects(ConnectionInformation connection, IEnumerable<SonarQubeProject> projects)
        {
            if (this.Host.UIDispatcher.CheckAccess())
            {
                this.SetProjectsUIThread(connection, projects);
            }
            else
            {
                this.Host.UIDispatcher.BeginInvoke(new Action(() => this.SetProjectsUIThread(connection, projects)));
            }
        }

        public void SetBoundProject(SonarQubeProject project)
        {
            this.ClearBindingErrorNotifications();
            ProjectViewModel projectViewModel = this.ManagedState.ConnectedServers.SelectMany(s => s.Projects).SingleOrDefault(p => p.SonarQubeProject == project);
            Debug.Assert(projectViewModel != null, "Expecting a single project mapped to project information");
            this.ManagedState.SetBoundProject(projectViewModel);
            Debug.Assert(this.HasBoundProject, "Expected to have a bound project");

            this.OnBindingStateChanged();
        }

        public void ClearBoundProject()
        {
            this.ClearBindingErrorNotifications();
            this.ManagedState.ClearBoundProject();
            Debug.Assert(!this.HasBoundProject, "Expected not to have a bound project");

            this.OnBindingStateChanged();
        }

        public void SyncCommandFromActiveSection()
        {
            foreach (ServerViewModel serverVM in this.ManagedState.ConnectedServers)
            {
                this.SetServerVMCommands(serverVM);
                this.SetServerProjectsVMCommands(serverVM);
            }
        }
        #endregion

        #region Non public API
        private void OnIsBusyChanged(bool isBusy)
        {
            this.IsBusyChanged?.Invoke(this, isBusy);
        }

        private void OnBindingStateChanged()
        {
            this.BindingStateChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnStatePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(this.ManagedState.IsBusy))
            {
                this.OnIsBusyChanged(this.IsBusy);
            }
        }

        private void SetProjectsUIThread(ConnectionInformation connection, IEnumerable<SonarQubeProject> projects)
        {
            Debug.Assert(connection != null);
            this.ClearBindingErrorNotifications();

            // !!! Avoid using the service to detect disconnects since it's not thread safe !!!
            if (projects == null)
            {
                // Disconnected, clear all
                this.ClearBoundProject();
                this.DisposeConnections();
                this.ManagedState.ConnectedServers.Clear();
            }
            else
            {
                var existingServerVM = this.ManagedState.ConnectedServers.SingleOrDefault(serverVM => serverVM.Url == connection.ServerUri);
                ServerViewModel serverViewModel;
                if (existingServerVM == null)
                {
                    // Add new server
                    serverViewModel = new ServerViewModel(connection);
                    this.SetServerVMCommands(serverViewModel);
                    this.ManagedState.ConnectedServers.Add(serverViewModel);
                }
                else
                {
                    // Update existing server
                    serverViewModel = existingServerVM;
                }

                serverViewModel.SetProjects(projects);
                Debug.Assert(serverViewModel.ShowAllProjects, "ShowAllProjects should have been set");
                this.SetServerProjectsVMCommands(serverViewModel);
                this.RestoreBoundProject(serverViewModel);
            }
        }

        private void DisposeConnections()
        {
            this.ManagedState.ConnectedServers
                .Select(s => s.ConnectionInformation)
                .ToList()
                .ForEach(c => c.Dispose());
        }

        private void ClearBindingErrorNotifications()
        {
            this.Host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToFindBoundProjectKeyId);
        }

        private void RestoreBoundProject(ServerViewModel serverViewModel)
        {
            if (this.BoundProjectKey == null)
            {
                // Nothing to restore
                return;
            }

            SonarQubeProject boundProject = serverViewModel.Projects.FirstOrDefault(pvm => SonarQubeProject.KeyComparer.Equals(pvm.Key, this.BoundProjectKey))?.SonarQubeProject;
            if (boundProject == null)
            {
                // Defensive coding: invoked asynchronous and it's safer to assume that value could be null
                // and just not do anything since if they are null it means that there's no solution open.
                this.Host.ActiveSection?.UserNotifications?.ShowNotificationError(string.Format(CultureInfo.CurrentCulture, Strings.BoundProjectNotFound, this.BoundProjectKey), NotificationIds.FailedToFindBoundProjectKeyId, null);
            }
            else
            {
                this.SetBoundProject(boundProject);
            }
        }

        private void SetServerVMCommands(ServerViewModel serverVM)
        {
            serverVM.Commands.Clear();
            if (this.Host.ActiveSection == null)
            {
                // Don't add command (which will be disabled).
                return;
            }


            var refreshContextualCommand = new ContextualCommandViewModel(serverVM, this.Host.ActiveSection.RefreshCommand)
            {
                DisplayText = Strings.RefreshCommandDisplayText,
                Tooltip = Strings.RefreshCommandTooltip,
                Icon = new IconViewModel(KnownMonikers.Refresh)
            };

            var disconnectContextualCommand = new ContextualCommandViewModel(serverVM, this.Host.ActiveSection.DisconnectCommand)
            {
                DisplayText = Strings.DisconnectCommandDisplayText,
                Tooltip = Strings.DisconnectCommandTooltip,
                Icon = new IconViewModel(KnownMonikers.Disconnect)
            };

            var browseServerContextualCommand = new ContextualCommandViewModel(serverVM.Url.ToString(), this.Host.ActiveSection.BrowseToUrlCommand)
            {
                DisplayText = Strings.BrowseServerMenuItemDisplayText,
                Tooltip = Strings.BrowserServerMenuItemTooltip,
                Icon = new IconViewModel(KnownMonikers.OpenWebSite)
            };

            var toggleShowAllProjectsCommand = new ContextualCommandViewModel(serverVM, this.Host.ActiveSection.ToggleShowAllProjectsCommand)
            {
                Tooltip = Strings.ToggleShowAllProjectsCommandTooltip
            };
            toggleShowAllProjectsCommand.SetDynamicDisplayText(x =>
            {
                ServerViewModel ctx = x as ServerViewModel;
                Debug.Assert(ctx != null, "Unexpected fixed context for ToggleShowAllProjects context command");
                return ctx?.ShowAllProjects ?? false ? Strings.HideUnboundProjectsCommandText : Strings.ShowAllProjectsCommandText;
            });

            serverVM.Commands.Add(refreshContextualCommand);
            serverVM.Commands.Add(disconnectContextualCommand);
            serverVM.Commands.Add(browseServerContextualCommand);
            serverVM.Commands.Add(toggleShowAllProjectsCommand);
        }

        private void SetServerProjectsVMCommands(ServerViewModel serverVM)
        {
            foreach (ProjectViewModel projectVM in serverVM.Projects)
            {
                projectVM.Commands.Clear();

                if (this.Host.ActiveSection == null)
                {
                    // Don't add command (which will be disabled).
                    continue;
                }

                var bindContextCommand = new ContextualCommandViewModel(projectVM, this.Host.ActiveSection.BindCommand);
                bindContextCommand.SetDynamicDisplayText(x =>
                {
                    var ctx = x as ProjectViewModel;
                    Debug.Assert(ctx != null, "Unexpected fixed context for bind context command");
                    return ctx?.IsBound ?? false ? Strings.SyncButtonText : Strings.BindButtonText;
                });
                bindContextCommand.SetDynamicIcon(x =>
                {
                    var ctx = x as ProjectViewModel;
                    Debug.Assert(ctx != null, "Unexpected fixed context for bind context command");
                    return new IconViewModel(ctx?.IsBound ?? false ? KnownMonikers.Sync : KnownMonikers.Link);
                });

                var openProjectDashboardCommand = new ContextualCommandViewModel(projectVM, this.Host.ActiveSection.BrowseToProjectDashboardCommand)
                {
                    DisplayText = Strings.ViewInSonarQubeMenuItemDisplayText,
                    Tooltip = Strings.ViewInSonarQubeMenuItemTooltip,
                    Icon = new IconViewModel(KnownMonikers.OpenWebSite)
                };

                projectVM.Commands.Add(bindContextCommand);
                projectVM.Commands.Add(openProjectDashboardCommand);
            }
        }
        #endregion

        #region IDisposable Support
        private void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.ManagedState.PropertyChanged -= this.OnStatePropertyChanged;
                    this.DisposeConnections();
                }

                this.isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
