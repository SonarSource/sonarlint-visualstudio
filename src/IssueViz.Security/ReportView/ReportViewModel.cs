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
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal class ReportViewModel : ServerViewModel
{
    private readonly IHotspotsReportViewModel hotspotsReportViewModel;
    private readonly IDependencyRisksReportViewModel dependencyRisksReportViewModel;
    private readonly ITaintsReportViewModel taintsReportViewModel;
    private readonly ITelemetryManager telemetryManager;
    private readonly IThreadHandling threadHandling;
    private readonly object @lock = new();
    private IIssueViewModel selectedItem;

    public ReportViewModel(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
        ILocationNavigator locationNavigator,
        IHotspotsReportViewModel hotspotsReportViewModel,
        IDependencyRisksReportViewModel dependencyRisksReportViewModel,
        ITaintsReportViewModel taintsReportViewModel,
        ITelemetryManager telemetryManager,
        IThreadHandling threadHandling) : base(activeSolutionBoundTracker)
    {
        this.hotspotsReportViewModel = hotspotsReportViewModel;
        this.dependencyRisksReportViewModel = dependencyRisksReportViewModel;
        this.taintsReportViewModel = taintsReportViewModel;
        this.telemetryManager = telemetryManager;
        this.threadHandling = threadHandling;

        threadHandling.RunOnUIThread(() => { BindingOperations.EnableCollectionSynchronization(GroupViewModels, @lock); });
        hotspotsReportViewModel.IssuesChanged += HotspotsChanged;
        dependencyRisksReportViewModel.DependencyRisksChanged += DependencyRisksChanged;
        taintsReportViewModel.IssuesChanged += TaintsChanged;

        InitializeCommands(navigateToRuleDescriptionCommand, locationNavigator);
        InitializeViewModels();
    }

    public ObservableCollection<IGroupViewModel> GroupViewModels { get; } = [];
    public bool HasGroups => GroupViewModels.Count > 0;
    public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; set; }
    public ICommand NavigateToLocationCommand { get; set; }

    public IIssueViewModel SelectedItem
    {
        get => selectedItem;
        set
        {
            if (selectedItem != value)
            {
                selectedItem = value;
                UpdateTelemetry(selectedItem);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        hotspotsReportViewModel.IssuesChanged -= HotspotsChanged;
        hotspotsReportViewModel.Dispose();

        dependencyRisksReportViewModel.DependencyRisksChanged -= DependencyRisksChanged;
        dependencyRisksReportViewModel.Dispose();

        taintsReportViewModel.IssuesChanged -= TaintsChanged;
        taintsReportViewModel.Dispose();

        foreach (var groupViewModel in GroupViewModels)
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
        var currentHotspotViewModels = GroupViewModels.SelectMany(group => group.FilteredIssues).Where(vm => vm is HotspotViewModel).Cast<HotspotViewModel>();
        var addedHotspotsViewModels = e.AddedIssues.Select(viz => new HotspotViewModel(LocalHotspot.ToLocalHotspot(viz))).ToList();
        var removedHotspotViewModels = currentHotspotViewModels.Where(vm => e.RemovedIssues.Any(vm.IsSameAnalysisIssue)).ToList();
        UpdateChangedIssues(addedHotspotsViewModels, removedHotspotViewModels);
    }

    private void UpdateChangedIssues(IReadOnlyCollection<IIssueViewModel> addedIssueViewModels, IReadOnlyCollection<IIssueViewModel> removedIssues)
    {
        UpdateDeletedIssueViewModels(removedIssues);
        UpdateAddedIssueViewModels(addedIssueViewModels);
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void DependencyRisksChanged(object sender, EventArgs e)
    {
        if (GroupViewModels.SingleOrDefault(vm => vm is GroupDependencyRiskViewModel) is { } groupDependencyRiskViewModel)
        {
            GroupViewModels.Remove(groupDependencyRiskViewModel);
        }
        InitializeDependencyRisks();
    }

    private void TaintsChanged(object sender, IssuesChangedEventArgs e)
    {
        var addedHotspotsViewModels = e.AddedIssues.Select(viz => new TaintViewModel(viz)).ToList();
        var currentHotspotViewModels = GroupViewModels.SelectMany(group => group.FilteredIssues).Where(vm => vm is TaintViewModel).Cast<TaintViewModel>();
        var removedHotspotViewModels = currentHotspotViewModels.Where(vm => e.RemovedIssues.Any(vm.IsSameAnalysisIssue)).ToList();
        UpdateChangedIssues(addedHotspotsViewModels, removedHotspotViewModels);
    }

    private void InitializeViewModels()
    {
        GroupViewModels.Clear();
        InitializeDependencyRisks();
        InitializeHotspots();
        InitializeTaints();
    }

    private void InitializeDependencyRisks()
    {
        var groupDependencyRisk = dependencyRisksReportViewModel.GetDependencyRisksGroup();
        if (groupDependencyRisk != null)
        {
            GroupViewModels.Add(groupDependencyRisk);
        }
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void InitializeHotspots()
    {
        var groups = hotspotsReportViewModel.GetHotspotsGroupViewModels();
        groups.ToList().ForEach(g => GroupViewModels.Add(g));
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void InitializeTaints()
    {
        var groups = taintsReportViewModel.GetTaintsGroupViewModels();
        groups.ToList().ForEach(g => GroupViewModels.Add(g));
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void UpdateDeletedIssueViewModels(IReadOnlyCollection<IIssueViewModel> removedIssues)
    {
        foreach (var removedIssueVm in removedIssues)
        {
            if (GetGroupViewModelOfIssueViewModel(removedIssueVm) is { } group)
            {
                group.FilteredIssues.Remove(removedIssueVm);
                if (!group.FilteredIssues.Any())
                {
                    GroupViewModels.Remove(group);
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
                group.FilteredIssues.Add(addedIssueViewModel);
            }
            else
            {
                GroupViewModels.Add(new GroupFileViewModel(addedIssueViewModel.FilePath, [addedIssueViewModel], threadHandling));
            }
        }
    }

    private IGroupViewModel GetGroupViewModelOfIssueViewModel(IIssueViewModel issueViewModel) =>
        GroupViewModels.FirstOrDefault(groupVm => groupVm is GroupFileViewModel && issueViewModel.FilePath == ((GroupFileViewModel)groupVm).FilePath);

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
}
