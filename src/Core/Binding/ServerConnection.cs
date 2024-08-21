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

using Newtonsoft.Json;

namespace SonarLint.VisualStudio.Core.Binding;

public enum ServerConnectionType
{
    SonarCloud,
    SonarQube
}

public class ServerConnection
{
    public static readonly ServerConnectionSettings DefaultSettings = new(true);

    public string Id { get; }
    
    [JsonIgnore]
    public ServerConnectionType Type { get; }
    
    /// <returns>
    /// SonarQube Uri if this is a SonarQube connection, null otherwise
    /// </returns>
    public Uri SonarQubeUri { get; }
    
    /// <returns>
    /// SonarCloud Organization if this is a SonarCloud connection, null otherwise
    /// </returns>
    public string SonarCloudOrganization { get; }

    public ServerConnectionSettings Settings { get; }

    [JsonIgnore] 
    public ICredentials Credentials { get; set; }

    public ServerConnection(Uri sonarQubeUri, ServerConnectionSettings settings = null) 
        : this(sonarQubeUri?.ToString(), 
            sonarQubeUri, 
            null, 
            settings ?? DefaultSettings)
    {
    }

    public ServerConnection(string sonarCloudOrganizationKey, ServerConnectionSettings settings = null) 
        : this(sonarCloudOrganizationKey, 
            null, 
            sonarCloudOrganizationKey, 
            settings ?? DefaultSettings)
    {
    }

    [JsonConstructor]
    internal ServerConnection(string id,
        Uri sonarQubeUri,
        string sonarCloudOrganization,
        ServerConnectionSettings settings)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        
        if (sonarQubeUri is not null && sonarCloudOrganization is not null)
        {
            throw new ArgumentException($"{nameof(SonarQubeUri)} and {nameof(SonarCloudOrganization)} cannot be not null at the same time");
        }

        if (sonarQubeUri is null && sonarCloudOrganization is null)
        {
            throw new ArgumentException($"{nameof(SonarQubeUri)} and {nameof(SonarCloudOrganization)} cannot be null at the same time");
        }
        
        Type = sonarQubeUri is not null ? ServerConnectionType.SonarQube : ServerConnectionType.SonarCloud;
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        SonarQubeUri = sonarQubeUri;
        SonarCloudOrganization = sonarCloudOrganization;
    }
}
