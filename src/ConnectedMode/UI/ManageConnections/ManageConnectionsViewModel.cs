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

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections
{
    public class ManageConnectionsViewModel(IConnectedModeServices connectedModeServices, IProgressReporterViewModel progressReporterViewModel) : ViewModelBase
    {
        public IProgressReporterViewModel ProgressReporterViewModel { get; } = progressReporterViewModel;
        public ObservableCollection<ConnectionViewModel> ConnectionViewModels { get; } = [];
        public bool NoConnectionExists => ConnectionViewModels.Count == 0;

        public async Task LoadConnectionsWithProgressAsync()
        {
            var validationParams = new TaskToPerformParams<AdapterResponse>(
                async () => await SafeExecuteActionAsync(InitializeConnections),
                UiResources.LoadingConnectionsText,
                UiResources.LoadingConnectionsFailedText);
            await ProgressReporterViewModel.ExecuteTaskWithProgressAsync(validationParams);
        }

        public async Task CreateConnectionsWithProgressAsync(Connection connection, ICredentialsModel credentialsModel)
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

        internal bool InitializeConnections()
        {
            ConnectionViewModels.Clear();
            var succeeded = connectedModeServices.ServerConnectionsRepositoryAdapter.TryGetAllConnections(out var connections);
            connections?.ForEach(AddConnectionViewModel);
            return succeeded;
        }

        internal void RemoveConnection(ConnectionViewModel connectionViewModel)
        {
            ConnectionViewModels.Remove(connectionViewModel);
            RaisePropertyChanged(nameof(NoConnectionExists));
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
