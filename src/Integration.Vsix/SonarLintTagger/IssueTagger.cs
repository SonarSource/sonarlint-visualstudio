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

using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    ///<summary>
    /// Tracks SonarLint errors for a specific buffer.
    ///</summary>
    /// <remarks><para>The lifespan of this object is tied to the lifespan of the taggers on the view. On creation of the first tagger,
    /// it starts tracking errors. On the disposal of the last tagger, it shuts down.</para>
    /// </remarks>
    internal sealed class IssueTagger : ITagger<IErrorTag>, IDisposable
    {
        private readonly DTE dte;
        private readonly TaggerProvider provider;
        private readonly ITextBuffer textBuffer;
        private readonly IEnumerable<SonarLanguage> detectedLanguages;

        internal ProjectItem ProjectItem { get; private set; }
        private ITextSnapshot currentSnapshot;
        private NormalizedSnapshotSpanCollection dirtySpans;

        private readonly ITextDocument document;
        private readonly IIssueConverter issueConverter;

        internal string FilePath { get; private set; }
        internal string Charset { get; }
        internal SnapshotFactory Factory { get; }

        internal IssuesSnapshot Snapshot { get; set; }

        internal IssueTagger(DTE dte, TaggerProvider provider, ITextBuffer buffer, ITextDocument document,
            IEnumerable<SonarLanguage> detectedLanguages)
            : this(dte, provider, buffer, document, detectedLanguages, new IssueConverter()) { }

        internal IssueTagger(DTE dte, TaggerProvider provider, ITextBuffer buffer, ITextDocument document,
            IEnumerable<SonarLanguage> detectedLanguages, IIssueConverter issueConverter)
        {
            this.dte = dte;
            this.provider = provider;
            this.textBuffer = buffer;
            this.detectedLanguages = detectedLanguages;
            this.issueConverter = issueConverter;
            this.currentSnapshot = buffer.CurrentSnapshot;
            this.dirtySpans = new NormalizedSnapshotSpanCollection();

            this.document = document;
            this.FilePath = document.FilePath;
            this.ProjectItem = dte.Solution.FindProjectItem(this.FilePath);
            this.Charset = document.Encoding.WebName;
            this.Factory = new SnapshotFactory(new IssuesSnapshot(this.ProjectItem.ContainingProject.Name, this.FilePath, 0,
                new List<IssueMarker>()));

            this.Initialize();

        }

        private void FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.DocumentRenamed)
            {
                provider.Rename(FilePath, e.FilePath);
                FilePath = e.FilePath;
                ProjectItem = dte.Solution.FindProjectItem(this.FilePath);
            }
            else if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                RequestAnalysis();
            }
        }

        private void Initialize()
        {
            document.FileActionOccurred += FileActionOccurred;
            textBuffer.ChangedLowPriority += OnBufferChange;
            provider.AddIssueTagger(this);
            RequestAnalysis();
        }

        internal void DaemonStarted(object sender, EventArgs e)
        {
            RequestAnalysis();
        }

        private void RequestAnalysis()
        {
            provider.RequestAnalysis(FilePath, Charset, detectedLanguages);
        }

        public void Dispose()
        {
            document.FileActionOccurred -= FileActionOccurred;
            textBuffer.ChangedLowPriority -= OnBufferChange;
            provider.RemoveIssueTagger(this);
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

            return new IssuesSnapshot(this.ProjectItem.ContainingProject.Name, this.FilePath, oldSnapshot.VersionNumber + 1, newMarkers);
        }

        internal void UpdateIssues(IEnumerable<Issue> issues)
        {
            var oldSnapshot = this.Factory.CurrentSnapshot;
            var newMarkers = issues.Where(IsValidIssueTextRange).Select(CreateIssueMarker);
            var newSnapshot = new IssuesSnapshot(this.ProjectItem.ContainingProject.Name, this.FilePath, oldSnapshot.VersionNumber + 1, newMarkers);
            SnapToNewSnapshot(newSnapshot);
        }

        private bool IsValidIssueTextRange(Issue issue) =>
            1 <= issue.StartLine && issue.EndLine <= currentSnapshot.LineCount;

        private IssueMarker CreateIssueMarker(Issue issue) =>
            issueConverter.ToMarker(issue, currentSnapshot);

        private void SnapToNewSnapshot(IssuesSnapshot snapshot)
        {
            this.Factory.UpdateMarkers(snapshot);

            provider.UpdateAllSinks();

            UpdateMarkers(currentSnapshot, snapshot);

            this.Snapshot = snapshot;
        }

        private void UpdateMarkers(ITextSnapshot currentSnapshot, IssuesSnapshot snapshot)
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
                start = oldSnapshot.IssueMarkers.Select(i => i.Span.Start.TranslateTo(currentSnapshot, PointTrackingMode.Negative)).Min();
                end = oldSnapshot.IssueMarkers.Select(i => i.Span.End.TranslateTo(currentSnapshot, PointTrackingMode.Positive)).Max();
            }

            if (snapshot.Count > 0)
            {
                start = Math.Min(start, snapshot.IssueMarkers.Select(i => i.Span.Start.Position).Min());
                end = Math.Max(end, snapshot.IssueMarkers.Select(i => i.Span.End.Position).Max());
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
