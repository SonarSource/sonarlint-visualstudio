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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Service.Connection.Models;

namespace SonarLint.VisualStudio.SLCore.State;

internal interface IServerConnectionsProvider
{
    Dictionary<string, ServerConnectionConfiguration> GetServerConnections();
}

[Export(typeof(IServerConnectionsProvider))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class ServerConnectionsProvider : IServerConnectionsProvider
{
    private readonly ISolutionBindingRepository solutionBindingRepository;
    private readonly IConnectionIdHelper connectionIdHelper;

    [ImportingConstructor]
    public ServerConnectionsProvider(ISolutionBindingRepository solutionBindingRepository, IConnectionIdHelper connectionIdHelper)
    {
        this.solutionBindingRepository = solutionBindingRepository;
        this.connectionIdHelper = connectionIdHelper;
    }

    public Dictionary<string, ServerConnectionConfiguration> GetServerConnections()
    {
        return GetUniqueConnections(solutionBindingRepository.List());
    }

    private Dictionary<string, ServerConnectionConfiguration> GetUniqueConnections(IEnumerable<BoundServerProject> bindings)
    {
        var connections = new Dictionary<string, ServerConnectionConfiguration>();

        foreach (var binding in bindings)
        {
            var serverUri = binding.ServerConnection.ServerUri;
            var organization = (binding.ServerConnection as ServerConnection.SonarCloud)?.OrganizationKey;
            var connectionId = connectionIdHelper.GetConnectionIdFromServerConnection(GetServerConnection(serverUri, organization));

            connections[connectionId] = serverUri == ConnectionIdHelper.SonarCloudUri
                ? new SonarCloudConnectionConfigurationDto(connectionId, true, organization)
                : new SonarQubeConnectionConfigurationDto(connectionId, true, serverUri.ToString());
        }

        return connections;
    }

    private static ServerConnection GetServerConnection(Uri serverUri, string organization)
    {
        if (organization is not null)
        {
            return new ServerConnection.SonarCloud(organization);
        }

        if (serverUri is not null)
        {
            return new ServerConnection.SonarQube(serverUri);
        }

        return null;
    }
}
