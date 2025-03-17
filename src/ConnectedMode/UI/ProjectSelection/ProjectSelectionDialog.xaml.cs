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

namespace SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;

[ExcludeFromCodeCoverage]
public partial class ProjectSelectionDialog
{
    public ProjectSelectionViewModel ViewModel { get; }
    public IConnectedModeUIServices ConnectedModeUiServices { get; }

    public ProjectSelectionDialog(ConnectionInfo connectionInfo, IConnectedModeServices connectedModeServices, IConnectedModeUIServices connectedModeUiServices)
    {
        ConnectedModeUiServices = connectedModeUiServices;
        ViewModel = new ProjectSelectionViewModel(connectionInfo,
            connectedModeServices,
            new ProgressReporterViewModel(connectedModeServices.Logger));
        InitializeComponent();
    }

    private void BindButton_OnClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private async void ProjectSelectionDialog_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeProjectWithProgressAsync();
    }

    private async void ChooseAnotherProjectButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedProject = null;
        var manualProjectSelectionDialog = new ManualProjectSelectionDialog();
        var manualSelectionDialogSucceeded = manualProjectSelectionDialog.ShowDialog(this);
        if (manualSelectionDialogSucceeded is not true)
        {
            return;
        }

        ViewModel.AddManualProject(manualProjectSelectionDialog.ViewModel.ProjectKey);
    }
}
