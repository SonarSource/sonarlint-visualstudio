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
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal class ReportViewModel : ServerViewModel
{
    private readonly IHotspotsReportViewModel hotspotsReportViewModel;
    private readonly IDependencyRisksReportViewModel dependencyRisksReportViewModel;
    private readonly ITelemetryManager telemetryManager;
    private readonly object @lock = new();
    private IIssueViewModel selectedItem;

    public ReportViewModel(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
        ILocationNavigator locationNavigator,
        IHotspotsReportViewModel hotspotsReportViewModel,
        IDependencyRisksReportViewModel dependencyRisksReportViewModel,
        ITelemetryManager telemetryManager,
        IThreadHandling threadHandling) : base(activeSolutionBoundTracker)
    {
        this.hotspotsReportViewModel = hotspotsReportViewModel;
        this.dependencyRisksReportViewModel = dependencyRisksReportViewModel;
        this.telemetryManager = telemetryManager;

        threadHandling.RunOnUIThread(() => { BindingOperations.EnableCollectionSynchronization(GroupViewModels, @lock); });
        hotspotsReportViewModel.HotspotsChanged += HotspotsChanged;
        dependencyRisksReportViewModel.DependencyRisksChanged += DependencyRisksChanged;

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
        hotspotsReportViewModel.HotspotsChanged -= HotspotsChanged;
        hotspotsReportViewModel.Dispose();

        dependencyRisksReportViewModel.DependencyRisksChanged -= DependencyRisksChanged;
        dependencyRisksReportViewModel.Dispose();

        foreach (var groupViewModel in GroupViewModels)
        {
            groupViewModel.Dispose();
        }
        base.Dispose(disposing);
    }

    private void UpdateTelemetry(IIssueViewModel issueViewModel)
    {
        if (issueViewModel is DependencyRiskViewModel)
        {
            telemetryManager.DependencyRiskInvestigatedLocally();
        }
    }

    private void HotspotsChanged(object sender, EventArgs e)
    {
        foreach (var groupViewModel in GroupViewModels.Where(vm => vm is not GroupDependencyRiskViewModel).ToList())
        {
            GroupViewModels.Remove(groupViewModel);
        }
        InitializeHotspots();
    }

    private void DependencyRisksChanged(object sender, EventArgs e)
    {
        if (GroupViewModels.SingleOrDefault(vm => vm is GroupDependencyRiskViewModel) is { } groupDependencyRiskViewModel)
        {
            GroupViewModels.Remove(groupDependencyRiskViewModel);
        }
        InitializeDependencyRisks();
    }

    private void InitializeViewModels()
    {
        GroupViewModels.Clear();
        InitializeDependencyRisks();
        InitializeHotspots();
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
