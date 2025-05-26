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
using System.Windows.Controls;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using static SonarLint.VisualStudio.ConnectedMode.UI.WindowExtensions;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
internal sealed partial class HotspotsControl : UserControl
{
    public HotspotsControlViewModel ViewModel { get; }

    public HotspotsControl(HotspotsControlViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }

    private async void ReviewHotspotMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: IHotspotViewModel hotspotViewModel } ||
            await ViewModel.GetAllowedStatusesAsync() is not { } allowedStatuses)
        {
            return;
        }
        var dialog = new ReviewHotspotWindow(hotspotViewModel.HotspotStatus, allowedStatuses);
        if (dialog.ShowDialog(Application.Current.MainWindow) is true)
        {
            await ViewModel.ChangeHotspotStatusAsync(dialog.ViewModel.SelectedStatusViewModel.HotspotStatus);
        }
    }

    private void PriorityButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PriorityFilterViewModel vm })
        {
            ViewModel.UpdatePriorityFilter(vm, !vm.IsSelected);
        }
    }

    private async void ViewHotspotInBrowser_OnClick(object sender, RoutedEventArgs e) => await ViewModel.ViewHotspotInBrowserAsync();
}
