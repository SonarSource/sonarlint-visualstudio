/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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
using Sonarlint;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class IssueMarker
    {
        public Issue Issue { get; }
        public SnapshotSpan Span { get; }

        public IssueMarker(Issue issue, SnapshotSpan span)
        {
            this.Issue = issue;
            this.Span = span;
        }

        public static IssueMarker Clone(IssueMarker marker)
        {
            return new IssueMarker(marker.Issue, marker.Span);
        }

        public static IssueMarker CloneAndTranslateTo(IssueMarker marker, ITextSnapshot newSnapshot)
        {
            var newSpan = marker.Span.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive);

            // If the span changed, the marker is no longer valid
            if (newSpan.Length != marker.Span.Length)
            {
                return null;
            }
            return new IssueMarker(marker.Issue, newSpan);
        }
    }
}
