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
using System.Windows;
using System.Windows.Data;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal class ReportViewModel : ServerViewModel
{
    private readonly IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler;
    private readonly IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler;
    private readonly IMessageBox messageBox;
    private readonly ITelemetryManager telemetryManager;
    private readonly IDependencyRisksStore dependencyRisksStore;
    private readonly ILocalHotspotsStore hotspotsStore;
    private readonly object @lock = new();
    private IIssueViewModel selectedItem;

    public ReportViewModel(
        IActiveSolutionBoundTracker activeSolutionBoundTracker,
        IDependencyRisksStore dependencyRisksStore,
        ILocalHotspotsStore hotspotsStore,
        IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler,
        IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler,
        INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand,
        IMessageBox messageBox,
        ITelemetryManager telemetryManager,
        IThreadHandling threadHandling) : base(activeSolutionBoundTracker)
    {
        NavigateToRuleDescriptionCommand = navigateToRuleDescriptionCommand;
        this.dependencyRisksStore = dependencyRisksStore;
        this.hotspotsStore = hotspotsStore;
        this.showDependencyRiskInBrowserHandler = showDependencyRiskInBrowserHandler;
        this.changeDependencyRiskStatusHandler = changeDependencyRiskStatusHandler;
        this.messageBox = messageBox;
        this.telemetryManager = telemetryManager;

        threadHandling.RunOnUIThread(() => { BindingOperations.EnableCollectionSynchronization(GroupViewModels, @lock); });
        hotspotsStore.IssuesChanged += HotspotsStore_IssuesChanged;
        dependencyRisksStore.DependencyRisksChanged += DependencyRisksStore_DependencyRiskChanged;
        InitializeViewModels();
    }

    public ObservableCollection<IGroupViewModel> GroupViewModels { get; } = [];
    public bool HasGroups => GroupViewModels.Count > 0;
    public INavigateToRuleDescriptionCommand NavigateToRuleDescriptionCommand { get; }

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

    public async Task ChangeStatusAsync(IDependencyRisk dependencyRisk, DependencyRiskTransition? selectedTransition, string getNormalizedComment)
    {
        if (selectedTransition is not { } transition)
        {
            ShowFailureMessage(Resources.DependencyRiskNullTransitionError);
            return;
        }

        var result = await changeDependencyRiskStatusHandler.ChangeStatusAsync(dependencyRisk.Id, transition, getNormalizedComment);

        if (!result)
        {
            ShowFailureMessage(Resources.DependencyRiskStatusChangeError);
        }
    }

    private void ShowFailureMessage(string errorMessage) => messageBox.Show(Resources.DependencyRiskStatusChangeFailedTitle, errorMessage, MessageBoxButton.OK, MessageBoxImage.Error);

    public void ShowInBrowser(IDependencyRisk dependencyRisk) => showDependencyRiskInBrowserHandler.ShowInBrowser(dependencyRisk.Id);

    protected override void Dispose(bool disposing)
    {
        hotspotsStore.IssuesChanged -= HotspotsStore_IssuesChanged;
        dependencyRisksStore.DependencyRisksChanged -= DependencyRisksStore_DependencyRiskChanged;
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

    private void HotspotsStore_IssuesChanged(object sender, IssuesChangedEventArgs e)
    {
        foreach (var groupViewModel in GroupViewModels.Where(vm => vm is not GroupDependencyRiskViewModel).ToList())
        {
            GroupViewModels.Remove(groupViewModel);
        }
        InitializeHotspots();
    }

    private void DependencyRisksStore_DependencyRiskChanged(object sender, EventArgs e)
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
        var groupDependencyRisk = new GroupDependencyRiskViewModel(dependencyRisksStore);
        groupDependencyRisk.InitializeRisks();
        if (groupDependencyRisk.FilteredIssues.Any())
        {
            GroupViewModels.Add(groupDependencyRisk);
        }
        RaisePropertyChanged(nameof(HasGroups));
    }

    private void InitializeHotspots()
    {
        var hotspots = hotspotsStore.GetAllLocalHotspots().Select(x => new HotspotViewModel(x));
        var groups = GetGroupViewModel(hotspots);
        groups.ToList().ForEach(g => GroupViewModels.Add(g));
        RaisePropertyChanged(nameof(HasGroups));
    }

    private static ObservableCollection<IGroupViewModel> GetGroupViewModel(IEnumerable<IIssueViewModel> issueViewModels)
    {
        var issuesByFileGrouping = issueViewModels.GroupBy(vm => vm.FilePath);
        var groupViewModels = new ObservableCollection<IGroupViewModel>();
        foreach (var group in issuesByFileGrouping)
        {
            groupViewModels.Add(new GroupFileViewModel(group.Key, new ObservableCollection<IIssueViewModel>(group)));
        }

        return groupViewModels;
    }
}
