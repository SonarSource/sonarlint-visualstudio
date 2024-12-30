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

using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;

namespace SonarLint.VisualStudio.IssueVisualization.FixSuggestion;

public interface IDiffToolWindow
{
    bool ShowDiff(ITextBuffer before, ITextBuffer after);
}

[Guid(ToolWindowIdAsString)]
public class DiffToolWindowPane : ToolWindowPane, IDiffToolWindow
{
    private readonly IDifferenceBufferFactoryService differenceBufferFactoryService;
    private readonly IWpfDifferenceViewerFactoryService differenceViewerFactoryService;
    private const string ToolWindowIdAsString = "BCC137BA-B2E0-44EB-B161-A7E8D73AD883";
    public static readonly Guid ToolWindowId = new(ToolWindowIdAsString);

    public DiffToolWindowPane(
        IDifferenceBufferFactoryService differenceBufferFactoryService,
        IWpfDifferenceViewerFactoryService differenceViewerFactoryService) : base(null)
    {
        this.differenceBufferFactoryService = differenceBufferFactoryService;
        this.differenceViewerFactoryService = differenceViewerFactoryService;
        Caption = "Diff Tool Window";
        Content = new DiffToolWindow(differenceBufferFactoryService, differenceViewerFactoryService);
    }

    public bool ShowDiff(ITextBuffer before, ITextBuffer after)
    {
        var diffToolWindow = new DiffToolWindow(differenceBufferFactoryService, differenceViewerFactoryService);
        diffToolWindow.InitializeDifferenceViewer(before, after);
        diffToolWindow.Owner = Application.Current.MainWindow;
        Content = diffToolWindow;

        return diffToolWindow.ShowDialog() == true;
    }
}
