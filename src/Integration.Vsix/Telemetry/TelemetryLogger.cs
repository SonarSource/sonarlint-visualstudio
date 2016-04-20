//-----------------------------------------------------------------------
// <copyright file="TelemetryLogger.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ITelemetryLogger)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class TelemetryLogger : ITelemetryLogger
    {
        private readonly IServiceProvider serviceProvider;
        private readonly Dictionary<TelemetryEvent, Action> eventLoggerMap = new Dictionary<TelemetryEvent, Action>();

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

            this.Initialize();
        }

        private void Initialize()
        {
            this.eventLoggerMap[TelemetryEvent.BoundSolutionDetected] = SonarLintSqmFacade.BoundSolutionDetected;
            this.eventLoggerMap[TelemetryEvent.ConnectCommandCommandCalled] = SonarLintSqmFacade.ConnectCommand;
            this.eventLoggerMap[TelemetryEvent.BindCommandCommandCalled] = SonarLintSqmFacade.BindCommand;
            this.eventLoggerMap[TelemetryEvent.BrowseToProjectDashboardCommandCommandCalled] = SonarLintSqmFacade.BrowseToProjectDashboardCommand;
            this.eventLoggerMap[TelemetryEvent.BrowseToUrlCommandCommandCalled] = SonarLintSqmFacade.BrowseToUrlCommand;
            this.eventLoggerMap[TelemetryEvent.DisconnectCommandCommandCalled] = SonarLintSqmFacade.DisconnectCommand;
            this.eventLoggerMap[TelemetryEvent.RefreshCommandCommandCalled] = SonarLintSqmFacade.RefreshCommand;
            this.eventLoggerMap[TelemetryEvent.ToggleShowAllProjectsCommandCommandCalled] = SonarLintSqmFacade.ToggleShowAllProjectsCommand;
            this.eventLoggerMap[TelemetryEvent.DontWarnAgainCommandCalled] = SonarLintSqmFacade.DontWarnAgainCommand;
            this.eventLoggerMap[TelemetryEvent.FixConflictsCommandCalled] = SonarLintSqmFacade.FixConflictsCommand;
            this.eventLoggerMap[TelemetryEvent.FixConflictShow] = SonarLintSqmFacade.FixConflictsShow;
            this.eventLoggerMap[TelemetryEvent.ErrorListInfoBarUpdateCalled] = SonarLintSqmFacade.ErrorListInfoBarUpdateCommand;
            this.eventLoggerMap[TelemetryEvent.ErrorListInfoBarShow] = SonarLintSqmFacade.ErrorListInfoBarShow;
        }

        public void ReportEvent(TelemetryEvent telemetryEvent)
        {
            Action logTelemetry;
            if (this.eventLoggerMap.TryGetValue(telemetryEvent, out logTelemetry))
            {
                logTelemetry();
            }
            else
            {
                Debug.Fail("Unsupported event: " + telemetryEvent);
            }
        }
    }
}
