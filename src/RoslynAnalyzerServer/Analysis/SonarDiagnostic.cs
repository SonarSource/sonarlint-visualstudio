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

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

public class SonarDiagnostic(
    string ruleKey,
    SonarDiagnosticLocation primaryLocation,
    IReadOnlyList<SonarDiagnosticFlow>? flows = null,
    IReadOnlyList<IQuickFix>? fixes = null)
{
    private static readonly IReadOnlyList<SonarDiagnosticFlow> EmptyFlows = [];
    private static readonly IReadOnlyList<IQuickFix> EmptyFixes = [];

    public string RuleKey { get; } = ruleKey;
    public SonarDiagnosticLocation PrimaryLocation { get; } = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
    public IReadOnlyList<SonarDiagnosticFlow> Flows { get; } = flows ?? EmptyFlows;
    public IReadOnlyList<IQuickFix> Fixes { get; } = fixes ?? EmptyFixes;
}

public class SonarDiagnosticFlow(IReadOnlyList<SonarDiagnosticLocation> locations)
{
    public IReadOnlyList<SonarDiagnosticLocation> Locations { get; } = locations ?? throw new ArgumentNullException(nameof(locations));
}

public class SonarDiagnosticLocation(string message, string filePath, SonarTextRange textRange)
{
    public string FilePath { get; } = filePath;
    public string Message { get; } = message;
    public SonarTextRange TextRange { get; } = textRange;
}

public class SonarTextRange(
    int startLine,
    int endLine,
    int startLineOffset,
    int endLineOffset,
    string? lineHash)
{
    public int StartLine { get; } = startLine;
    public int EndLine { get; } = endLine;
    public int StartLineOffset { get; } = startLineOffset;
    public int EndLineOffset { get; } = endLineOffset;
    public string? LineHash { get; } = lineHash;
}
