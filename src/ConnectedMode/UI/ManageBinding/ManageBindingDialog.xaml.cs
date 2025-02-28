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
using System.Windows.Navigation;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
internal partial class ManageBindingDialog : Window
{
    private readonly IConnectedModeServices connectedModeServices;
    private readonly IConnectedModeBindingServices connectedModeBindingServices;
    private readonly AutomaticBindingRequest automaticBinding;

    public ManageBindingDialog(
        IConnectedModeServices connectedModeServices,
        IConnectedModeBindingServices connectedModeBindingServices,
        AutomaticBindingRequest automaticBinding = null)
    {
        this.connectedModeServices = connectedModeServices;
        this.connectedModeBindingServices = connectedModeBindingServices;
        this.automaticBinding = automaticBinding;
        ViewModel = new ManageBindingViewModel(connectedModeServices,
            connectedModeBindingServices,
            new ProgressReporterViewModel(connectedModeServices.Logger));
        InitializeComponent();
    }

    public ManageBindingViewModel ViewModel { get; }

    private async void ManageConnections_OnClick(object sender, RoutedEventArgs e)
    {
        new ManageConnectionsDialog(connectedModeServices, connectedModeBindingServices).ShowDialog(this);
        await ViewModel.InitializeDataAsync();
    }

    private async void Binding_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.BindWithProgressAsync();
    }

    private void SelectProject_OnClick(object sender, RoutedEventArgs e)
    {
        var projectSelection = new ProjectSelectionDialog(ViewModel.SelectedConnectionInfo, connectedModeServices);
        if(projectSelection.ShowDialog(this) == true)
        {
            ViewModel.SelectedProject = projectSelection.ViewModel.SelectedProject;
        }
    }

    private async void ManageBindingDialog_OnInitialized(object sender, EventArgs e)
    {
        await ViewModel.InitializeDataAsync();

        if (automaticBinding is not null)
        {
            await ViewModel.PerformAutomaticBindingWithProgressAsync(automaticBinding);
        }
    }

    private async void Unbind_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UnbindWithProgressAsync();
    }

    private async void UseSharedBinding_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.PerformAutomaticBindingWithProgressAsync(new AutomaticBindingRequest.Shared());
    }

    private void ExportBindingConfigurationButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ExportBindingConfigurationAsync().Forget();
    }

    private void ViewWebsite(object sender, RequestNavigateEventArgs e)
    {
        connectedModeServices.BrowserService.Navigate(e.Uri.AbsoluteUri);
    }
}
