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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.ConnectedMode.Binding;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence
{
    [Export(typeof(ISolutionBindingCredentialsLoaderImpl))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    [method: ImportingConstructor]
    internal class DefaultBindingCredentialsLoader(ICredentialStoreService store) : ISolutionBindingCredentialsLoaderImpl
    {
        public CredentialStoreType StoreType => CredentialStoreType.Default;

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

            return credentials.ToConnectionCredentials();
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
