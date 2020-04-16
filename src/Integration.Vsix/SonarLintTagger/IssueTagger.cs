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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Flyweight wrapper that provides data and notifications about tags
    /// in a single view to a single consumer
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    internal sealed class IssueTagger : ITagger<IErrorTag>, IDisposable
    {
        private IssuesSnapshot issues;
        private bool isDisposed;
        internal /* for testing */ IIssueTracker IssueTracker { get; }

        public IssueTagger(IIssueTracker issueTracker)
        {
            this.IssueTracker = issueTracker;
            this.issues = issueTracker.LastIssues;

            issueTracker.AddTagger(this);
        }

        public void UpdateMarkers(IssuesSnapshot newIssues, SnapshotSpan? affectedSpan)
        {
            this.issues = newIssues;

            var handler = this.TagsChanged;
            if (handler != null && affectedSpan.HasValue)
            {
                handler(this, new SnapshotSpanEventArgs(affectedSpan.Value));
            }
        }
        
        public void Dispose()
        {
            if (!isDisposed)
            {
                // Called when the tagger is no longer needed (generally when the ITextView is closed).
                this.IssueTracker.RemoveTagger(this);

                this.isDisposed = true;
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            Debug.Assert(!isDisposed, "Not expecting GetTags to be called after the tagger has been disposed");

            if (issues == null)
            {
                return Enumerable.Empty<ITagSpan<IErrorTag>>();
            }

            return issues.IssueMarkers
                .Where(marker => spans.IntersectsWith(marker.Span))
                .Select(marker => new TagSpan<IErrorTag>(marker.Span, new ErrorTag(PredefinedErrorTypeNames.Warning, marker.Issue.Message)));
        }
    }
}
