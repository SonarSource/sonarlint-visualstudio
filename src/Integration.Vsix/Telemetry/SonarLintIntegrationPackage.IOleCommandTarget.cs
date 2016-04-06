//-----------------------------------------------------------------------
// <copyright file="SonarLintIntegrationPackage.IOleCommandTarget.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.Diagnostics;
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
            IOleCommandTarget target = this.GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
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
                IOleCommandTarget target = this.GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
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