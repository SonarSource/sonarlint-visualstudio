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
using System.ComponentModel.Composition;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;
using SonarLint.VisualStudio.Integration.Vsix.Resources;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    /// <summary>
    /// Factory for the <see cref="ITagger{T}"/>. There will be one instance of this class/VS session.
    ///
    /// It is also the <see cref="ITableDataSource"/> that reports Sonar errors to the Error List
    /// </summary>
    /// <remarks>
    /// See the README.md in this folder for more information
    /// </remarks>
    [Export(typeof(IViewTaggerProvider))]
    [TagType(typeof(IErrorTag))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class TaggerProvider : IViewTaggerProvider, ITableDataSource
    {
        internal readonly ITableManager errorTableManager;
        internal readonly ITextDocumentFactoryService textDocumentFactoryService;
        internal readonly DTE dte;

        private readonly ISet<SinkManager> managers = new HashSet<SinkManager>();
        private readonly ISet<TextBufferIssueTracker> issueTrackers = new HashSet<TextBufferIssueTracker>();

        private readonly IAnalyzerController analyzerController;
        private readonly ISonarLanguageRecognizer languageRecognizer;
        private readonly ILogger logger;

        private readonly SingleFileMonitor settingsFileMonitor;

        [ImportingConstructor]
        internal TaggerProvider(ITableManagerProvider tableManagerProvider,
            ITextDocumentFactoryService textDocumentFactoryService,
            IAnalyzerController analyzerController,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
            
            ISonarLanguageRecognizer languageRecognizer,
            ILogger logger)
        {
            this.errorTableManager = tableManagerProvider.GetTableManager(StandardTables.ErrorsTable);
            this.textDocumentFactoryService = textDocumentFactoryService;

            this.errorTableManager.AddSource(this, StandardTableColumnDefinitions.DetailsExpander,
                                                   StandardTableColumnDefinitions.ErrorSeverity, StandardTableColumnDefinitions.ErrorCode,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.BuildTool,
                                                   StandardTableColumnDefinitions.ErrorSource, StandardTableColumnDefinitions.ErrorCategory,
                                                   StandardTableColumnDefinitions.Text, StandardTableColumnDefinitions.DocumentName,
                                                   StandardTableColumnDefinitions.Line, StandardTableColumnDefinitions.Column,
                                                   StandardTableColumnDefinitions.ProjectName);

            this.analyzerController = analyzerController;
            this.dte = serviceProvider.GetService<DTE>();
            this.languageRecognizer = languageRecognizer;
            this.logger = logger;

            this.settingsFileMonitor = new SingleFileMonitor(UserSettings.UserSettingsFilePath, logger);
            this.settingsFileMonitor.FileChanged += OnSettingsFileChanged;
        }

        private readonly object reanalysisLockObject = new object();
        private CancellableJobRunner reanalysisJob;

        private void OnSettingsFileChanged(object sender, EventArgs e)
        {
            // Handle notification from the single file monitor that the settings file has changed.

            // Re-analysis could take multiple seconds so it's possible that we'll get another
            // file change notification before the re-analysis has completed.
            // If that happens we'll cancel the current re-analysis and start another one.
            lock (reanalysisLockObject)
            {
                reanalysisJob?.Cancel();

                var operations = this.issueTrackers
                    .Select<TextBufferIssueTracker, Action>(it => () => it.RequestAnalysis())
                    .ToArray(); // create a fixed list - the user could close a file before the reanalysis completes which would cause the enumeration to change

                reanalysisJob = CancellableJobRunner.Start(Strings.JobRunner_JobDescription_ReaanalyzeOpenDocs, operations, logger);
            }
        }

        internal IEnumerable<TextBufferIssueTracker> ActiveTrackersForTesting { get { return this.issueTrackers; } }

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
                var issueTracker = buffer.Properties.GetOrCreateSingletonProperty(typeof(TextBufferIssueTracker),
                    () => new TextBufferIssueTracker(dte, this, textDocument, detectedLanguages, logger));

                // Always create a new tagger for each request
                return new IssueTagger(issueTracker) as ITagger<T>;
            }

            return null;
        }

        #endregion IViewTaggerProvider members

        #region ITableDataSource members

        public string DisplayName => "SonarLint";

        public string Identifier => "SonarLint";

        public string SourceTypeIdentifier => StandardTableDataSources.ErrorTableDataSource;

        // Note: Error List is the only expected subscriber
        public IDisposable Subscribe(ITableDataSink sink) => new SinkManager(this, sink);

        #endregion

        public void RequestAnalysis(string path, string charset, IEnumerable<SonarLanguage> detectedLanguages, IIssueConsumer issueConsumer, ProjectItem projectItem)
        {
            // May be called on the UI thread -> unhandled exceptions will crash VS
            try
            {
                analyzerController.RequestAnalysis(path, charset, detectedLanguages, issueConsumer, projectItem);
            }
            catch (Exception ex) when (!Microsoft.VisualStudio.ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Analysis error: {ex.ToString()}");
            }
        }

        public void AddSinkManager(SinkManager manager)
        {
            lock (managers)
            {
                managers.Add(manager);

                foreach (var issueTracker in issueTrackers)
                {
                    manager.AddFactory(issueTracker.Factory);
                }
            }
        }

        public void RemoveSinkManager(SinkManager manager)
        {
            lock (managers)
            {
                managers.Remove(manager);
            }
        }

        public void AddIssueTracker(TextBufferIssueTracker bufferHandler)
        {
            lock (managers)
            {
                issueTrackers.Add(bufferHandler);

                foreach (var manager in managers)
                {
                    manager.AddFactory(bufferHandler.Factory);
                }
            }
        }

        public void RemoveIssueTracker(TextBufferIssueTracker bufferHandler)
        {
            lock (managers)
            {
                issueTrackers.Remove(bufferHandler);

                foreach (var manager in managers)
                {
                    manager.RemoveFactory(bufferHandler.Factory);
                }
            }
        }

        public void UpdateAllSinks()
        {
            lock (managers)
            {
                foreach (var manager in managers)
                {
                    manager.UpdateSink();
                }
            }
        }
    }
}
