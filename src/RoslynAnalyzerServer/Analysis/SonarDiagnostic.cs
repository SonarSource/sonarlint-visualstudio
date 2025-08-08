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

public class DiagnosticDuplicatesComparer : IEqualityComparer<SonarDiagnostic>
{
    public static DiagnosticDuplicatesComparer Instance { get; } = new();

    private DiagnosticDuplicatesComparer()
    {
    }

    public bool Equals(SonarDiagnostic x, SonarDiagnostic y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }
        if (x is null)
        {
            return false;
        }
        if (y is null)
        {
            return false;
        }

        return x.RuleKey == y.RuleKey && LocationEquals(x.PrimaryLocation, y.PrimaryLocation);
    }

    public int GetHashCode(SonarDiagnostic obj)
    {
        unchecked
        {
            var hc = obj.RuleKey.GetHashCode();
            hc = (hc * 397) ^ obj.PrimaryLocation.FilePath.GetHashCode();
            hc = (hc * 397) ^ obj.PrimaryLocation.TextRange.StartLine;
            hc = (hc * 397) ^ obj.PrimaryLocation.TextRange.StartLineOffset;
            hc = (hc * 397) ^ obj.PrimaryLocation.TextRange.EndLine;
            hc = (hc * 397) ^ obj.PrimaryLocation.TextRange.EndLineOffset;
            return hc;
        }
    }

    private static bool LocationEquals(SonarDiagnosticLocation xPrimaryLocation, SonarDiagnosticLocation yPrimaryLocation) =>
        xPrimaryLocation.FilePath == yPrimaryLocation.FilePath &&
        xPrimaryLocation.TextRange.StartLine == yPrimaryLocation.TextRange.StartLine &&
        xPrimaryLocation.TextRange.EndLine == yPrimaryLocation.TextRange.EndLine &&
        xPrimaryLocation.TextRange.StartLineOffset == yPrimaryLocation.TextRange.StartLineOffset &&
        xPrimaryLocation.TextRange.EndLineOffset == yPrimaryLocation.TextRange.EndLineOffset;
}

public class SonarDiagnostic
{
    private static readonly IReadOnlyList<SonarDiagnosticFlow> EmptyFlows = [];
    private static readonly IReadOnlyList<IQuickFix> EmptyFixes = [];

    public SonarDiagnostic(
        string ruleKey,
        bool isWarning, // todo
        SonarDiagnosticLocation primaryLocation,
        IReadOnlyList<SonarDiagnosticFlow> flows,
        IReadOnlyList<IQuickFix> fixes = null)
    {
        RuleKey = ruleKey;
        IsWarning = isWarning;
        PrimaryLocation = primaryLocation ?? throw new ArgumentNullException(nameof(primaryLocation));
        Flows = flows ?? EmptyFlows;
        Fixes = fixes ?? EmptyFixes;
    }

    public string RuleKey { get; }
    public bool IsWarning { get; }
    public IReadOnlyList<SonarDiagnosticFlow> Flows { get; }
    public SonarDiagnosticLocation PrimaryLocation { get; }
    public IReadOnlyList<IQuickFix> Fixes { get; }
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
    string lineHash)
{
    public int StartLine { get; } = startLine;
    public int EndLine { get; } = endLine;
    public int StartLineOffset { get; } = startLineOffset;
    public int EndLineOffset { get; } = endLineOffset;
    public string LineHash { get; } = lineHash;
}
