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
using System.Windows.Data;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
internal partial class ManageBindingDialog : Window
{
    private readonly IConnectedModeServices connectedModeServices;
    private readonly IConnectedModeBindingServices connectedModeBindingServices;
    private readonly IConnectedModeUIManager connectedModeUiManager;
    private readonly BindingRequest.AutomaticBindingRequest automaticBindingRequest;

    public ManageBindingDialog(
        IConnectedModeServices connectedModeServices,
        IConnectedModeBindingServices connectedModeBindingServices,
        IConnectedModeUIServices connectedModeUiServices,
        IConnectedModeUIManager connectedModeUiManager,
        BindingRequest.AutomaticBindingRequest automaticBindingRequest)
    {
        this.connectedModeServices = connectedModeServices;
        this.connectedModeBindingServices = connectedModeBindingServices;
        this.connectedModeUiManager = connectedModeUiManager;
        this.automaticBindingRequest = automaticBindingRequest;
        ConnectedModeUiServices = connectedModeUiServices;
        ViewModel = new ManageBindingViewModel(connectedModeServices,
            connectedModeBindingServices,
            connectedModeUiManager,
            new ProgressReporterViewModel(connectedModeServices.Logger));
        InitializeComponent();
    }

    public ManageBindingViewModel ViewModel { get; }
    public IConnectedModeUIServices ConnectedModeUiServices { get; }

    private async void ManageConnections_OnClick(object sender, RoutedEventArgs e)
    {
        new ManageConnectionsDialog(connectedModeUiManager, connectedModeServices, connectedModeBindingServices, ConnectedModeUiServices).ShowDialog(this);
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
        if (automaticBindingRequest is not null)
        {
            await ViewModel.PerformBindingWithProgressAsync(automaticBindingRequest);
        }
    }

    private async void Unbind_OnClick(object sender, RoutedEventArgs e) => await ViewModel.UnbindWithProgressAsync();

    private async void UseSharedBinding_OnClick(object sender, RoutedEventArgs e) => await ViewModel.PerformSharedBindingWithProgressAsync();

    private async void ExportBindingConfigurationButton_OnClick(object sender, RoutedEventArgs e) => await ViewModel.ExportBindingConfigurationWithProgressAsync();

    private void ViewWebsite(object sender, RequestNavigateEventArgs e) => ConnectedModeUiServices.BrowserService.Navigate(e.Uri.AbsoluteUri);

    /// <summary>
    /// It is important to use the <see cref="ItemsControl.SourceUpdated"/> event and not the <see cref="ItemsControl.SelectionChanged"/> event, because we only want to detect the changes
    /// that are triggered by user in the UI and not the ones triggered by the binding (i.e. during initialization of the view model).
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ConnectionsComboBox_OnSourceUpdated(object sender, DataTransferEventArgs e) => ViewModel.ShowInvalidTokenWarningIfNeeded();
}
