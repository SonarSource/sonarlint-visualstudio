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

using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal interface IReportViewModel
{
    ObservableCollection<IGroupViewModel> FilteredGroupViewModels { get; }
}

internal class ReportViewModel : ServerViewModel, IReportViewModel
{
    private readonly IHotspotsReportViewModel hotspotsReportViewModel;
    private readonly IDependencyRisksReportViewModel dependencyRisksReportViewModel;
    private readonly ITaintsReportViewModel taintsReportViewModel;
    private readonly ITelemetryManager telemetryManager;
    private readonly IIssueSelectionService selectionService;
    private readonly IActiveDocumentLocator activeDocumentLocator;
    private readonly IActiveDocumentTracker activeDocumentTracker;
    private readonly IThreadHandling threadHandling;
    private IIssueViewModel selectedItem;
    private string activeDocumentFilePath;
    private readonly List<IGroupViewModel> allGroupViewModels = [];

    public ReportViewModel(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
        ILocationNavigator locationNavigator,
        IHotspotsReportViewModel hotspotsReportViewModel,
        IDependencyRisksReportViewModel dependencyRisksReportViewModel,
        ITaintsReportViewModel taintsReportViewModel,
        ITelemetryManager telemetryManager,
        IIssueSelectionService selectionService,
        IActiveDocumentLocator activeDocumentLocator,
        IActiveDocumentTracker activeDocumentTracker,
        IThreadHandling threadHandling) : base(activeSolutionBoundTracker)
    {
        this.hotspotsReportViewModel = hotspotsReportViewModel;
        this.dependencyRisksReportViewModel = dependencyRisksReportViewModel;
        this.taintsReportViewModel = taintsReportViewModel;
        this.telemetryManager = telemetryManager;
        this.selectionService = selectionService;
        this.activeDocumentLocator = activeDocumentLocator;
        this.activeDocumentTracker = activeDocumentTracker;
        this.threadHandling = threadHandling;

        hotspotsReportViewModel.IssuesChanged += HotspotsChanged;
        dependencyRisksReportViewModel.DependencyRisksChanged += DependencyRisksChanged;
        taintsReportViewModel.IssuesChanged += TaintsChanged;

        InitializeActiveDocument();
        InitializeCommands(navigateToRuleDescriptionCommand, locationNavigator);
        InitializeViewModels();
    }

