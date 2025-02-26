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
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.TrustConnection;

public class TrustConnectionViewModel(
    IConnectedModeServices connectedModeServices,
    IProgressReporterViewModel progressReporterViewModel,
    ServerConnection serverConnection,
    SecureString token)
    : ViewModelBase
{
    public IConnectedModeServices ConnectedModeServices { get; } = connectedModeServices;
    public ServerConnection ServerConnection { get; } = serverConnection;
    public SecureString Token { get; } = token;
    public Connection Connection => ServerConnection.ToConnection();
    public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;

    internal async Task CreateConnectionsWithProgressAsync()
    {
        var validationParams = new TaskToPerformParams<AdapterResponse>(CreateNewConnectionAsync,
            UiResources.CreatingConnectionProgressText,
            UiResources.CreatingConnectionFailedText);
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
    }

    internal async Task<AdapterResponse> CreateNewConnectionAsync()
    {
        var succeeded = ConnectedModeServices.ServerConnectionsRepositoryAdapter.TryAddConnection(Connection, new TokenCredentialsModel(Token));
        return await Task.FromResult(new AdapterResponse(succeeded));
    }
}
