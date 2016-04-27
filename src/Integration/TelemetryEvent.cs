//-----------------------------------------------------------------------
// <copyright file="TelemetryEvent.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------


namespace SonarLint.VisualStudio.Integration
{
    public enum TelemetryEvent
    {
        BoundSolutionDetected,

        // Commands
        ConnectCommandCommandCalled,
        BindCommandCommandCalled,
        BrowseToUrlCommandCommandCalled,
        BrowseToProjectDashboardCommandCommandCalled,
        RefreshCommandCommandCalled,
        DisconnectCommandCommandCalled,
        ToggleShowAllProjectsCommandCommandCalled,
        DontWarnAgainCommandCalled,
        FixConflictsCommandCalled,
        FixConflictShow,

        // Info bar
        ErrorListInfoBarShow,
        ErrorListInfoBarUpdateCalled
    }
}
