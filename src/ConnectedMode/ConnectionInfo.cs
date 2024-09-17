﻿/*
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

using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode;

public enum ConnectionServerType
{
    SonarQube,
    SonarCloud
}

public record ConnectionInfo(string Id, ConnectionServerType ServerType)
{
    public static ConnectionInfo From(ServerConnection serverConnection)
    {
        return new ConnectionInfo(
            serverConnection.Id,
            serverConnection is ServerConnection.SonarCloud
                ? ConnectionServerType.SonarCloud
                : ConnectionServerType.SonarQube);
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
            return UiResources.SonarCloudUrl;
        }
        return connection.Id;
    }
    
}
