/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using OLEConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Partial class to provide handling for SQM commands.
    /// </summary>
    [ProvideMenuResource("Menus.ctmenu", 1)]
    partial class SonarLintIntegrationPackage : IOleCommandTarget
    {
        private SonarLintSqmCommandTarget sqmCommandHandler;

        private void InitializeSqm()
        {
            this.sqmCommandHandler = new SonarLintSqmCommandTarget();

            // Initialize SQM, can be initialized from multiple places (will no-op once initialized)
            SonarLintSqmFacade.Initialize(this);

        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            Debug.Assert(this.sqmCommandHandler != null, "SQM handler should not be null");

            // Delegate to SQM handler if commandIds are in SQM range.
            if (SonarLintSqmCommandTarget.IsSqmCommand(pguidCmdGroup, (int)nCmdID))
            {
                return this.sqmCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            // Otherwise delegate to the package's default implementation.
            IOleCommandTarget target = this.GetService<IOleCommandTarget>();
            if (target != null)
            {
                return target.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            return (int)OLEConstants.OLECMDERR_E_NOTSUPPORTED;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            Debug.Assert(this.sqmCommandHandler != null, "SQM handler should not be null");

            // Delegate to SQM handler to see if the if commandIds are in SQM range.
            int result = this.sqmCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

            Debug.Assert(result == (int)OLEConstants.OLECMDERR_E_NOTSUPPORTED ||
                result == VSConstants.S_OK, "Unexpected return value from the generated SQM target handler");

            if (!ErrorHandler.Succeeded(result))
            {
                // Otherwise delegate to the package's default implementation.
                IOleCommandTarget target = this.GetService<IOleCommandTarget>();
                if (target != null)
                {
                    result = target.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
                }
                else
                {
                    result = VSConstants.OLE_E_ADVISENOTSUPPORTED;
                }
            }
            return result;
        }
    }
}
