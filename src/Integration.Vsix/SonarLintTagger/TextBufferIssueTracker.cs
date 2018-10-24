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
using System.Diagnostics;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    ///<summary>
    /// Tracks SonarLint errors for a specific buffer.
    ///</summary>
    /// <remarks>
    /// <para>The lifespan of this object is tied to the lifespan of the taggers on the view. On creation of the first tagger,
    /// it starts tracking errors. On the disposal of the last tagger, it shuts down.</para>
    /// <para>
    /// See the README.md in this folder for more information
    /// </para>
    /// </remarks>
    internal sealed class TextBufferIssueTracker : IIssueConsumer
    {
        private readonly DTE dte;
        internal /* for testing */ TaggerProvider Provider { get; }
        private readonly ITextBuffer textBuffer;
        private readonly IEnumerable<SonarLanguage> detectedLanguages;

        internal ProjectItem ProjectItem { get; private set; }
        private ITextSnapshot currentSnapshot;
        private NormalizedSnapshotSpanCollection dirtySpans;

        private readonly ITextDocument document;
        private readonly IIssueConverter issueConverter;
        private readonly string charset;

        public string FilePath { get; private set; }
        public SnapshotFactory Factory { get; }

        public IssuesSnapshot LastIssues { get; private set; }

        private readonly ISet<IssueTagger> activeTaggers = new HashSet<IssueTagger>();

        public TextBufferIssueTracker(DTE dte, TaggerProvider provider, ITextDocument document,
            IEnumerable<SonarLanguage> detectedLanguages)
            : this(dte, provider, document, detectedLanguages, new IssueConverter()) { }

        internal TextBufferIssueTracker(DTE dte, TaggerProvider provider, ITextDocument document,
            IEnumerable<SonarLanguage> detectedLanguages, IIssueConverter issueConverter)
        {
            this.dte = dte;

            this.Provider = provider;
            this.textBuffer = document.TextBuffer;
            this.currentSnapshot = document.TextBuffer.CurrentSnapshot;

            this.detectedLanguages = detectedLanguages;
            this.issueConverter = issueConverter;
            
            this.document = document;
            this.FilePath = document.FilePath;

            this.ProjectItem = dte.Solution.FindProjectItem(this.FilePath);
            this.charset = document.Encoding.WebName;

            // Bug #676: https://github.com/SonarSource/sonarlint-visualstudio/issues/676
            // It's possible to have a ProjectItem that doesn't have a ContainingProject
            // e.g. files under the "External Dependencies" project folder in the Solution Explorer
            var projectName = this.ProjectItem?.ContainingProject.Name ?? "{none}";
            this.Factory = new SnapshotFactory(new IssuesSnapshot(projectName, this.FilePath, 0,
                new List<IssueMarker>()));
        }

        public void AddTagger(IssueTagger tagger)
        {
            Debug.Assert(!activeTaggers.Contains(tagger), "Not expecting the tagger to be already registered");
            activeTaggers.Add(tagger);

            if (activeTaggers.Count == 1)
            {
                // First tagger created... start doing stuff
                document.FileActionOccurred += FileActionOccurred;

                textBuffer.ChangedLowPriority += OnBufferChange;

                this.dirtySpans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(currentSnapshot, 0, currentSnapshot.Length));

                Provider.AddIssueTracker(this);

                RequestAnalysis();
            }
        }

        public void RemoveTagger(IssueTagger tagger)
        {
            Debug.Assert(activeTaggers.Contains(tagger), "Not expecting RemoveTagger to be called for an unregistered tagger");
            activeTaggers.Remove(tagger);

            if (activeTaggers.Count == 0)
            {
                // Last tagger was disposed of. This is means there are no longer any open views on the buffer so we can safely shut down
                // issue tracking for that buffer.
                document.FileActionOccurred -= FileActionOccurred;
                textBuffer.ChangedLowPriority -= OnBufferChange;
                textBuffer.Properties.RemoveProperty(typeof(TextBufferIssueTracker));
                Provider.RemoveIssueTracker(this);
            }
        }

        private void FileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.DocumentRenamed)
            {
                FilePath = e.FilePath;
                ProjectItem = dte.Solution.FindProjectItem(this.FilePath);
            }
            else if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                RequestAnalysis();
            }
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

        private bool IsValidIssueTextRange(Issue issue) =>
            1 <= issue.StartLine && issue.EndLine <= currentSnapshot.LineCount;

        private IssueMarker CreateIssueMarker(Issue issue) =>
            issueConverter.ToMarker(issue, currentSnapshot);

        private void SnapToNewSnapshot(IssuesSnapshot newIssues)
        {
            // Tell our factory to snap to a new snapshot.
            this.Factory.UpdateSnapshot(newIssues);

            // Tell the provider to mark all the sinks dirty (so, as a side-effect, they will start an update pass that will get the new snapshot
            // from the factory).
            Provider.UpdateAllSinks();

            // Work out which part of the document has been affected by the changes, and tell
            // the taggers about the changes
            SnapshotSpan? affectedSpan = CalculateAffectedSpan(LastIssues, newIssues);
            foreach (var tagger in activeTaggers)
            {
                tagger.UpdateMarkers(newIssues, affectedSpan);
            }

            this.LastIssues = newIssues;
        }

        private SnapshotSpan? CalculateAffectedSpan(IssuesSnapshot oldIssues, IssuesSnapshot newIssues)
        {
            // Calculate the whole span affected by the all of the issues, old and new
            int start = int.MaxValue;
            int end = int.MinValue;

            if (oldIssues != null && oldIssues.Count > 0)
            {
                start = oldIssues.IssueMarkers.Select(i => i.Span.Start.TranslateTo(currentSnapshot, PointTrackingMode.Negative)).Min();
                end = oldIssues.IssueMarkers.Select(i => i.Span.End.TranslateTo(currentSnapshot, PointTrackingMode.Positive)).Max();
            }

            if (newIssues.Count > 0)
            {
                start = Math.Min(start, newIssues.IssueMarkers.Select(i => i.Span.Start.Position).Min());
                end = Math.Max(end, newIssues.IssueMarkers.Select(i => i.Span.End.Position).Max());
            }

            if (start < end)
            {
                return new SnapshotSpan(currentSnapshot, Span.FromBounds(start, end));
            }

            return null;
        }

        #region Daemon interaction

        public void DaemonStarted(object sender, EventArgs e)
        {
            RequestAnalysis();
        }

        private void RequestAnalysis()
        {
            Provider.RequestAnalysis(FilePath, charset, detectedLanguages, this, ProjectItem);
        }

        void IIssueConsumer.Accept(string path, IEnumerable<Issue> issues)
        {
            // Callback from the daemon when new results are available
            if (path != FilePath)
            {
                Debug.Fail("Issues returned for an unexpected file path");
                return;
            }
            UpdateIssues(issues);
        }

        private void UpdateIssues(IEnumerable<Issue> issues)
        {
            var oldSnapshot = this.Factory.CurrentSnapshot;
            var newMarkers = issues.Where(IsValidIssueTextRange).Select(CreateIssueMarker);
            var newSnapshot = new IssuesSnapshot(this.ProjectItem.ContainingProject.Name, this.FilePath, oldSnapshot.VersionNumber + 1, newMarkers);
            SnapToNewSnapshot(newSnapshot);
        }

        #endregion
    }
}
