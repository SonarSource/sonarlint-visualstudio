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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging
{
    // Buffer tagger for Sonar issue locations
    // * adds location tags for primary and secondary locations for a file
    // * tracks buffer edits, re-calculate spans, and notifies the issues location store about the changes
    internal sealed class LocationTagger : ITagger<IIssueLocationTag>, IDisposable
    {
        private readonly ITextBuffer buffer;
        private readonly IIssueLocationStore locationService;
        private readonly IIssueSpanCalculator spanCalculator;
        private readonly ILogger logger;

        internal /* for testing */ IList<ITagSpan<IIssueLocationTag>> TagSpans { get; private set; }

        public LocationTagger(ITextBuffer buffer, IIssueLocationStore locationService, IIssueSpanCalculator spanCalculator, ILogger logger)
        {
            this.buffer = buffer;
            this.locationService = locationService;
            this.spanCalculator = spanCalculator;
            this.logger = logger;

            UpdateTags();

            locationService.IssuesChanged += OnIssuesChanged;
            buffer.ChangedLowPriority += SafeOnBufferChange;
        }

        /// <summary>
        /// We're not tracking file renames so we have to fetch the current
        /// file name each time we need it.
        /// </summary>
        internal /* for testing */ string GetCurrentFilePath() => buffer.GetFilePath();

        private void OnIssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            if (!e.AnalyzedFiles.Contains(GetCurrentFilePath(), StringComparer.OrdinalIgnoreCase))
            {
                return; // no changes in this file
            }

            UpdateTags();
        }

        private void UpdateTags()
        {
            var textSnapshot = buffer.CurrentSnapshot;

            // Get the new locations and calculate the spans if necessary
            var locations = locationService.GetLocations(GetCurrentFilePath());
            EnsureSpansExist(locations, textSnapshot);

            var oldTags = TagSpans;
            TagSpans = locations.Select(CreateTagSpan).ToList();

            var affectedSpan = CalculateSpanOfAllTags(oldTags, textSnapshot);
            NotifyTagsChanged(affectedSpan);
        }

        private void EnsureSpansExist(IEnumerable<IAnalysisIssueLocationVisualization> locVizs, ITextSnapshot currentSnapshot)
        {
            // All spans for issues in the this file should have been calculated when the file was analysed (whether primary or secondary).
            // However, there could be secondary locations relating to issues in other files that have not been calculated.
            foreach (var locViz in locVizs)
            {
                if (!locViz.Span.HasValue)
                {
                    locViz.Span = spanCalculator.CalculateSpan(locViz.Location, currentSnapshot);
                }
            }
        }

        private static ITagSpan<IIssueLocationTag> CreateTagSpan(IAnalysisIssueLocationVisualization locViz) =>
            new TagSpan<IIssueLocationTag>(locViz.Span.Value, new IssueLocationTag(locViz));

        #region ITagger implementation

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IIssueLocationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans.Count == 0 || TagSpans.Count == 0) { yield break; }

            // If the requested snapshot isn't the same as our tags are on, translate our spans to the expected snapshot 
            if (spans[0].Snapshot != TagSpans[0].Span.Snapshot)
            {
                TranslateTagSpans(spans[0].Snapshot);
            }

            // Find any tags in that overlap with that range
            foreach (var tagSpan in TagSpans)
            {
                if (OverlapsExists(tagSpan.Span, spans))
                {
                    yield return tagSpan;
                }
            }
        }

        private void TranslateTagSpans(ITextSnapshot newSnapshot)
        {
            var translatedTagSpans = new List<ITagSpan<IIssueLocationTag>>();

            foreach (var old in TagSpans)
            {
                var newSpan = old.Span.TranslateTo(newSnapshot, SpanTrackingMode.EdgeExclusive);

                if (newSpan.Length != old.Span.Length)
                {
                    old.Tag.Location.InvalidateSpan();
                }
                else
                {
                    old.Tag.Location.Span = newSpan;
                    translatedTagSpans.Add(new TagSpan<IIssueLocationTag>(newSpan, old.Tag));
                }
            }

            TagSpans = translatedTagSpans;
        }

        private static bool OverlapsExists(Span span, NormalizedSnapshotSpanCollection spans) =>
            spans.Any(x => x.OverlapsWith(span));

        #endregion ITagger implementation

        #region Buffer change tracking

        private void SafeOnBufferChange(object sender, TextContentChangedEventArgs e)
        {
            // Handles callback from VS. Suppress non-critical errors to prevent them
            // propagating to VS, which would display a dialogue and disable the extension.
            try
            {
                // The text buffer has been edited (i.e.text added, deleted or modified).
                // The spans we have stored for location relate to the previous text buffer and
                // are no longer valid, so we need to translate them to the equivalent spans
                // in the new text buffer.
                var newTextSnapshot = e.After;

                TranslateTagSpans(newTextSnapshot);
                locationService.Refresh(new[] { GetCurrentFilePath() });

                var affectedSpan = CalculateSpanOfChanges(e.Changes, newTextSnapshot);
                NotifyTagsChanged(affectedSpan);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_HandlingBufferChange, ex.Message);
            }
        }

        private void NotifyTagsChanged(SnapshotSpan affectedSpan)
        {
            // Notify the editor that our set of tags has changed
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(affectedSpan));
        }

        /// <summary>
        /// Method calculates the span from the start of (old+new) TagSpans and until the end of (old+new) TagSpans
        /// </summary>
        private SnapshotSpan CalculateSpanOfAllTags(IList<ITagSpan<IIssueLocationTag>> oldTags, ITextSnapshot textSnapshot)
        {
            var allTagSpans = TagSpans
                .Union(oldTags ?? Array.Empty<ITagSpan<IIssueLocationTag>>())
                .Select(x => x.Span.Span)
                .ToArray();

            return CalculateAffectedSpan(textSnapshot, allTagSpans, allTagSpans);
        }

        /// <summary>
        /// Method calculates the span from the start of the editor changes and until the end of TagSpans
        /// </summary>
        private SnapshotSpan CalculateSpanOfChanges(INormalizedTextChangeCollection changeCollection, ITextSnapshot newTextSnapshot)
        {
            var startSpans = changeCollection.Select(x => x.NewSpan).ToArray();
            var endSpans = TagSpans.Select(x => x.Span.Span);
            
            return CalculateAffectedSpan(newTextSnapshot, startSpans, endSpans);
        }

        private static SnapshotSpan CalculateAffectedSpan(ITextSnapshot textSnapshot, IEnumerable<Span> startSpans, IEnumerable<Span> endSpans)
        {
            var start = startSpans.Select(x => x.Start).DefaultIfEmpty().Min();
            var end = endSpans.Select(x => x.End).DefaultIfEmpty(textSnapshot.Length).Max();

            return start < end
                ? new SnapshotSpan(textSnapshot, Span.FromBounds(start, end))
                : new SnapshotSpan(textSnapshot, Span.FromBounds(start, textSnapshot.Length));
        }

        #endregion

        public void Dispose()
        {
            locationService.IssuesChanged -= OnIssuesChanged;
            buffer.ChangedLowPriority -= SafeOnBufferChange;
        }
    }
}
