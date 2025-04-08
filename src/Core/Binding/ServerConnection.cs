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

using SonarLint.VisualStudio.Core.Helpers;

namespace SonarLint.VisualStudio.Core.Binding;

public abstract class ServerConnection
{
    internal static readonly ServerConnectionSettings DefaultSettings = new(true);

    public string Id { get; }
    public ServerConnectionSettings Settings { get; set; }
    public IConnectionCredentials Credentials { get; set; }

    public abstract Uri ServerUri { get; }
    public Uri CredentialsUri => new(Id);

    private ServerConnection(string id, ServerConnectionSettings settings = null, IConnectionCredentials credentials = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Settings = settings ?? DefaultSettings;
        Credentials = credentials;
    }

    public sealed class SonarCloud(
        string organizationKey,
        CloudServerRegion region,
        ServerConnectionSettings settings = null,
        IConnectionCredentials credentials = null)
        : ServerConnection(OrganizationKeyToId(region, organizationKey), settings, credentials)
    {
        public SonarCloud(
            string organizationKey,
            ServerConnectionSettings settings = null,
            IConnectionCredentials credentials = null)
            : this(organizationKey, CloudServerRegion.Eu, settings, credentials)
        {
        }

        public string OrganizationKey { get; } = organizationKey.WithoutSpaces();
        public CloudServerRegion Region { get; } = region;

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
        public override Uri ServerUri { get; } = serverUri == null
            ? null
            : new Uri(serverUri.ToString().WithoutSpaces());
    }
}
