/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.Automation;

/// <summary>
/// Creates a SonarQube Server connection and binds the open solution without showing any dialog, by driving the
/// view models of the Connected Mode dialogs the way their buttons do. Only meant for the hidden E2E test commands.
/// </summary>
public interface IConnectedModeAutomation
{
    /// <summary>What the New Connection dialogs do: validate the token, then save the connection.</summary>
    Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token);

    /// <summary>What the Bind button does, with the connection to <paramref name="serverUrl"/> and <paramref name="projectKey"/> selected.</summary>
    Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey);
}

[Export(typeof(IConnectedModeAutomation))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ConnectedModeAutomation(
    IConnectedModeServices connectedModeServices,
    IConnectedModeBindingServices connectedModeBindingServices,
    IConnectedModeUIManager connectedModeUiManager)
    : IConnectedModeAutomation
{
    public async Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token)
    {
        var connectionInfo = new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube);

        // The Credentials dialog's OK button.
        var credentials = new CredentialsViewModel(connectionInfo, connectedModeServices.SlCoreConnectionAdapter, CreateProgressReporter())
        {
            Token = token.ToSecureString()
        };
        if (!await credentials.ValidateConnectionAsync())
        {
            return Outcome(credentials.ProgressReporterViewModel);
        }

        // What the Manage Connections dialog does once the Credentials dialog closes.
        var manageConnections = new ManageConnectionsViewModel(connectedModeUiManager, connectedModeServices, connectedModeBindingServices, CreateProgressReporter());
        await manageConnections.CreateConnectionsWithProgressAsync(new Connection(connectionInfo), credentials.GetCredentialsModel());
        return Outcome(manageConnections.ProgressReporterViewModel);
    }

    public async Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey)
    {
        var connectionId = new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube).GetServerIdFromConnectionInfo();

        // The Manage Binding dialog's Bind button.
        using var manageBinding = new ManageBindingViewModel(connectedModeServices, connectedModeBindingServices, connectedModeUiManager, CreateProgressReporter());
        await manageBinding.PerformBindingWithProgressAsync(new BindingRequest.Manual(projectKey, connectionId));
        return Outcome(manageBinding.ProgressReporter);
    }

    private ProgressReporterViewModel CreateProgressReporter() => new(connectedModeServices.Logger);

    // The view models report the result where the dialogs show it: a failure always sets a warning.
    private static ResponseStatus Outcome(IProgressReporterViewModel progressReporter) =>
        progressReporter.HasWarning ? new ResponseStatus(false, progressReporter.Warning) : new ResponseStatus(true);
}
