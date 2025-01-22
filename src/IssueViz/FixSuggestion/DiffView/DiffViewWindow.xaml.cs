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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public sealed partial class DiffViewWindow : Window
{
    private readonly IDifferenceBufferFactoryService differenceBufferFactoryService;
    private readonly IWpfDifferenceViewerFactoryService wpfDifferenceViewerFactoryService;

    public DiffViewWindow(
        IDifferenceBufferFactoryService differenceBufferFactoryService,
        IWpfDifferenceViewerFactoryService wpfDifferenceViewerFactoryService)
    {
        this.differenceBufferFactoryService = differenceBufferFactoryService;
        this.wpfDifferenceViewerFactoryService = wpfDifferenceViewerFactoryService;
        InitializeComponent();
    }

    public void InitializeDifferenceViewer(FixSuggestionDetails fixSuggestionDetails, ITextBuffer before, ITextBuffer after)
    {
        Title = BuildTitle(fixSuggestionDetails);
        var differenceBuffer = differenceBufferFactoryService.CreateDifferenceBuffer(before, after);
        var differenceViewer = wpfDifferenceViewerFactoryService.CreateDifferenceView(differenceBuffer);

        DiffGrid.Children.Clear();
        DiffGrid.Children.Add(differenceViewer.VisualElement);
    }

    private static string BuildTitle(FixSuggestionDetails fixSuggestionDetails) =>
        string.Format(IssueVisualization.Resources.DiffViewWindow_Title, fixSuggestionDetails.ChangeIndex, fixSuggestionDetails.TotalChangesFixes, fixSuggestionDetails.FileName);

    private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnDecline(object sender, RoutedEventArgs e) => DialogResult = false;
}
