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

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

public interface IRoslynDiagnosticsConverter
{
    SonarDiagnostic ConvertToSonarDiagnostic(Diagnostic diagnostic);
}

[Export(typeof(IRoslynDiagnosticsConverter))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class SonarRoslynDiagnosticsConverter : IRoslynDiagnosticsConverter
{
    public SonarDiagnostic ConvertToSonarDiagnostic(Diagnostic diagnostic)
    {
        var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();

        var textRange = new SonarTextRange(
            fileLinePositionSpan.StartLinePosition.Line + 1,
            fileLinePositionSpan.EndLinePosition.Line + 1,
            fileLinePositionSpan.StartLinePosition.Character,
            fileLinePositionSpan.EndLinePosition.Character,
            null); // todo line hash calculation

        var location = new SonarDiagnosticLocation(
            diagnostic.GetMessage(),
            fileLinePositionSpan.Path,
            textRange);

        return new SonarDiagnostic(
            diagnostic.Id,
            location,
            []); // todo secondary locations and quick fixes
    }
}
