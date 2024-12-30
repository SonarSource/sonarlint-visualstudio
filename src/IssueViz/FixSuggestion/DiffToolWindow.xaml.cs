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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
public sealed partial class DiffToolWindow : Window
{
    private readonly IDifferenceBufferFactoryService differenceBufferFactoryService;
    private readonly IWpfDifferenceViewerFactoryService differenceViewerFactoryService;

    public DiffToolWindow(IDifferenceBufferFactoryService differenceBufferFactoryService,
        IWpfDifferenceViewerFactoryService differenceViewerFactoryService)
    {
        this.differenceBufferFactoryService = differenceBufferFactoryService;
        this.differenceViewerFactoryService = differenceViewerFactoryService;
        InitializeComponent();
    }

    public void InitializeDifferenceViewer(ITextBuffer before, ITextBuffer after)
    {
        var differenceBuffer = differenceBufferFactoryService.CreateDifferenceBuffer(before, after);
        var differenceViewer = differenceViewerFactoryService.CreateDifferenceView(differenceBuffer);

        DiffGrid.Children.Clear();
        DiffGrid.Children.Add(differenceViewer.VisualElement);
    }

    private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnDecline(object sender, RoutedEventArgs e) => DialogResult = false;

}
