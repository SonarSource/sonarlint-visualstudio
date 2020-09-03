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
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.ErrorTagging
{
    internal sealed class ErrorTagger : ITagger<IErrorTag>, IDisposable
    {
        private readonly ITextBuffer textBuffer;
        private readonly ITagAggregator<IIssueLocationTag> tagAggregator;

        public ErrorTagger(ITextBuffer textBuffer, ITagAggregator<IIssueLocationTag> tagAggregator)
        {
            this.textBuffer = textBuffer;
            this.tagAggregator = tagAggregator;

            tagAggregator.BatchedTagsChanged += OnAggregatorTagsChanged;
        }

        private void OnAggregatorTagsChanged(object sender, BatchedTagsChangedEventArgs e)
        {
            // We need to propagate the change notification, otherwise our GetTags method won't be called.

            // Note: we're currently reporting the whole snapshot as having changed. We could be more specific
            // if our buffer raised a more specific notification.
            var wholeSpan = new SnapshotSpan(textBuffer.CurrentSnapshot, 0, textBuffer.CurrentSnapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(wholeSpan));
        }

        #region ITagger methods

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0) { yield break; }

            var issueLocationTagsSpans = tagAggregator.GetTags(spans);
            if (!issueLocationTagsSpans.Any()) { yield break; }

            Debug.Assert(issueLocationTagsSpans.All(x => x.Tag.Location.Span.HasValue), "Expecting all tagged locations to have a span");

            // We're only interested in primary locations, not secondary locations
            var issueTagSpans = issueLocationTagsSpans
                .Where(x => IsValidPrimaryLocation(x.Tag.Location))
                .ToArray();
            if (issueTagSpans.Length == 0) { yield break; }
            
            var callerSnapshot = spans[0].Snapshot;
            var tagSnaphot = issueTagSpans[0].Tag.Location.Span.Value.Snapshot;

            if (callerSnapshot.Version != tagSnaphot.Version)
            {
                Debug.Fail($"Expecting the tags to reference the correct snapshot. Caller snapshot version: {callerSnapshot.Version}, tag snapshot version: {tagSnaphot.Version}");
                yield break;
            }

            foreach (var dataTagSpan in issueTagSpans)
            {
                if (spans.IntersectsWith(dataTagSpan.Tag.Location.Span.Value))
                {
                    yield return new TagSpan<IErrorTag>(dataTagSpan.Tag.Location.Span.Value, new ErrorTag(PredefinedErrorTypeNames.Warning, "XXXXXXXXXXXXXX " + dataTagSpan.Tag.Location.Location.Message));
                }
            }
        }

        private static bool IsValidPrimaryLocation(IAnalysisIssueLocationVisualization locViz) =>
            locViz is Models.IAnalysisIssueLocationVisualization && locViz.Span.HasValue;

        public void Dispose()
        {
            tagAggregator.BatchedTagsChanged -= OnAggregatorTagsChanged;
        }

        #endregion
    }
}
