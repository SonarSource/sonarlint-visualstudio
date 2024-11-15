/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.SLCore.Common.Helpers;
using SonarLint.VisualStudio.SLCore.Common.Models;
using SonarLint.VisualStudio.SLCore.Service.Rules.Models;
using IssueSeverity = SonarLint.VisualStudio.SLCore.Common.Models.IssueSeverity;
using SoftwareQuality = SonarLint.VisualStudio.Core.Analysis.SoftwareQuality;

namespace SonarLint.VisualStudio.Education.Rule;

internal interface IRuleInfoConverter
{
    IRuleInfo Convert(IRuleDetails details);
}

[Export(typeof(IRuleInfoConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class RuleInfoConverter : IRuleInfoConverter
{
    [ImportingConstructor]
    public RuleInfoConverter() { }

    public IRuleInfo Convert(IRuleDetails details) =>
        new RuleInfo(details.key,
            HtmlXmlCompatibilityHelper.EnsureHtmlIsXml(details.description?.Left?.htmlContent),
            details.name,
            Convert(details.severityDetails.Left?.severity),
            Convert(details.severityDetails.Left?.type),
            details.description?.Right,
            (details.severityDetails.Right?.cleanCodeAttribute).ToCleanCodeAttribute(),
            Convert(details.severityDetails.Right?.impacts));

    private static RuleIssueSeverity? Convert(IssueSeverity? issueSeverity) =>
        issueSeverity switch
        {
            IssueSeverity.BLOCKER => RuleIssueSeverity.Blocker,
            IssueSeverity.CRITICAL => RuleIssueSeverity.Critical,
            IssueSeverity.MAJOR => RuleIssueSeverity.Major,
            IssueSeverity.MINOR => RuleIssueSeverity.Minor,
            IssueSeverity.INFO => RuleIssueSeverity.Info,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(issueSeverity), issueSeverity, null)
        };

    private static RuleIssueType? Convert(RuleType? ruleType) =>
        ruleType switch
        {
            RuleType.CODE_SMELL => RuleIssueType.CodeSmell,
            RuleType.BUG => RuleIssueType.Bug,
            RuleType.VULNERABILITY => RuleIssueType.Vulnerability,
            RuleType.SECURITY_HOTSPOT => RuleIssueType.Hotspot,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(ruleType), ruleType, null)
        };

    private static Dictionary<SoftwareQuality, SoftwareQualitySeverity> Convert(List<ImpactDto> cleanCodeAttribute) =>
        cleanCodeAttribute?.ToDictionary(x => x.softwareQuality.ToSoftwareQuality(), x => x.impactSeverity.ToSoftwareQualitySeverity());
}
