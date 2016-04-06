//-----------------------------------------------------------------------
// <copyright file="TelemetryLogger.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ITelemetryLogger)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class TelemetryLogger : ITelemetryLogger
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

            // Initialize SQM, can be initialized from multiple places (will no-op once initialized)
            SonarLintSqmFacade.Initialize(serviceProvider);
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
    }
}
