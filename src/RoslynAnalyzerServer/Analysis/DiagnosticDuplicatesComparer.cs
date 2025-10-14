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

namespace SonarLint.VisualStudio.RoslynAnalyzerServer.Analysis;

public class DiagnosticDuplicatesComparer : IEqualityComparer<RoslynIssue>
{
    public static DiagnosticDuplicatesComparer Instance { get; } = new();

    private DiagnosticDuplicatesComparer()
    {
    }

    public bool Equals(RoslynIssue? x, RoslynIssue? y)
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

        return x.RuleId == y.RuleId && LocationEquals(x.PrimaryLocation, y.PrimaryLocation);
    }

    public int GetHashCode(RoslynIssue obj)
    {
        unchecked
        {
            var hc = obj.RuleId.GetHashCode();
            const int prime = 397;
            hc = (hc * prime) ^ obj.PrimaryLocation.FileUri.GetHashCode();
            hc = (hc * prime) ^ obj.PrimaryLocation.TextRange.StartLine;
            hc = (hc * prime) ^ obj.PrimaryLocation.TextRange.StartLineOffset;
            hc = (hc * prime) ^ obj.PrimaryLocation.TextRange.EndLine;
            hc = (hc * prime) ^ obj.PrimaryLocation.TextRange.EndLineOffset;
            return hc;
        }
    }

    private static bool LocationEquals(RoslynIssueLocation xPrimaryLocation, RoslynIssueLocation yPrimaryLocation) =>
        xPrimaryLocation.FileUri == yPrimaryLocation.FileUri &&
        xPrimaryLocation.TextRange.StartLine == yPrimaryLocation.TextRange.StartLine &&
        xPrimaryLocation.TextRange.EndLine == yPrimaryLocation.TextRange.EndLine &&
        xPrimaryLocation.TextRange.StartLineOffset == yPrimaryLocation.TextRange.StartLineOffset &&
        xPrimaryLocation.TextRange.EndLineOffset == yPrimaryLocation.TextRange.EndLineOffset;
}
