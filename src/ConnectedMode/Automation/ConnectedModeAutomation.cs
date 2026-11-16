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
/// Creates a SonarQube Server connection and binds the open solution without UI, by calling the same
/// production code as the Connected Mode dialogs. Only meant for the hidden E2E test commands.
/// </summary>
public interface IConnectedModeAutomation
{
    /// <summary>Validates the token, then saves the connection, as the New Connection dialogs do.</summary>
    Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token);

    /// <summary>Binds the open solution through the connection to <paramref name="serverUrl"/>, as the Bind button does.</summary>
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
        var connection = new Connection(new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube));
        var credentials = new TokenCredentialsModel(token.ToSecureString());
        var creator = connectedModeServices.ServerConnectionCreator;

        var validation = await creator.ValidateAsync(connection.Info, credentials);
        return validation.Success ? creator.Add(connection, credentials) : validation;
    }

    public async Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey)
    {
        var connectionId = new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube).GetServerIdFromConnectionInfo();
        var result = await connectedModeBindingServices.BindingControllerAdapter.ValidateAndBindAsync(
            new BindingRequest.Manual(projectKey, connectionId),
            connectedModeUiManager,
            CancellationToken.None);
        return new ResponseStatus(result.IsSuccessful, result.ProblemDescription);
    }
}
