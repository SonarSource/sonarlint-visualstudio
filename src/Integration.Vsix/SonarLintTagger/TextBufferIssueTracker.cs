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
using EnvDTE;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
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
    internal class TextBufferIssueTracker : IIssueTracker
    {
        private readonly DTE dte;
        internal /* for testing */ TaggerProvider Provider { get; }
        private readonly ITextBuffer textBuffer;
        private readonly IEnumerable<AnalysisLanguage> detectedLanguages;

        internal ProjectItem ProjectItem { get; private set; }
        private ITextSnapshot currentSnapshot;

        private readonly ITextDocument document;
        private readonly string charset;
        private readonly ILogger logger;
        private readonly IIssuesFilter issuesFilter;
        private readonly ISonarErrorListDataSource sonarErrorDataSource;

        public string FilePath { get; private set; }
        internal /* for testing */ SnapshotFactory Factory { get; }

        private readonly ISet<IssueTagger> activeTaggers = new HashSet<IssueTagger>();

        public TextBufferIssueTracker(DTE dte, TaggerProvider provider, ITextDocument document,
            IEnumerable<AnalysisLanguage> detectedLanguages, IIssuesFilter issuesFilter,
            ISonarErrorListDataSource sonarErrorDataSource, ILogger logger)
        {
            this.dte = dte;

            this.Provider = provider;
            this.textBuffer = document.TextBuffer;
            this.currentSnapshot = document.TextBuffer.CurrentSnapshot;

            this.detectedLanguages = detectedLanguages;
            this.sonarErrorDataSource = sonarErrorDataSource;
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

        public IssueTagger CreateTagger()
        {
            var tagger = new IssueTagger(this.Factory.CurrentSnapshot.IssueMarkers, RemoveTagger);
            this.AddTagger(tagger);

            return tagger;
        }

        private void AddTagger(IssueTagger tagger)
        {
            Debug.Assert(!activeTaggers.Contains(tagger), "Not expecting the tagger to be already registered");
            activeTaggers.Add(tagger);

            if (activeTaggers.Count == 1)
            {
                // First tagger created... start doing stuff

                textBuffer.ChangedLowPriority += SafeOnBufferChange;

                sonarErrorDataSource.AddFactory(this.Factory);
                Provider.AddIssueTracker(this);

                RequestAnalysis(null /* no options */);
            }
        }

        private void RemoveTagger(IssueTagger tagger)
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
                sonarErrorDataSource.RemoveFactory(this.Factory);
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
                    // that the name change propagates to items in the error list.
                    // No need in this case to translate the tagger spans.
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
                // The text buffer has been edited (i.e.text added, deleted or modified).
                // The spans we have stored for issues relate to the previous text buffer and
                // are no longer valid, so we need to translate them to the equivalent spans
                // in the new text buffer.
                currentSnapshot = e.After;

                var newMarkers = TranslateSpans(Factory.CurrentSnapshot.IssueMarkers, currentSnapshot);
                UpdateIssues(newMarkers);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Daemon_Editor_ERROR, ex);
            }
        }

        protected virtual /* for testing */ IEnumerable<IssueMarker> TranslateSpans(IEnumerable<IssueMarker> issueMarkers, ITextSnapshot activeSnapshot)
        {
            var newMarkers = issueMarkers
                .Select(marker => marker.CloneAndTranslateTo(activeSnapshot))
                .Where(clone => clone != null)
                .ToArray();

            return newMarkers;
        }

        private void SnapToNewSnapshot(IssuesSnapshot newIssues)
        {
            var oldIssues = Factory.CurrentSnapshot;

            // Tell our factory to snap to a new snapshot.
            this.Factory.UpdateSnapshot(newIssues);

            sonarErrorDataSource.RefreshErrorList();

            // Work out which part of the document has been affected by the changes, and tell
            // the taggers about the changes
            SnapshotSpan? affectedSpan = CalculateAffectedSpan(oldIssues, newIssues);
            foreach (var tagger in activeTaggers)
            {
                tagger.UpdateMarkers(newIssues.IssueMarkers, affectedSpan);
            }
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
            var issueConsumer = new AccumulatingIssueConsumer(currentSnapshot, FilePath, HandleNewIssues);
            Provider.RequestAnalysis(FilePath, charset, detectedLanguages, issueConsumer, options);
        }

        internal /* for testing */ void HandleNewIssues(IEnumerable<IssueMarker> issueMarkers)
        {
            var filteredMarkers = RemoveSuppressedIssues(issueMarkers);

            // The text buffer might have changed since the analysis was triggered, so translate
            // all issues to the current snapshot.
            // See bug #1487: https://github.com/SonarSource/sonarlint-visualstudio/issues/1487
            var translatedMarkers = TranslateSpans(filteredMarkers, currentSnapshot);

            UpdateIssues(translatedMarkers);
        }

        private IEnumerable<IssueMarker> RemoveSuppressedIssues(IEnumerable<IssueMarker> issues)
        {
            var filterableIssues = issues.OfType<IFilterableIssue>().ToArray();

            var filteredIssues = issuesFilter.Filter(filterableIssues);
            Debug.Assert(filteredIssues.All(x => x is IssueMarker), "Not expecting the issue filter to change the list item type");

            var suppressedCount = filterableIssues.Count() - filteredIssues.Count();
            logger.WriteLine(Strings.Daemon_SuppressedIssuesInfo, suppressedCount);

            return filteredIssues.OfType<IssueMarker>().ToArray();
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
