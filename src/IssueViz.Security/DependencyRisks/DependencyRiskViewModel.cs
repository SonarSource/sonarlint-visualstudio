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
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;

internal class DependencyRiskViewModel : ViewModelBase
{
    private string packageName;
    private string packageVersion;
    private DependencyRiskImpactSeverity impactSeverity;
    private DependencyRiskType type;
    private DependencyRiskStatus status;

    public string PackageName
    {
        get => packageName;
        set
        {
            packageName = value;
            RaisePropertyChanged();
        }
    }

    public string PackageVersion
    {
        get => packageVersion;
        set
        {
            packageVersion = value;
            RaisePropertyChanged();
        }
    }

    public DependencyRiskImpactSeverity ImpactSeverity
    {
        get => impactSeverity;
        set
        {
            impactSeverity = value;
            RaisePropertyChanged();
        }
    }

    public DependencyRiskType Type
    {
        get => type;
        set
        {
            type = value;
            RaisePropertyChanged();
        }
    }

    public DependencyRiskStatus Status
    {
        get => status;
        set
        {
            status = value;
            RaisePropertyChanged();
        }
    }
}
