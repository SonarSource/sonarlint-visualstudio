//-----------------------------------------------------------------------
// <copyright file="ConnectSection.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Client.CommandTarget;
using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    /// <summary>
    /// SonarQube section on the connect page
    /// </summary>
    [TeamExplorerSection(ConnectSection.SectionId, SonarQubePage.PageId, ConnectSection.Priority)]
    internal class ConnectSection : TeamExplorerSectionBase, IConnectSection
    {
        public const string SectionId = "25AB05EF-8132-453E-A990-55587C0C5CD3";
        public const int Priority = 300;

        internal const int CommandNotHandled = (int)OleConstants.OLECMDERR_E_UNKNOWNGROUP;

        [ImportingConstructor]
        public ConnectSection([Import] ConnectSectionController controller)
        {
            this.Controller = controller;

            this.CommandTargets.Add(this.Controller.ConnectCommand);
            this.CommandTargets.Add(this.Controller.BindCommand);
        }

        internal /*for test purposes*/ ConnectSectionController Controller
        {
            get;
            set;
        }

        #region IConnectSection
        DependencyObject IConnectSection.View
        {
            get { return this.View as DependencyObject; }
        }

        ConnectSectionViewModel IConnectSection.ViewModel
        {
            get { return this.ViewModel as ConnectSectionViewModel; }
        }
        #endregion

        #region TeamExplorerSectionBase
        internal /*for testing purposes*/ List<IOleCommandTarget> CommandTargets
        {
            get;
        } = new List<IOleCommandTarget>();

        public override void Initialize(object sender, SectionInitializeEventArgs e)
        {
            base.Initialize(sender, e);

            this.Controller.Attach(this);
        }

        public override void Refresh()
        {
            base.Refresh();

            if (this.Controller.RefreshCommand.CanExecute(null))
            {
                this.Controller.RefreshCommand.Execute(null);
            }
        }

        /// <summary>
        /// Delegate QueryStatus to commands
        /// </summary>
        protected override int IOleCommandTargetQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            int result = CommandNotHandled;
            foreach (IOleCommandTarget commandTarget in this.CommandTargets)
            {
                result = commandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

                // If handed, stop the loop
                if (result != CommandNotHandled)
                {
                    break; 
                }
            }

            return result;
        }

        protected override int IOleCommandTargetExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return base.IOleCommandTargetExec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        protected override object CreateView(SectionInitializeEventArgs e)
        {
            return new ConnectSectionView();
        }

        protected override ITeamExplorerSection CreateViewModel(SectionInitializeEventArgs e)
        {
            return new ConnectSectionViewModel();
        }

        public override void Dispose()
        {
            this.Controller.Detach(this);

            base.Dispose();
        }

        #endregion

    }
}
