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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal sealed class GroupDependencyRiskViewModel : ViewModelBase, IDisposable
{
    private readonly IDependencyRisksStore dependencyRisksStore;
    private readonly IReadOnlyCollection<IDependencyRiskFilter> filters;
    private readonly ITelemetryManager telemetryManager;
    private readonly IThreadHandling threadHandling;
    private DependencyRiskViewModel selectedItem;
    private readonly ObservableCollection<DependencyRiskViewModel> risks = new();
    private readonly ObservableCollection<DependencyRiskViewModel> filteredRisks = new();

    public GroupDependencyRiskViewModel(
        IDependencyRisksStore dependencyRisksStore,
        IReadOnlyCollection<IDependencyRiskFilter> filters,
        ITelemetryManager telemetryManager,
        IThreadHandling threadHandling)
    {
        this.dependencyRisksStore = dependencyRisksStore;
        this.filters = filters;
        this.telemetryManager = telemetryManager;
        this.threadHandling = threadHandling;
        dependencyRisksStore.DependencyRisksChanged += OnDependencyRiskChanged;
    }

    public static string Title => Resources.DependencyRisksGroupTitle;

    public ObservableCollection<DependencyRiskViewModel> Risks => risks;
    public ObservableCollection<DependencyRiskViewModel> FilteredRisks => filteredRisks;

    public bool HasRisks => risks.Count > 0;

    public DependencyRiskViewModel SelectedItem
    {
        get => selectedItem;
        set
        {
            if (selectedItem != value)
            {
                selectedItem = value;
                if (selectedItem != null)
                {
                    telemetryManager.DependencyRiskInvestigatedLocally();
                }
            }
        }
    }

    public void InitializeRisks() =>
        threadHandling.RunOnUIThread(() =>
        {
            risks.Clear();
            var dependencyRisks = dependencyRisksStore.GetAll();
            var newDependencyRiskViewModels = dependencyRisks
                .OrderByDescending(x => x.Severity)
                .ThenBy(x => x.Status)
                .Select(x => new DependencyRiskViewModel(x));
            foreach (var riskViewModel in newDependencyRiskViewModels)
            {
                risks.Add(riskViewModel);
            }
            RefreshFiltering();
            RaisePropertyChanged(nameof(Risks));
            RaisePropertyChanged(nameof(HasRisks));
        });

    private void OnDependencyRiskChanged(object sender, EventArgs e) => InitializeRisks();

    public void Dispose() => dependencyRisksStore.DependencyRisksChanged -= OnDependencyRiskChanged;

    public void RefreshFiltering()
    {
        UpdateFilteredHotspots();
        RaisePropertyChanged(nameof(FilteredRisks));
    }

    private void UpdateFilteredHotspots()
    {
        filteredRisks.Clear();
        foreach (var dependencyRiskViewModel in risks.Where(x => filters.All(f => !f.IsFilteredOut(x))))
        {
            filteredRisks.Add(dependencyRiskViewModel);
        }
    }
}
