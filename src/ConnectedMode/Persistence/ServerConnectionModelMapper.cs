/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

public interface IServerConnectionModelMapper
{
    ServerConnection GetServerConnection(ServerConnectionJsonModel jsonModel);
    ServerConnectionsListJsonModel GetServerConnectionsListJsonModel(IEnumerable<ServerConnection> serverConnections);
}

[Export(typeof(IServerConnectionModelMapper))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class ServerConnectionModelMapper : IServerConnectionModelMapper
{
    [ImportingConstructor]
    public ServerConnectionModelMapper() { }

    public ServerConnection GetServerConnection(ServerConnectionJsonModel jsonModel)
    {
        return jsonModel.ServerType switch
        {
            ConnectionServerType.SonarCloud => new ServerConnection.SonarCloud(jsonModel.OrganizationKey, jsonModel.Settings),
            ConnectionServerType.SonarQube => new ServerConnection.SonarQube(new Uri(jsonModel.ServerUri), jsonModel.Settings),
            _ => throw new InvalidOperationException($"Invalid {nameof(ConnectionServerType)} {jsonModel.ServerType}")
        };
    }

    public ServerConnectionsListJsonModel GetServerConnectionsListJsonModel(IEnumerable<ServerConnection> serverConnections)
    {
        var model = new ServerConnectionsListJsonModel();
        foreach (var serverConnection in serverConnections)
        {
            model.ServerConnections.Add(new ServerConnectionJsonModel
            {
                Id = serverConnection.Id,
                ServerType = serverConnection is ServerConnection.SonarCloud ? ConnectionServerType.SonarCloud : ConnectionServerType.SonarQube,
                Settings = serverConnection.Settings ?? throw new InvalidOperationException($"{nameof(ServerConnection.Settings)} can not be null"),
                OrganizationKey = GetOrganizationKey(serverConnection),
                ServerUri = GetServerUri(serverConnection)
            });
        }

        return model;
    }

    private static string GetOrganizationKey(ServerConnection serverConnection)
    {
        if (serverConnection is not ServerConnection.SonarCloud sonarCloud)
        {
            return null;
        }

        if(string.IsNullOrWhiteSpace(sonarCloud.OrganizationKey))
        {
            throw new InvalidOperationException($"{nameof(ServerConnection.SonarCloud.OrganizationKey)} can not be null");
        }

        return sonarCloud.OrganizationKey;
    }

    private static string GetServerUri(ServerConnection serverConnection)
    {
        if (serverConnection is not ServerConnection.SonarQube sonarQube)
        {
            return null;
        }

        if (sonarQube.ServerUri == null)
        {
            throw new InvalidOperationException($"{nameof(ServerConnection.SonarQube.ServerUri)} can not be null");
        }

        return sonarQube.ServerUri.ToString();
    }
}
