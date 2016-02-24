//-----------------------------------------------------------------------
// <copyright file="TelemetryLogger.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System.ComponentModel.Composition;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [Export(typeof(ITelemetryLogger)), PartCreationPolicy(CreationPolicy.Shared)]
    internal class TelemetryLogger : ITelemetryLogger
    {
        public void ReportEvent(TelemetryEvent telemetryEvent)
        {
            switch(telemetryEvent)
            {
                case TelemetryEvent.BoundSolutionDetected:
                    SonarLintSqmFacade.BoundSolutionDetected();
                    break;
                default:
                    Debug.Fail("Unsupported event: " + telemetryEvent);
                    break;
            }
        }
    }
}
