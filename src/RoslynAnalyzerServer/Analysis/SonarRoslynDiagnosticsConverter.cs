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
    public RoslynIssue ConvertToSonarDiagnostic(Diagnostic diagnostic, Language language)
    {
        var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();

        var textRange = new RoslynIssueTextRange(
            fileLinePositionSpan.StartLinePosition.Line + 1, // roslyn lines are 0-based, while we use 1-based
            fileLinePositionSpan.EndLinePosition.Line + 1, // roslyn lines are 0-based, while we use 1-based
            fileLinePositionSpan.StartLinePosition.Character,
            fileLinePositionSpan.EndLinePosition.Character);

        var location = new RoslynIssueLocation(
            diagnostic.GetMessage(),
            fileLinePositionSpan.Path,
            textRange);

        // todo SLVS-2427 quick fixes
        // todo SLVS-2428 secondary locations
        return new RoslynIssue(
            SonarCompositeRuleId.GetFullErrorCode(language.RepoInfo.Key, diagnostic.Id),
            location);
    }
}
