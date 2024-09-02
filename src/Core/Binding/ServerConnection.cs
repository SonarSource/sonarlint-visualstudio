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

namespace SonarLint.VisualStudio.Core.Binding;

public abstract class ServerConnection
{
    internal static readonly ServerConnectionSettings DefaultSettings = new(true);
    
    public string Id { get; }
    public ServerConnectionSettings Settings { get; set; }
    public ICredentials Credentials { get; set; }
    
    public abstract Uri ServerUri { get; }
    public abstract Uri CredentialsUri { get; }

    private ServerConnection(string id, ServerConnectionSettings settings = null, ICredentials credentials = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Settings = settings ?? DefaultSettings;
        Credentials = credentials;
    }

    public sealed class SonarCloud : ServerConnection
    {
        public SonarCloud(string organizationKey, ServerConnectionSettings settings = null, ICredentials credentials = null) : base(organizationKey, settings, credentials)
        {
            OrganizationKey = organizationKey ?? throw new ArgumentNullException(nameof(organizationKey));
            CredentialsUri = new Uri(ServerUri, $"organizations/{organizationKey}");
        }

        public string OrganizationKey { get; } 
        
        public override Uri ServerUri { get; } = new("https://sonarcloud.io");
        public override Uri CredentialsUri { get; }
    }
    
    public sealed class SonarQube(Uri serverUri, ServerConnectionSettings settings = null, ICredentials credentials = null)
        : ServerConnection(serverUri?.ToString(), settings, credentials)
    {
        public override Uri ServerUri { get; } = serverUri;
        public override Uri CredentialsUri { get; } = serverUri;
    }
}
