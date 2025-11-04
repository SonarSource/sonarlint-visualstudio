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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal sealed class GroupDependencyRiskViewModel : GroupViewModelBase
{
    private readonly IDependencyRisksStore dependencyRisksStore;
    private readonly List<IIssueViewModel> risks = new();

    public GroupDependencyRiskViewModel(IDependencyRisksStore dependencyRisksStore) : base(Resources.DependencyRisksGroupTitle, null)
    {
        this.dependencyRisksStore = dependencyRisksStore;
        PreFilteredIssues = risks;
        FilteredIssues = new ObservableCollection<IIssueViewModel>(risks);
    }
    public override List<IIssueViewModel> AllIssues => risks;
    public override List<IIssueViewModel> PreFilteredIssues { get; protected set; }
    public override ObservableCollection<IIssueViewModel> FilteredIssues { get; }

    public void InitializeRisks()
    {
        risks.Clear();
        FilteredIssues.Clear();
        var dependencyRisks = dependencyRisksStore.GetAll();
        var newDependencyRiskViewModels = dependencyRisks
            .Where(x => x.Status != DependencyRiskStatus.Fixed)
            .OrderByDescending(x => x.Severity)
            .ThenBy(x => x.Status)
            .Select(x => new DependencyRiskViewModel(x));
        foreach (var riskViewModel in newDependencyRiskViewModels)
        {
            risks.Add(riskViewModel);
            FilteredIssues.Add(riskViewModel);
        }
        RaisePropertyChanged(nameof(FilteredIssues));
    }
}
