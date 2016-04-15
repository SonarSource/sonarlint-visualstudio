//-----------------------------------------------------------------------
// <copyright file="ITelemetryLogger.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


namespace SonarLint.VisualStudio.Integration
{
    public interface ITelemetryLogger
    {
        void ReportEvent(TelemetryEvent telemetryEvent);
    }
}
