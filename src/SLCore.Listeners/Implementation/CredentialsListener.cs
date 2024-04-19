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

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
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
        private readonly ICredentialProvider credentialProvider;
        private readonly IConnectionIdHelper connectionIdHelper;

        [ImportingConstructor]
        public CredentialsListener(ICredentialProvider credentialProvider, IConnectionIdHelper connectionIdHelper)
        {
            this.credentialProvider = credentialProvider;
            this.connectionIdHelper = connectionIdHelper;
        }

        public Task<GetCredentialsResponse> GetCredentialsAsync(GetCredentialsParams parameters)
        {
            var serverUri = connectionIdHelper.GetUriFromConnectionId(parameters?.connectionId);

            if (serverUri == null)
            {
                return Task.FromResult(GetCredentialsResponse.NoCredentials);
            }

            var credentials = credentialProvider.GetCredentials(serverUri);

            if (credentials == null)
            {
                return Task.FromResult(GetCredentialsResponse.NoCredentials);
            }

            return Task.FromResult(string.IsNullOrEmpty(credentials.Password)
                ? new GetCredentialsResponse(new TokenDto(credentials.Username))
                : new GetCredentialsResponse(new UsernamePasswordDto(credentials.Username, credentials.Password)));
        }
    }
}
