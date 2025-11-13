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

using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;

namespace SonarQube.Client.Models
{
    /// <summary>
    /// Represents the connection information needed to connect to SonarQube service
    /// </summary>
    public sealed class ConnectionInformation : ICloneable, IDisposable
    {
        private bool isDisposed;

        public ConnectionInformation(Uri serverUri, IConnectionCredentials credentials)
        {
            if (serverUri == null)
            {
                throw new ArgumentNullException(nameof(serverUri));
            }

            ServerUri = GetFormattedSonarCloudUri(serverUri).EnsureTrailingSlash();
            Credentials = (IConnectionCredentials)credentials?.Clone() ?? new NoCredentials();
            IsSonarCloud = ServerUri == CloudServerRegion.Eu.Url || ServerUri == CloudServerRegion.Us.Url;
        }

        public ConnectionInformation(Uri serverUri)
            : this(serverUri, null)
        {
        }

        public Uri ServerUri { get; }

        public bool IsSonarCloud { get; }

        public IConnectionCredentials Credentials { get; }

        public bool IsDisposed => isDisposed;

        public SonarQubeOrganization Organization { get; set; }

        public ConnectionInformation Clone()
        {
            return new ConnectionInformation(ServerUri, (IConnectionCredentials)Credentials?.Clone()) { Organization = Organization };
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// When the provided serverUri contains 'sonarcloud.io' returns 'https://sonarcloud.io', otherwise
        /// returns serverUri. This method tries to prevent slightly incorrect SonarCloud URIs (e.g. with
        /// http instead of https, or www.sonarcloud.io instead of sonarcloud.io) from redirecting to the
        /// correct scheme and url. See https://github.com/SonarSource/sonarlint-visualstudio/issues/796
        /// </summary>
        private static Uri GetFormattedSonarCloudUri(Uri serverUri) => FixSonarCloudUri(serverUri, CloudServerRegion.Eu.Url) ?? FixSonarCloudUri(serverUri, CloudServerRegion.Us.Url) ?? serverUri;

        private static Uri FixSonarCloudUri(Uri serverUri, Uri expectedUri) =>
            (serverUri.Host.Equals(expectedUri.Host, StringComparison.OrdinalIgnoreCase) || serverUri.Host.Equals($"www.{expectedUri.Host}", StringComparison.OrdinalIgnoreCase))
                ? expectedUri
                : null;

        #region IDisposable Support

        public void Dispose()
        {
            if (!isDisposed)
            {
                Credentials?.Dispose();
                isDisposed = true;
            }
        }

        #endregion IDisposable Support
    }
}
