/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Diagnostics;
using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.Core;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.Integration.Persistence
{
    internal class SolutionBindingCredentialsLoader : ISolutionBindingCredentialsLoader
    {
        private readonly ICredentialStoreService store;

        public SolutionBindingCredentialsLoader(ICredentialStoreService store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public ICredentials Load(Uri boundServerUri)
        {
            if (boundServerUri == null)
            {
                return null;
            }
            var credentials = store.ReadCredentials(boundServerUri);

            return credentials == null
                ? null
                : new BasicAuthCredentials(credentials.Username, credentials.Password.ToSecureString());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability",
            "S3215:\"interface\" instances should not be cast to concrete types",
            Justification = "Casting as BasicAuthCredentials is because it's the only credential type we support. Once we add more we need to think again on how to refactor the code to avoid this",
            Scope = "member",
            Target = "~M:SonarLint.VisualStudio.Integration.Persistence.FileBindingSerializer.WriteBindingInformation(System.String,SonarLint.VisualStudio.Integration.Persistence.BoundProject)~System.Boolean")]
        public void Save(ICredentials credentials, Uri boundServerUri)
        {
            if (boundServerUri == null || !(credentials is BasicAuthCredentials basicCredentials))
            {
                return;
            }

            Debug.Assert(basicCredentials.UserName != null, "User name is not expected to be null");
            Debug.Assert(basicCredentials.Password != null, "Password name is not expected to be null");

            var credentialToSave = new Credential(basicCredentials.UserName, basicCredentials.Password.ToUnsecureString());
            store.WriteCredentials(boundServerUri, credentialToSave);
        }
    }
}
