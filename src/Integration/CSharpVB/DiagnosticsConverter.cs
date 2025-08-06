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

using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public interface IDiagnosticsConverter
{
    IEnumerable<SonarDiagnostic> ConvertToDiagnostics(ImmutableArray<Diagnostic> syntaxDiagnostics, ImmutableArray<Diagnostic> semanticDiagnostics);

    IAnalysisIssue ConvertToAnalysisIssue(SonarDiagnostic diagnostic);
}

[Export(typeof(IDiagnosticsConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class DiagnosticsConverter : IDiagnosticsConverter
{
    public IEnumerable<IAnalysisIssue> Convert(ImmutableArray<Diagnostic> syntaxDiagnostics, ImmutableArray<Diagnostic> semanticDiagnostics)
    {
        return ConvertToDiagnostics(syntaxDiagnostics, semanticDiagnostics)
            .Select(ConvertToAnalysisIssue);
    }

    public IEnumerable<SonarDiagnostic> ConvertToDiagnostics(ImmutableArray<Diagnostic> syntaxDiagnostics, ImmutableArray<Diagnostic> semanticDiagnostics)
    {
        var diagnostics = semanticDiagnostics.Concat(syntaxDiagnostics)
            .Select(diagnostic =>
            {
                var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();
                var isWarning = diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;

                var textRange = new SonarTextRange(
                    fileLinePositionSpan.StartLinePosition.Line + 1,
                    fileLinePositionSpan.EndLinePosition.Line + 1,
                    fileLinePositionSpan.StartLinePosition.Character,
                    fileLinePositionSpan.EndLinePosition.Character,
                    null); // todo line hash calculation

                var location = new SonarDiagnosticLocation(
                    diagnostic.GetMessage(),
                    diagnostic.Location.SourceTree.FilePath,
                    textRange);

                return new SonarDiagnostic(
                    diagnostic.Id + ":" + "GG",
                    isWarning,
                    location,
                    []); // todo secondary locations and quick fixes
            });
        return diagnostics;
    }

    public IAnalysisIssue ConvertToAnalysisIssue(SonarDiagnostic diagnostic)
    {
        var analysisIssueFlows = diagnostic.Flows.Select(flow =>
            new AnalysisIssueFlow(
                flow.Locations.Select(location =>
                    new AnalysisIssueLocation(
                        location.Message,
                        location.FilePath,
                        new TextRange(
                            location.TextRange.StartLine,
                            location.TextRange.EndLine,
                            location.TextRange.StartLineOffset,
                            location.TextRange.EndLineOffset,
                            location.TextRange.LineHash)
                    )
                ).ToList()
            )
        ).ToList();

        var primaryLocation = new AnalysisIssueLocation(
            diagnostic.PrimaryLocation.Message,
            diagnostic.PrimaryLocation.FilePath,
            new TextRange(
                diagnostic.PrimaryLocation.TextRange.StartLine,
                diagnostic.PrimaryLocation.TextRange.EndLine,
                diagnostic.PrimaryLocation.TextRange.StartLineOffset,
                diagnostic.PrimaryLocation.TextRange.EndLineOffset,
                diagnostic.PrimaryLocation.TextRange.LineHash)
        );

        return new AnalysisIssue(
            null,
            diagnostic.RuleKey,
            null,
            false,
            diagnostic.IsWarning ? AnalysisIssueSeverity.Critical : AnalysisIssueSeverity.Minor,
            AnalysisIssueType.CodeSmell,
            new Impact(SoftwareQuality.Maintainability, diagnostic.IsWarning ? SoftwareQualitySeverity.High : SoftwareQualitySeverity.Low),
            primaryLocation,
            analysisIssueFlows,
            diagnostic.Fixes);
    }
}
