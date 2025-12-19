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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal interface IReportViewModel
{
    ObservableCollection<IGroupViewModel> FilteredGroupViewModels { get; }
    ObservableCollection<IGroupViewModel> AllGroupViewModels { get; }
}

internal class
    ReportViewModel : ServerViewModel, IReportViewModel
{
    private readonly ILocationNavigator locationNavigator;
    private readonly IHotspotsReportViewModel hotspotsReportViewModel;
    private readonly IDependencyRisksReportViewModel dependencyRisksReportViewModel;
    private readonly IIssuesReportViewModel issuesReportViewModel;
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
        IIssuesReportViewModel issuesReportViewModel,
        ITelemetryManager telemetryManager,
        IIssueSelectionService selectionService,
        IActiveDocumentLocator activeDocumentLocator,
        IActiveDocumentTracker activeDocumentTracker,
        IDocumentTracker documentTracker,
        IFocusOnNewCodeServiceUpdater focusOnNewCodeService,
        IThreadHandling threadHandling,
        IInitializationProcessorFactory initializationProcessorFactory) : base(activeSolutionBoundTracker, initializationProcessorFactory)
    {
        this.locationNavigator = locationNavigator;
        this.hotspotsReportViewModel = hotspotsReportViewModel;
        this.dependencyRisksReportViewModel = dependencyRisksReportViewModel;
        this.taintsReportViewModel = taintsReportViewModel;
        this.telemetryManager = telemetryManager;
        this.selectionService = selectionService;
        this.activeDocumentLocator = activeDocumentLocator;
        this.activeDocumentTracker = activeDocumentTracker;
        this.documentTracker = documentTracker;
        this.threadHandling = threadHandling;
        this.issuesReportViewModel = issuesReportViewModel;
        NavigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
        AllGroupViewModels = new ObservableCollection<IGroupViewModel>();
        ReportViewFilter = new ReportViewFilterViewModel(focusOnNewCodeService, threadHandling);
        FilteredGroupViewModels = new ObservableCollection<IGroupViewModel>();

        hotspotsReportViewModel.IssuesChanged += ViewModel_AnalysisIssuesChanged;
        dependencyRisksReportViewModel.DependencyRisksChanged += DependencyRisksViewModel_DependencyRisksChanged;
        taintsReportViewModel.IssuesChanged += ViewModel_AnalysisIssuesChanged;
        issuesReportViewModel.IssuesChanged += ViewModel_AnalysisIssuesChanged;
        documentTracker.DocumentOpened += DocumentTracker_DocumentOpened;
        documentTracker.DocumentClosed += DocumentTracker_DocumentClosed;

        InitializeActiveDocument();
        InitializeViewModels();
    }

    private void DocumentTracker_DocumentOpened(object sender, DocumentEventArgs e) =>
        threadHandling.RunOnUIThread(() =>
        {
            if (GetGroupViewModelOfIssueViewModel(e.Document.FullPath) != null)
            {
                return;
            }

            AllGroupViewModels.Add(new GroupFileViewModel(e.Document.FullPath, taintsReportViewModel.GetIssueViewModels().Where(x => x.FilePath == e.Document.FullPath).ToList()));
            ApplyFilter();
        });

    private void DocumentTracker_DocumentClosed(object sender, DocumentEventArgs e) =>
        threadHandling.RunOnUIThread(() =>
        {
            if (AllGroupViewModels.FirstOrDefault(x => x.FilePath == e.Document.FullPath) is not { } group)
            {
                return;
            }

            AllGroupViewModels.Remove(group);
            ApplyFilter();
        });

    public ObservableCollection<IGroupViewModel> AllGroupViewModels { get; }
    public ObservableCollection<IGroupViewModel> FilteredGroupViewModels { get; }
    public bool HasAnyGroups => AllGroupViewModels.Any();
    public bool HasFilteredGroups => FilteredGroupViewModels.Any(x => x.FilteredIssues.Any());
    // this indicates whether to show the 'too restrictive filters' warning, we only want to do that if filtered issues are 0 but prefiltered are not
    public bool HasNoFilteredIssuesForGroupsWithIssues => AllGroupViewModels.Any() && FilteredGroupViewModels.All(x => !x.FilteredIssues.Any() && x.PreFilteredIssues.Any());
    public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }
    public ReportViewFilterViewModel ReportViewFilter { get; }

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

    internal void InitializeViewModels()
    {
        foreach (var openDocument in documentTracker.GetOpenDocuments())
        {
            AllGroupViewModels.Add(new GroupFileViewModel(openDocument.FullPath, []));
        }

        InitializeDependencyRisks();
        UpdateFileLevelAddedIssueViewModels(hotspotsReportViewModel.GetIssueViewModels().Concat(taintsReportViewModel.GetIssueViewModels()).Concat(issuesReportViewModel.GetIssueViewModels()));

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

    internal void ResetFilters()
    {
        ReportViewFilter.ClearAllFilters();
        ApplyFilter();
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

    protected override void HandleBindingChange(BindingConfiguration newBinding)
    {
        // todo https://sonarsource.atlassian.net/browse/SLVS-2620 taints are global, not based on open file events.
        // remove this clear logic as it is only needed due to taints being raised for files without TBIT, which don't trigger close event
        AllGroupViewModels.Clear();
        FilteredGroupViewModels.Clear();
        ResetFilters();
    }

    protected override void Dispose(bool disposing)
    {
        hotspotsReportViewModel.IssuesChanged -= ViewModel_AnalysisIssuesChanged;
        hotspotsReportViewModel.Dispose();

        dependencyRisksReportViewModel.DependencyRisksChanged -= DependencyRisksViewModel_DependencyRisksChanged;
        dependencyRisksReportViewModel.Dispose();

        taintsReportViewModel.IssuesChanged -= ViewModel_AnalysisIssuesChanged;
        taintsReportViewModel.Dispose();

        issuesReportViewModel.IssuesChanged -= ViewModel_AnalysisIssuesChanged;
        issuesReportViewModel.Dispose();

        activeDocumentTracker.ActiveDocumentChanged -= OnActiveDocumentChanged;

        documentTracker.DocumentOpened -= DocumentTracker_DocumentOpened;
        documentTracker.DocumentClosed -= DocumentTracker_DocumentClosed;

        ReportViewFilter.Dispose();

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

    private void ViewModel_AnalysisIssuesChanged(object sender, ViewModelAnalysisIssuesChangedEventArgs e) =>
        UpdateChangedIssues(e.AddedIssues, e.RemovedIssues);

    private void UpdateChangedIssues(IEnumerable<IIssueViewModel> addedIssueViewModels, HashSet<Guid> removedIssues)
    {
        UpdateFileLevelDeletedIssueViewModels(removedIssues);
        UpdateFileLevelAddedIssueViewModels(addedIssueViewModels);
        ApplyFilter();
    }

    private void UpdateFileLevelDeletedIssueViewModels(HashSet<Guid> removedIssues)
    {
        foreach (var group in AllGroupViewModels.Where(x => x is not GroupDependencyRiskViewModel))
        {
            group.AllIssues.RemoveAll(x => removedIssues.Contains(x.Id));
        }
    }

    private void UpdateFileLevelAddedIssueViewModels(IEnumerable<IIssueViewModel> addedIssueViewModels)
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

    public void Navigate(IAnalysisIssueViewModel analysisIssueViewModel) => locationNavigator.TryNavigate(analysisIssueViewModel.Issue);

    public void Navigate(IGroupViewModel groupViewModel)
    {
        if (groupViewModel is not GroupFileViewModel fileViewModel)
        {
            return;
        }

        locationNavigator.TryNavigateFile(fileViewModel.FilePath);
    }
}
