/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
    public class CredentialStore : ICredentialStore, ILocalService
    {
        private readonly SecretStore store;

        public CredentialStore(SecretStore store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;
        }

        public string Namespace =>
            store.Namespace;

        public Secret.UriNameConversion UriNameConversion =>
            store.UriNameConversion;

        public void DeleteCredentials(TargetUri targetUri) =>
            store.DeleteCredentials(targetUri);

        public Credential ReadCredentials(TargetUri targetUri) =>
            store.ReadCredentials(targetUri);

        public void WriteCredentials(TargetUri targetUri, Credential credentials) =>
            store.WriteCredentials(targetUri, credentials);
    }
}
