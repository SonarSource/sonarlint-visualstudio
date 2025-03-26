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

using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UI;

internal class InteractiveConnectionForBindingProvider(IConnectedModeUIManager connectedModeUiManager, IServerConnectionsRepositoryAdapter serverConnectionsRepositoryAdapter) : IConnectionForBindingProvider
{
    public async Task<ServerConnection> GetServerConnectionAsync(BindingRequest request)
    {
        if (serverConnectionsRepositoryAdapter.TryGet(request.ConnectionId, out var existingConnection))
        {
            return await CheckCredentialsAsync(existingConnection);
        }

        if (request is BindingRequest.Shared shared
            && await connectedModeUiManager.ShowTrustConnectionDialogAsync(shared.Model.CreateConnectionInfo().GetServerConnectionFromConnectionInfo(), token: null) is true
            && serverConnectionsRepositoryAdapter.TryGet(shared.ConnectionId, out var newConnection))
        {
            return await CheckCredentialsAsync(newConnection);
        }

        return null;
    }

    private async Task<ServerConnection> CheckCredentialsAsync(ServerConnection originalConnection)
    {
        if (originalConnection.Credentials != null)
        {
            return originalConnection;
        }

        if (await connectedModeUiManager.ShowEditCredentialsDialogAsync(originalConnection.ToConnection()) is not true)
        {
            return originalConnection;
        }

        return serverConnectionsRepositoryAdapter.TryGet(originalConnection.Id, out var updatedConnection)
            ? updatedConnection
            : null;
    }
}
