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
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.Automation;

/// <summary>
/// Creates a SonarQube Server connection and binds the open solution without any UI, through the same
/// services the Connected Mode dialogs use. Only meant for the hidden end-to-end test commands.
/// </summary>
public interface IConnectedModeAutomation
{
    /// <summary>
    /// Validates the token against the server, then saves the connection and its token,
    /// like the New Connection dialogs do.
    /// </summary>
    Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token);

    /// <summary>
    /// Binds the open solution to <paramref name="projectKey"/> through the existing connection to
    /// <paramref name="serverUrl"/>, like the Bind button of the Manage Binding dialog does.
    /// </summary>
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
    internal const string InvalidServerUrlText = "The server URL must be an absolute http or https URL.";
    internal const string MissingArgumentText = "A non-empty value is required.";
    internal const string SaveConnectionFailedText = "The connection could not be saved.";

    public async Task<ResponseStatus> AddSonarQubeConnectionAsync(string serverUrl, string token)
    {
        if (!IsHttpUrl(serverUrl))
        {
            return new ResponseStatus(false, InvalidServerUrlText);
        }
        if (string.IsNullOrWhiteSpace(token))
        {
            return new ResponseStatus(false, MissingArgumentText);
        }

        var connection = new Connection(new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube));
        var repository = connectedModeServices.ServerConnectionsRepositoryAdapter;
        if (repository.TryGet(connection.Info, out var existingConnection) && existingConnection != null)
        {
            return new ResponseStatus(false, UiResources.ConnectionAlreadyExistsText);
        }

        var credentials = new TokenCredentialsModel(token.ToSecureString());
        var validation = await connectedModeServices.SlCoreConnectionAdapter.ValidateConnectionAsync(connection.Info, credentials);
        if (!validation.Success)
        {
            return validation;
        }

        return repository.TryAddConnection(connection, credentials)
            ? new ResponseStatus(true)
            : new ResponseStatus(false, SaveConnectionFailedText);
    }

    public async Task<ResponseStatus> BindOpenSolutionAsync(string serverUrl, string projectKey)
    {
        if (!IsHttpUrl(serverUrl))
        {
            return new ResponseStatus(false, InvalidServerUrlText);
        }
        if (string.IsNullOrWhiteSpace(projectKey))
        {
            return new ResponseStatus(false, MissingArgumentText);
        }

        var connectionId = new ConnectionInfo(serverUrl, ConnectionServerType.SonarQube).GetServerIdFromConnectionInfo();
        var result = await connectedModeBindingServices.BindingControllerAdapter.ValidateAndBindAsync(
            new BindingRequest.Manual(projectKey, connectionId),
            connectedModeUiManager,
            CancellationToken.None);
        return new ResponseStatus(result.IsSuccessful, result.ProblemDescription);
    }

    // An absolute URI alone is not enough: "localhost:9000" parses as one, with "localhost" as its scheme.
    private static bool IsHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
