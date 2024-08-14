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
            new ServerSelectionDialog(browserService).ShowDialog();
        }

        private void ManageConnectionsWindow_OnInitialized(object sender, EventArgs e)
        {
            ViewModel.InitializeConnections([
                new Connection("http://localhost:9000", ServerType.SonarQube, true),
                new Connection("https://sonarcloud.io/myOrg", ServerType.SonarCloud, false)
            ]);
        }
    }
}
