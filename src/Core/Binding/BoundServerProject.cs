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

using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.Binding;

public class BoundServerProject
{
    public string LocalBindingKey { get; }
    public string ServerProjectKey { get; }
    public ServerConnection ServerConnection { get; }
    public Dictionary<Language, ApplicableQualityProfile> Profiles { get; set; }
        
    public BoundServerProject(string localBindingKey, string serverProjectKey, ServerConnection serverConnection)
    {
        if (string.IsNullOrWhiteSpace(localBindingKey))
        {
            throw new ArgumentNullException(nameof(localBindingKey));
        }
        
        if (string.IsNullOrWhiteSpace(serverProjectKey))
        {
            throw new ArgumentNullException(nameof(serverProjectKey));
        }
        
        ServerProjectKey = serverProjectKey;
        ServerConnection = serverConnection ?? throw new ArgumentNullException(nameof(serverConnection));
        LocalBindingKey = localBindingKey;
    }

    public static BoundServerProject FromBoundSonarQubeProject(BoundSonarQubeProject boundProject, string localBindingKey = null, ServerConnection connection = null) =>
        new(localBindingKey ?? "Solution Name Placeholder", // todo https://sonarsource.atlassian.net/browse/SLVS-1424
            boundProject.ProjectKey,
            connection ?? ServerConnection.FromBoundSonarQubeProject(boundProject))
        {
            Profiles = boundProject.Profiles
        };

    public BoundSonarQubeProject ToBoundSonarQubeProject()
    {
        return new BoundSonarQubeProject(ServerConnection.ServerUri,
            ServerProjectKey,
            null,
            ServerConnection.Credentials,
            ServerConnection is ServerConnection.SonarCloud sc
                ? new SonarQubeOrganization(sc.OrganizationKey, null)
                : null)
        {
            Profiles = Profiles
        };
    }
}
