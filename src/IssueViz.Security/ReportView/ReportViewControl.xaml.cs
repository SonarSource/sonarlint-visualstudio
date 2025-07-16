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
using System.Windows.Media;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
internal sealed partial class ReportViewControl : UserControl
{
    public ReportViewModel ReportViewModel { get; } = new();
    public IResourceFinder ResourceFinder { get; } = new ResourceFinder();

    public ReportViewControl()
    {
        InitializeComponent();
    }

    private void TreeViewItem_OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem treeViewItem && IsOriginalClickedTreeViewItem(e.OriginalSource as FrameworkElement, treeViewItem))
        {
            treeViewItem.IsExpanded = !treeViewItem.IsExpanded;
        }
    }

    /// <summary>
    /// Make sure that the original source of the mouse event is the same as the TreeViewItem that was clicked.
    /// This is to prevent handling the click event when the mouse is released on a child element of the TreeViewItem
    /// </summary>
    private static bool IsOriginalClickedTreeViewItem(FrameworkElement originalSource, TreeViewItem treeViewItem) => FindParentOfType<TreeViewItem>(originalSource) == treeViewItem;

    private static T FindParentOfType<T>(FrameworkElement element) where T : FrameworkElement
    {
        var parent = VisualTreeHelper.GetParent(element);
        while (parent != null)
        {
            if (parent is T typedParent)
            {
                return typedParent;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}
