/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.SLCore.Common;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Core;
using SonarLint.VisualStudio.SLCore.Listener.Credentials;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation
{
    /// <summary>
    /// Credentials provider for SLCore
    /// </summary>
    [Export(typeof(ISLCoreListener))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class CredentialsListener : ICredentialsListener
    {
        private static readonly Uri SonarCloudUri = new Uri("https://sonarcloud.io");
        private readonly ICredentialStoreService credentialStore;

        [ImportingConstructor]
        public CredentialsListener(ICredentialStoreService credentialStore)
        {
            this.credentialStore = credentialStore;
        }

        public Task<GetCredentialsResponse> GetCredentialsAsync(GetCredentialsParams parameters)
        {
            var serverUri = GetServerUriFromConnectionId(parameters?.connectionId);

            if (serverUri == null)
            {
                return Task.FromResult(GetCredentialsResponse.NoCredentials);
            }
            
            var credentials = credentialStore.ReadCredentials(serverUri);

            if (credentials == null)
            {
                return Task.FromResult(GetCredentialsResponse.NoCredentials);
            }

            return Task.FromResult(string.IsNullOrEmpty(credentials.Password)
                ? new GetCredentialsResponse(new TokenDto(credentials.Username))
                : new GetCredentialsResponse(new UsernamePasswordDto(credentials.Username, credentials.Password)));
        }

        private static Uri GetServerUriFromConnectionId(string connectionId)
        {
            if (connectionId == null)
            {
                return null;
            }
            
            if (connectionId.StartsWith(ConnectionIdPrefix.SonarCloudPrefix))
            {
                return SonarCloudUri;
            }

            if (connectionId.StartsWith(ConnectionIdPrefix.SonarQubePrefix))
            {
                return Uri.TryCreate(connectionId.Substring(ConnectionIdPrefix.SonarQubePrefix.Length), UriKind.Absolute,
                    out var uri)
                    ? uri
                    : null;
            }

            return null;
        }
    }
}
