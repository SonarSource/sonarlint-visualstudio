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
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Binding
{
    public static class BoundSonarQubeProjectExtensions
    {
        public static ServerConnection FromBoundSonarQubeProject(this BoundSonarQubeProject boundProject) =>
            boundProject switch
            {
                { Organization: not null } => new ServerConnection.SonarCloud(boundProject.Organization.Key, credentials: boundProject.Credentials),
                { ServerUri: not null } => new ServerConnection.SonarQube(boundProject.ServerUri, credentials: boundProject.Credentials),
                _ => null
            };

        public static BoundServerProject FromBoundSonarQubeProject(this BoundSonarQubeProject boundProject, string localBindingKey, ServerConnection connection) =>
            new(localBindingKey ?? throw new ArgumentNullException(nameof(localBindingKey)),
                boundProject?.ProjectKey ?? throw new ArgumentNullException(nameof(boundProject)),
                connection ?? throw new ArgumentNullException(nameof(connection))) { Profiles = boundProject.Profiles };

        public static ConnectionInformation CreateConnectionInformation(this BoundSonarQubeProject binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            var connection = new ConnectionInformation(binding.ServerUri, binding.Credentials);

            connection.Organization = binding.Organization;
            return connection;
        }

        public static ConnectionInformation CreateConnectionInformation(this BoundServerProject binding)
        {
            if (binding == null)
            {
                throw new ArgumentNullException(nameof(binding));
            }

            var connection = new ConnectionInformation(binding.ServerConnection.ServerUri, binding.ServerConnection.Credentials);

            connection.Organization = binding.ServerConnection is ServerConnection.SonarCloud sc ? new SonarQubeOrganization(sc.OrganizationKey, null) : null;
            return connection;
        }
    }
}
