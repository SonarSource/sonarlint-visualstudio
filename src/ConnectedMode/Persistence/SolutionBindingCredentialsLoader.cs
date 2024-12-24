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

using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence
{
    internal class SolutionBindingCredentialsLoader : ISolutionBindingCredentialsLoader
    {
        private readonly ICredentialStoreService store;

        public SolutionBindingCredentialsLoader(ICredentialStoreService store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public void DeleteCredentials(Uri boundServerUri)
        {
            if (boundServerUri == null)
            {
                return;
            }
            store.DeleteCredentials(boundServerUri);
        }

        public IConnectionCredentials Load(Uri boundServerUri)
        {
            if (boundServerUri == null)
            {
                return null;
            }
            var credentials = store.ReadCredentials(boundServerUri);

            return credentials.ToICredentials();
        }

        public void Save(IConnectionCredentials credentials, Uri boundServerUri)
        {
            if (boundServerUri == null || credentials is null)
            {
                return;
            }

            var credentialToSave = credentials.ToCredential();
            store.WriteCredentials(boundServerUri, credentialToSave);
        }
    }
}
