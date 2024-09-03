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
        if (IsServerConnectionForSonarCloud(jsonModel))
        {
            return new ServerConnection.SonarCloud(jsonModel.OrganizationKey, jsonModel.Settings);
        }
        if (IsServerConnectionForSonarQube(jsonModel))
        {
            return new ServerConnection.SonarQube(new Uri(jsonModel.ServerUri), jsonModel.Settings);
        }

        throw new InvalidOperationException($"Invalid {nameof(ServerConnectionJsonModel)}. {nameof(ServerConnection)} could not be created");
    }

    public ServerConnectionsListJsonModel GetServerConnectionsListJsonModel(IEnumerable<ServerConnection> serverConnections)
    {
        var model = new ServerConnectionsListJsonModel
        {
            ServerConnections = serverConnections.Select(GetServerConnectionJsonModel).ToList()
        };

        return model;
    }

    internal static bool IsServerConnectionForSonarCloud(ServerConnectionJsonModel jsonModel)
    {
        return IsOrganizationKeyFilled(jsonModel) && !IsServerUriFilled(jsonModel);
    }

    internal static bool IsServerConnectionForSonarQube(ServerConnectionJsonModel jsonModel)
    {
        return IsServerUriFilled(jsonModel) && !IsOrganizationKeyFilled(jsonModel);
    }

    private static ServerConnectionJsonModel GetServerConnectionJsonModel(ServerConnection serverConnection)
    {
        return new ServerConnectionJsonModel
        {
            Id = serverConnection.Id,
            Settings = serverConnection.Settings ?? throw new InvalidOperationException($"{nameof(ServerConnection.Settings)} can not be null"),
            OrganizationKey = GetOrganizationKey(serverConnection),
            ServerUri = GetServerUri(serverConnection)
        };
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

    private static bool IsServerUriFilled(ServerConnectionJsonModel jsonModel)
    {
        return !string.IsNullOrWhiteSpace(jsonModel.ServerUri);
    }

    private static bool IsOrganizationKeyFilled(ServerConnectionJsonModel jsonModel)
    {
        return !string.IsNullOrWhiteSpace(jsonModel.OrganizationKey);
    }
}
