/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarQube.Client.Models;

namespace SonarQube.Client;

public interface ISonarQubeService
{
    bool IsConnected { get; }

    /// <summary>
    ///     Returns <see cref="ServerInfo" /> that the service is connected to at this moment. Subsequent calls can result in different values.
    /// </summary>
    ServerInfo GetServerInfo();

    Task ConnectAsync(ConnectionInformation connection, CancellationToken token);

    void Disconnect();

    /// <summary>
    ///     Returns the URI to view the specified issue on the server
    /// </summary>
    /// <remarks>The method does not check whether the project or issue exists or not</remarks>
    Uri GetViewIssueUrl(string projectKey, string issueKey);
}
