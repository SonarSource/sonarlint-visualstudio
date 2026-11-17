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
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.Automation;

/// <summary>
/// Creates a SonarQube Server connection and binds the open solution without any UI, through the services
/// the Connected Mode dialogs' view models call. Only meant for the hidden E2E test commands.
/// </summary>
public interface IConnectedModeAutomation
{
    /// <summary>Validates the token against the server, then saves the connection and its token.</summary>
    Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token);

    /// <summary>Sends the Bind button's manual binding request for <paramref name="projectKey"/> through the connection to <paramref name="serverUrl"/>.</summary>
    Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey);
}

[Export(typeof(IConnectedModeAutomation))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ConnectedModeAutomation(
    ISlCoreConnectionAdapter slCoreConnectionAdapter,
    IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter,
    IBindingControllerAdapter bindingControllerAdapter,
    IConnectedModeUIManager connectedModeUiManager)
    : IConnectedModeAutomation
{
    internal const string SaveConnectionFailedText = "The connection could not be saved (it may already exist).";

    public async Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token)
    {
        var connection = new Connection(new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube));
        var credentials = new TokenCredentialsModel(token.ToSecureString());

        // What the Credentials dialog runs on OK, then what saving the new connection runs.
        var validation = await slCoreConnectionAdapter.ValidateConnectionAsync(connection.Info, credentials);
        if (!validation.Success)
        {
            return validation;
        }
        return serverConnectionsRepositoryAdapter.TryAddConnection(connection, credentials)
            ? new ResponseStatus(true)
            : new ResponseStatus(false, SaveConnectionFailedText);
    }

    public async Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey)
    {
        var connectionId = new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube).GetServerIdFromConnectionInfo();
        var result = await bindingControllerAdapter.ValidateAndBindAsync(
            new BindingRequest.Manual(projectKey, connectionId),
            connectedModeUiManager,
            CancellationToken.None);
        return new ResponseStatus(result.IsSuccessful, result.ProblemDescription);
    }
}
