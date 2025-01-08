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

using SonarLint.VisualStudio.Core;
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
/// <param name="Id">The organization key for SonarCloud or the server uri for SonarQube</param>
/// <param name="ServerType">The type of server (SonarCloud, SonarQube)</param>
public record ConnectionInfo(string Id, ConnectionServerType ServerType)
{
    public static ConnectionInfo From(ServerConnection serverConnection)
    {
        return serverConnection switch
        {
            ServerConnection.SonarQube sonarQubeConnection => new ConnectionInfo(sonarQubeConnection.Id, ConnectionServerType.SonarQube),
            ServerConnection.SonarCloud sonarCloudConnection => new ConnectionInfo(sonarCloudConnection.OrganizationKey, ConnectionServerType.SonarCloud),
            _ => throw new ArgumentException(Resources.UnexpectedConnectionType)
        };
    }
}

public class Connection(ConnectionInfo info, bool enableSmartNotifications = true)
{
    public ConnectionInfo Info { get; } = info;
    public bool EnableSmartNotifications { get; set; } = enableSmartNotifications;
}

public static class ConnectionInfoExtensions
{
    public static string GetIdForTransientConnection(this ConnectionInfo connection)
    {
        if (connection.Id == null && connection.ServerType == ConnectionServerType.SonarCloud)
        {
            return CoreStrings.SonarCloudUrl;
        }
        return connection.Id;
    }

}
