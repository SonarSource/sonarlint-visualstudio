/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security
{
    internal interface IIssueVizDisplayPositionCalculator
    {
        int GetLine(IAnalysisIssueVisualization issueViz);

        int GetColumn(IAnalysisIssueVisualization issueViz);
    }

    internal class IssueVizDisplayPositionCalculator : IIssueVizDisplayPositionCalculator
    {
        int IIssueVizDisplayPositionCalculator.GetColumn(IAnalysisIssueVisualization issueViz)
        {
            int zeroBasedColumn;

            if (!CanUseSpan(issueViz))
            {
                zeroBasedColumn = issueViz.Issue.PrimaryLocation.TextRange.StartLineOffset;
            }
            else
            {
                var position = issueViz.Span.Value.Start;
                var line = position.GetContainingLine();
                zeroBasedColumn = position.Position - line.Start.Position;
            }

            // Both SQ issue column and VS spans are zero-based.
            // The editor displays lines and columns as one-based.
            return zeroBasedColumn + 1;
        }

        int IIssueVizDisplayPositionCalculator.GetLine(IAnalysisIssueVisualization issueViz) =>
                        CanUseSpan(issueViz)
                // VS spans are zero-based, Sonar line numbers are one-based
                // The editor displays lines and columns as one-based.
                ? issueViz.Span.Value.Start.GetContainingLine().LineNumber + 1
                : issueViz.Issue.PrimaryLocation.TextRange.StartLine;

        private bool CanUseSpan(IAnalysisIssueVisualization issueViz) =>
            issueViz.Span.HasValue && !issueViz.Span.Value.IsEmpty;
    }
}
