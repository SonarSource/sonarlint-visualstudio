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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections
{
    public class ManageConnectionsViewModel(IConnectedModeServices connectedModeServices, IConnectedModeBindingServices connectedModeBindingServices, IProgressReporterViewModel progressReporterViewModel) : ViewModelBase
    {
        public IConnectedModeBindingServices ConnectedModeBindingServices { get; } = connectedModeBindingServices;
        public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;
        public ObservableCollection<ConnectionViewModel> ConnectionViewModels { get; } = [];
        public bool NoConnectionExists => ConnectionViewModels.Count == 0;

        internal async Task LoadConnectionsWithProgressAsync()
        {
            var validationParams = new TaskToPerformParams<AdapterResponse>(
                async () => await SafeExecuteActionAsync(InitializeConnectionViewModels),
                UiResources.LoadingConnectionsText,
                UiResources.LoadingConnectionsFailedText);
            await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        }

        internal async Task RemoveConnectionWithProgressAsync(ConnectionViewModel connectionViewModel)
        {
            var validationParams = new TaskToPerformParams<AdapterResponse>(
                async () => await SafeExecuteActionAsync(() => RemoveConnectionViewModel(connectionViewModel)),
                UiResources.RemovingConnectionText,
                UiResources.RemovingConnectionFailedText);
            await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        }

        internal async Task<List<string>> GetConnectionReferencesWithProgressAsync(ConnectionViewModel connectionViewModel)
        {
            var validationParams = new TaskToPerformParams<AdapterResponseWithData<List<string>>>(
                async () => await GetConnectionReferencesOnBackgroundThreadAsync(connectionViewModel),
                UiResources.CalculatingConnectionReferencesText,
                UiResources.CalculatingConnectionReferencesFailedText);
            var response = await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);

            return response.ResponseData;
        }

        internal async Task<AdapterResponseWithData<List<string>>> GetConnectionReferencesOnBackgroundThreadAsync(ConnectionViewModel connectionViewModel)
        {
            return await connectedModeServices.ThreadHandling.RunOnBackgroundThread(() =>
            {
                var adapterResponse = GetConnectionReferences(connectionViewModel);
                return Task.FromResult(adapterResponse);
            });
        }

        internal AdapterResponseWithData<List<string>> GetConnectionReferences(ConnectionViewModel connectionViewModel)
        {
            try
            {
                var bindings = ConnectedModeBindingServices.SolutionBindingRepository.List();
                var references = bindings
                    .Where(x => ConnectionInfo.From(x.ServerConnection).Id == connectionViewModel.Connection.Info.Id)
                    .Select(x => x.LocalBindingKey).ToList();
                return new AdapterResponseWithData<List<string>>(true, references);
            }
            catch (Exception ex)
            {
                connectedModeServices.Logger.WriteLine(nameof(GetConnectionReferences), ex.Message);
                return new AdapterResponseWithData<List<string>>(false, []);
            }
        }

        internal async Task CreateConnectionsWithProgressAsync(Connection connection, ICredentialsModel credentialsModel)
        {
            var validationParams = new TaskToPerformParams<AdapterResponse>(
                async () => await SafeExecuteActionAsync(() => CreateNewConnection(connection, credentialsModel)),
                UiResources.CreatingConnectionProgressText,
                UiResources.CreatingConnectionFailedText);
            await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        }

        internal async Task UpdateConnectionCredentialsWithProgressAsync(Connection connection, ICredentialsModel credentialsModel)
        {
            var validationParams = new TaskToPerformParams<AdapterResponse>(
                async () => await SafeExecuteActionAsync(() => UpdateConnectionCredentials(connection, credentialsModel)),
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
                var refreshBinding = new TaskToPerformParams<AdapterResponse>(
                    async () => await RebindAsync(connection, boundServerProject.ServerProjectKey),
                    UiResources.RebindingProgressText,
                    UiResources.RebindingFailedText);
                await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(refreshBinding);
            }
        }

        internal async Task<AdapterResponse> SafeExecuteActionAsync(Func<bool> funcToExecute)
        {
            var succeeded = false;
            try
            {
                await connectedModeServices.ThreadHandling.RunOnUIThreadAsync(() => succeeded = funcToExecute());
            }
            catch (Exception ex)
            {
                connectedModeServices.Logger.WriteLine(ex.Message);
                succeeded = false;
            }

            return new AdapterResponse(succeeded);
        }

        internal bool InitializeConnectionViewModels()
        {
            ConnectionViewModels.Clear();
            var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryGetAllConnections(out var connections);
            connections?.ForEach(AddConnectionViewModel);
            return succeeded;
        }

        internal bool RemoveConnectionViewModel(ConnectionViewModel connectionViewModel)
        {
            var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryRemoveConnection(connectionViewModel.Connection.Info);
            if (succeeded)
            {
                ConnectionViewModels.Remove(connectionViewModel);
                RaisePropertyChanged(nameof(NoConnectionExists));
            }
            return succeeded;
        }

        internal bool CreateNewConnection(Connection connection, ICredentialsModel credentialsModel)
        {
            var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryAddConnection(connection, credentialsModel);
            if (succeeded)
            {
                AddConnectionViewModel(connection);
            }
            return succeeded;
        }

        internal bool UpdateConnectionCredentials(Connection connection, ICredentialsModel credentialsModel)
        {
            if (connection is null)
            {
                return false;
            }

            return connectedModeServices.ServerConnectionsRepositoryAdapter.TryUpdateCredentials(connection, credentialsModel);
        }

        internal async Task<AdapterResponse> RebindAsync(Connection connection, string serverProjectKey)
        {
            if (!connectedModeServices.ServerConnectionsRepositoryAdapter.TryGet(connection.Info, out var serverConnection))
            {
                return new AdapterResponse(false);
            }

            try
            {
                var localBindingKey = await connectedModeBindingServices.SolutionInfoProvider.GetSolutionNameAsync();
                var boundServerProject = new BoundServerProject(localBindingKey, serverProjectKey, serverConnection);
                await connectedModeBindingServices.BindingController.BindAsync(boundServerProject, CancellationToken.None);
                return new AdapterResponse(true);
            }
            catch (Exception)
            {
                return new AdapterResponse(false);
            }
        }

        internal void AddConnectionViewModel(Connection connection)
        {
           ConnectionViewModels.Add(new ConnectionViewModel(connection));
           RaisePropertyChanged(nameof(NoConnectionExists));
        }
    }
}
