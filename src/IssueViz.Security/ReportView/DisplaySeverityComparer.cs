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

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

public class DisplaySeverityComparer : IComparer<DisplaySeverity>
{
    private DisplaySeverityComparer()
    {
    }

    public static IComparer<DisplaySeverity> Instance { get; } = new DisplaySeverityComparer();

    private static readonly Dictionary<DisplaySeverity, int> Ranks = new()
    {
        { DisplaySeverity.Info, 0 },
        { DisplaySeverity.Low, 1 },
        { DisplaySeverity.Medium, 2 },
        { DisplaySeverity.High, 3 },
        { DisplaySeverity.Blocker, 4 }
    };

    public int Compare(DisplaySeverity x, DisplaySeverity y)
    {
        var rankX = Ranks[x];
        var rankY = Ranks[y];

        return rankX.CompareTo(rankY);
    }
}
