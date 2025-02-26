/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode;

public interface IServerConnectionsRepositoryAdapter
{
    bool TryGetAllConnections(out List<Connection> connections);

    bool TryGetAllConnectionsInfo(out List<ConnectionInfo> connectionInfos);

    bool TryRemoveConnection(ConnectionInfo connectionInfo);

    bool TryAddConnection(Connection connection, ICredentialsModel credentialsModel);

    bool TryUpdateCredentials(Connection connection, ICredentialsModel credentialsModel);

    bool TryGet(string serverConnectionId, out ServerConnection serverConnection);
}

[Export(typeof(IServerConnectionsRepositoryAdapter))]
[method: ImportingConstructor]
internal class ServerConnectionsRepositoryAdapter(IServerConnectionsRepository serverConnectionsRepository) : IServerConnectionsRepositoryAdapter
{
    public bool TryGetAllConnections(out List<Connection> connections)
    {
        var succeeded = serverConnectionsRepository.TryGetAll(out var serverConnections);
        connections = serverConnections?.Select(x => x.ToConnection()).ToList();
        return succeeded;
    }

    public bool TryGetAllConnectionsInfo(out List<ConnectionInfo> connectionInfos)
    {
        var succeeded = TryGetAllConnections(out var connections);
        connectionInfos = connections?.Select(conn => conn.Info).ToList();
        return succeeded;
    }

    public bool TryAddConnection(Connection connection, ICredentialsModel credentialsModel)
    {
        var serverConnection = connection.ToServerConnection();
        serverConnection.Credentials = MapCredentials(credentialsModel);
        return serverConnectionsRepository.TryAdd(serverConnection);
    }

    public bool TryUpdateCredentials(Connection connection, ICredentialsModel credentialsModel)
    {
        var serverConnection = connection.ToServerConnection();
        serverConnection.Credentials = MapCredentials(credentialsModel);
        return serverConnectionsRepository.TryUpdateCredentialsById(serverConnection.Id, serverConnection.Credentials);
    }

    public bool TryGet(string serverConnectionId, out ServerConnection serverConnection) =>
        serverConnectionsRepository.TryGet(serverConnectionId, out serverConnection);

    public bool TryRemoveConnection(ConnectionInfo connectionInfo)
    {
        var connectionId = connectionInfo.GetServerIdFromConnectionInfo();
        return serverConnectionsRepository.TryDelete(connectionId);
    }

    private static IConnectionCredentials MapCredentials(ICredentialsModel credentialsModel)
    {
        switch (credentialsModel)
        {
            case TokenCredentialsModel tokenCredentialsModel:
                return new TokenAuthCredentials(tokenCredentialsModel.Token);
            default:
                return null;
        }
    }
}

internal static class ServerConnectionsRepositoryAdapterExtensions
{
    public static bool TryGet(this IServerConnectionsRepositoryAdapter adapter, ConnectionInfo connection, out ServerConnection serverConnection) =>
        adapter.TryGet(connection.GetServerIdFromConnectionInfo(), out serverConnection);
}
