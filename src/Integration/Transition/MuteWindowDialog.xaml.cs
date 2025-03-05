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
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Navigation;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Transition;

/// <summary>
///     Interaction logic for UserControl1.xaml
/// </summary>
[ContentProperty(nameof(MuteWindowDialog))]
[ExcludeFromCodeCoverage]
public partial class MuteWindowDialog : DialogWindow
{
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IBrowserService browserService;

    public MuteViewModel ViewModel { get; set; } = new();

    public MuteWindowDialog(IActiveSolutionBoundTracker activeSolutionBoundTracker, IBrowserService browserService, IEnumerable<SonarQubeIssueTransition> allowedTransitions)
    {
        ViewModel.InitializeStatuses(allowedTransitions);
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.browserService = browserService;

        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Submit_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void FormattingHelpHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        var serverConnection = activeSolutionBoundTracker.CurrentConfiguration.Project.ServerConnection;
        var serverUri = serverConnection.ServerUri.ToString();

        browserService.Navigate(serverConnection is ServerConnection.SonarCloud ? $"{serverUri}markdown/help" : $"{serverUri}formatting/help");
        e.Handled = true;
    }

    private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Border { Child: Panel panel })
        {
            return;
        }
        var radioButton = panel.Children.OfType<RadioButton>().FirstOrDefault();
        if (radioButton != null)
        {
            radioButton.IsChecked = true;
        }
    }

    private void RadioButton_OnChecked(object sender, RoutedEventArgs e)
    {
        // The RadioButton consumes the click event, so the click event is not bubbled back to the parent
        // ListBox.SelectedItem is not triggered, so we have to update the view model manually
        if (sender is RadioButton { IsChecked: true, DataContext: StatusViewModel statusViewModel })
        {
            ViewModel.SelectedStatusViewModel = statusViewModel;
        }
    }
}
