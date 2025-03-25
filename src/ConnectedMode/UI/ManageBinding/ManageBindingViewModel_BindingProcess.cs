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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

internal sealed partial class ManageBindingViewModel
{
    private async Task<BindingResult> ValidateAndBindAsync(BindingRequest binding)
    {
        if (ValidateAutomaticBindingArguments(binding) is {} failure && !await CreateConnectionIfMissingAsync(failure, binding))
        {
            return failure;
        }

        try
        {
            var localBindingKey = await connectedModeBindingServices.SolutionInfoProvider.GetSolutionNameAsync();
            var boundServerProject = new BoundServerProject(localBindingKey, GetServerProjectKey(binding), GetServerConnection(binding));
            await connectedModeBindingServices.BindingController.BindAsync(boundServerProject, cancellationTokenSource.Token);
            return (await DisplayBindStatusAsync()).ResponseData;
        }
        catch (Exception ex)
        {
            connectedModeServices.Logger.WriteLine(ConnectedMode.Resources.Binding_Fails, ex.Message);
            return BindingResult.Failed;
        }
    }

    private async Task<bool> CreateConnectionIfMissingAsync(BindingResult.ValidationFailure validationFailure, BindingRequest bindingRequest)
    {
        if (validationFailure != BindingResult.ValidationFailure.ConnectionNotFound
            || bindingRequest is not BindingRequest.Shared
            || SharedBindingConfigModel == null)
        {
            return false;
        }

        var connectionInfo = SharedBindingConfigModel.CreateConnectionInfo();
        if (await connectedModeUiManager.ShowTrustConnectionDialogAsync(connectionInfo.GetServerConnectionFromConnectionInfo(), token: null) is not true)
        {
            return false;
        }

        // this is to ensure that the newly added connection is added to the view model properties
        await LoadDataAsync();
        return true;
    }

    private void UpdateBindingTelemetry(BindingRequest binding, BindingResult bindingResult)
    {
        if (bindingResult != BindingResult.Success)
        {
            return;
        }

        switch (binding)
        {
            case BindingRequest.Assisted { IsFromSharedBinding: true } or BindingRequest.Shared:
                connectedModeServices.TelemetryManager.AddedFromSharedBindings();
                break;
            case BindingRequest.Assisted:
                connectedModeServices.TelemetryManager.AddedAutomaticBindings();
                break;
            case BindingRequest.Manual:
                connectedModeServices.TelemetryManager.AddedManualBindings();
                break;
        }
    }

    private BindingResult.ValidationFailure ValidateAutomaticBindingArguments(BindingRequest binding)
    {
        var logContext = new MessageLevelContext
        {
            Context = [ConnectedMode.Resources.ConnectedModeBindingLogContext, binding.TypeName], VerboseContext = [binding.ToString()]
        };

        if (binding is BindingRequest.Shared && SharedBindingConfigModel == null)
        {
            connectedModeServices.Logger.WriteLine(logContext, ConnectedMode.Resources.SharedBinding_ConfigurationNotAvailable);
            return BindingResult.ValidationFailure.SharedConfigurationNotAvailable;
        }

        var serverProjectKey = GetServerProjectKey(binding);
        if (string.IsNullOrEmpty(serverProjectKey))
        {
            connectedModeServices.Logger.WriteLine(logContext, ConnectedMode.Resources.Binding_ProjectKeyNotFound);
            return BindingResult.ValidationFailure.ProjectKeyNotFound;
        }

        var serverConnection = GetServerConnection(binding);
        if (serverConnection == null)
        {
            connectedModeServices.Logger.WriteLine(logContext, ConnectedMode.Resources.Binding_ConnectionNotFound);
            return BindingResult.ValidationFailure.ConnectionNotFound;
        }

        if (serverConnection is { Credentials: null})
        {
            connectedModeServices.Logger.WriteLine(logContext, ConnectedMode.Resources.Binding_CredentiasNotFound, serverConnection.Id);
            return BindingResult.ValidationFailure.CredentialsNotFound;
        }

        return null;
    }

    private ServerConnection GetServerConnection(BindingRequest binding)
    {
        var serverConnectionId = binding switch
        {
            BindingRequest.Assisted assistedBinding => assistedBinding.ServerConnectionId,
            BindingRequest.Shared => sharedBindingConfigModel?.CreateConnectionInfo().GetServerIdFromConnectionInfo(),
            BindingRequest.Manual => SelectedConnectionInfo?.GetServerIdFromConnectionInfo(),
            _ => null
        };

        return connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(serverConnectionId, out var serverConnection) ? serverConnection : null;
    }

    private string GetServerProjectKey(BindingRequest binding) =>
        binding switch
        {
            BindingRequest.Assisted assistedBinding => assistedBinding.ServerProjectKey,
            BindingRequest.Shared => SharedBindingConfigModel?.ProjectKey,
            BindingRequest.Manual => SelectedProject?.Key,
            _ => null
        };
}
