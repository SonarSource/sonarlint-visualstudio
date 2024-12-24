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

using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

internal static class CredentialsExtensionMethods
{
    internal static Credential ToCredential(this ICredentials credentials) =>
        credentials switch
        {
            BasicAuthCredentials basicAuthCredentials => new Credential(basicAuthCredentials.UserName, basicAuthCredentials.Password.ToUnsecureString()),
            // the ICredentialStoreService requires a username, but then proceeds to store the username as the password and a hard-coded string as the username
            TokenAuthCredentials tokenAuthCredentials => new Credential(tokenAuthCredentials.Token.ToUnsecureString(), string.Empty),
            _ => throw new NotSupportedException($"Unexpected credentials type: {credentials?.GetType()}")
        };

    internal static ICredentials ToICredentials(this Credential credential)
    {
        if (credential is null)
        {
            return null;
        }
        if (credential.Password == string.Empty)
        {
            return new TokenAuthCredentials(credential.Username.ToSecureString());
        }
        return new BasicAuthCredentials(credential.Username, credential.Password.ToSecureString());
    }
}
