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

using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UI.Credentials;

internal class EditCredentialsViewModel(
    Connection connection,
    IConnectedModeUIManager uiManager,
    IConnectedModeServices connectedModeServices,
    IConnectedModeBindingServices connectedModeBindingServices,
    IProgressReporterViewModel progressReporterViewModel)
    : CredentialsViewModel(connection.Info, connectedModeServices.SlCoreConnectionAdapter, progressReporterViewModel)
{
    internal async Task UpdateConnectionCredentialsWithProgressAsync()
    {
        var validationParams = new TaskToPerformParams<ResponseStatus>(
            UpdateConnectionCredentialsAsync,
            UiResources.UpdatingConnectionCredentialsProgressText,
            UiResources.UpdatingConnectionCredentialsFailedText);
        var response = await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);

        if (!response.Success)
        {
            return;
        }

        var boundServerProject = connectedModeServices.ConfigurationProvider.GetConfiguration()?.Project;
        if (boundServerProject != null && ConnectionInfo.From(boundServerProject.ServerConnection).Id == connection.Info.Id)
        {
            var refreshBinding = new TaskToPerformParams<ResponseStatus>(
                async () => await RebindAsync(boundServerProject.ServerProjectKey, boundServerProject.ServerConnection.Id),
                UiResources.RebindingProgressText,
                UiResources.RebindingFailedText);
            await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(refreshBinding);
        }
    }

    internal Task<ResponseStatus> UpdateConnectionCredentialsAsync()
    {
        var success = connectedModeServices.ServerConnectionsRepositoryAdapter.TryUpdateCredentials(connection, GetCredentialsModel());
        return Task.FromResult(new ResponseStatus(success));
    }

    internal async Task<ResponseStatus> RebindAsync(string serverProjectKey, string serverConnectionId)
    {
        var validateAndBindAsync = await connectedModeBindingServices.BindingControllerAdapter.ValidateAndBindAsync(
            new BindingRequest.Manual(serverProjectKey, serverConnectionId),
            uiManager,
            CancellationToken.None);
        return new ResponseStatus(validateAndBindAsync.IsSuccessful, validateAndBindAsync.ProblemDescription);
    }
}
