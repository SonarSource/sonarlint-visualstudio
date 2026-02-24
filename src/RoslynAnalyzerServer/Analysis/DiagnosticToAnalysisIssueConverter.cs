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
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

internal interface IDiagnosticToAnalysisIssueConverter
{
    IAnalysisIssue Convert(Diagnostic diagnostic, List<RoslynQuickFix> quickFixes);
}

[Export(typeof(IDiagnosticToAnalysisIssueConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal class DiagnosticToAnalysisIssueConverter : IDiagnosticToAnalysisIssueConverter
{
    public IAnalysisIssue Convert(Diagnostic diagnostic, List<RoslynQuickFix> quickFixes)
    {
        var mappedSpan = diagnostic.Location.GetMappedLineSpan();
        var primaryLocation = CreateLocation(mappedSpan, diagnostic.GetMessage());

        var flows = CreateFlows(diagnostic);

        return new AnalysisIssue(
            id: Guid.NewGuid(),
            ruleKey: "csharpsquid:" + diagnostic.Id,
            issueServerKey: null,
            isResolved: false,
            isOnNewCode: true,
            severity: null,
            type: null,
            highestImpact: new Impact(SoftwareQuality.Maintainability, SoftwareQualitySeverity.Low),
            primaryLocation: primaryLocation,
            flows: flows,
            fixes: quickFixes.Count > 0
                ? quickFixes.Cast<IQuickFixBase>().ToList()
                : []);
    }

    private static IReadOnlyList<IAnalysisIssueFlow> CreateFlows(Diagnostic diagnostic)
    {
        if (diagnostic.AdditionalLocations.Count == 0)
        {
            return [];
        }

        var locations = diagnostic.AdditionalLocations
            .Select(loc => CreateLocation(loc.GetMappedLineSpan(), null))
            .ToList();

        return [new AnalysisIssueFlow(locations)];
    }

    private static AnalysisIssueLocation CreateLocation(FileLinePositionSpan span, string message)
    {
        var startLine = span.StartLinePosition.Line + 1;
        var endLine = span.EndLinePosition.Line + 1;
        var startLineOffset = span.StartLinePosition.Character;
        var endLineOffset = span.EndLinePosition.Character;

        var textRange = new TextRange(startLine, endLine, startLineOffset, endLineOffset, null);
        return new AnalysisIssueLocation(message, span.Path, textRange);
    }
}
