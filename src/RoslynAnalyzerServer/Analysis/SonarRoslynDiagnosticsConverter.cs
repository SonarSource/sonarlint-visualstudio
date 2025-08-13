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

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis;
using SonarLint.VisualStudio.Core;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

[Export(typeof(IDiagnosticToRoslynIssueConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class DiagnosticToRoslynIssueConverter : IDiagnosticToRoslynIssueConverter
{
    private const string DefaultSecondaryLocationTitleTemplate = "Location {0}";

    public RoslynIssue ConvertToSonarDiagnostic(Diagnostic diagnostic, Language language) =>
        // todo SLVS-2427 quick fixes
        new(new SonarCompositeRuleId(language.RepoInfo.Key, diagnostic.Id).ErrorListErrorCode,
            ConvertLocation(diagnostic.Location.GetMappedLineSpan(), diagnostic.GetMessage()),
            ConvertSecondaryLocations(diagnostic));

    private static IReadOnlyList<RoslynIssueFlow> ConvertSecondaryLocations(Diagnostic diagnostic)
    {
        if (diagnostic.AdditionalLocations.Count == 0)
        {
            return [];
        }

        return
        [
            // this will need to be modified once multi-flow locations are supported by the dotnet analyzer
            new(diagnostic
                .AdditionalLocations
                .Select((location, index) =>
                {
                    if (!diagnostic.Properties.TryGetValue(index.ToString(), out var title) || title is null)
                    {
                        title = string.Format(DefaultSecondaryLocationTitleTemplate, index);
                    }
                    return ConvertLocation(location.GetMappedLineSpan(), title);
                })
                .ToList())
        ];
    }

    private static RoslynIssueLocation ConvertLocation(FileLinePositionSpan fileLinePositionSpan, string message)
    {
        var textRange = new RoslynIssueTextRange(
            fileLinePositionSpan.StartLinePosition.Line + 1, // roslyn lines are 0-based, while we use 1-based
            fileLinePositionSpan.EndLinePosition.Line + 1, // roslyn lines are 0-based, while we use 1-based
            fileLinePositionSpan.StartLinePosition.Character,
            fileLinePositionSpan.EndLinePosition.Character);

        var location = new RoslynIssueLocation(
            message,
            fileLinePositionSpan.Path,
            textRange);

        return location;
    }
}
