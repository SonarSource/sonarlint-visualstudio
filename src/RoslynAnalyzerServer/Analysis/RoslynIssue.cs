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

using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

public class RoslynIssue(
    string ruleId,
    RoslynIssueLocation primaryLocation,
    IReadOnlyList<RoslynIssueFlow>? flows = null,
    IReadOnlyList<RoslynIssueQuickFix>? quickFixes = null)
{
    private static readonly IReadOnlyList<RoslynIssueFlow> EmptyFlows = [];

    public string RuleId { get; } = ruleId;
    public RoslynIssueLocation PrimaryLocation { get; } = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
    public IReadOnlyList<RoslynIssueFlow> Flows { get; } = flows ?? EmptyFlows;
    public IReadOnlyList<RoslynIssueQuickFix> QuickFixes { get; } = quickFixes ?? [];
}

public class RoslynIssueQuickFix(string value)
{
    public string Value { get; } = value;
}

public class RoslynIssueFlow(IReadOnlyList<RoslynIssueLocation> locations)
{
    public IReadOnlyList<RoslynIssueLocation> Locations { get; } = locations ?? throw new ArgumentNullException(nameof(locations));
}

public class RoslynIssueLocation(string message, FileUri fileUri, RoslynIssueTextRange textRange)
{
    public FileUri FileUri { get; } = fileUri;
    public string Message { get; } = message;
    public RoslynIssueTextRange TextRange { get; } = textRange;
}

public class RoslynIssueTextRange(
    int startLine,
    int endLine,
    int startLineOffset,
    int endLineOffset)
{
    public int StartLine { get; } = startLine;
    public int EndLine { get; } = endLine;
    public int StartLineOffset { get; } = startLineOffset;
    public int EndLineOffset { get; } = endLineOffset;
}
