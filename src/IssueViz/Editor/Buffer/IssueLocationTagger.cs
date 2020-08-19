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
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.BufferTagger
{
    internal partial class IssueLocationTagger : ITagger<IssueLocationTag>, IDisposable
    {
        private readonly ITextBuffer textBuffer;
        private readonly IAnalysisIssueSelectionService issueSelectionService;
        private readonly IBufferTagCalculator bufferTagCalculator;

        private IList<ITagSpan<IssueLocationTag>> issueTagSpans;

        private bool disposedValue;

        public IssueLocationTagger(ITextBuffer textBuffer, IAnalysisIssueSelectionService issueSelectionService)
            : this(textBuffer, issueSelectionService, new BufferTagCalculator())
        {
        }

        internal /* for testing */ IssueLocationTagger(ITextBuffer textBuffer, IAnalysisIssueSelectionService issueSelectionService,
            IBufferTagCalculator bufferTagCalculator)
        {
            this.textBuffer = textBuffer;
            this.issueSelectionService = issueSelectionService;
            this.bufferTagCalculator = bufferTagCalculator;

            issueTagSpans = new List<ITagSpan<IssueLocationTag>>();

            UpdateTagSpans(issueSelectionService.SelectedFlow, textBuffer.CurrentSnapshot);

            // Changing any of the selected issue/flow/location will always result in the
            // "SelectedLocationChanged" event being raised
            issueSelectionService.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.SelectionChangeLevel == SelectionChangeLevel.Flow || e.SelectionChangeLevel == SelectionChangeLevel.Issue)
            {
                UpdateTagSpans(e.SelectedFlow, textBuffer.CurrentSnapshot);
            }
        }

        #region ITagger methods

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IssueLocationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || (issueTagSpans?.Count ?? 0) == 0) { yield break; }

            // If the requested snapshot isn't the same as the one our words are on, translate our spans to the expected snapshot 
            if (spans[0].Snapshot != issueTagSpans[0].Span.Snapshot)
            {
                TranslateTagSpans(spans[0].Snapshot);
            }

            // Find any tags in that overlap with that range
            foreach(var tagSpan in issueTagSpans)
            {
                if (OverlapsExists(tagSpan.Span, spans))
                {
                    yield return tagSpan;
                }
            }
        }

        private void TranslateTagSpans(ITextSnapshot newSnapshot)
        {
            var translatedTagSpans = new List<ITagSpan<IssueLocationTag>>();

            foreach(var old in issueTagSpans)
            {
                var newSpan = old.Span.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive);
                translatedTagSpans.Add(new TagSpan<IssueLocationTag>(newSpan, old.Tag));
            }

            issueTagSpans = translatedTagSpans;
        }

        private static bool OverlapsExists(Span span, NormalizedSnapshotSpanCollection spans) =>
            spans.Any(x => x.OverlapsWith(span));

        #endregion

        private void UpdateTagSpans(IAnalysisIssueFlowVisualization flowViz, ITextSnapshot textSnapshot)
        {
            issueTagSpans = bufferTagCalculator.GetTagSpans(flowViz, textSnapshot);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(new SnapshotSpan(textBuffer.CurrentSnapshot, 0, textBuffer.CurrentSnapshot.Length)));
        }

        #region IDisposable implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    issueSelectionService.SelectionChanged -= OnSelectionChanged;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
