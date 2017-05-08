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
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Sonarlint;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    ///<summary>
    /// Tracks Sonar errors for a specific buffer.
    ///</summary>
    /// <remarks><para>The lifespan of this object is tied to the lifespan of the taggers on the view. On creation of the first tagger,
    /// it starts tracking errors. On the disposal of the last tagger, it shuts down.</para>
    /// </remarks>
    internal class IssueTracker : ITagger<IErrorTag>, IDisposable
    {
        private readonly TaggerProvider provider;
        private readonly ITextBuffer textBuffer;

        private ITextSnapshot currentSnapshot;
        private NormalizedSnapshotSpanCollection dirtySpans;

        internal string FilePath { get; private set; }
        internal string Charset { get; }
        internal SnapshotFactory Factory { get; }

        internal IssuesSnapshot Snapshot { get; set; }

        internal IssueTracker(TaggerProvider provider, ITextBuffer buffer, ITextDocument document)
        {
            this.provider = provider;
            this.textBuffer = buffer;
            this.currentSnapshot = buffer.CurrentSnapshot;
            this.dirtySpans = new NormalizedSnapshotSpanCollection();

            this.FilePath = document.FilePath;
            this.Charset = document.Encoding.WebName;
            this.Factory = new SnapshotFactory(new IssuesSnapshot(this.FilePath, 0, new List<IssueMarker>()));

            document.FileActionOccurred += FileActionOccurred;

            this.Initialize();
        }

        private void FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.DocumentRenamed)
            {
                provider.Rename(FilePath, e.FilePath);
                FilePath = e.FilePath;
            }
            else if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                provider.RequestAnalysis(FilePath, Charset);
            }
        }

        private void Initialize()
        {
            textBuffer.ChangedLowPriority += this.OnBufferChange;
            provider.AddIssueTracker(this);
            provider.RequestAnalysis(FilePath, Charset);
        }

        public void Dispose()
        {
            textBuffer.ChangedLowPriority -= this.OnBufferChange;
            provider.RemoveIssueTracker(this);
        }

        private void OnBufferChange(object sender, TextContentChangedEventArgs e)
        {
            UpdateDirtySpans(e);

            var newMarkers = TranslateMarkerSpans();

            SnapToNewSnapshot(newMarkers);
        }

        private void UpdateDirtySpans(TextContentChangedEventArgs e)
        {
            currentSnapshot = e.After;

            var newDirtySpans = dirtySpans.CloneAndTrackTo(e.After, SpanTrackingMode.EdgeInclusive);

            foreach (var change in e.Changes)
            {
                newDirtySpans = NormalizedSnapshotSpanCollection.Union(newDirtySpans, new NormalizedSnapshotSpanCollection(e.After, change.NewSpan));
            }

            dirtySpans = newDirtySpans;
        }

        private IssuesSnapshot TranslateMarkerSpans()
        {
            var oldSnapshot = this.Factory.CurrentSnapshot;
            var newMarkers = oldSnapshot.IssueMarkers
                .Select(marker => marker.CloneAndTranslateTo(currentSnapshot))
                .Where(clone => clone != null);

            return new IssuesSnapshot(this.FilePath, oldSnapshot.VersionNumber + 1, newMarkers);
        }

        internal void UpdateIssues(IEnumerable<Issue> issues)
        {
            var oldSnapshot = this.Factory.CurrentSnapshot;
            var newMarkers = issues.Select(CreateIssueMarker);
            var newSnapshot = new IssuesSnapshot(this.FilePath, oldSnapshot.VersionNumber + 1, newMarkers);
            SnapToNewSnapshot(newSnapshot);
        }

        private IssueMarker CreateIssueMarker(Issue issue)
        {
            int startPos = currentSnapshot.GetLineFromLineNumber(issue.StartLine - 1).Start.Position + issue.StartLineOffset;
            var start = new SnapshotPoint(currentSnapshot, startPos);

            int endPos = currentSnapshot.GetLineFromLineNumber(issue.EndLine - 1).Start.Position + issue.EndLineOffset;
            var end = new SnapshotPoint(currentSnapshot, endPos);

            return new IssueMarker(issue, new SnapshotSpan(start, end));
        }

        private void SnapToNewSnapshot(IssuesSnapshot snapshot)
        {
            this.Factory.UpdateMarkers(snapshot);

            provider.UpdateAllSinks();

            UpdateMarkers(currentSnapshot, snapshot);

            this.Snapshot = snapshot;
        }

        internal void UpdateMarkers(ITextSnapshot currentSnapshot, IssuesSnapshot snapshot)
        {
            var oldSnapshot = this.Snapshot;

            var handler = this.TagsChanged;
            if (handler == null)
            {
                return;
            }

            // Raise a single tags changed event over the entire affected span.
            int start = int.MaxValue;
            int end = int.MinValue;

            if (oldSnapshot != null && oldSnapshot.Count > 0)
            {
                start = oldSnapshot.IssueMarkers.First().Span.Start.TranslateTo(currentSnapshot, PointTrackingMode.Negative);
                end = oldSnapshot.IssueMarkers.Last().Span.End.TranslateTo(currentSnapshot, PointTrackingMode.Positive);
            }

            if (snapshot.Count > 0)
            {
                start = Math.Min(start, snapshot.IssueMarkers.First().Span.Start.Position);
                end = Math.Max(end, snapshot.IssueMarkers.Last().Span.End.Position);
            }

            if (start < end)
            {
                handler(this, new SnapshotSpanEventArgs(new SnapshotSpan(currentSnapshot, Span.FromBounds(start, end))));
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (Snapshot == null)
            {
                return Enumerable.Empty<ITagSpan<IErrorTag>>();
            }

            return Snapshot.IssueMarkers
                .Select(issue => issue.Span)
                .Where(spans.IntersectsWith)
                .Select(span => new TagSpan<IErrorTag>(span, new ErrorTag(PredefinedErrorTypeNames.Warning)));
        }
    }
}
