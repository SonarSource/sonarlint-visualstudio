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
using SonarLint.VisualStudio.Core.WPF;
using VSLangProj;

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
            List<string> references = [];
            var validationParams = new TaskToPerformParams<AdapterResponseWithData<List<string>>>(
                async () =>
                {
                    return await connectedModeServices.ThreadHandling.RunOnBackgroundThread(() =>
                    {
                        var adapterResponse = GetConnectionReferences(connectionViewModel);
                        references = adapterResponse.ResponseData;
                        return Task.FromResult(adapterResponse);
                    });
                },
                UiResources.CalculatingConnectionReferencesText,
                UiResources.CalculatingConnectionReferencesFailedText);
            await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
            return references;
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
            var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryRemoveConnection(connectionViewModel.Connection.Info.Id);
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

        internal void AddConnectionViewModel(Connection connection)
        {
           ConnectionViewModels.Add(new ConnectionViewModel(connection));
           RaisePropertyChanged(nameof(NoConnectionExists));
        }
    }
}
