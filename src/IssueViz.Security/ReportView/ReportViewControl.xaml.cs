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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using SonarLint.VisualStudio.ConnectedMode.UI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;
using HotspotViewModel = SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots.HotspotViewModel;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

[ExcludeFromCodeCoverage] // UI, not really unit-testable
internal sealed partial class ReportViewControl : UserControl
{
    public static readonly Uri ClearAllFiltersUri = new Uri("sonarlint://clearfilters");

    private readonly IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private readonly IBrowserService browserService;

    public ReportViewModel ReportViewModel { get; }
    public IHotspotsReportViewModel HotspotsReportViewModel { get; }
    public IDependencyRisksReportViewModel DependencyRisksReportViewModel { get; }
    public ITaintsReportViewModel TaintsReportViewModel { get; }
    public IIssuesReportViewModel IssuesReportViewModel { get; }
    public IResourceFinder ResourceFinder { get; } = new ResourceFinder();

    public ReportViewControl(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IBrowserService browserService,
        IHotspotsReportViewModel hotspotsReportViewModel,
        IDependencyRisksReportViewModel dependencyRisksReportViewModel,
        ITaintsReportViewModel taintsReportViewModel,
        IFileAwareTaintsReportViewModel fileAwareTaintsReportViewModel,
        IIssuesReportViewModel issuesReportViewModel,
        INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
        ILocationNavigator locationNavigator,
        ITelemetryManager telemetryManager,
        IIssueSelectionService selectionService,
        IActiveDocumentLocator activeDocumentLocator,
        IActiveDocumentTracker activeDocumentTracker,
        IDocumentTracker documentTracker,
        IThreadHandling threadHandling,
        IInitializationProcessorFactory initializationProcessorFactory,
        IFocusOnNewCodeServiceUpdater focusOnNewCodeServiceUpdater)
    {
        this.activeSolutionBoundTracker = activeSolutionBoundTracker;
        this.browserService = browserService;
        HotspotsReportViewModel = hotspotsReportViewModel;
        DependencyRisksReportViewModel = dependencyRisksReportViewModel;
        TaintsReportViewModel = taintsReportViewModel;
        IssuesReportViewModel = issuesReportViewModel;
        ReportViewModel = new ReportViewModel(activeSolutionBoundTracker,
            navigateToRuleDescriptionCommand,
            locationNavigator,
            HotspotsReportViewModel,
            DependencyRisksReportViewModel,
            TaintsReportViewModel,
            fileAwareTaintsReportViewModel,
            IssuesReportViewModel,
            telemetryManager,
            selectionService,
            activeDocumentLocator,
            activeDocumentTracker,
            documentTracker,
            focusOnNewCodeServiceUpdater, threadHandling, initializationProcessorFactory);
        InitializeComponent();
    }

    private static T FindParentOfType<T>(FrameworkElement element) where T : FrameworkElement
    {
        if (element == null)
        {
            return null;
        }
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
        if (ReportViewModel.SelectedItem is not DependencyRiskViewModel selectedDependencyRiskViewModel)
        {
            return;
        }

        DependencyRisksReportViewModel.ShowDependencyRiskInBrowser(selectedDependencyRiskViewModel.DependencyRisk);
    }

    private void DependencyRiskContextMenu_OnLoaded(object sender, RoutedEventArgs e) => SetDataContextToReportViewModel<ContextMenu>(sender);

    private void SetDataContextToReportViewModel<T>(object sender) where T : FrameworkElement
    {
        if (sender is T contextMenu)
        {
            // workaround that allows setting the DataContext to the ReportViewModel, which is not accessible from the TreeViewItem context menu
            // due to the fact that a context menu is a popup and is not part of the visual tree
            contextMenu.DataContext = ReportViewModel;
        }
    }

    private void ShowMenuItemInBrowserMenuItem_OnLoaded(object sender, RoutedEventArgs e) => SetDataContextToReportViewModel<MenuItem>(sender);

