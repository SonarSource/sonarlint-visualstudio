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

using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode;

public enum ConnectionServerType
{
    SonarQube,
    SonarCloud
}

/// <summary>
/// Model containing connection information, intended to be used by UI components
/// </summary>
public record ConnectionInfo
{
    /// <summary>
    /// Model containing connection information, intended to be used by UI components
    /// </summary>
    /// <param name="id">The organization key for SonarCloud or the server uri for SonarQube</param>
    /// <param name="serverType">The type of server (SonarCloud, SonarQube)</param>
    /// <param name="cloudServerRegion"> Only applicable to <see cref="ConnectionServerType.SonarCloud"/>. If not provided, defaults to <see cref="Core.Binding.CloudServerRegion.Eu"/>
    /// It is ignored for <see cref="ConnectionServerType.SonarQube"/> </param>
    public ConnectionInfo(string id, ConnectionServerType serverType, CloudServerRegion cloudServerRegion = null)
    {
        Id = id;
        ServerType = serverType;
        CloudServerRegion = serverType == ConnectionServerType.SonarCloud ? (cloudServerRegion ?? CloudServerRegion.Eu) : null;
    }

    public static ConnectionInfo From(ServerConnection serverConnection) =>
        serverConnection switch
        {
            ServerConnection.SonarQube sonarQubeConnection => new ConnectionInfo(sonarQubeConnection.Id, ConnectionServerType.SonarQube),
            ServerConnection.SonarCloud sonarCloudConnection => new ConnectionInfo(sonarCloudConnection.OrganizationKey, ConnectionServerType.SonarCloud, sonarCloudConnection.Region),
            _ => throw new ArgumentException(Resources.UnexpectedConnectionType)
        };

    public string Id { get; }
    public ConnectionServerType ServerType { get; }
    public CloudServerRegion CloudServerRegion { get; }
}

public class Connection(ConnectionInfo info, bool enableSmartNotifications = true)
{
    public ConnectionInfo Info { get; } = info;
    public bool EnableSmartNotifications { get; set; } = enableSmartNotifications;
}

public static class ConnectionExtensions
{
    public static ServerConnection ToServerConnection(this Connection connection)
    {
        if (connection.Info.ServerType == ConnectionServerType.SonarCloud)
        {
            return new ServerConnection.SonarCloud(connection.Info.Id, connection.Info.CloudServerRegion, new ServerConnectionSettings(connection.EnableSmartNotifications));
        }

        return new ServerConnection.SonarQube(new Uri(connection.Info.Id), new ServerConnectionSettings(connection.EnableSmartNotifications));
    }

    public static Connection ToConnection(this ServerConnection serverConnection)
    {
        var connectionInfo = ConnectionInfo.From(serverConnection);
        return new Connection(connectionInfo, serverConnection.Settings.IsSmartNotificationsEnabled);
    }
}

public static class ConnectionInfoExtensions
{
    public static string GetServerIdFromConnectionInfo(this ConnectionInfo connectionInfo) => GetServerConnectionFromConnectionInfo(connectionInfo).Id;

    public static ServerConnection GetServerConnectionFromConnectionInfo(this ConnectionInfo connectionInfo)
    {
        ServerConnection partialServerConnection = connectionInfo.ServerType == ConnectionServerType.SonarCloud
            ? new ServerConnection.SonarCloud(connectionInfo.Id, connectionInfo.CloudServerRegion)
            : new ServerConnection.SonarQube(new Uri(connectionInfo.Id));

        return partialServerConnection;
    }

    public static bool IsSameAs(this ConnectionInfo connectionInfo, ServerConnection serverConnection)
    {
        var connectionInfoToCompare = ConnectionInfo.From(serverConnection);

        return connectionInfoToCompare.Id == connectionInfo.Id &&
               connectionInfoToCompare.CloudServerRegion == connectionInfo.CloudServerRegion &&
               connectionInfoToCompare.ServerType == connectionInfo.ServerType;
    }
}
