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
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
internal sealed partial class ReportViewControl : UserControl
{
    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IBrowserService browserService;
    // TODO by https://sonarsource.atlassian.net/browse/SLVS-2376: get the allowed statuses returned by SlCore
    private readonly DependencyRiskStatus[] allowedDependencyRiskStatuses = [DependencyRiskStatus.Open, DependencyRiskStatus.Confirmed, DependencyRiskStatus.Accepted, DependencyRiskStatus.Safe];

    public ReportViewModel ReportViewModel { get; }
    public IResourceFinder ResourceFinder { get; } = new ResourceFinder();

    public ReportViewControl(IActiveSolutionBoundTracker activeSolutionBoundTracker, IBrowserService browserService)
    {
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.browserService = browserService;
        ReportViewModel = new ReportViewModel(activeSolutionBoundTracker);
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

    private void ViewDependencyRiskInBrowser_OnClick(object sender, RoutedEventArgs e)
    {
        // TODO by https://sonarsource.atlassian.net/browse/SLVS-2372: implement actual navigation to the browser 
    }

    private void DependencyRiskContextMenu_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu contextMenu)
        {
            // setting the DataContext directly on the context menu does not work for the TreeViewItem
            contextMenu.DataContext = ReportViewModel;
        }
    }

    private void TreeViewItem_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && FindParentOfType<TreeViewItem>(element) is { } treeViewItem)
        {
            treeViewItem.IsSelected = true;
        }
    }

    private void ChangeScaStatusMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (GetSelectedTreeViewItem(GroupDependencyRiskTreeViewItem) is not { DataContext: DependencyRiskViewModel selectedDependencyRiskViewModel })
        {
            return;
        }

        var changeStatusViewModel = new ChangeDependencyRiskStatusViewModel(selectedDependencyRiskViewModel.Status, allowedDependencyRiskStatuses);
        var dialog = new ChangeStatusWindow(changeStatusViewModel, browserService, activeSolutionBoundTracker);
        if (dialog.ShowDialog(Application.Current.MainWindow) is true)
        {
            // TODO by https://sonarsource.atlassian.net/browse/SLVS-2376: implement actual status change
        }
    }

    private static TreeViewItem GetSelectedTreeViewItem(TreeViewItem parent)
    {
        foreach (var child in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(child) is TreeViewItem { IsSelected: true } childContainer)
            {
                return childContainer;
            }
        }
        return null;
    }
}
