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

using System.Security;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Helpers;

namespace SonarLint.VisualStudio.ConnectedMode.Persistence;

internal sealed class UsernameAndPasswordCredentials(string userName, SecureString password) : IUsernameAndPasswordCredentials
{
    public string UserName { get; } = userName ?? throw new ArgumentNullException(nameof(userName));

    public SecureString Password { get; } = password ?? throw new ArgumentNullException(nameof(password));

    public void Dispose() => Password?.Dispose();

    public object Clone() => new UsernameAndPasswordCredentials(UserName, Password.CopyAsReadOnly());
}
