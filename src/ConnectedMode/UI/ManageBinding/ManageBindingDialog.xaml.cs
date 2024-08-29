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
using System.Windows.Navigation;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public partial class ManageBindingDialog : Window
{
    private readonly IConnectedModeServices connectedModeServices;

    public ManageBindingDialog(IConnectedModeServices connectedModeServices, SolutionInfoModel solutionInfoModel)
    {
        this.connectedModeServices = connectedModeServices;
        ViewModel = new ManageBindingViewModel(connectedModeServices, solutionInfoModel, new ProgressReporterViewModel());
        InitializeComponent();
    }

    public ManageBindingViewModel ViewModel { get; }

    private void ManageConnections_OnClick(object sender, RoutedEventArgs e)
    {
        new ManageConnectionsDialog(connectedModeServices).ShowDialog(this);
    }

    private void Binding_OnClick(object sender, RoutedEventArgs e)
    { 
        ViewModel.BindAsync().Forget();
    }

    private void SelectProject_OnClick(object sender, RoutedEventArgs e)
    {
        var projectSelection = new ProjectSelectionDialog(ViewModel.SelectedConnectionInfo);
        if(projectSelection.ShowDialog(this) == true)
        {
            ViewModel.SelectedProject = projectSelection.ViewModel.SelectedProject;
        }
    }

    private async void ManageBindingDialog_OnInitialized(object sender, EventArgs e)
    {
        await ViewModel.InitializeDataAsync();
    }

    private void Unbind_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Unbind();
    }

    private void UseSharedBinding_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.UseSharedBindingAsync().Forget();
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
