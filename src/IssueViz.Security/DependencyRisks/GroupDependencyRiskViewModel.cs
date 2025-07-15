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
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal class GroupDependencyRiskViewModel : ViewModelBase
{
    public static string Title => Resources.DependencyRisksGroupTitle;

    public ObservableCollection<DependencyRiskViewModel> Risks { get; } = new();

    public bool HasRisks => Risks.Count > 0;

    public void InitializeRisks()
    {
        // TOOD by https://sonarsource.atlassian.net/browse/SLVS-2371: remove hard coded implementation and show risks from store
        Risks.Add(new DependencyRiskViewModel
        {
            PackageName = "System.ComponentModel.Composition", PackageVersion = "9.0.70", ImpactSeverity = DependencyRiskImpactSeverity.Blocker, Type = DependencyRiskType.Vulnerability
        });
        Risks.Add(
            new DependencyRiskViewModel
            {
                PackageName = "System.Windows.Presentation", PackageVersion = "1.9", ImpactSeverity = DependencyRiskImpactSeverity.High, Type = DependencyRiskType.ProhibitedLicense
            });
        Risks.Add(
            new DependencyRiskViewModel
            {
                PackageName = "Microsoft.Owin.Host.HttpListener", PackageVersion = "13.6.6", ImpactSeverity = DependencyRiskImpactSeverity.Info, Type = DependencyRiskType.Vulnerability
            });
        RaisePropertyChanged(nameof(HasRisks));
    }
}
