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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.CSharpVB;

public interface IDiagnosticsConverter
{
    /// <summary>
    /// Converts Roslyn diagnostics to SonarLint analysis issues
    /// </summary>
    /// <param name="syntaxDiagnostics">Syntax diagnostics from Roslyn analyzer</param>
    /// <param name="semanticDiagnostics">Semantic diagnostics from Roslyn analyzer</param>
    /// <returns>A list of SonarLint analysis issues</returns>
    IEnumerable<IAnalysisIssue> Convert(ImmutableArray<Diagnostic> syntaxDiagnostics, ImmutableArray<Diagnostic> semanticDiagnostics);

    /// <summary>
    /// Determines if an issue is a duplicate of an existing issue
    /// </summary>
    /// <param name="existingIssues">List of existing issues</param>
    /// <param name="newIssue">New issue to check</param>
    /// <returns>True if the issue is a duplicate, otherwise false</returns>
    bool IsDuplicateIssue(List<IAnalysisIssue> existingIssues, IAnalysisIssue newIssue);
}

[Export(typeof(IDiagnosticsConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class DiagnosticsConverter : IDiagnosticsConverter
{
    public IEnumerable<IAnalysisIssue> Convert(ImmutableArray<Diagnostic> syntaxDiagnostics, ImmutableArray<Diagnostic> semanticDiagnostics)
    {
        var issues = semanticDiagnostics.Concat(syntaxDiagnostics)
            .Select(diagnostic =>
            {
                var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();
                var isWarning = diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;
                return new AnalysisIssue(
                    null,
                    diagnostic.Id + ":" + "GG",
                    null,
                    diagnostic.IsSuppressed,
                    isWarning ? AnalysisIssueSeverity.Critical : AnalysisIssueSeverity.Minor,
                    AnalysisIssueType.CodeSmell,
                    new Impact(SoftwareQuality.Maintainability, isWarning ? SoftwareQualitySeverity.High : SoftwareQualitySeverity.Low),
                    new AnalysisIssueLocation(
                        diagnostic.GetMessage(),
                        diagnostic.Location.SourceTree.FilePath,
                        new TextRange(
                            fileLinePositionSpan.StartLinePosition.Line + 1,
                            fileLinePositionSpan.EndLinePosition.Line + 1,
                            fileLinePositionSpan.StartLinePosition.Character,
                            fileLinePositionSpan.EndLinePosition.Character,
                            null)),
                    []);
            }).Cast<IAnalysisIssue>();
        return issues;
    }

    public bool IsDuplicateIssue(List<IAnalysisIssue> existingIssues, IAnalysisIssue newIssue) =>
        existingIssues.Any(existing =>
            existing.RuleKey == newIssue.RuleKey
            && existing.PrimaryLocation.FilePath == newIssue.PrimaryLocation.FilePath
            && AreTextRangesEqual(existing.PrimaryLocation.TextRange, newIssue.PrimaryLocation.TextRange));

    private static bool AreTextRangesEqual(ITextRange range1, ITextRange range2)
    {
        if (range1 == null && range2 == null)
        {
            return true;
        }

        if (range1 == null || range2 == null)
        {
            return false;
        }

        return range1.StartLine == range2.StartLine &&
               range1.EndLine == range2.EndLine &&
               range1.StartLineOffset == range2.StartLineOffset &&
               range1.EndLineOffset == range2.EndLineOffset;
    }
}
