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

using System.Security;
using SonarLint.VisualStudio.ConnectedMode.Persistence;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials;

public interface ICredentialsModel
{
    IConnectionCredentials ToICredentials();
}

public class TokenCredentialsModel(SecureString token) : ICredentialsModel
{
    public SecureString Token { get; } = token;

    public IConnectionCredentials ToICredentials() => new TokenAuthCredentials(Token);
}

public class UsernamePasswordModel(string username, SecureString password) : ICredentialsModel
{
    public string Username { get; } = username;
    public SecureString Password { get; } = password;

    public IConnectionCredentials ToICredentials() => new UsernameAndPasswordCredentials(Username, Password);
}
