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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal class DependencyRiskViewModel(IDependencyRisk dependencyRisk)
    : ViewModelBase, IIssueViewModel
{
    public bool IsTransitionAllowed { get; } = dependencyRisk.Transitions.Any();

    public bool IsResolved { get; } = dependencyRisk.Status is DependencyRiskStatus.Accepted or DependencyRiskStatus.Safe;

    public IDependencyRisk DependencyRisk { get; } = dependencyRisk;
    public int? Line => null;
    public int? Column => null;
    public string Title => DependencyRisk.VulnerabilityId;
    public string FilePath => null;
    public RuleInfoViewModel RuleInfo => null;
    public DisplaySeverity DisplaySeverity { get; } = dependencyRisk.Severity switch
    {
        DependencyRiskImpactSeverity.Info => DisplaySeverity.Info,
        DependencyRiskImpactSeverity.Low => DisplaySeverity.Low,
        DependencyRiskImpactSeverity.Medium => DisplaySeverity.Medium,
        DependencyRiskImpactSeverity.High => DisplaySeverity.High,
        DependencyRiskImpactSeverity.Blocker => DisplaySeverity.Blocker,
        _ => DisplaySeverity.Info
    };

    public IssueType IssueType => IssueType.DependencyRisk;
    public DisplayStatus Status { get; } = dependencyRisk.Status switch
    {
        DependencyRiskStatus.Fixed => DisplayStatus.Resolved,
        DependencyRiskStatus.Open => DisplayStatus.Open,
        DependencyRiskStatus.Confirmed => DisplayStatus.Open,
        DependencyRiskStatus.Accepted => DisplayStatus.Resolved,
        DependencyRiskStatus.Safe => DisplayStatus.Resolved,
        _ => DisplayStatus.Open
    };
    public bool IsOnNewCode => true;
    public bool IsServerIssue => true;
}