    public ObservableCollection<IGroupViewModel> FilteredGroupViewModels { get; private set; }
    public bool HasGroups => FilteredGroupViewModels.Count > 0;
    public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; set; }
    public ICommand NavigateToLocationCommand { get; set; }
    public ReportViewFilterViewModel ReportViewFilter { get; } = new();

    public IIssueViewModel SelectedItem
    {
        get => selectedItem;
        set
        {
            if (selectedItem != value)
            {
                selectedItem = value;
                selectionService.SelectedIssue = (selectedItem as IAnalysisIssueViewModel)?.Issue;
                UpdateTelemetry(selectedItem);
            }
        }
    }

    internal void ApplyFilter()
    {
        FilteredGroupViewModels.Clear();
        FilterGroupsByLocationFilter();
        FilteredGroupViewModels.ToList().ForEach(group => group.ApplyFilter(ReportViewFilter));
        RemoveEmptyGroups();
        RaisePropertyChanged(nameof(HasGroups));
    }

    protected override void Dispose(bool disposing)
    {
        hotspotsReportViewModel.IssuesChanged -= HotspotsChanged;
        hotspotsReportViewModel.Dispose();

        dependencyRisksReportViewModel.DependencyRisksChanged -= DependencyRisksChanged;
        dependencyRisksReportViewModel.Dispose();

        taintsReportViewModel.IssuesChanged -= TaintsChanged;
        taintsReportViewModel.Dispose();

        activeDocumentTracker.ActiveDocumentChanged -= OnActiveDocumentChanged;

        foreach (var groupViewModel in allGroupViewModels)
        {
            groupViewModel.Dispose();
        }
        base.Dispose(disposing);
    }

    private void UpdateTelemetry(IIssueViewModel issueViewModel)
    {
        switch (issueViewModel)
        {
            case DependencyRiskViewModel:
                telemetryManager.DependencyRiskInvestigatedLocally();
                break;
            case HotspotViewModel:
                telemetryManager.HotspotInvestigatedLocally();
                break;
            case TaintViewModel:
                telemetryManager.TaintIssueInvestigatedLocally();
                break;
        }
    }

    private void HotspotsChanged(object sender, IssuesChangedEventArgs e)
    {
        var currentHotspotViewModels = allGroupViewModels.SelectMany(group => group.AllIssues).Where(vm => vm is HotspotViewModel).Cast<HotspotViewModel>();
        var addedHotspotsViewModels = e.AddedIssues.Select(viz => new HotspotViewModel(LocalHotspot.ToLocalHotspot(viz))).ToList();
        var removedHotspotViewModels = currentHotspotViewModels.Where(vm => e.RemovedIssues.Any(vm.IsSameAnalysisIssue)).ToList();
        UpdateChangedIssues(addedHotspotsViewModels, removedHotspotViewModels);
    }

    private void UpdateChangedIssues(IReadOnlyCollection<IIssueViewModel> addedIssueViewModels, IReadOnlyCollection<IIssueViewModel> removedIssues) =>
        threadHandling.RunOnUIThread(() =>
        {
            UpdateDeletedIssueViewModels(removedIssues);
            UpdateAddedIssueViewModels(addedIssueViewModels);
            ApplyFilter();
        });

    private void RemoveGroupViewModel(IGroupViewModel groupViewModel) =>
        threadHandling.RunOnUIThread(() =>
        {
            allGroupViewModels.Remove(groupViewModel);
        });

    private void AddGroupViewModel(IGroupViewModel groupViewModel) =>
        threadHandling.RunOnUIThread(() =>
        {
            if (groupViewModel != null)
            {
                allGroupViewModels.Add(groupViewModel);
            }
        });

    private void DependencyRisksChanged(object sender, EventArgs e)
    {
        if (allGroupViewModels.SingleOrDefault(vm => vm is GroupDependencyRiskViewModel) is { } groupDependencyRiskViewModel)
        {
            RemoveGroupViewModel(groupDependencyRiskViewModel);
        }
        InitializeDependencyRisks();
        ApplyFilter();
    }

    private void TaintsChanged(object sender, IssuesChangedEventArgs e)
    {
        var addedHotspotsViewModels = e.AddedIssues.Select(viz => new TaintViewModel(viz)).ToList();
        var currentHotspotViewModels = allGroupViewModels.SelectMany(group => group.AllIssues).Where(vm => vm is TaintViewModel).Cast<TaintViewModel>();
        var removedHotspotViewModels = currentHotspotViewModels.Where(vm => e.RemovedIssues.Any(vm.IsSameAnalysisIssue)).ToList();
        UpdateChangedIssues(addedHotspotsViewModels, removedHotspotViewModels);
    }

    private void InitializeViewModels()
    {
        allGroupViewModels.Clear();
        InitializeDependencyRisks();
        InitializeHotspots();
        InitializeTaints();
        FilteredGroupViewModels = new ObservableCollection<IGroupViewModel>(allGroupViewModels);
    }

    private void InitializeDependencyRisks()
    {
        var groupDependencyRisk = dependencyRisksReportViewModel.GetDependencyRisksGroup();
        AddGroupViewModel(groupDependencyRisk);
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void InitializeHotspots()
    {
        var groups = hotspotsReportViewModel.GetHotspotsGroupViewModels();
        groups.ToList().ForEach(g => allGroupViewModels.Add(g));
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void InitializeTaints()
    {
        var groups = taintsReportViewModel.GetTaintsGroupViewModels();
        groups.ToList().ForEach(g => allGroupViewModels.Add(g));
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void UpdateDeletedIssueViewModels(IReadOnlyCollection<IIssueViewModel> removedIssues)
    {
        foreach (var removedIssueVm in removedIssues)
        {
            if (GetGroupViewModelOfIssueViewModel(removedIssueVm) is { } group)
            {
                group.AllIssues.Remove(removedIssueVm);
                if (!group.AllIssues.Any())
                {
                    allGroupViewModels.Remove(group);
                }
            }
        }
    }

    private void UpdateAddedIssueViewModels(IReadOnlyCollection<IIssueViewModel> addedIssueViewModels)
    {
        foreach (var addedIssueViewModel in addedIssueViewModels)
        {
            if (GetGroupViewModelOfIssueViewModel(addedIssueViewModel) is { } group)
            {
                group.AllIssues.Add(addedIssueViewModel);
            }
            else
            {
                allGroupViewModels.Add(new GroupFileViewModel(addedIssueViewModel.FilePath, [addedIssueViewModel]));
            }
        }
    }

    private IGroupViewModel GetGroupViewModelOfIssueViewModel(IIssueViewModel issueViewModel) =>
        allGroupViewModels.FirstOrDefault(groupVm => groupVm is GroupFileViewModel && issueViewModel.FilePath == ((GroupFileViewModel)groupVm).FilePath);

    private void InitializeCommands(
        INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
        ILocationNavigator locationNavigator)
    {
        NavigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
        NavigateToLocationCommand = new DelegateCommand(parameter =>
        {
            var analysisIssueViewModel = (IAnalysisIssueViewModel)parameter;
            locationNavigator.TryNavigate(analysisIssueViewModel.Issue);
        }, parameter => parameter is IAnalysisIssueViewModel);
    }

    private void InitializeActiveDocument()
    {
        activeDocumentFilePath = activeDocumentLocator.FindActiveDocument()?.FilePath;
        activeDocumentTracker.ActiveDocumentChanged += OnActiveDocumentChanged;
    }

    private void OnActiveDocumentChanged(object sender, ActiveDocumentChangedEventArgs e)
    {
        activeDocumentFilePath = e.ActiveTextDocument?.FilePath;
        if (ReportViewFilter.SelectedLocationFilter.LocationFilter == LocationFilter.CurrentDocument)
        {
            ApplyFilter();
        }
    }

    private void FilterGroupsByLocationFilter()
    {
        var groupsToShow = allGroupViewModels;
        if (ReportViewFilter.SelectedLocationFilter.LocationFilter == LocationFilter.CurrentDocument)
        {
            groupsToShow = allGroupViewModels.Where(vm => vm.FilePath == activeDocumentFilePath).ToList();
        }

        groupsToShow.ForEach(vm => FilteredGroupViewModels.Add(vm));
    }

    private void RemoveEmptyGroups()
    {
        var emptyGroups = FilteredGroupViewModels.Where(g => !g.FilteredIssues.Any()).ToList();
        emptyGroups.ForEach(g => FilteredGroupViewModels.Remove(g));
    }
}
