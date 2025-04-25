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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.CSharpVB;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.ConnectedMode.Binding;

public class SonarQubeRoslynRuleStatus(SonarQubeRule sonarQubeRule, IEnvironmentSettings environmentSettings) : IRoslynRuleStatus
{
    public string Key => sonarQubeRule.Key;

    public RuleAction GetSeverity()
    {
        if (!sonarQubeRule.IsActive)
        {
            return RuleAction.None;
        }
        return GetVsSeverity(sonarQubeRule.SoftwareQualitySeverities?.Values) ?? GetVsSeverity(sonarQubeRule.Severity);
    }

    private RuleAction? GetVsSeverity(ICollection<SonarQubeSoftwareQualitySeverity> severities) =>
        severities is not { Count: > 0 }
            ? null
            : GetVsSeverity(severities.Max());

    private RuleAction GetVsSeverity(SonarQubeSoftwareQualitySeverity severity) =>
        severity switch
        {
            SonarQubeSoftwareQualitySeverity.Blocker or SonarQubeSoftwareQualitySeverity.High => environmentSettings.TreatBlockerSeverityAsError() ? RuleAction.Error : RuleAction.Warning,
            SonarQubeSoftwareQualitySeverity.Medium => RuleAction.Warning,
            SonarQubeSoftwareQualitySeverity.Low or SonarQubeSoftwareQualitySeverity.Info => RuleAction.Info,
            _ => throw new ArgumentOutOfRangeException($"Unsupported SonarQube issue severity: {severity}")
        };

    private RuleAction GetVsSeverity(SonarQubeIssueSeverity sqSeverity) =>
        sqSeverity switch
        {
            SonarQubeIssueSeverity.Info or SonarQubeIssueSeverity.Minor => RuleAction.Info,
            SonarQubeIssueSeverity.Major or SonarQubeIssueSeverity.Critical => RuleAction.Warning,
            SonarQubeIssueSeverity.Blocker => environmentSettings.TreatBlockerSeverityAsError() ? RuleAction.Error : RuleAction.Warning,
            _ => throw new ArgumentOutOfRangeException($"Unsupported SonarQube issue severity: {sqSeverity}")
        };
}
