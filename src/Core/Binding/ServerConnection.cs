﻿/*
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

using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Core.Binding;

public abstract class ServerConnection
{
    internal static readonly ServerConnectionSettings DefaultSettings = new(true);

    public string Id { get; }
    public ServerConnectionSettings Settings { get; set; }
    public IConnectionCredentials Credentials { get; set; }

    public abstract Uri ServerUri { get; }
    public Uri CredentialsUri => new(Id);

    public static ServerConnection FromBoundSonarQubeProject(BoundSonarQubeProject boundProject) =>
        boundProject switch
        {
            { Organization: not null } => new SonarCloud(boundProject.Organization.Key, credentials: boundProject.Credentials),
            { ServerUri: not null } => new SonarQube(boundProject.ServerUri, credentials: boundProject.Credentials),
            _ => null
        };

    private ServerConnection(string id, ServerConnectionSettings settings = null, IConnectionCredentials credentials = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Settings = settings ?? DefaultSettings;
        Credentials = credentials;
    }

    public sealed class SonarCloud : ServerConnection
    {
        public SonarCloud(
            string organizationKey,
            ServerConnectionSettings settings = null,
            IConnectionCredentials credentials = null)
            : this(organizationKey, CloudServerRegion.Eu, settings, credentials)
        {
        }

        public SonarCloud(
            string organizationKey,
            CloudServerRegion region,
            ServerConnectionSettings settings = null,
            IConnectionCredentials credentials = null)
            : base(OrganizationKeyToId(region, organizationKey), settings, credentials)
        {
            OrganizationKey = organizationKey;
            Region = region;
        }

        public string OrganizationKey { get; }
        public CloudServerRegion Region { get; }

        public override Uri ServerUri => Region.Url;

        private static string OrganizationKeyToId(CloudServerRegion region, string organizationKey)
        {
            if (string.IsNullOrWhiteSpace(organizationKey))
            {
                throw new ArgumentNullException(nameof(organizationKey));
            }

            return new Uri(region.Url, $"organizations/{organizationKey}").ToString();
        }
    }

    public sealed class SonarQube(Uri serverUri, ServerConnectionSettings settings = null, IConnectionCredentials credentials = null)
        : ServerConnection(serverUri?.ToString(), settings, credentials)
    {
        public override Uri ServerUri { get; } = serverUri;
    }
}
