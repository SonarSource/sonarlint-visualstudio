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
using SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;
using SonarLint.VisualStudio.ConnectedMode.UI.OrganizationSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.ServerSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections
{
    [ExcludeFromCodeCoverage] // UI, not really unit-testable
    public partial class ManageConnectionsDialog : Window
    {
        private readonly IConnectedModeServices connectedModeServices;

        public ManageConnectionsViewModel ViewModel { get; } = new();

        public ManageConnectionsDialog(IConnectedModeServices connectedModeServices)
        {
            this.connectedModeServices = connectedModeServices;
            InitializeComponent();
        }

        private void EditConnection_Clicked(object sender, RoutedEventArgs e)
        {
            if(sender is System.Windows.Controls.Button button && button.DataContext is ConnectionViewModel connectionViewModel)
            {
                new CredentialsDialog(connectedModeServices, connectionViewModel.Connection.Info, false).ShowDialog(this);
            }
        }

        private void NewConnection_Clicked(object sender, RoutedEventArgs e)
        {
            if (GetTransientConnection() is {} transientConnection && CredentialsDialogSucceeded(transientConnection) && GetCompleteConnection(transientConnection) is {} completeConnection)
            {
               ViewModel.AddConnection(new Connection(completeConnection));
            }
        }

        private ConnectionInfo GetTransientConnection()
        {
            var serverSelectionDialog = new ServerSelectionDialog(connectedModeServices.BrowserService);
            return serverSelectionDialog.ShowDialog(this) != true ? null : serverSelectionDialog.ViewModel.CreateTransientConnectionInfo();
        }

        private bool CredentialsDialogSucceeded(ConnectionInfo newConnectionInfo)
        {
            var isAnyDialogFollowing = newConnectionInfo.ServerType == ConnectionServerType.SonarCloud; 
            var credentialsDialog = new CredentialsDialog(connectedModeServices, newConnectionInfo, withNextButton: isAnyDialogFollowing);
            return credentialsDialog.ShowDialog(this) == true;
        }

        private ConnectionInfo GetCompleteConnection(ConnectionInfo newConnectionInfo)
        {
            if (newConnectionInfo.ServerType == ConnectionServerType.SonarQube)
            {
                return newConnectionInfo;
            }
            
            var organizationSelectionDialog = new OrganizationSelectionDialog([new OrganizationDisplay("a", "a"), new OrganizationDisplay("b", "b")]);
            if (organizationSelectionDialog.ShowDialog(this) == true)
            {
                return newConnectionInfo with { Id = organizationSelectionDialog.ViewModel.SelectedOrganization.Key };
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

            var deleteConnectionDialog = new DeleteConnectionDialog([
                new ConnectedModeProject(new ServerProject("my proj key", "my proj name"), new SolutionInfoModel("my sol", SolutionType.Solution)),
                new ConnectedModeProject(new ServerProject("my folder key", "my folder name"), new SolutionInfoModel("my folder", SolutionType.Folder))
            ], connectionViewModel.Connection.Info);
            if(deleteConnectionDialog.ShowDialog(this) == true)
            {
                ViewModel.RemoveConnection(connectionViewModel);
            }
        }
    }
}
