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
        private readonly Dictionary<TelemetryEvent, Action> eventLoggerMap = new Dictionary<TelemetryEvent, Action>();

        [ImportingConstructor]
        public TelemetryLogger([Import(typeof(SVsServiceProvider))] System.IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

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
