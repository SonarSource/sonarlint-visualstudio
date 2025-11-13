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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.State;

internal interface IServerConnectionsProvider
{
    Dictionary<string, ServerConnectionConfigurationDtoBase> GetServerConnections();
}

[Export(typeof(IServerConnectionsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class ServerConnectionsProvider(IServerConnectionsRepository serverConnectionsRepository) : IServerConnectionsProvider
{
    public Dictionary<string, ServerConnectionConfigurationDtoBase> GetServerConnections()
    {
        var succeeded = serverConnectionsRepository.TryGetAll(out var serverConnections);
        return succeeded ? GetServerConnectionConfigurations(serverConnections).ToDictionary(conf => conf.connectionId) : [];
    }

    private static List<ServerConnectionConfigurationDtoBase> GetServerConnectionConfigurations(IReadOnlyList<ServerConnection> serverConnections)
    {
        var serverConnectionConfigurations = new List<ServerConnectionConfigurationDtoBase>();
        foreach (var serverConnection in serverConnections)
        {
            switch (serverConnection)
            {
                case ServerConnection.SonarQube sonarQubeConnection:
                    serverConnectionConfigurations.Add(new SonarQubeConnectionConfigurationDto(sonarQubeConnection.Id, !sonarQubeConnection.Settings.IsSmartNotificationsEnabled,
                        sonarQubeConnection.ServerUri.ToString()));
                    break;
                case ServerConnection.SonarCloud sonarCloudConnection:
                    serverConnectionConfigurations.Add(new SonarCloudConnectionConfigurationDto(sonarCloudConnection.Id, !sonarCloudConnection.Settings.IsSmartNotificationsEnabled,
                        sonarCloudConnection.OrganizationKey, sonarCloudConnection.Region.ToSlCoreRegion()));
                    break;
                default:
                    throw new InvalidOperationException(SLCoreStrings.UnexpectedServerConnectionType);
            }
        }
        return serverConnectionConfigurations;
    }
}
