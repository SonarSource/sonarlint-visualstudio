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

using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Windows;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.UI.Credentials;
using SonarLint.VisualStudio.ConnectedMode.UI.Resources;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.ConnectedMode.UI.TrustConnection;

[ExcludeFromCodeCoverage]
public partial class TrustConnectionDialog : Window
{
    private readonly IConnectedModeServices connectedModeServices;
    private readonly IConnectedModeUIServices connectedModeUiServices;

    public TrustConnectionViewModel ViewModel { get; }

    public TrustConnectionDialog(
        IConnectedModeServices connectedModeServices,
        IConnectedModeUIServices connectedModeUiServices,
        ServerConnection serverConnection,
        SecureString token)
    {
        this.connectedModeServices = connectedModeServices;
        this.connectedModeUiServices = connectedModeUiServices;
        ViewModel = new TrustConnectionViewModel(connectedModeServices, new ProgressReporterViewModel(connectedModeServices.Logger), serverConnection, token);
        InitializeComponent();
        Title = ViewModel.IsCloud ? UiResources.TrustOrganizationDialogTitle : UiResources.TrustServerDialogTitle;
    }

    private void ViewWebsite(object sender, RequestNavigateEventArgs e) => connectedModeUiServices.BrowserService.Navigate(e.Uri.AbsoluteUri);

    private async void TrustServerButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Token == null && !AddToken())
        {
            ViewModel.ProgressReporterViewModel.Warning = UiResources.TrustConnectionAddTokenFailed;
            return;
        }
        await ViewModel.CreateConnectionsWithProgressAsync();
        DialogResult = true;
        Close();
    }

    private bool AddToken()
    {
        var credentialsDialog = new CredentialsDialog(connectedModeServices, connectedModeUiServices, ViewModel.Connection.Info, withNextButton: false);
        var wasTokenAdded = credentialsDialog.ShowDialog(this) == true;
        if (wasTokenAdded)
        {
            ViewModel.Token = credentialsDialog.ViewModel.Token;
        }

        return wasTokenAdded;
    }
}
