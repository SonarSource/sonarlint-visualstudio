﻿/*
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
using EnvDTE;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix.Helpers;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

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
    internal sealed class TextBufferIssueTracker : IIssueTracker, IIssueConsumer
    {
        private readonly DTE dte;
        internal /* for testing */ TaggerProvider Provider { get; }
        private readonly ITextBuffer textBuffer;
        private readonly IEnumerable<AnalysisLanguage> detectedLanguages;

        internal ProjectItem ProjectItem { get; private set; }
        private ITextSnapshot currentSnapshot;
        private NormalizedSnapshotSpanCollection dirtySpans;

        private readonly ITextDocument document;
        private readonly IIssueConverter issueConverter;
        private readonly string charset;
        private readonly ILogger logger;
        private readonly IIssuesFilter issuesFilter;

        public string FilePath { get; private set; }
        public SnapshotFactory Factory { get; }

        public IssuesSnapshot LastIssues { get; private set; }

        private readonly ISet<IssueTagger> activeTaggers = new HashSet<IssueTagger>();

        public TextBufferIssueTracker(DTE dte, TaggerProvider provider, ITextDocument document,
            IEnumerable<AnalysisLanguage> detectedLanguages, ILogger logger, IIssuesFilter issuesFilter)
            : this(dte, provider, document, detectedLanguages, new IssueConverter(), logger, issuesFilter)
        {
        }

        internal TextBufferIssueTracker(DTE dte, TaggerProvider provider, ITextDocument document,
            IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConverter issueConverter, ILogger logger, IIssuesFilter issuesFilter)
        {
            this.dte = dte;

            this.Provider = provider;
            this.textBuffer = document.TextBuffer;
            this.currentSnapshot = document.TextBuffer.CurrentSnapshot;

            this.detectedLanguages = detectedLanguages;
            this.issueConverter = issueConverter;
            this.logger = logger;
            this.issuesFilter = issuesFilter;

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

            document.FileActionOccurred += SafeOnFileActionOccurred;
        }

        public void AddTagger(IssueTagger tagger)
        {
            Debug.Assert(!activeTaggers.Contains(tagger), "Not expecting the tagger to be already registered");
            activeTaggers.Add(tagger);

            if (activeTaggers.Count == 1)
            {
                // First tagger created... start doing stuff

                textBuffer.ChangedLowPriority += SafeOnBufferChange;

                this.dirtySpans = new NormalizedSnapshotSpanCollection(new SnapshotSpan(currentSnapshot, 0, currentSnapshot.Length));

                Provider.AddIssueTracker(this);

                RequestAnalysis(null /* no options */);
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
                document.FileActionOccurred -= SafeOnFileActionOccurred;
                textBuffer.ChangedLowPriority -= SafeOnBufferChange;
                textBuffer.Properties.RemoveProperty(typeof(TextBufferIssueTracker));
                Provider.RemoveIssueTracker(this);
            }
        }

        private void SafeOnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            // Handles callback from VS. Suppress non-critical errors to prevent them
            // propagating to VS, which would display a dialogue and disable the extension.
            try
            {
                if (e.FileActionType == FileActionTypes.DocumentRenamed)
                {
                    FilePath = e.FilePath;
                    ProjectItem = dte.Solution.FindProjectItem(this.FilePath);

                    // Update and publish a new snapshow with the existing issues so 
                    // that the name change propagates to items in the error list
                    RefreshIssues();
                }
                else if (e.FileActionType == FileActionTypes.ContentSavedToDisk
                    && activeTaggers.Count > 0)
                {
                    RequestAnalysis(null /* no options */);
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Daemon_Editor_ERROR, ex);
            }
        }

        private void SafeOnBufferChange(object sender, TextContentChangedEventArgs e)
        {
            // Handles callback from VS. Suppress non-critical errors to prevent them
            // propagating to VS, which would display a dialogue and disable the extension.
            try
            {
                UpdateDirtySpans(e);

                var newMarkers = TranslateMarkerSpans();

                SnapToNewSnapshot(newMarkers);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Daemon_Editor_ERROR, ex);
            }
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

        private bool IsValidIssueTextRange(IAnalysisIssue issue) =>
            1 <= issue.StartLine && issue.EndLine <= currentSnapshot.LineCount;

        private IssueMarker CreateIssueMarker(IAnalysisIssue issue) =>
            issueConverter.ToMarker(issue, currentSnapshot);

        private void SnapToNewSnapshot(IssuesSnapshot newIssues)
        {
            // Tell our factory to snap to a new snapshot.
            this.Factory.UpdateSnapshot(newIssues);

            Provider.RefreshErrorList();

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

            if (newIssues != null && newIssues.Count > 0)
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

        public void RequestAnalysis(IAnalyzerOptions options)
        {
            Provider.RequestAnalysis(FilePath, charset, detectedLanguages, this, ProjectItem, options);
        }

        void IIssueConsumer.Accept(string path, IEnumerable<IAnalysisIssue> issues)
        {
            // Callback from the daemon when new results are available
            if (path != FilePath)
            {
                Debug.Fail("Issues returned for an unexpected file path");
                return;
            }

            var filteredIssues = RemoveSuppressedIssues(issues);

            var newMarkers = filteredIssues.Where(IsValidIssueTextRange).Select(CreateIssueMarker);
            UpdateIssues(newMarkers);
        }

        private IEnumerable<IAnalysisIssue> RemoveSuppressedIssues(IEnumerable<IAnalysisIssue> issues)
        {
            var filterableIssues = IssueToFilterableIssueConverter.Convert(issues, currentSnapshot);

            var filteredIssues = issuesFilter.Filter(filterableIssues);
            Debug.Assert(filteredIssues.All(x => x is FilterableIssueAdapter), "Not expecting the issue filter to change the list item type");

            var suppressedCount = filterableIssues.Count() - filteredIssues.Count();
            logger.WriteLine(Strings.Daemon_SuppressedIssuesInfo, suppressedCount);

            return filteredIssues.OfType<FilterableIssueAdapter>()
                .Select(x => x.SonarLintIssue);
        }

        private void RefreshIssues()
        {
            UpdateIssues(this.Factory.CurrentSnapshot.IssueMarkers ?? Enumerable.Empty<IssueMarker>());
        }

        private void UpdateIssues(IEnumerable<IssueMarker> issueMarkers)
        {
            var oldSnapshot = this.Factory.CurrentSnapshot;
            var newSnapshot = new IssuesSnapshot(this.ProjectItem.ContainingProject.Name, this.FilePath, oldSnapshot.VersionNumber + 1, issueMarkers);
            SnapToNewSnapshot(newSnapshot);
        }

        #endregion
    }
}
