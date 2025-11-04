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
    ObservableCollection<IGroupViewModel> AllGroupViewModels { get; }
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
    private readonly IDocumentTracker documentTracker;
    private readonly IThreadHandling threadHandling;
    private IIssueViewModel selectedItem;
    private string activeDocumentFilePath;

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
        IDocumentTracker documentTracker,
        IThreadHandling threadHandling) : base(activeSolutionBoundTracker)
    {
        this.hotspotsReportViewModel = hotspotsReportViewModel;
        this.dependencyRisksReportViewModel = dependencyRisksReportViewModel;
        this.taintsReportViewModel = taintsReportViewModel;
        this.telemetryManager = telemetryManager;
        this.selectionService = selectionService;
        this.activeDocumentLocator = activeDocumentLocator;
        this.activeDocumentTracker = activeDocumentTracker;
        this.documentTracker = documentTracker;
        this.threadHandling = threadHandling;

        hotspotsReportViewModel.IssuesChanged += HotspotsViewModel_IssuesChanged;
        dependencyRisksReportViewModel.DependencyRisksChanged += DependencyRisksViewModel_DependencyRisksChanged;
        taintsReportViewModel.IssuesChanged += TaintViewModel_IssuesChanged;
        documentTracker.DocumentOpened += DocumentTracker_DocumentOpened;
        documentTracker.DocumentClosed += DocumentTracker_DocumentClosed;

        InitializeActiveDocument();
        InitializeCommands(navigateToRuleDescriptionCommand, locationNavigator);
        InitializeViewModels();
    }

    private void DocumentTracker_DocumentOpened(object sender, DocumentEventArgs e) =>
        threadHandling.RunOnUIThread(() =>
        {
            if (GetGroupViewModelOfIssueViewModel(e.Document.FullPath) != null)
            {
                return;
            }

            AllGroupViewModels.Add(new GroupFileViewModel(e.Document.FullPath, []));
            ApplyFilter();
        });

    private void DocumentTracker_DocumentClosed(object sender, DocumentEventArgs e) =>
        threadHandling.RunOnUIThread(() =>
        {
            if (AllGroupViewModels.FirstOrDefault(x => x.FilePath == e.Document.FullPath) is not { } group)
            {
                return;
            }

            if (group.AllIssues.Any(x => x.IssueType == IssueType.TaintVulnerability))
            {
                // todo https://sonarsource.atlassian.net/browse/SLVS-2620 taints are global, not based on open file events
                return;
            }

            AllGroupViewModels.Remove(group);
            ApplyFilter();
        });

    public ObservableCollection<IGroupViewModel> AllGroupViewModels { get; private set; }
    public ObservableCollection<IGroupViewModel> FilteredGroupViewModels { get; private set; }
    public bool HasAnyGroups => AllGroupViewModels.Any(x => x.AllIssues.Any());
    public bool HasFilteredGroups => FilteredGroupViewModels.Any(x => x.FilteredIssues.Any());
    public bool HasNoFilteredIssuesForGroupsWithIssues => FilteredGroupViewModels.All(x => !x.FilteredIssues.Any() && x.PreFilteredIssues.Any()); // todo test
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

    private void InitializeViewModels()
    {
        AllGroupViewModels = new ObservableCollection<IGroupViewModel>();
        foreach (var openDocument in documentTracker.GetOpenDocuments())
        {
            AllGroupViewModels.Add(new GroupFileViewModel(openDocument.FullPath, []));
        }

        InitializeDependencyRisks();
        UpdateAddedIssueViewModels(hotspotsReportViewModel.GetIssueViewModels().Concat(taintsReportViewModel.GetIssueViewModels()));

        FilteredGroupViewModels = new ObservableCollection<IGroupViewModel>();
        ApplyFilter();
    }

    private void InitializeDependencyRisks()
    {
        var groupDependencyRisk = dependencyRisksReportViewModel.GetDependencyRisksGroup();
        if (groupDependencyRisk != null)
        {
            AllGroupViewModels.Add(groupDependencyRisk);
        }
        RaisePropertyChanged(nameof(HasAnyGroups));
        RaisePropertyChanged(nameof(HasFilteredGroups));
        RaisePropertyChanged(nameof(HasNoFilteredIssuesForGroupsWithIssues));
    }

    internal void ApplyFilter()
    {
        FilteredGroupViewModels.Clear();
        FilterGroupsByLocationFilter();
        FilteredGroupViewModels.ToList().ForEach(group => group.ApplyFilter(ReportViewFilter));
        RaisePropertyChanged(nameof(HasAnyGroups));
        RaisePropertyChanged(nameof(HasFilteredGroups));
        RaisePropertyChanged(nameof(HasNoFilteredIssuesForGroupsWithIssues));
    }

    protected override void Dispose(bool disposing)
    {
        hotspotsReportViewModel.IssuesChanged -= HotspotsViewModel_IssuesChanged;
        hotspotsReportViewModel.Dispose();

        dependencyRisksReportViewModel.DependencyRisksChanged -= DependencyRisksViewModel_DependencyRisksChanged;
        dependencyRisksReportViewModel.Dispose();

        taintsReportViewModel.IssuesChanged -= TaintViewModel_IssuesChanged;
        taintsReportViewModel.Dispose();

        activeDocumentTracker.ActiveDocumentChanged -= OnActiveDocumentChanged;

        documentTracker.DocumentOpened -= DocumentTracker_DocumentOpened;
        documentTracker.DocumentClosed -= DocumentTracker_DocumentClosed;

        foreach (var groupViewModel in AllGroupViewModels)
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

    private void DependencyRisksViewModel_DependencyRisksChanged(object sender, EventArgs e)
    {
        if (AllGroupViewModels.SingleOrDefault(vm => vm is GroupDependencyRiskViewModel) is { } groupDependencyRiskViewModel)
        {
            AllGroupViewModels.Remove(groupDependencyRiskViewModel);
        }
        InitializeDependencyRisks();
        ApplyFilter();
    }

    private void HotspotsViewModel_IssuesChanged(object sender, IssuesChangedEventArgs e)
    {
        var currentHotspotViewModels = AllGroupViewModels.SelectMany(group => group.AllIssues).Where(vm => vm is HotspotViewModel).Cast<HotspotViewModel>();
        var addedHotspotsViewModels = e.AddedIssues.Select(viz => new HotspotViewModel(LocalHotspot.ToLocalHotspot(viz))).ToList();
        var removedHotspotViewModels = currentHotspotViewModels.Where(vm => e.RemovedIssues.Any(vm.IsSameAnalysisIssue)).ToList();
        UpdateChangedIssues(addedHotspotsViewModels, removedHotspotViewModels);
    }

    private void TaintViewModel_IssuesChanged(object sender, IssuesChangedEventArgs e)
    {
        var addedHotspotsViewModels = e.AddedIssues.Select(viz => new TaintViewModel(viz)).ToList();
        var currentHotspotViewModels = AllGroupViewModels.SelectMany(group => group.AllIssues).Where(vm => vm is TaintViewModel).Cast<TaintViewModel>();
        var removedHotspotViewModels = currentHotspotViewModels.Where(vm => e.RemovedIssues.Any(vm.IsSameAnalysisIssue)).ToList();
        UpdateChangedIssues(addedHotspotsViewModels, removedHotspotViewModels);
    }

    private void UpdateChangedIssues(IReadOnlyCollection<IIssueViewModel> addedIssueViewModels, IReadOnlyCollection<IIssueViewModel> removedIssues)
    {
        UpdateDeletedIssueViewModels(removedIssues);
        UpdateAddedIssueViewModels(addedIssueViewModels);
        ApplyFilter();
    }

    private void UpdateDeletedIssueViewModels(IReadOnlyCollection<IIssueViewModel> removedIssues)
    {
        foreach (var removedIssueVm in removedIssues)
        {
            if (GetGroupViewModelOfIssueViewModel(removedIssueVm.FilePath) is { } group)
            {
                group.AllIssues.Remove(removedIssueVm);
            }
        }
    }

    private void UpdateAddedIssueViewModels(IEnumerable<IIssueViewModel> addedIssueViewModels)
    {
        foreach (var addedIssueViewModel in addedIssueViewModels)
        {
            if (GetGroupViewModelOfIssueViewModel(addedIssueViewModel.FilePath) is { } group)
            {
                group.AllIssues.Add(addedIssueViewModel);
            }
            else
            {
                AllGroupViewModels.Add(new GroupFileViewModel(addedIssueViewModel.FilePath, [addedIssueViewModel]));
            }
        }
    }

    private IGroupViewModel GetGroupViewModelOfIssueViewModel(string filePath) =>
        AllGroupViewModels.FirstOrDefault(groupVm => groupVm is GroupFileViewModel && filePath == ((GroupFileViewModel)groupVm).FilePath);

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
        IEnumerable<IGroupViewModel> groupsToShow = AllGroupViewModels;
        if (ReportViewFilter.SelectedLocationFilter.LocationFilter == LocationFilter.CurrentDocument)
        {
            groupsToShow = AllGroupViewModels.Where(vm => vm.FilePath == activeDocumentFilePath);
        }

        foreach (var groupViewModel in groupsToShow.OrderBy(x => x.FilePath is not null).ThenBy(x => x.Title))
        {
            FilteredGroupViewModels.Add(groupViewModel);
        }
    }
}
