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
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal class GroupDependencyRiskViewModel : ViewModelBase, IDisposable
{
    private readonly IDependencyRisksStore dependencyRisksStore;

    public GroupDependencyRiskViewModel(IDependencyRisksStore dependencyRisksStore)
    {
        this.dependencyRisksStore = dependencyRisksStore;
        dependencyRisksStore.DependencyRisksChanged += OnDependencyRiskChanged;
        InitializeRisks();
    }

    public static string Title => Resources.DependencyRisksGroupTitle;

    public ObservableCollection<DependencyRiskViewModel> Risks { get; } = new();

    public bool HasRisks => Risks.Count > 0;

    public void InitializeRisks()
    {
        Risks.Clear();
        var newDependencyRiskViewModels = dependencyRisksStore.GetAll().Select(x => new DependencyRiskViewModel(x)).ToList();
        newDependencyRiskViewModels.ForEach(Risks.Add);
        RaisePropertyChanged(nameof(HasRisks));
    }

    private void OnDependencyRiskChanged(object sender, EventArgs e) => InitializeRisks();

    public void Dispose() => dependencyRisksStore.DependencyRisksChanged -= OnDependencyRiskChanged;
}
