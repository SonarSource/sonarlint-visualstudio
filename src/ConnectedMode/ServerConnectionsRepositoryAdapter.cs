﻿/*
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
using System.Security;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode;

public interface IServerConnectionsRepositoryAdapter
{
    bool TryGetAllConnections(out List<Connection> connections);
    bool TryGetAllConnectionsInfo(out List<ConnectionInfo> connectionInfos);
    bool TryRemoveConnection(ConnectionInfo connectionInfo);
    bool TryAddConnection(Connection connection, ICredentialsModel credentialsModel);
    bool TryUpdateCredentials(Connection connection, ICredentialsModel credentialsModel);
    bool TryGet(ConnectionInfo connectionInfo, out ServerConnection serverConnection);
}

[Export(typeof(IServerConnectionsRepositoryAdapter))]
[method: ImportingConstructor]
internal class ServerConnectionsRepositoryAdapter(IServerConnectionsRepository serverConnectionsRepository) : IServerConnectionsRepositoryAdapter
{
    public bool TryGetAllConnections(out List<Connection> connections)
    {
        var succeeded = serverConnectionsRepository.TryGetAll(out var serverConnections);
        connections = serverConnections?.Select(MapServerConnectionModel).ToList();
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
        var serverConnection = MapConnection(connection);
        serverConnection.Credentials = MapCredentials(credentialsModel);
        return serverConnectionsRepository.TryAdd(serverConnection);
    }

    public bool TryUpdateCredentials(Connection connection, ICredentialsModel credentialsModel)
    {
        var serverConnection = MapConnection(connection);
        serverConnection.Credentials = MapCredentials(credentialsModel);
        return serverConnectionsRepository.TryUpdateCredentialsById(serverConnection.Id, serverConnection.Credentials);
    }

    public bool TryGet(ConnectionInfo connectionInfo, out ServerConnection serverConnection)
    {
        var connectionId = GetServerIdFromConnectionInfo(connectionInfo);
        return serverConnectionsRepository.TryGet(connectionId, out serverConnection);
    }

    public bool TryRemoveConnection(ConnectionInfo connectionInfo)
    {
        var connectionId = GetServerIdFromConnectionInfo(connectionInfo);
        return serverConnectionsRepository.TryDelete(connectionId);
    }

    private static Connection MapServerConnectionModel(ServerConnection serverConnection)
    {
        var connectionInfo = ConnectionInfo.From(serverConnection);
        return new Connection(connectionInfo, serverConnection.Settings.IsSmartNotificationsEnabled);
    }

    private static ServerConnection MapConnection(Connection connection)
    {
        if (connection.Info.ServerType == ConnectionServerType.SonarCloud)
        {
            return new ServerConnection.SonarCloud(connection.Info.Id, new ServerConnectionSettings(connection.EnableSmartNotifications));
        }

        return new ServerConnection.SonarQube(new Uri(connection.Info.Id), new ServerConnectionSettings(connection.EnableSmartNotifications));
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

    private static string GetServerIdFromConnectionInfo(ConnectionInfo connectionInfo)
    {
        ServerConnection partialServerConnection = connectionInfo.ServerType == ConnectionServerType.SonarCloud
            ? new ServerConnection.SonarCloud(connectionInfo.Id)
            : new ServerConnection.SonarQube(new Uri(connectionInfo.Id));

        return partialServerConnection.Id;
    }
}
