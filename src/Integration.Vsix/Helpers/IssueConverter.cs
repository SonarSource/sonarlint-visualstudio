/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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

using Microsoft.VisualStudio.Text;

namespace SonarLint.VisualStudio.Integration.Vsix.Helpers
{
    internal class IssueConverter : IIssueConverter
    {
        public IssueMarker ToMarker(Sonarlint.Issue issue, ITextSnapshot currentSnapshot)
        {
            int startPos = currentSnapshot.GetLineFromLineNumber(issue.StartLine - 1).Start.Position + issue.StartLineOffset;
            var start = new SnapshotPoint(currentSnapshot, startPos);

            int endPos = currentSnapshot.GetLineFromLineNumber(issue.EndLine - 1).Start.Position + issue.EndLineOffset;
            var end = new SnapshotPoint(currentSnapshot, endPos);

            return new IssueMarker(issue, new SnapshotSpan(start, end));
        }
    }
}
