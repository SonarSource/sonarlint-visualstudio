//-----------------------------------------------------------------------
// <copyright file="HostedCommandControllerBase.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Client.CommandTarget;
using System;

namespace SonarLint.VisualStudio.Integration
{
    internal abstract class HostedCommandControllerBase : IOleCommandTarget
    {
        protected HostedCommandControllerBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider
        {
            get;
        }

        #region IOleCommandTarget
        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return this.OnQueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return this.OnExec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }
        #endregion

        #region Ole command extensibility
        /// <summary>
        /// Redirected call from <see cref="IOleCommandTarget.QueryStatus(ref Guid, uint, OLECMD[], IntPtr)"/>
        /// </summary>
        protected virtual int OnQueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return (int)OleConstants.OLECMDERR_E_UNKNOWNGROUP;

        }

        /// <summary>
        /// Redirected call from <see cref="IOleCommandTarget.Exec(ref Guid, uint, uint, IntPtr, IntPtr)"/>
        /// </summary>
        protected virtual int OnExec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            return (int)OleConstants.OLECMDERR_E_UNKNOWNGROUP;
        }
        #endregion
    }
}
