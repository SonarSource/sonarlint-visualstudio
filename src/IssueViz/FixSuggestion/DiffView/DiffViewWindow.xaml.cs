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
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public sealed partial class DiffViewWindow : Window
{
    private readonly IDifferenceBufferFactoryService differenceBufferFactoryService;
    private readonly IWpfDifferenceViewerFactoryService wpfDifferenceViewerFactoryService;
    private IWpfDifferenceViewer wpfDifferenceViewer;
    private DiffViewViewModel diffViewViewModel;

    public DiffViewWindow(
        IDifferenceBufferFactoryService differenceBufferFactoryService,
        IWpfDifferenceViewerFactoryService wpfDifferenceViewerFactoryService)
    {
        this.differenceBufferFactoryService = differenceBufferFactoryService;
        this.wpfDifferenceViewerFactoryService = wpfDifferenceViewerFactoryService;
        InitializeComponent();
    }

    public void InitializeDifferenceViewer(DiffViewViewModel diffViewModel)
    {
        diffViewViewModel = diffViewModel;
        ChangesGrid.DataContext = diffViewModel;
        ApplyChanges();
    }

    private void ApplyChanges()
    {
        diffViewViewModel.ApplySuggestedChanges();
        wpfDifferenceViewer = CreateDifferenceViewer();

        DiffGrid.Children.Clear();
        DiffGrid.Children.Add(wpfDifferenceViewer.VisualElement);
    }

    private IWpfDifferenceViewer CreateDifferenceViewer()
    {
        var differenceBuffer = differenceBufferFactoryService.CreateDifferenceBuffer(diffViewViewModel.Before, diffViewViewModel.After);
        var differenceViewer = wpfDifferenceViewerFactoryService.CreateDifferenceView(differenceBuffer);
        differenceViewer.LeftView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginName, true);
        differenceViewer.RightView.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginName, true);
        return differenceViewer;
    }

    private void IsSelectedCheckbox_OnClick(object sender, RoutedEventArgs e)
    {
        ApplyChanges();
        diffViewViewModel.CalculateAllChangesSelected();
        if (sender is FrameworkElement { DataContext: ChangeViewModel changeViewModel })
        {
            diffViewViewModel.GoToChangeLocation(wpfDifferenceViewer.RightView, changeViewModel);
        }
    }

    private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnDecline(object sender, RoutedEventArgs e) => DialogResult = false;

    private void SelectAllCheckbox_IsClicked(object sender, RoutedEventArgs e)
    {
        ApplyChanges();
        diffViewViewModel.GoToChangeLocation(wpfDifferenceViewer.RightView, diffViewViewModel.ChangeViewModels[0]);
    }
}
