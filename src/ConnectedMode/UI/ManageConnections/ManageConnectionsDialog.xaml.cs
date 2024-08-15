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

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.DeleteConnection;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;
using SonarLint.VisualStudio.Core;
using static SonarLint.VisualStudio.ConnectedMode.ConnectionInfo;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections
{
    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public partial class ManageConnectionsDialog : Window
    {
        private readonly IBrowserService browserService;

        public ManageConnectionsViewModel ViewModel { get; } = new();

        public ManageConnectionsDialog(IBrowserService browserService)
        {
            Owner = Application.Current.MainWindow;
            this.browserService = browserService;
            InitializeComponent();
        }

        private void EditConnection_Clicked(object sender, RoutedEventArgs e)
        {
            if(sender is System.Windows.Controls.Button button && button.DataContext is ConnectionViewModel connectionViewModel)
            {
                new CredentialsDialog(browserService, connectionViewModel.Connection, false).ShowDialog();
            }
        }

        private void NewConnection_Clicked(object sender, RoutedEventArgs e)
        {
            if (GetNewConnection() is {} partialConnection && CredentialsDialogSucceeded(partialConnection) && GetCompleteConnection(partialConnection) is {} completeConnection)
            {
               ViewModel.AddConnection(completeConnection);
            }
        }

        private Connection GetNewConnection()
        {
            var serverSelectionDialog = new ServerSelectionDialog(browserService);
            return serverSelectionDialog.ShowDialog() != true ? null : serverSelectionDialog.ViewModel.CreateConnection();
        }

        private bool CredentialsDialogSucceeded(Connection newConnection)
        {
            var isAnyDialogFollowing = newConnection.ServerType == ServerType.SonarCloud; 
            var credentialsDialog = new CredentialsDialog(browserService, newConnection, withNextButton: isAnyDialogFollowing);
            return credentialsDialog.ShowDialog() == true;
        }

        private Connection GetCompleteConnection(Connection newConnection)
        {
            if (newConnection.ServerType == ServerType.SonarQube)
            {
                return newConnection;
            }
            
            var organizationSelectionDialog = new OrganizationSelectionDialog([new OrganizationDisplay("a", "a"), new OrganizationDisplay("b", "b")]);
            if (organizationSelectionDialog.ShowDialog() == true)
            {
                return newConnection with { Id = organizationSelectionDialog.ViewModel.SelectedOrganization.Key };
            }
            return null;
        }

        private void ManageConnectionsWindow_OnInitialized(object sender, EventArgs e)
        {
            ViewModel.InitializeConnections([]);
        }

        private void RemoveConnectionButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button { DataContext: ConnectionViewModel connectionViewModel })
            {
                return;
            }

            var deleteConnectionDialog = new DeleteConnectionDialog(["my proj", "vs sample 2019", "vs sample 2022"], connectionViewModel.Connection);
            if(deleteConnectionDialog.ShowDialog() == true)
            {
                ViewModel.RemoveConnection(connectionViewModel);
            }
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
