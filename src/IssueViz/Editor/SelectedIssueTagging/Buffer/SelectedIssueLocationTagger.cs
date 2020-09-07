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
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Editor.SelectedIssueTagging.Buffer
{
    //internal class SelectedIssueLocationTagger : ITagger<ISelectedIssueLocationTag>, IDisposable
    //{
    //    private readonly ITextBuffer textBuffer;
    //    private readonly IAnalysisIssueSelectionService issueSelectionService;
    //    private readonly ITagAggregator<IIssueLocationTag> tagAggregator;

    //    private bool disposedValue;

    //    public SelectedIssueLocationTagger(ITextBuffer textBuffer, IAnalysisIssueSelectionService issueSelectionService, ITagAggregator<IIssueLocationTag> tagAggregator)
    //    {
    //        this.textBuffer = textBuffer;
    //        this.issueSelectionService = issueSelectionService;
    //        this.tagAggregator = tagAggregator;

    //        // Changing any of the selected issue/flow/location will always result in the
    //        // "SelectedLocationChanged" event being raised
    //        issueSelectionService.SelectionChanged += OnSelectionChanged;
    //        tagAggregator.BatchedTagsChanged += OnAggregatorTagsChanged;
    //    }

    //    private void OnAggregatorTagsChanged(object sender, BatchedTagsChangedEventArgs e)
    //    {
    //        // We need to propagate the change notification, otherwise our GetTags method won't be called.
    //        UpdateWholeSpan(textBuffer.CurrentSnapshot);
    //    }

    //    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    //    {
    //        if (e.SelectionChangeLevel == SelectionChangeLevel.Flow || e.SelectionChangeLevel == SelectionChangeLevel.Issue)
    //        {
    //            UpdateWholeSpan(textBuffer.CurrentSnapshot);
    //        }
    //    }

    //    private void UpdateWholeSpan(ITextSnapshot textSnapshot)
    //    {
    //        // Note: we're currently reporting the whole snapshot as having changed. We could be more specific
    //        // if our buffer raised a more specific notification.
    //        var wholeSpan = new SnapshotSpan(textSnapshot, 0, textSnapshot.Length);
    //        TagsChanged?.Invoke(this, new SnapshotSpanEventArgs(wholeSpan));
    //    }

    //    #region ITagger methods

    //    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    //    public IEnumerable<ITagSpan<ISelectedIssueLocationTag>> GetTagsOLD(NormalizedSnapshotSpanCollection spans)
    //    {
    //        if (spans.Count == 0) { yield break; }

    //        var selectedFlowLocations = issueSelectionService.SelectedFlow?.Locations;
    //        if (selectedFlowLocations == null || selectedFlowLocations.Count == 0) { yield break; }

    //        var issueLocationTags = tagAggregator.GetTags(spans);
            
    //        if (!issueLocationTags.Any()) { yield break; }

    //        // The tags can only be for this file.
    //        // Filter to any selected locations.
    //        // There could be multiple secondary issues locations in this file for different issues.

    //        // ******* Relying on object reference comparison *******

    //        var secondaryLocations = issueLocationTags.Select(x => x.Tag.Location).Where(loc => !(loc is IAnalysisIssueVisualization)).ToArray();

    //        var matchingTagLocations = issueLocationTags.Where(x => selectedFlowLocations.Contains(x.Tag.Location, LocVizComparer))
    //            .Select(x => x.Tag.Location).ToArray();
    //        if (matchingTagLocations.Length == 0) { yield break; }

    //        ITextSnapshot snapshot = spans[0].Snapshot;
    //        foreach (var dataTagSpan in issueLocationTags)
    //        {
    //            var spansCollection = dataTagSpan.Span.GetSpans(snapshot);

    //            // Ignore data tags that are split by project.
    //            // This is theoretically possible but unlikely in the current scenarios.
    //            if (spansCollection.Count != 1) { continue; }

    //            if (spans.IntersectsWith(spansCollection[0]) && matchingTagLocations.Contains(dataTagSpan.Tag.Location))
    //            {
    //                yield return new TagSpan<ISelectedIssueLocationTag>(spansCollection[0], new SelectedIssueLocationTag(dataTagSpan.Tag.Location));
    //            }
    //        }
    //    }

    //    public IEnumerable<ITagSpan<ISelectedIssueLocationTag>> GetTags(NormalizedSnapshotSpanCollection spans)
    //    {
    //        if (spans.Count == 0) { yield break; }

    //        var selectedSecondaryLocVizs = issueSelectionService.SelectedFlow?.Locations;
    //        if (selectedSecondaryLocVizs == null || selectedSecondaryLocVizs.Count == 0) { yield break; }

    //        var issueLocationTags = tagAggregator.GetTags(spans).Select(x => x.Tag);
    //        if (!issueLocationTags.Any()) { yield break; }

    //        // Filter to any selected locations.
    //        // There could be multiple secondary issues locations in this file for different issues.
    //        var matchingTags = issueLocationTags
    //            .Where(tag => selectedSecondaryLocVizs.Contains(tag.Location, LocVizComparer)) // tag.IssueLocation.Span.HasValue && 
    //            .ToArray();
    //        if (matchingTags.Length == 0) { yield break; }


    //        var callerSnapshot = spans[0].Snapshot;

    //        // Check our assumption that our buffer tagger will have been called by the tag aggregator
    //        // so our spans have been translated to the current snapshot. If this isn't the case then
    //        // we'll need to re-translate them here.
    //        //Debug.Assert(matchingTags.All(x => x.IssueLocation.Span.Value.Snapshot == callerSnapshot),
    //        //    "Expecting the spans to have been mapped to the correct snapshot already");

    //        foreach (var matchingTag in matchingTags)
    //        {
    //            if (spans.IntersectsWith(matchingTag.Location.Span.Value))
    //            {
    //                yield return new TagSpan<ISelectedIssueLocationTag>(matchingTag.Location.Span.Value, new SelectedIssueLocationTag(matchingTag.Location));
    //            }
    //        }
    //    }

    //    #endregion

    //    #region IDisposable implementation

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!disposedValue)
    //        {
    //            if (disposing)
    //            {
    //                issueSelectionService.SelectionChanged -= OnSelectionChanged;
    //            }
    //            disposedValue = true;
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //        Dispose(disposing: true);
    //        GC.SuppressFinalize(this);
    //    }

    //    #endregion

    //    private static readonly IssueLocVizComparer LocVizComparer = new IssueLocVizComparer();

    //    private class IssueLocVizComparer : IEqualityComparer<IAnalysisIssueLocationVisualization>
    //    {
    //        public bool Equals(IAnalysisIssueLocationVisualization x, IAnalysisIssueLocationVisualization y)
    //        {
    //            if ((x == null && y != null)|| (x != null && y == null)) { return false; }
    //            if (x.GetType() != y.GetType()) { return false; }

    //            var isMatch = x.Location == y.Location;
    //            isMatch = ReferenceEquals(x.Location, y.Location);
    //            return isMatch;
    //        }

    //        public int GetHashCode(IAnalysisIssueLocationVisualization obj)
    //        {
    //            return obj.Location.LineHash.GetHashCode();
    //        }
    //    }
    //}



    internal class SelectedIssueLocationTagger: FilteringTaggerBase<IIssueLocationTag, ISelectedIssueLocationTag>
    {
        private readonly IAnalysisIssueSelectionService issueSelectionService;

        private bool disposedValue;

        public SelectedIssueLocationTagger(ITagAggregator<IIssueLocationTag> tagAggregator, ITextBuffer textBuffer, IAnalysisIssueSelectionService issueSelectionService)
            : base(tagAggregator, textBuffer)
        {
            this.issueSelectionService = issueSelectionService;

            // Changing any of the selected issue/flow/location will always result in the
            // "SelectedLocationChanged" event being raised
            issueSelectionService.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.SelectionChangeLevel == SelectionChangeLevel.Flow || e.SelectionChangeLevel == SelectionChangeLevel.Issue)
            {
                UpdateWholeSpan();
            }
        }

        #region ITagger methods

        protected override TagSpan<ISelectedIssueLocationTag> CreateTagSpan(IIssueLocationTag trackedTag, NormalizedSnapshotSpanCollection spans)
        {
            return new TagSpan<ISelectedIssueLocationTag>(trackedTag.Location.Span.Value, new SelectedIssueLocationTag(trackedTag.Location));
        }

        protected override IEnumerable<IMappingTagSpan<IIssueLocationTag>> Filter(IEnumerable<IMappingTagSpan<IIssueLocationTag>> trackedTagSpans)
        {
            var selectedSecondaryLocVizs = issueSelectionService.SelectedFlow?.Locations;
            if (selectedSecondaryLocVizs == null || selectedSecondaryLocVizs.Count == 0) { return Array.Empty<IMappingTagSpan<IIssueLocationTag>>(); }

            // Filter to any selected locations.
            // There could be multiple secondary issues locations in this file for different issues.
            return trackedTagSpans
                .Where(tagSpan => selectedSecondaryLocVizs.Contains(tagSpan.Tag.Location, LocVizComparer));
        }

        #endregion

        private static readonly IssueLocVizComparer LocVizComparer = new IssueLocVizComparer();

        private class IssueLocVizComparer : IEqualityComparer<IAnalysisIssueLocationVisualization>
        {
            public bool Equals(IAnalysisIssueLocationVisualization x, IAnalysisIssueLocationVisualization y)
            {
                if ((x == null && y != null) || (x != null && y == null)) { return false; }
                if (x.GetType() != y.GetType()) { return false; }

                var isMatch = x.Location == y.Location;
                isMatch = ReferenceEquals(x.Location, y.Location);
                return isMatch;
            }

            public int GetHashCode(IAnalysisIssueLocationVisualization obj)
            {
                return obj.Location.LineHash.GetHashCode();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && !disposedValue)
            {
                issueSelectionService.SelectionChanged -= OnSelectionChanged;
                disposedValue = true;
            }
        }
    }
}
