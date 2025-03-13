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
using System.Windows;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.Shared;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.ConnectedMode.UI.TrustConnection;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
internal partial class ManageBindingDialog : Window
{
    private readonly AutomaticBindingRequest automaticBinding;
    private readonly IConnectedModeServices connectedModeServices;
    private readonly IConnectedModeBindingServices connectedModeBindingServices;

    public ManageBindingDialog(
        IConnectedModeServices connectedModeServices,
        IConnectedModeBindingServices connectedModeBindingServices,
        IConnectedModeUIServices connectedModeUiServices,
        AutomaticBindingRequest automaticBinding = null)
    {
        this.connectedModeServices = connectedModeServices;
        this.connectedModeBindingServices = connectedModeBindingServices;
        ConnectedModeUiServices = connectedModeUiServices;
        this.automaticBinding = automaticBinding;
        ViewModel = new ManageBindingViewModel(connectedModeServices,
            connectedModeBindingServices,
            connectedModeUiServices,
            new ProgressReporterViewModel(connectedModeServices.Logger));
        InitializeComponent();
    }

    public ManageBindingViewModel ViewModel { get; }
    public IConnectedModeUIServices ConnectedModeUiServices { get; }

    private async void ManageConnections_OnClick(object sender, RoutedEventArgs e)
    {
        new ManageConnectionsDialog(connectedModeServices, connectedModeBindingServices, ConnectedModeUiServices).ShowDialog(this);
        await ViewModel.InitializeDataAsync();
    }

    private async void Binding_OnClick(object sender, RoutedEventArgs e) => await ViewModel.PerformManualBindingWithProgressAsync();

    private void SelectProject_OnClick(object sender, RoutedEventArgs e)
    {
        var projectSelection = new ProjectSelectionDialog(ViewModel.SelectedConnectionInfo, connectedModeServices, ConnectedModeUiServices);
        if (projectSelection.ShowDialog(this) == true)
        {
            ViewModel.SelectedProject = projectSelection.ViewModel.SelectedProject;
        }
    }

    private async void ManageBindingDialog_OnInitialized(object sender, EventArgs e)
    {
        await ViewModel.InitializeDataAsync();
        if (automaticBinding is not null)
        {
            await PerformAutomaticBindingAsync(automaticBinding);
        }
    }

    private async void Unbind_OnClick(object sender, RoutedEventArgs e) => await ViewModel.UnbindWithProgressAsync();

    private async void UseSharedBinding_OnClick(object sender, RoutedEventArgs e) => await PerformAutomaticBindingAsync(new AutomaticBindingRequest.Shared());

    private async Task PerformAutomaticBindingAsync(AutomaticBindingRequest automaticBindingRequest)
    {
        var validationResult = ViewModel.ValidateAutomaticBindingArguments(automaticBindingRequest, ViewModel.GetServerConnection(automaticBindingRequest),
            ViewModel.GetServerProjectKey(automaticBindingRequest));
        await CreateConnectionIfMissingAsync(validationResult, automaticBindingRequest);
        await ViewModel.PerformAutomaticBindingWithProgressAsync(automaticBindingRequest);
    }

    private async Task CreateConnectionIfMissingAsync(BindingResult result, AutomaticBindingRequest automaticBindingRequest)
    {
        if (result != BindingResult.ConnectionNotFound || automaticBindingRequest is not AutomaticBindingRequest.Shared || ViewModel.SharedBindingConfigModel == null)
        {
            return;
        }
        var connectionInfo = ViewModel.SharedBindingConfigModel.CreateConnectionInfo();
        var trustConnectionDialog = new TrustConnectionDialog(connectedModeServices, ConnectedModeUiServices, connectionInfo.GetServerConnectionFromConnectionInfo(), token: null);
        trustConnectionDialog.ShowDialog(Application.Current.MainWindow);
        await ViewModel.InitializeDataAsync();
    }

    private async void ExportBindingConfigurationButton_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportBindingConfigurationWithProgressAsync();
    }

    private void ViewWebsite(object sender, RequestNavigateEventArgs e) => ConnectedModeUiServices.BrowserService.Navigate(e.Uri.AbsoluteUri);
}
