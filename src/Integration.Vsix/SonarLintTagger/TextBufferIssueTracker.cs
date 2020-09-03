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
using SonarLint.VisualStudio.IssueVisualization.Models;
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

        private readonly ITextDocument document;
        private readonly string charset;
        private readonly ILogger logger;
        private readonly IIssuesFilter issuesFilter;
        private readonly ISonarErrorListDataSource sonarErrorDataSource;
        private readonly IAnalysisIssueVisualizationConverter converter;

        public string FilePath { get; private set; }
        internal /* for testing */ SnapshotFactory Factory { get; }

        private readonly ISet<IssueTagger> activeTaggers = new HashSet<IssueTagger>();

        public TextBufferIssueTracker(DTE dte, TaggerProvider provider, ITextDocument document,
            IEnumerable<AnalysisLanguage> detectedLanguages, IIssuesFilter issuesFilter,
            ISonarErrorListDataSource sonarErrorDataSource, IAnalysisIssueVisualizationConverter converter, ILogger logger)
        {
            this.dte = dte;

            this.Provider = provider;
            this.textBuffer = document.TextBuffer;

            this.detectedLanguages = detectedLanguages;
            this.sonarErrorDataSource = sonarErrorDataSource;
            this.converter = converter;
            this.logger = logger;
            this.issuesFilter = issuesFilter;

            this.document = document;
            this.FilePath = document.FilePath;
            this.charset = document.Encoding.WebName;

            this.Factory = new SnapshotFactory(IssuesSnapshot.CreateNew(GetProjectName(), FilePath, new List<IAnalysisIssueVisualization>()));

            document.FileActionOccurred += SafeOnFileActionOccurred;
        }

        public IssueTagger CreateTagger()
        {
            var tagger = new IssueTagger(this.Factory.CurrentSnapshot.Issues, RemoveTagger);
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
                if (e.FileActionType == FileActionTypes.ContentSavedToDisk
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

        protected virtual /* for testing */ IEnumerable<IAnalysisIssueVisualization> TranslateSpans(IEnumerable<IAnalysisIssueVisualization> issues, ITextSnapshot activeSnapshot)
        {
            var issuesWithTranslatedSpans = issues
                .Where(x => x.Span.HasValue)
                .Select(x =>
                {
                    var oldSpan = x.Span.Value;
                    var newSpan = oldSpan.TranslateTo(activeSnapshot, SpanTrackingMode.EdgeExclusive);
                    x.Span = oldSpan.Length == newSpan.Length ? newSpan : (SnapshotSpan?) null;
                    return x;
                })
                .Where(x => x.Span.HasValue)
                .ToArray();

            return issuesWithTranslatedSpans;
        }

        private void SnapToNewSnapshot(IIssuesSnapshot newSnapshot)
        {
            // Tell our factory to snap to a new snapshot.
            Factory.UpdateSnapshot(newSnapshot);

            sonarErrorDataSource.RefreshErrorList(Factory);
        }

        #region Daemon interaction

        public void RequestAnalysis(IAnalyzerOptions options)
        {
            FilePath = document.FilePath; // Refresh the stored file path in case the document has been renamed
            var issueConsumer = new AccumulatingIssueConsumer(textBuffer.CurrentSnapshot, FilePath, HandleNewIssues, converter);

            // Call the consumer with no analysis issues to immediately clear issies for this file
            // from the error list
            issueConsumer.Accept(FilePath, Enumerable.Empty<IAnalysisIssue>());

            Provider.RequestAnalysis(FilePath, charset, detectedLanguages, issueConsumer, options);
        }

        internal /* for testing */ void HandleNewIssues(IEnumerable<IAnalysisIssueVisualization> issues)
        {
            var filteredIssues = RemoveSuppressedIssues(issues);

            // The text buffer might have changed since the analysis was triggered, so translate
            // all issues to the current snapshot.
            // See bug #1487: https://github.com/SonarSource/sonarlint-visualstudio/issues/1487
            var translatedIssues = TranslateSpans(filteredIssues, textBuffer.CurrentSnapshot);

            var newSnapshot = IssuesSnapshot.CreateNew(GetProjectName(), FilePath, translatedIssues);
            SnapToNewSnapshot(newSnapshot);
        }

        private IEnumerable<IAnalysisIssueVisualization> RemoveSuppressedIssues(IEnumerable<IAnalysisIssueVisualization> issues)
        {
            var filterableIssues = issues.OfType<IFilterableIssue>().ToArray();

            var filteredIssues = issuesFilter.Filter(filterableIssues);
            Debug.Assert(filteredIssues.All(x => x is IAnalysisIssueVisualization), "Not expecting the issue filter to change the list item type");

            return filteredIssues.OfType<IAnalysisIssueVisualization>().ToArray();
        }

        #endregion

        private string GetProjectName()
        {
            // Bug #676: https://github.com/SonarSource/sonarlint-visualstudio/issues/676
            // It's possible to have a ProjectItem that doesn't have a ContainingProject
            // e.g. files under the "External Dependencies" project folder in the Solution Explorer
            var projectItem = dte.Solution.FindProjectItem(this.FilePath);
            var projectName = projectItem?.ContainingProject.Name ?? "{none}";

            return projectName;
        }
    }
}
