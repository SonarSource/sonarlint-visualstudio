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
using Microsoft.Alm.Authentication;

namespace SonarLint.VisualStudio.Integration
{
    /// <summary>
    /// Simple wrapper around SecretStore to allow it to be registered as a ILocalService
    /// </summary>
    public class CredentialStore : ICredentialStoreService
    {
        private readonly ICredentialStore store;

        // This class implements an ugly fix for #768 - SonarQube token is visible in Credential Manager
        // https://github.com/SonarSource/sonarlint-visualstudio/issues/768
        // If we only have a user name and no password, we assume the user name is a token.
        // All other classes are still unaware of tokens and just use user name and password.

        // We have to supply a "user name" when storing credentials, even if we are storing
        // a token. Other apps (e.g. the Git credential manager) use "PersonalAccessToken".
        internal const string UserNameForTokenCredential = "PersonalAccessToken";

        public CredentialStore(ICredentialStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;
        }

        public void DeleteCredentials(TargetUri targetUri) =>
            store.DeleteCredentials(targetUri);

        public Credential ReadCredentials(TargetUri targetUri)
        {
            var storedCreds = store.ReadCredentials(targetUri);
            if (UserNameForTokenCredential.Equals(storedCreds.Username, StringComparison.OrdinalIgnoreCase))
            {
                storedCreds = new Credential(storedCreds.Password);
            }
            return storedCreds;
        }


        public void WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            var credsToStore = credentials;
            if (string.IsNullOrEmpty(credentials.Password))
            {
                credsToStore = new Credential(UserNameForTokenCredential, credentials.Username);
            }

            store.WriteCredentials(targetUri, credsToStore);
        }
    }
}
