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
using SonarLint.VisualStudio.ConnectedMode.UI.ManageConnections;
using SonarLint.VisualStudio.ConnectedMode.UI.ProjectSelection;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.ConnectedMode.UI.ManageBinding;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public partial class ManageBindingDialog : Window
{
    private readonly IBrowserService browserService;

    public ManageBindingDialog(IBrowserService browserService, SolutionInfoModel solutionInfoModel)
    {
        this.browserService = browserService;
        ViewModel = new ManageBindingViewModel(solutionInfoModel);
        InitializeComponent();
    }

    public ManageBindingViewModel ViewModel { get; }

    private void ManageConnections_OnClick(object sender, RoutedEventArgs e)
    {
        new ManageConnectionsWindow(browserService).ShowDialog();
    }

    private async void Binding_OnClick(object sender, RoutedEventArgs e)
    { 
        await ViewModel.BindAsync();
    }

    private void SelectProject_OnClick(object sender, RoutedEventArgs e)
    {
        var projectSelection = new ProjectSelectionWindow(ViewModel.SelectedConnection);
        if(projectSelection.ShowDialog() == true)
        {
            ViewModel.SelectedProject = projectSelection.ViewModel.SelectedProject;
        }
    }

    private void ManageBindingDialog_OnInitialized(object sender, EventArgs e)
    {
        ViewModel.InitializeConnections();
    }

    private void Unbind_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Unbind();
    }

    private async void UseSharedBinding_OnClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.UseSharedBindingAsync();
    }
}
