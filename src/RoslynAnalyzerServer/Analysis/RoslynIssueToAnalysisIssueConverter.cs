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

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

internal interface IDiagnosticToAnalysisIssueConverter
{
    IAnalysisIssue Convert(RoslynIssue roslynIssue);
}

[Export(typeof(IDiagnosticToAnalysisIssueConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
[method: ImportingConstructor]
internal class RoslynIssueToAnalysisIssueConverter(ISonarLintSettings sonarLintSettings) : IDiagnosticToAnalysisIssueConverter
{
    public IAnalysisIssue Convert(RoslynIssue roslynIssue)
    {
        var primaryLocation = CreateLocation(roslynIssue.PrimaryLocation);
        var flows = CreateFlows(roslynIssue.Flows);
        var quickFixes = CreateQuickFixes(roslynIssue.QuickFixes);

        return new AnalysisIssue(
            id: Guid.NewGuid(),
            ruleKey: roslynIssue.RuleId,
            issueServerKey: null,
            isResolved: false,
            isOnNewCode: true,
            severity: null,
            type: null,
            highestImpact: new Impact(SoftwareQuality.Maintainability, GetImpactSeverity()),
            primaryLocation: primaryLocation,
            flows: flows,
            fixes: quickFixes);
    }

    private SoftwareQualitySeverity GetImpactSeverity() =>
        sonarLintSettings.PragmaRuleSeverity == PragmaRuleSeverity.Warn
            ? SoftwareQualitySeverity.Medium
            : SoftwareQualitySeverity.Low;

    private static IReadOnlyList<IAnalysisIssueFlow> CreateFlows(IReadOnlyList<RoslynIssueFlow> flows)
    {
        if (flows.Count == 0)
        {
            return [];
        }

        return flows.Select(flow =>
            new AnalysisIssueFlow(flow.Locations.Select(CreateLocation).ToList()) as IAnalysisIssueFlow).ToList();
    }

    private static AnalysisIssueLocation CreateLocation(RoslynIssueLocation location)
    {
        var textRange = new TextRange(
            location.TextRange.StartLine,
            location.TextRange.EndLine,
            location.TextRange.StartLineOffset,
            location.TextRange.EndLineOffset,
            null);
        return new AnalysisIssueLocation(location.Message, location.FileUri.LocalPath, textRange);
    }

    private static IReadOnlyList<IQuickFixBase> CreateQuickFixes(IReadOnlyList<RoslynIssueQuickFix> quickFixes)
    {
        if (quickFixes.Count == 0)
        {
            return [];
        }

        var result = new List<IQuickFixBase>();
        foreach (var quickFix in quickFixes)
        {
            if (RoslynQuickFix.TryParse(quickFix.Value, out var parsed))
            {
                result.Add(parsed);
            }
        }
        return result;
    }
}
