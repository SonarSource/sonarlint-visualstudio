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

namespace SonarLint.VisualStudio.ConnectedMode;

/// <summary>
/// The steps of creating a server connection, shared by the New Connection and Edit Credentials dialogs
/// and the hidden E2E commands.
/// </summary>
public interface IServerConnectionCreator
{
    /// <summary>Checks the credentials against the server.</summary>
    Task<ResponseStatus> ValidateAsync(ConnectionInfo connectionInfo, ICredentialsModel credentialsModel);

    /// <summary>Saves a new connection and its credentials. Fails if a connection with the same ID already exists.</summary>
    ResponseStatus Add(Connection connection, ICredentialsModel credentialsModel);
}

[Export(typeof(IServerConnectionCreator))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ServerConnectionCreator(ISlCoreConnectionAdapter slCoreConnectionAdapter, IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter)
    : IServerConnectionCreator
{
    public Task<ResponseStatus> ValidateAsync(ConnectionInfo connectionInfo, ICredentialsModel credentialsModel) =>
        slCoreConnectionAdapter.ValidateConnectionAsync(connectionInfo, credentialsModel);

    public ResponseStatus Add(Connection connection, ICredentialsModel credentialsModel)
    {
        if (serverConnectionsRepositoryAdapter.TryGet(connection.Info, out var serverConnection) && serverConnection != null)
        {
            return new ResponseStatus(false, UiResources.ConnectionAlreadyExistsText);
        }

        return new ResponseStatus(serverConnectionsRepositoryAdapter.TryAddConnection(connection, credentialsModel));
    }
}
