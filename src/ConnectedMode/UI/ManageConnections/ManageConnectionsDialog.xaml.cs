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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.DeleteConnection;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections
{
    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public partial class ManageConnectionsDialog : Window
    {
        private readonly IConnectedModeServices connectedModeServices;

        public ManageConnectionsViewModel ViewModel { get; }

        public ManageConnectionsDialog(IConnectedModeServices connectedModeServices, IConnectedModeBindingServices connectedModeBindingServices)
        {
            this.connectedModeServices = connectedModeServices;
            ViewModel = new ManageConnectionsViewModel(connectedModeServices,
                connectedModeBindingServices,
                new ProgressReporterViewModel(connectedModeServices.Logger));
            InitializeComponent();
        }

        private async void EditConnection_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: ConnectionViewModel connectionViewModel })
            {
                return;
            }

            var credentialsDialog = new CredentialsDialog(connectedModeServices, connectionViewModel.Connection.Info, false);
            if (!CredentialsDialogSucceeded(credentialsDialog))
            {
                return;
            }

            await ViewModel.UpdateConnectionCredentialsWithProgressAsync(connectionViewModel.Connection, credentialsDialog.ViewModel.GetCredentialsModel());
        }

        private async void NewConnection_Clicked(object sender, RoutedEventArgs e)
        {
            if (GetTransientConnection() is not { } transientConnection)
            {
                return;
            }

            var credentialsDialog = GetCredentialsDialog(transientConnection);
            if (!CredentialsDialogSucceeded(credentialsDialog) || FinalizeConnection(transientConnection, credentialsDialog) is not { } completeConnection)
            {
                return;
            }

            await ViewModel.CreateConnectionsWithProgressAsync(new Connection(completeConnection), credentialsDialog.ViewModel.GetCredentialsModel());
        }

        private ConnectionInfo GetTransientConnection()
        {
            var serverSelectionDialog = new ServerSelectionDialog(connectedModeServices.BrowserService, connectedModeServices.TelemetryManager, connectedModeServices.DogfoodingService);
            return serverSelectionDialog.ShowDialog(this) != true ? null : serverSelectionDialog.ViewModel.CreateTransientConnectionInfo();
        }

        private CredentialsDialog GetCredentialsDialog(ConnectionInfo newConnectionInfo)
        {
            var isAnyDialogFollowing = newConnectionInfo.ServerType == ConnectionServerType.SonarCloud;
            return new CredentialsDialog(connectedModeServices, newConnectionInfo, withNextButton: isAnyDialogFollowing);
        }

        private bool CredentialsDialogSucceeded(CredentialsDialog credentialsDialog)
        {
            return credentialsDialog.ShowDialog(this) == true;
        }

        private ConnectionInfo FinalizeConnection(ConnectionInfo newConnectionInfo, CredentialsDialog credentialsDialog)
        {
            if (newConnectionInfo.ServerType == ConnectionServerType.SonarQube)
            {
                return newConnectionInfo;
            }

            var organizationSelectionDialog = new OrganizationSelectionDialog(connectedModeServices, credentialsDialog.ViewModel.GetCredentialsModel());

            return organizationSelectionDialog.ShowDialog(this) == true ? organizationSelectionDialog.ViewModel.FinalConnectionInfo : null;
        }

        private async void ManageConnectionsWindow_OnInitialized(object sender, EventArgs e)
        {
            await ViewModel.LoadConnectionsWithProgressAsync();
        }

        private async void RemoveConnectionButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: ConnectionViewModel connectionViewModel })
            {
                return;
            }

            var connectionReferences = await ViewModel.GetConnectionReferencesWithProgressAsync(connectionViewModel);
            var deleteConnectionDialog = new DeleteConnectionDialog(connectionReferences, connectionViewModel.Connection.Info);
            if (deleteConnectionDialog.ShowDialog(this) == true)
            {
                await ViewModel.RemoveConnectionWithProgressAsync(connectionReferences, connectionViewModel);
            }
        }
    }
}
