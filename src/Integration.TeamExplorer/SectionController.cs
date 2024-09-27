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

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.WPF;
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
    [TeamExplorerSection(SectionId, SonarQubePage.PageId, Priority)]
    internal class SectionController : TeamExplorerSectionBase, ISectionController
    {
        public const string SectionId = "25AB05EF-8132-453E-A990-55587C0C5CD3";
        public const int Priority = 300;

        internal const int CommandNotHandled = (int)Microsoft.VisualStudio.OLE.Interop.Constants.OLECMDERR_E_UNKNOWNGROUP;

        private readonly IServiceProvider serviceProvider;
        private readonly IWebBrowser webBrowser;

        [ImportingConstructor]
        public SectionController(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            IHost host,
            IWebBrowser webBrowser)
        {
            this.serviceProvider = serviceProvider;
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

        [SuppressMessage("Reliability",
            "S3215:\"interface\" instances should not be cast to concrete types",
            Justification = "The base class is not defined by us, so we can't force the type to be something else",
            Scope = "member",
            Target = "~P:SonarLint.VisualStudio.Integration.TeamExplorer.SectionController.SonarLint#VisualStudio#Integration#TeamExplorer#IConnectSection#ViewModel")]
        IConnectSectionViewModel ISectionController.ViewModel
        {
            get { return (ConnectSectionViewModel)this.ViewModel; }
        }

        #endregion

        #region TeamExplorerSectionBase overrides

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

            this.InitializeProvidedCommands();

            this.Host.SetActiveSection(this);
        }

        public override void Dispose()
        {
            this.Host.ClearActiveSection();

            this.Host.VisualStateManager.IsBusyChanged -= this.OnIsBusyChanged;
            CommandTargets.Clear();
            this.CleanProvidedCommands();

            // Dispose the View & ViewModel
            base.Dispose();
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

        public ICommand<ServerViewModel> ToggleShowAllProjectsCommand
        {
            get;
            private set;
        }

        private void InitializeProvidedCommands()
        {
            // Simple commands provided by this class directly
            this.ToggleShowAllProjectsCommand = new RelayCommand<ServerViewModel>(this.ToggleShowAllProjects, this.CanToggleShowAllProjects);
        }

        private void CleanProvidedCommands()
        {
            this.ToggleShowAllProjectsCommand = null;
        }

        private bool CanToggleShowAllProjects(ServerViewModel server)
        {
            return server.Projects.Any(x => x.IsBound);
        }

        private void ToggleShowAllProjects(ServerViewModel server)
        {
            server.ShowAllProjects = !server.ShowAllProjects;
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
