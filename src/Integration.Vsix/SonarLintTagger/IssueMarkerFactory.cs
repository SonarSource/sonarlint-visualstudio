/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class IssueMarkerFactory : IIssueMarkerFactory
    {
        public IssueMarker Create(IAnalysisIssue issue, ITextSnapshot currentSnapshot)
        {
            // SonarLint issues line numbers are 1-based, spans lines are 0-based

            var maxLength = currentSnapshot.Length;

            var startLine = currentSnapshot.GetLineFromLineNumber(issue.StartLine - 1);
            int startPos = startLine.Start.Position + issue.StartLineOffset;

            int endPos;
            if (issue.EndLine == 0          // Special case : EndLine = 0 means "select whole of the start line, ignoring the offset"
                || startPos > maxLength)    // Defensive : issue start position is beyond the end of the file. Just select the last line.
            {
                startPos = startLine.Start.Position;
                endPos = startLine.Start.Position + startLine.Length;
            }
            else
            {
                endPos = currentSnapshot.GetLineFromLineNumber(issue.EndLine - 1).Start.Position + issue.EndLineOffset;
                // Make sure the end position isn't beyond the end of the snapshot either
                endPos = Math.Min(maxLength, endPos);
            }

            var start = new SnapshotPoint(currentSnapshot, startPos);
            var end = new SnapshotPoint(currentSnapshot, endPos);

            return new IssueMarker(issue, new SnapshotSpan(start, end));
        }
    }
}
