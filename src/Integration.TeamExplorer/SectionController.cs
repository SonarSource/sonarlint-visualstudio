﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using SonarLint.VisualStudio.Integration.Binding;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.WPF;
using SonarQube.Client.Models;
using IVSOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
using TF_OLECMD = Microsoft.TeamFoundation.Client.CommandTarget.OLECMD;
using VS_OLECMD = Microsoft.VisualStudio.OLE.Interop.OLECMD;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    /// <summary>
    /// Controller for the SonarLint section in team explorer tool window
    /// The class is responsible for view and view model creation also hosting the commands
    /// relevant during the life time of the section (initialized when activated and disposed when navigated to a different section).
    /// </summary>
    [TeamExplorerSection(SectionController.SectionId, SonarQubePage.PageId, SectionController.Priority)]
    internal class SectionController : TeamExplorerSectionBase, ISectionController
    {
        public const string SectionId = "25AB05EF-8132-453E-A990-55587C0C5CD3";
        public const int Priority = 300;

        internal const int CommandNotHandled = (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;

        private readonly IWebBrowser webBrowser;

        [ImportingConstructor]
        public SectionController(IHost host, IWebBrowser webBrowser)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (webBrowser == null)
            {
                throw new ArgumentNullException(nameof(webBrowser));
            }

            this.Host = host;
            this.webBrowser = webBrowser;
        }

        internal /*for testing purposes*/ List<IVSOleCommandTarget> CommandTargets
        {
            get;
        } = new List<IVSOleCommandTarget>();

        protected IHost Host
        {
            get;
        }

        #region IConnectSection
        IProgressControlHost ISectionController.ProgressHost
        {
            get { return (IProgressControlHost)this.View; }
        }

        IConnectSectionView ISectionController.View
        {
            get { return (ConnectSectionView)this.View; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S3215:\"interface\" instances should not be cast to concrete types",
            Justification = "The base class is not defined by us, so we can't force the type to be something else",
            Scope = "member",
            Target = "~P:SonarLint.VisualStudio.Integration.TeamExplorer.SectionController.SonarLint#VisualStudio#Integration#TeamExplorer#IConnectSection#ViewModel")]
        IConnectSectionViewModel ISectionController.ViewModel
        {
            get { return (ConnectSectionViewModel)this.ViewModel; }
        }

        IUserNotification ISectionController.UserNotifications
        {
            get { return (IUserNotification)this.ViewModel; }
        }
        #endregion

        #region TeamExplorerSectionBase overrides

#if VS2019

        protected override void InitializeViewModel(SectionInitializeEventArgs e)
        {
            base.InitializeViewModel(e);

            // Warning - cargo cult code!
            // This property isn't documented. However, without it the Team Explorer will add a scroll bar to the section during
            // binding if the section height is not large enough to display the user control (so there can be two scrollbars, the
            // section scrollbar and the project tree view scrollbar).
            // With it, the control is correctly sized to the available height with just the project tree view scrollbar when required.
            // This property is was added in VS2019.
            ITeamExplorerSectionExtensions.SetProperty(this, SectionProperties.FillVerticalSpace, 600d);
        }
#endif

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return new ConnectSectionView();
        }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            return new ConnectSectionViewModel();
        }

        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            // Create the View & ViewModel
            base.Initialize(sender, e);

            this.Host.VisualStateManager.IsBusyChanged += this.OnIsBusyChanged;

            this.InitializeControllerCommands();
            this.InitializeProvidedCommands();
            this.SyncCommands();

            this.Host.SetActiveSection(this);
        }

        public override void Dispose()
        {
            this.Host.ClearActiveSection();

            this.Host.VisualStateManager.IsBusyChanged -= this.OnIsBusyChanged;
            this.CleanControllerCommands();
            this.CleanProvidedCommands();
            this.SyncCommands();

            // Dispose the View & ViewModel
            base.Dispose();
        }

        public override void Refresh()
        {
            base.Refresh();

            if (this.RefreshCommand.CanExecute(null))
            {
                this.RefreshCommand.Execute(null);
            }
        }
        
        /// <summary>
        /// Delegate QueryStatus to commands
        /// </summary>
        protected override int IOleCommandTargetQueryStatus(ref Guid pguidCmdGroup, uint cCmds,  
            TF_OLECMD[] prgCmds, IntPtr pCmdText)
        {
            var result = CommandNotHandled;

            var vsPrgCmds = GetVsPrgCmds(prgCmds);

            foreach (var commandTarget in this.CommandTargets)
            {
                result = commandTarget.QueryStatus(ref pguidCmdGroup, cCmds, vsPrgCmds, pCmdText);

                // If handed, stop the loop
                if (result != CommandNotHandled)
                {
                    break;
                }
            }

            return result;
        }

        private void OnIsBusyChanged(object sender, bool isBusy)
        {
            ((ISectionController)this).ViewModel.IsBusy = isBusy;
        }
        #endregion

        #region Commands
        public ICommand ConnectCommand
        {
            get;
            private set;
        }

        public ICommand<BindCommandArgs> BindCommand
        {
            get;
            private set;
        }

        public ICommand<string> BrowseToUrlCommand
        {
            get;
            private set;
        }

        public ICommand<ProjectViewModel> BrowseToProjectDashboardCommand
        {
            get;
            private set;
        }
        public ICommand ReconnectCommand { get; private set; }

        public ICommand<ConnectionInformation> RefreshCommand
        {
            get;
            internal /*for test purposes*/ set;
        }

        public ICommand DisconnectCommand
        {
            get;
            private set;
        }

        public ICommand<ServerViewModel> ToggleShowAllProjectsCommand
        {
            get;
            private set;
        }

        private void InitializeControllerCommands()
        {
            // Due to complexity of connect and bind we "outsource" the controlling part
            // to separate controllers which just expose commands
            var connectionController = new Connection.ConnectionController(this.Host);
            var bindingController = new Binding.BindingController(this.Host);

            this.CommandTargets.Add(connectionController);
            this.CommandTargets.Add(bindingController);

            this.ConnectCommand = connectionController.ConnectCommand;
            this.RefreshCommand = connectionController.RefreshCommand;
            this.BindCommand = bindingController.BindCommand;
        }

        private void CleanControllerCommands()
        {
            this.CommandTargets.Clear();

            this.ConnectCommand = null;
            this.RefreshCommand = null;
            this.BindCommand = null;

            ISectionController section = (ISectionController)this;
            if (section.ViewModel != null)
            {
                section.ViewModel.ConnectCommand = null;
                section.ViewModel.BindCommand = null;
                section.ViewModel.BrowseToUrlCommand = null;
            }
        }

        private void InitializeProvidedCommands()
        {
            // Simple commands provided by this class directly
            this.DisconnectCommand = new RelayCommand(this.Disconnect, this.CanDisconnect);
            this.ToggleShowAllProjectsCommand = new RelayCommand<ServerViewModel>(this.ToggleShowAllProjects, this.CanToggleShowAllProjects);
            this.BrowseToUrlCommand = new RelayCommand<string>(this.ExecBrowseToUrl, this.CanExecBrowseToUrl);
            this.BrowseToProjectDashboardCommand = new RelayCommand<ProjectViewModel>(this.ExecBrowseToProjectDashboard, this.CanExecBrowseToProjectDashboard);
            this.ReconnectCommand = new RelayCommand(() =>
                {
                    this.Disconnect();
                    this.ConnectCommand.Execute(null);
                },
                this.CanDisconnect);
        }

        private void CleanProvidedCommands()
        {
            this.DisconnectCommand = null;
            this.ToggleShowAllProjectsCommand = null;
            this.BrowseToUrlCommand = null;
            this.BrowseToProjectDashboardCommand = null;
        }

        private void SyncCommands()
        {
            ISectionController section = (ISectionController)this;
            if (section.ViewModel != null)
            {
                section.ViewModel.ConnectCommand = this.ConnectCommand;
                section.ViewModel.BindCommand = this.BindCommand;
                section.ViewModel.BrowseToUrlCommand = this.BrowseToUrlCommand;
            }
        }

        private bool CanDisconnect()
        {
            // We just support one at the moment, will need to pass in the server as an argument to this
            // command once we will want to support more than once connected server
            return this.Host.VisualStateManager.GetConnectedServers().Any();
        }

        private void Disconnect()
        {
            Debug.Assert(this.CanDisconnect());

            // Disconnect all (all being one)
            this.Host.VisualStateManager.GetConnectedServers().ToList().ForEach(c => this.Host.VisualStateManager.SetProjects(c, null));
            this.Host.SonarQubeService.Disconnect();
        }

        private bool CanToggleShowAllProjects(ServerViewModel server)
        {
            return server.Projects.Any(x => x.IsBound);
        }

        private void ToggleShowAllProjects(ServerViewModel server)
        {
            server.ShowAllProjects = !server.ShowAllProjects;
        }

        private bool CanExecBrowseToUrl(string url)
        {
            return Uri.IsWellFormedUriString(url, UriKind.Absolute);
        }

        private void ExecBrowseToUrl(string url)
        {
            Debug.Assert(this.CanExecBrowseToUrl(url), "Should not be able to execute!");

            this.webBrowser.NavigateTo(url);
        }

        private bool CanExecBrowseToProjectDashboard(ProjectViewModel project)
        {
            if (project != null && this.Host.SonarQubeService.IsConnected)
            {
                var url = this.Host.SonarQubeService.GetProjectDashboardUrl(project.Project.Key);
                return this.CanExecBrowseToUrl(url.ToString());
            }

            return false;
        }

        private void ExecBrowseToProjectDashboard(ProjectViewModel project)
        {
            Debug.Assert(this.CanExecBrowseToProjectDashboard(project), $"Shouldn't be able to execute {nameof(this.BrowseToProjectDashboardCommand)}");

            var url = this.Host.SonarQubeService.GetProjectDashboardUrl(project.Project.Key);
            this.webBrowser.NavigateTo(url.ToString());
        }

        #endregion Commands

        #region VS IOleCommandTarget conversion

        /*
         * The TeamFoundation client defines an IOleCommandTarget interface that is identical to the
         * VS IOleComandTarget. We want to limit our references to the TF-specific assemblies, so we
         * define our commands using the VS version.
         * This means we need to convert from the TF OLECMD structure to the equivalent VS OLECMD
         * when we are forwarding calls from this class to our VS OLE commands.
         */

        private static VS_OLECMD[] GetVsPrgCmds(TF_OLECMD[] prgCmds) =>
            prgCmds?.Select(ConvertTFtoVSOleCmd).ToArray();

        private static VS_OLECMD ConvertTFtoVSOleCmd(TF_OLECMD cmd) =>
            new VS_OLECMD
            {
                cmdf = cmd.cmdf,
                cmdID = cmd.cmdID
            };

        #endregion VS IOleCommandTarget conversion
    }
}