    private void TreeViewItem_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element && FindParentOfType<TreeViewItem>(element) is { } treeViewItem)
        {
            treeViewItem.IsSelected = true;
        }
    }

    private async void ChangeScaStatusMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ReportViewModel.SelectedItem is not DependencyRiskViewModel selectedDependencyRiskViewModel)
            {
                return;
            }

            var changeStatusViewModel = new ChangeDependencyRiskStatusViewModel(selectedDependencyRiskViewModel.DependencyRisk.Transitions);
            var dialog = new ChangeStatusWindow(changeStatusViewModel, browserService, activeSolutionBoundTracker);
            if (dialog.ShowDialog(Application.Current.MainWindow) is true)
            {
                await DependencyRisksReportViewModel.ChangeDependencyRiskStatusAsync(selectedDependencyRiskViewModel.DependencyRisk, changeStatusViewModel.GetSelectedTransition(),
                    changeStatusViewModel.GetNormalizedComment());
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            // suppress
        }
    }

    private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is null)
        {
            return;
        }
        ReportViewModel.SelectedItem = e.NewValue as IIssueViewModel;
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e) => browserService.Navigate(e.Uri.AbsoluteUri);

    private void ClearFiltersHyperlink(object sender, RequestNavigateEventArgs e) => ClearAllFilters_OnClick(sender, e);


    private void TreeViewItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount % 2 == 0)
        {
            NavigateToLocation(ReportViewModel.SelectedItem ?? (sender as FrameworkElement)?.DataContext);
            e.Handled = true; // setting as handled to prevent the default TreeViewItem behavior of toggling the expansion state
        }
    }

    private void NavigateToLocation(object context)
    {
        if (context is IAnalysisIssueViewModel analysisIssueViewModel)
        {
            ReportViewModel.Navigate(analysisIssueViewModel);
        }
        else if (context is IGroupViewModel groupViewModel)
        {
            ReportViewModel.Navigate(groupViewModel);
        }
    }

    private async void ViewHotspotInBrowser_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (ReportViewModel.SelectedItem is HotspotViewModel hotspotViewModel)
            {
                await HotspotsReportViewModel.ShowHotspotInBrowserAsync(hotspotViewModel.LocalHotspot);
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            // suppress
        }
    }

    private async void ChangeHotspotStatusMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem { DataContext: HotspotViewModel hotspotViewModel } ||
                await HotspotsReportViewModel.GetAllowedStatusesAsync(hotspotViewModel) is not { } allowedStatuses)
            {
                return;
            }

            var changeHotspotStatusViewModel = new ChangeHotspotStatusViewModel(hotspotViewModel.LocalHotspot.HotspotStatus, allowedStatuses);
            var dialog = new ChangeStatusWindow(changeHotspotStatusViewModel, browserService, activeSolutionBoundTracker);
            if (dialog.ShowDialog(Application.Current.MainWindow) is true)
            {
                var newStatus = changeHotspotStatusViewModel.SelectedStatusViewModel.GetCurrentStatus<HotspotStatus>();
                var wasChanged = await HotspotsReportViewModel.ChangeHotspotStatusAsync(hotspotViewModel, newStatus);
                if (wasChanged && newStatus is HotspotStatus.Fixed or HotspotStatus.Safe)
                {
                    ReportViewModel.FilteredGroupViewModels.ToList().ForEach(vm => vm.AllIssues.Remove(hotspotViewModel));
                }
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            // suppress
        }
    }

    private async void ChangeIssueStatusMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem { DataContext: IssueViewModel issueViewModel } ||
                await IssuesReportViewModel.GetAllowedStatusesAsync(issueViewModel) is not { } allowedStatuses)
            {
                return;
            }

            var changeIssueStatusViewModel = new ChangeIssueStatusViewModel(null, allowedStatuses);
            var dialog = new ChangeStatusWindow(changeIssueStatusViewModel, browserService, activeSolutionBoundTracker);
            if (dialog.ShowDialog(Application.Current.MainWindow) is true)
            {
                var newStatus = changeIssueStatusViewModel.SelectedStatusViewModel.GetCurrentStatus<ResolutionStatus>();
                await IssuesReportViewModel.ChangeIssueStatusAsync(issueViewModel, newStatus, changeIssueStatusViewModel.GetNormalizedComment());
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            // suppress
        }
    }

    private async void ReopenIssueMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not MenuItem { DataContext: IssueViewModel issueViewModel })
            {
                return;
            }

            await IssuesReportViewModel.ReopenIssueAsync(issueViewModel);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            // suppress
        }
    }

    private void ChangeTaintStatusMenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: TaintViewModel taintViewModel })
        {
            return;
        }

        TaintsReportViewModel.ChangeStatus(taintViewModel.Issue);
    }

    private void ViewTaintInBrowser_OnClick(object sender, RoutedEventArgs e)
    {
        if (ReportViewModel.SelectedItem is TaintViewModel taintViewModel)
        {
            TaintsReportViewModel.ShowTaintInBrowser(taintViewModel.TaintIssue);
        }
    }

    private void ShowIssueVisualizationForTaint_OnClick(object sender, RoutedEventArgs e)
    {
        if (ReportViewModel.SelectedItem is TaintViewModel taintViewModel)
        {
            NavigateToLocation(taintViewModel);
            TaintsReportViewModel.ShowIssueVisualization();
        }
    }

    private void ViewIssueInBrowser_OnClick(object sender, RoutedEventArgs e)
    {
        if (ReportViewModel.SelectedItem is IssueViewModel issueViewModel)
        {
            IssuesReportViewModel.ShowIssueInBrowser(issueViewModel.AnalysisIssue);
        }
    }

    private void ShowIssueVisualizationForIssue_OnClick(object sender, RoutedEventArgs e)
    {
        if (ReportViewModel.SelectedItem is IssueViewModel issueViewModel)
        {
            NavigateToLocation(issueViewModel);
            IssuesReportViewModel.ShowIssueVisualization();
        }
    }

    private void IssueTypeFilterButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IssueTypeFilterViewModel vm })
        {
            vm.IsSelected = !vm.IsSelected;
            Control_OnFilterChanged(sender, e);
        }
    }

    private void ShowAdvancedFilters_Click(object sender, RoutedEventArgs e) => ReportViewModel.ReportViewFilter.ShowAdvancedFilters = !ReportViewModel.ReportViewFilter.ShowAdvancedFilters;

    private void Control_OnFilterChanged(object sender, EventArgs e) => ReportViewModel.ApplyFilter();

    private void ClearAllFilters_OnClick(object sender, RoutedEventArgs e) => ReportViewModel.ResetFilters();

    private void CollapseAllButton_OnClick(object sender, RoutedEventArgs e) => SetGroupsExpansionState(false);

    private void ExpandAllButton_OnClick(object sender, RoutedEventArgs e) => SetGroupsExpansionState(true);

    private void SetGroupsExpansionState(bool isExpanded)
    {
        foreach (var groupViewModel in ReportViewModel.AllGroupViewModels)
        {
            groupViewModel.IsExpanded = isExpanded;
        }
    }
}
