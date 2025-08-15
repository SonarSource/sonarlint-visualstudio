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

using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

public interface IDependencyRisk
{
    Guid Id { get; }
    DependencyRiskType Type { get; }
    DependencyRiskImpactSeverity Severity { get; }
    DependencyRiskStatus Status { get; }
    string PackageName { get; }
    string PackageVersion { get; }
    string VulnerabilityId { get; }
    string CvssScore { get; }
    List<DependencyRiskTransition> Transitions { get; }
}

internal class DependencyRisk(
    Guid id,
    DependencyRiskType type,
    DependencyRiskImpactSeverity severity,
    DependencyRiskStatus status,
    string packageName,
    string packageVersion,
    string vulnerabilityId,
    string cvssScore,
    List<DependencyRiskTransition> transitions) : IDependencyRisk
{
    public Guid Id { get; } = id;
    public DependencyRiskType Type { get; } = type;
    public DependencyRiskImpactSeverity Severity { get; } = severity;
    public DependencyRiskStatus Status { get; } = status;
    public string PackageName { get; } = packageName;
    public string VulnerabilityId { get; } = vulnerabilityId;
    public string CvssScore { get; } = cvssScore;
    public string PackageVersion { get; } = packageVersion;
    public List<DependencyRiskTransition> Transitions { get; } = transitions;
}
