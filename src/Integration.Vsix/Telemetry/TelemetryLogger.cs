/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA and Microsoft Corporation
 * mailto: contact AT sonarsource DOT com
 *
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
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
