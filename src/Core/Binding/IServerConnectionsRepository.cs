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
using System.Diagnostics.CodeAnalysis;

namespace SonarLint.VisualStudio.Core.Binding;

public interface IServerConnectionsRepository
{
    bool TryGet(string connectionId, out ServerConnection serverConnection);
    List<ServerConnection> GetAll();
    bool TryAdd(ServerConnection connection);
    void Delete(string connectionId);
    Task<bool> TryUpdateSettingsById(string connectionId, ServerConnectionSettings connectionSettings);
    Task<bool> TryUpdateCredentialsById(string connectionId, ICredentials credentials);
}

[ExcludeFromCodeCoverage] // todo: remove this class in https://sonarsource.atlassian.net/browse/SLVS-1399
[Export(typeof(IServerConnectionsRepository))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class DummyServerConnectionsRepository : IServerConnectionsRepository
{
    public bool TryGet(string connectionId, out ServerConnection serverConnection)
    {
        throw new NotImplementedException();
    }

    public List<ServerConnection> GetAll()
    {
        throw new NotImplementedException();
    }

    public bool TryAdd(ServerConnection connection)
    {
        throw new NotImplementedException();
    }

    public void Delete(string connectionId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryUpdateSettingsById(string connectionId, ServerConnectionSettings connectionSettings)
    {
        throw new NotImplementedException();
    }

    public Task<bool> TryUpdateCredentialsById(string connectionId, ICredentials credentials)
    {
        throw new NotImplementedException();
    }
}
