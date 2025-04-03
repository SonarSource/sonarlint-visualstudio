﻿/*
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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;

internal class ManageConnectionsViewModel(
    IConnectedModeUIManager connectedModeUiManager,
    IConnectedModeServices connectedModeServices,
    IConnectedModeBindingServices connectedModeBindingServices,
    IProgressReporterViewModel progressReporterViewModel) : ViewModelBase
{
    public IConnectedModeUIManager ConnectedModeUiManager { get; } = connectedModeUiManager;
    public IConnectedModeBindingServices ConnectedModeBindingServices { get; } = connectedModeBindingServices;
    public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;
    public ObservableCollection<ConnectionViewModel> ConnectionViewModels { get; } = [];
    public bool NoConnectionExists => ConnectionViewModels.Count == 0;

    internal async Task LoadConnectionsWithProgressAsync()
    {
        var validationParams = new TaskToPerformParams<ResponseStatus>(
            InitializeConnectionViewModelsAsync,
            UiResources.LoadingConnectionsText,
            UiResources.LoadingConnectionsFailedText);
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
    }

    internal async Task RemoveConnectionWithProgressAsync(List<string> bindingKeysReferencedByConnection, ConnectionViewModel connectionViewModel)
    {
        var validationParams = new TaskToPerformParams<ResponseStatus>(
            () => RemoveConnectionViewModelAsync(bindingKeysReferencedByConnection, connectionViewModel),
            UiResources.RemovingConnectionText,
            UiResources.RemovingConnectionFailedText,
            string.Format(UiResources.RemovingConnectionSucceededText, connectionViewModel.Connection.Info.Id));
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
    }

    internal async Task<List<string>> GetConnectionReferencesWithProgressAsync(ConnectionViewModel connectionViewModel)
    {
        var validationParams = new TaskToPerformParams<ResponseStatusWithData<List<string>>>(
            async () => await GetConnectionReferencesOnBackgroundThreadAsync(connectionViewModel),
            UiResources.CalculatingConnectionReferencesText,
            UiResources.CalculatingConnectionReferencesFailedText);
        var response = await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);

        return response.ResponseData;
    }

    internal async Task EditConnectionWithProgressAsync(ConnectionViewModel connectionViewModel)
    {
        var validationParams = new TaskToPerformParams<ResponseStatus>(
            async () => await EditCredentialsAsync(connectionViewModel),
            UiResources.UpdatingConnectionCredentialsProgressText,
            UiResources.UpdatingConnectionCredentialsFailedText,
            string.Format(UiResources.UpdatingConnectionCredentialsSucceededText, connectionViewModel.Connection.Info.Id));
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
    }

    internal async Task<ResponseStatusWithData<List<string>>> GetConnectionReferencesOnBackgroundThreadAsync(ConnectionViewModel connectionViewModel) =>
        await connectedModeServices.ThreadHandling.RunOnBackgroundThread(() =>
        {
            var adapterResponse = GetConnectionReferences(connectionViewModel);
            return Task.FromResult(adapterResponse);
        });

    internal ResponseStatusWithData<List<string>> GetConnectionReferences(ConnectionViewModel connectionViewModel)
    {
        try
        {
            var bindings = ConnectedModeBindingServices.SolutionBindingRepository.List();
            var references = bindings
                .Where(x => ConnectionInfo.From(x.ServerConnection).Id == connectionViewModel.Connection.Info.Id)
                .Select(x => x.LocalBindingKey).ToList();
            return new ResponseStatusWithData<List<string>>(true, references);
        }
        catch (Exception ex)
        {
            connectedModeServices.Logger.WriteLine(ex.ToString());
            return new ResponseStatusWithData<List<string>>(false, []);
        }
    }

    internal async Task CreateConnectionsWithProgressAsync(Connection connection, ICredentialsModel credentialsModel)
    {
        var validationParams = new TaskToPerformParams<ResponseStatus>(
            () => CreateNewConnectionAsync(connection, credentialsModel),
            UiResources.CreatingConnectionProgressText,
            UiResources.CreatingConnectionFailedText,
            string.Format(UiResources.CreatingConnectionSucceededText, connection.Info.Id));
        await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
    }

    internal Task<ResponseStatus> InitializeConnectionViewModelsAsync()
    {
        ConnectionViewModels.Clear();
        var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryGetAllConnections(out var connections);
        connections?.ForEach(AddConnectionViewModel);
        return Task.FromResult(new ResponseStatus(succeeded));
    }

    /// <summary>
    /// Deleting a connection involves removing the connection from the repository and deleting the bindings that reference that connection.
    /// This is not an atomic operation: if deleting a binding failed, then the connection will not be removed.
    /// Which means that some bindings could have been deleted in the process. This is acceptable as there is no user data loss and rebinding is an easy process.
    /// </summary>
    /// <param name="referencedBindingKeys">The list of localBindingKeys that reference the connection to be removed </param>
    /// <param name="connectionViewModel">The <see cref="ConnectionViewModel" /> of the connection to be removed</param>
    /// <returns>Returns true if all the bindings and the connection have been deleted successfully</returns>
    internal Task<ResponseStatus> RemoveConnectionViewModelAsync(List<string> referencedBindingKeys, ConnectionViewModel connectionViewModel)
    {
        var bindingsRemoved = DeleteBindings(referencedBindingKeys);
        if (!bindingsRemoved)
        {
            return Task.FromResult(new ResponseStatus(false));
        }
        var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryRemoveConnection(connectionViewModel.Connection.Info);
        if (succeeded)
        {
            ConnectionViewModels.Remove(connectionViewModel);
            RaisePropertyChanged(nameof(NoConnectionExists));
        }
        return Task.FromResult(new ResponseStatus(succeeded));
    }

    internal async Task<ResponseStatus> EditCredentialsAsync(ConnectionViewModel connectionViewModel)
    {
        var result = await ConnectedModeUiManager.ShowEditCredentialsDialogAsync(connectionViewModel.Connection);
        connectionViewModel.RefreshInvalidToken();
        return new ResponseStatus(result == true);
    }

    private bool DeleteBindings(List<string> bindingKeys)
    {
        var currentSolutionName = ConnectedModeBindingServices.SolutionInfoProvider.GetSolutionName();
        foreach (var bindingKey in bindingKeys)
        {
            var bindingDeleted = currentSolutionName == bindingKey
                ? ConnectedModeBindingServices.BindingControllerAdapter.Unbind(bindingKey)
                : ConnectedModeBindingServices.SolutionBindingRepository.DeleteBinding(bindingKey);
            if (!bindingDeleted)
            {
                connectedModeServices.Logger.WriteLine(UiResources.DeleteConnection_DeleteBindingFails, bindingKey);
                return false;
            }
        }

        return true;
    }

    internal Task<ResponseStatus> CreateNewConnectionAsync(Connection connection, ICredentialsModel credentialsModel)
    {
        if (connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(connection.Info, out var serverConnection) && serverConnection != null)
        {
            return Task.FromResult(new ResponseStatus(false, UiResources.ConnectionAlreadyExistsText));
        }

        var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryAddConnection(connection, credentialsModel);
        if (succeeded)
        {
            AddConnectionViewModel(connection);
        }
        return Task.FromResult(new ResponseStatus(succeeded));
    }

    internal void AddConnectionViewModel(Connection connection)
    {
        ConnectionViewModels.Add(new ConnectionViewModel(connection, connectedModeServices.ServerConnectionsRepositoryAdapter));
        RaisePropertyChanged(nameof(NoConnectionExists));
    }
}
