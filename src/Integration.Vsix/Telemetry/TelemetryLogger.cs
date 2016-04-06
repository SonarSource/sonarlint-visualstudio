//-----------------------------------------------------------------------
// <copyright file="TelemetryLogger.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using OLEConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ITelemetryLogger)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class TelemetryLogger : ITelemetryLogger , IOleCommandTarget
    {
        private readonly System.IServiceProvider serviceProvider;
        private readonly SonarLintSqmCommandTarget sqmCommandHandler;

        [ImportingConstructor]
        public TelemetryLogger([Import(typeof(SVsServiceProvider))] System.IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;

            // Initialize SQM
            SonarLintSqmFacade.Initialize(serviceProvider);
            this.sqmCommandHandler = new SonarLintSqmCommandTarget();
        }

        public void ReportEvent(TelemetryEvent telemetryEvent)
        {
            switch(telemetryEvent)
            {
                case TelemetryEvent.BoundSolutionDetected:
                    SonarLintSqmFacade.BoundSolutionDetected();
                    break;
                case TelemetryEvent.ConnectCommandCommandCalled:
                    SonarLintSqmFacade.ConnectCommand();
                    break;
                case TelemetryEvent.BindCommandCommandCalled:
                    SonarLintSqmFacade.BindCommand();
                    break;
                case TelemetryEvent.BrowseToProjectDashboardCommandCommandCalled:
                    SonarLintSqmFacade.BrowseToProjectDashboardCommand();
                    break;
                case TelemetryEvent.BrowseToUrlCommandCommandCalled:
                    SonarLintSqmFacade.BrowseToUrlCommand();
                    break;
                case TelemetryEvent.DisconnectCommandCommandCalled:
                    SonarLintSqmFacade.DisconnectCommand();
                    break;
                case TelemetryEvent.RefreshCommandCommandCalled:
                    SonarLintSqmFacade.RefreshCommand();
                    break;
                case TelemetryEvent.ToggleShowAllProjectsCommandCommandCalled:
                    SonarLintSqmFacade.ToggleShowAllProjectsCommand();
                    break;
                case TelemetryEvent.DontWarnAgainCommandCalled:
                    SonarLintSqmFacade.DontWarnAgainCommand();
                    break;
                case TelemetryEvent.FixConflictsCommandCalled:
                    SonarLintSqmFacade.FixConflictsCommand();
                    break;
                default:
                    Debug.Fail("Unsupported event: " + telemetryEvent);
                    break;
            }
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
            IOleCommandTarget target = this.serviceProvider.GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
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
                IOleCommandTarget target = this.serviceProvider.GetService(typeof(IOleCommandTarget)) as IOleCommandTarget;
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
