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

using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal interface IDependencyRiskFilter
{
    bool IsFilteredOut(DependencyRiskViewModel dependencyRisk);
}

internal class ResolutionFilterViewModel(bool isResolved, bool isSelected) : ViewModelBase, IDependencyRiskFilter
{
    private bool isSelected = isSelected;
    public bool IsResolved { get; } = isResolved;
    public string Title { get; } = isResolved ? Resources.ResolutionFilter_Resolved : Resources.ResolutionFilter_Open;

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            RaisePropertyChanged();
        }
    }

    public bool IsFilteredOut(DependencyRiskViewModel dependencyRisk)
    {
        if (isSelected)
        {
            return false;
        }

        return IsResolved == dependencyRisk.IsResolved;
    }
}
