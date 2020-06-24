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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Factory for the <see cref="ITagger{T}"/>. There will be one instance of this class/VS session.
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class TaggerProvider : IViewTaggerProvider
    {
        internal readonly ISonarErrorListDataSource sonarErrorDataSource;
        internal readonly ITextDocumentFactoryService textDocumentFactoryService;
        internal readonly IIssuesFilter issuesFilter;
        internal readonly DTE dte;

        private readonly ISet<IIssueTracker> issueTrackers = new HashSet<IIssueTracker>();

        private readonly IAnalyzerController analyzerController;
        private readonly ISonarLanguageRecognizer languageRecognizer;
        private readonly IVsStatusbar vsStatusBar;
        private readonly ILogger logger;
        private readonly IScheduler scheduler;

        [ImportingConstructor]
        internal TaggerProvider(ISonarErrorListDataSource sonarErrorDataSource,
            ITextDocumentFactoryService textDocumentFactoryService,
            IIssuesFilter issuesFilter,
            IAnalyzerController analyzerController,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ISonarLanguageRecognizer languageRecognizer,
            IAnalysisRequester analysisRequester,
            ILogger logger,
            IScheduler scheduler)
        {
            this.sonarErrorDataSource = sonarErrorDataSource;
            this.textDocumentFactoryService = textDocumentFactoryService;
            this.issuesFilter = issuesFilter;

            this.analyzerController = analyzerController;
            this.dte = serviceProvider.GetService<DTE>();
            this.languageRecognizer = languageRecognizer;
            this.logger = logger;
            this.scheduler = scheduler;

            vsStatusBar = serviceProvider.GetService(typeof(IVsStatusbar)) as IVsStatusbar;
            analysisRequester.AnalysisRequested += OnAnalysisRequested;
        }

        private readonly object reanalysisLockObject = new object();
        private CancellableJobRunner reanalysisJob;
        private StatusBarReanalysisProgressHandler reanalysisProgressHandler;

        private void OnAnalysisRequested(object sender, AnalysisRequestEventArgs args)
        {
            // Handle notification from the single file monitor that the settings file has changed.

            // Re-analysis could take multiple seconds so it's possible that we'll get another
            // file change notification before the re-analysis has completed.
            // If that happens we'll cancel the current re-analysis and start another one.
            lock (reanalysisLockObject)
            {
                reanalysisJob?.Cancel();
                reanalysisProgressHandler?.Dispose();

                var filteredIssueTrackers = FilterIssuesTrackersByPath(this.issueTrackers, args.FilePaths);

                var operations = filteredIssueTrackers
                    .Select<IIssueTracker, Action>(it => () => it.RequestAnalysis(args.Options))
                    .ToArray(); // create a fixed list - the user could close a file before the reanalysis completes which would cause the enumeration to change

                reanalysisProgressHandler = new StatusBarReanalysisProgressHandler(vsStatusBar, logger);

                var message = string.Format(CultureInfo.CurrentCulture, Strings.JobRunner_JobDescription_ReaanalyzeDocs, operations.Length);
                reanalysisJob = CancellableJobRunner.Start(message, operations,
                    reanalysisProgressHandler, logger);
            }
        }

        internal /* for testing */ static IEnumerable<IIssueTracker> FilterIssuesTrackersByPath(
            IEnumerable<IIssueTracker> issueTrackers, IEnumerable<string> filePaths)
        {
            if (filePaths == null || !filePaths.Any())
            {
                return issueTrackers;
            }
            return issueTrackers.Where(it => filePaths.Contains(it.FilePath, StringComparer.OrdinalIgnoreCase));
        }

        internal IEnumerable<IIssueTracker> ActiveTrackersForTesting { get { return this.issueTrackers; } }

        #region IViewTaggerProvider members

        /// <summary>
        /// Create a tagger that will track SonarLint issues on the view/buffer combination.
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            // Only attempt to track the view's edit buffer.
            if (buffer != textView.TextBuffer ||
                typeof(T) != typeof(IErrorTag))
            {
                return null;
            }

            ITextDocument textDocument;
            if (!textDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out textDocument))
            {
                return null;
            }

            var detectedLanguages = languageRecognizer.Detect(textDocument, buffer);

            if (detectedLanguages.Any() && analyzerController.IsAnalysisSupported(detectedLanguages))
            {
                // Multiple views could have that buffer open simultaneously, so only create one instance of the tracker.
                var issueTracker = buffer.Properties.GetOrCreateSingletonProperty(typeof(IIssueTracker),
                    () => new TextBufferIssueTracker(dte, this, textDocument, detectedLanguages, issuesFilter, sonarErrorDataSource, logger));

                // Always create a new tagger for each request.
                // Delegate the actual creation to the tracker for the file.
                return issueTracker.CreateTagger() as ITagger<T>;
            }

            return null;
        }

        #endregion IViewTaggerProvider members

        public void RequestAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages, IIssueConsumer issueConsumer, IAnalyzerOptions analyzerOptions)
        {
            // May be called on the UI thread -> unhandled exceptions will crash VS
            try
            {
                var analysisTimeout = analyzerOptions?.AnalysisTimeoutInMilliseconds ?? Timeout.Infinite;

                scheduler.Schedule(path,
                    cancellationToken =>
                        analyzerController.ExecuteAnalysis(path, charset, detectedLanguages, issueConsumer,
                            analyzerOptions, cancellationToken),
                    analysisTimeout);
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Analysis error: {ex}");
            }
        }

        public void AddIssueTracker(IIssueTracker issueTracker)
        {
            lock (issueTrackers)
            {
                issueTrackers.Add(issueTracker);
            }
        }

        public void RemoveIssueTracker(IIssueTracker issueTracker)
        {
            lock (issueTrackers)
            {
                issueTrackers.Remove(issueTracker);
            }
        }
    }
}
