/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

/// <summary>
/// Interaction logic for ReviewHotspotWindow.xaml
/// </summary>
[ExcludeFromCodeCoverage]
public partial class ChangeStatusWindow : Window
{
    private readonly IBrowserService browserService;
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    public IChangeStatusViewModel ViewModel { get; }

    public ChangeStatusWindow(IChangeStatusViewModel changeStatusViewModel, IBrowserService browserService, IActiveSolutionBoundTracker activeSolutionBoundTracker)
    {
        this.browserService = browserService;
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        ViewModel = changeStatusViewModel;

        InitializeComponent();
    }

    private void Submit_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Child: Panel { DataContext: IStatusViewModel statusViewModel } })
        {
            return;
        }
        ViewModel.SelectedStatusViewModel = statusViewModel;
    }

    private void RadioButton_OnChecked(object sender, RoutedEventArgs e)
    {
        // The RadioButton consumes the click event, so the click event is not bubbled back to the parent
        // ListBox.SelectedItem is not triggered, so we have to update the view model manually
        if (sender is RadioButton { IsChecked: true, DataContext: IStatusViewModel statusViewModel })
        {
            ViewModel.SelectedStatusViewModel = statusViewModel;
        }
    }

    private void FormattingHelpHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var serverConnection = activeSolutionBoundTracker.CurrentConfiguration.Project.ServerConnection;
        var serverUri = serverConnection.ServerUri.ToString();

        browserService.Navigate(serverConnection is ServerConnection.SonarCloud ? $"{serverUri}markdown/help" : $"{serverUri}formatting/help");
        e.Handled = true;
    }
}
