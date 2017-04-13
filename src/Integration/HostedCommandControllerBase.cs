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
