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
    // TODO: track document renames: https://github.com/SonarSource/sonarlint-visualstudio/issues/1662

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

            FilePath = GetFileName(buffer);
            UpdateTags();

            locationService.IssuesChanged += OnIssuesChanged;
            buffer.ChangedLowPriority += SafeOnBufferChange;
        }

        private void OnIssuesChanged(object sender, IssuesChangedEventArgs e)
        {
            if (!e.AnalyzedFiles.Contains(FilePath, StringComparer.OrdinalIgnoreCase))
            {
                return; // no changes in this file
            }

            UpdateTags();
        }

        private void UpdateTags()
        {
            var textSnapshot = buffer.CurrentSnapshot;

            // Get the new locations and calculate the spans if necessary
            var locations = locationService.GetLocations(FilePath);
            EnsureSpansExist(locations, textSnapshot);

            TagSpans = locations.Select(CreateTagSpan).ToList();

            NotifyTagsChanged(textSnapshot);
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

        private string GetFileName(ITextBuffer textBuffer)
        {
            ITextDocument newTextDocument = null;
            textBuffer.Properties?.TryGetProperty(typeof(ITextDocument), out newTextDocument);
            return newTextDocument?.FilePath;
        }

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
                old.Tag.Location.Span = newSpan; // update the span stored in the location visualization 

                translatedTagSpans.Add(new TagSpan<IIssueLocationTag>(newSpan, old.Tag));
            }

            TagSpans = translatedTagSpans;
        }

        private static bool OverlapsExists(Span span, NormalizedSnapshotSpanCollection spans) =>
            spans.Any(x => x.OverlapsWith(span));

        #endregion ITagger implementation

        #region Buffer change tracking

        internal /* for testing */ string FilePath { get; }

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
                locationService.Refresh(new string[] { FilePath });

                NotifyTagsChanged(newTextSnapshot);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.ERR_HandlingBufferChange, ex.Message);
            }
        }

        private void NotifyTagsChanged(ITextSnapshot newTextSnapshot)
        {
            // Notify the editor that our set of tags has changed
            var wholeSpan = new SnapshotSpan(newTextSnapshot, 0, newTextSnapshot.Length);
            TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(wholeSpan));
        }

        #endregion

        public void Dispose()
        {
            locationService.IssuesChanged -= OnIssuesChanged;
            buffer.ChangedLowPriority -= SafeOnBufferChange;
        }
    }
}
