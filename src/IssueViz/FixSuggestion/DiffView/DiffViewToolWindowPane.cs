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
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Differencing;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

internal interface IDiffViewToolWindowPane
{
    FinalizedFixSuggestionChange[] ShowDiff(DiffViewViewModel diffViewViewModel);
}

[ExcludeFromCodeCoverage]
[Guid(DiffViewToolWindowPaneId)]
internal class DiffViewToolWindowPane : ToolWindowPane, IDiffViewToolWindowPane
{
    private const string DiffViewToolWindowPaneId = "BCC137BA-B2E0-44EB-B161-A7E8D73AD883";
    private readonly IDifferenceBufferFactoryService differenceBufferFactoryService;
    private readonly IWpfDifferenceViewerFactoryService differenceViewerFactoryService;

    public DiffViewToolWindowPane(
        IDifferenceBufferFactoryService differenceBufferFactoryService,
        IWpfDifferenceViewerFactoryService differenceViewerFactoryService) : base(null)
    {
        this.differenceBufferFactoryService = differenceBufferFactoryService;
        this.differenceViewerFactoryService = differenceViewerFactoryService;
        Caption = Resources.DiffViewWindow_DefaultCaption;
        Content = CreateDiffViewWindow(differenceBufferFactoryService, differenceViewerFactoryService);
    }

    public FinalizedFixSuggestionChange[] ShowDiff(DiffViewViewModel diffViewViewModel)
    {
        var diffToolWindow = CreateDiffViewWindow(differenceBufferFactoryService, differenceViewerFactoryService);
        diffToolWindow.InitializeDifferenceViewer(diffViewViewModel);
        Content = diffToolWindow;

        var showDialog = diffToolWindow.ShowDialog() is true;

        return diffViewViewModel.GetFinalResult(showDialog);
    }

    private static DiffViewWindow CreateDiffViewWindow(IDifferenceBufferFactoryService differenceBufferFactoryService, IWpfDifferenceViewerFactoryService differenceViewerFactoryService)
    {
        var diffToolWindow = new DiffViewWindow(differenceBufferFactoryService, differenceViewerFactoryService) { Owner = Application.Current.MainWindow };
        return diffToolWindow;
    }
}
