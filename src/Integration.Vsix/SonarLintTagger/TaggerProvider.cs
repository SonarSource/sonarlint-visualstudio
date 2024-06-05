/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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

using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Factory for the <see cref="ITagger{T}"/>. There will be one instance of this class/VS session.
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(IDocumentEvents))]
    [TagType(typeof(IErrorTag))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class TaggerProvider : ITaggerProvider, IDocumentEvents
    {
        internal static readonly Type SingletonManagerPropertyCollectionKey = typeof(SingletonDisposableTaggerManager<IErrorTag>);
        
        internal readonly ISonarErrorListDataSource sonarErrorDataSource;
        internal readonly ITextDocumentFactoryService textDocumentFactoryService;

        private readonly ISet<IIssueTracker> issueTrackers = new HashSet<IIssueTracker>();

        private readonly IAnalyzerController analyzerController;
        private readonly ISonarLanguageRecognizer languageRecognizer;
        private readonly IVsStatusbar vsStatusBar;
        private readonly ITaggableBufferIndicator taggableBufferIndicator;
        private readonly IVsAnalysisService vsAnalysisService;
        private readonly ILogger logger;

        [ImportingConstructor]
        internal TaggerProvider(ISonarErrorListDataSource sonarErrorDataSource,
            ITextDocumentFactoryService textDocumentFactoryService,
            IAnalyzerController analyzerController,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            ISonarLanguageRecognizer languageRecognizer,
            IAnalysisRequester analysisRequester,
            ITaggableBufferIndicator taggableBufferIndicator,
            IIssueConsumerFactory issueConsumerFactory,
            IVsAnalysisService vsAnalysisService,
            ILogger logger,
            IScheduler scheduler,
            IThreadHandling threadHandling)
        {
            this.sonarErrorDataSource = sonarErrorDataSource;
            this.textDocumentFactoryService = textDocumentFactoryService;

            this.analyzerController = analyzerController;
            this.languageRecognizer = languageRecognizer;
            this.taggableBufferIndicator = taggableBufferIndicator;
            this.vsAnalysisService = vsAnalysisService;
            this.logger = logger;

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
            return issueTrackers.Where(it => filePaths.Contains(it.LastAnalysisFilePath, StringComparer.OrdinalIgnoreCase));
        }

        internal IEnumerable<IIssueTracker> ActiveTrackersForTesting => issueTrackers;

        #region IViewTaggerProvider members

        /// <summary>
        /// Create a tagger that will track SonarLint issues on the view/buffer combination.
        /// </summary>
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            // Only attempt to track the view's edit buffer.
            if (typeof(T) != typeof(IErrorTag))
            {
                return null;
            }

            if (!taggableBufferIndicator.IsTaggable(buffer))
            {
                return null;
            }

            if (!textDocumentFactoryService.TryGetTextDocument(buffer, out var textDocument))
            {
                return null;
            }

            var detectedLanguages = languageRecognizer.Detect(textDocument.FilePath, buffer.ContentType);

            if (analyzerController.IsAnalysisSupported(detectedLanguages))
            {
                // We only want one TBIT per buffer and we don't want it be disposed until
                // it is not being used by any tag aggregators, so we're wrapping it in a SingletonDisposableTaggerManager
                var singletonTaggerManager = buffer.Properties.GetOrCreateSingletonProperty(SingletonManagerPropertyCollectionKey,
                    () => new SingletonDisposableTaggerManager<IErrorTag>(_ => InternalCreateTextBufferIssueTracker(textDocument, detectedLanguages)));

                var tagger = singletonTaggerManager.CreateTagger(buffer);
                return tagger as ITagger<T>;
            }

            return null;
        }

        private TextBufferIssueTracker InternalCreateTextBufferIssueTracker(ITextDocument textDocument, IEnumerable<AnalysisLanguage> analysisLanguages) =>
            new TextBufferIssueTracker(
                this,
                textDocument, 
                analysisLanguages,
                sonarErrorDataSource,
                vsAnalysisService,
                logger);

        #endregion IViewTaggerProvider members

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

                // The lifetime of an issue tracker is tied to a single document. A tracker is removed when
                // it is no longer needed i.e. the document has been closed.
                DocumentClosed?.Invoke(this, new DocumentClosedEventArgs(issueTracker.LastAnalysisFilePath));
            }
        }

        #region IDocumentEvents methods

        public event EventHandler<DocumentClosedEventArgs> DocumentClosed;

        #endregion IDocumentEvents methods
    }
}
