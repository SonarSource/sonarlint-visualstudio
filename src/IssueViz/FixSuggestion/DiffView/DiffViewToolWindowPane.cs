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

using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion.DiffView;

public interface IDiffToolWindow
{
    bool ShowDiff(FixSuggestionDetails fixSuggestionDetails, ITextBuffer before, ITextBuffer after);
}

[Guid(DiffViewToolWindowPaneId)]
public class DiffViewToolWindowPane : ToolWindowPane, IDiffToolWindow
{
    private readonly IDifferenceBufferFactoryService differenceBufferFactoryService;
    private readonly IWpfDifferenceViewerFactoryService differenceViewerFactoryService;
    private const string DiffViewToolWindowPaneId = "BCC137BA-B2E0-44EB-B161-A7E8D73AD883";

    public DiffViewToolWindowPane(
        IDifferenceBufferFactoryService differenceBufferFactoryService,
        IWpfDifferenceViewerFactoryService differenceViewerFactoryService) : base(null)
    {
        this.differenceBufferFactoryService = differenceBufferFactoryService;
        this.differenceViewerFactoryService = differenceViewerFactoryService;
        Caption = Resources.DiffViewWindow_DefaultCaption;
        Content = CreateDiffViewWindow(differenceBufferFactoryService, differenceViewerFactoryService);
    }

    public bool ShowDiff(FixSuggestionDetails fixSuggestionDetails, ITextBuffer before, ITextBuffer after)
    {
        var diffToolWindow = CreateDiffViewWindow(differenceBufferFactoryService, differenceViewerFactoryService);
        diffToolWindow.InitializeDifferenceViewer(fixSuggestionDetails, before, after);
        Content = diffToolWindow;

        return diffToolWindow.ShowDialog() == true;
    }

    private static DiffViewWindow CreateDiffViewWindow(IDifferenceBufferFactoryService differenceBufferFactoryService, IWpfDifferenceViewerFactoryService differenceViewerFactoryService)
    {
        var diffToolWindow = new DiffViewWindow(differenceBufferFactoryService, differenceViewerFactoryService) { Owner = Application.Current.MainWindow };
        return diffToolWindow;
    }
}
