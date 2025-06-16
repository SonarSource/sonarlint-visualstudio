/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger
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
    internal sealed class TextBufferIssueTracker : IIssueTracker, ITagger<IErrorTag>
    {
        internal /* for testing */ TaggerProvider Provider { get; }
        private readonly ITextBuffer textBuffer;

        private readonly ITextDocument document;
        private readonly ILogger logger;
        private readonly IVsAwareAnalysisService vsAwareAnalysisService;
        private readonly IVsProjectInfoProvider vsProjectInfoProvider;
        private readonly IIssueConsumerFactory issueConsumerFactory;
        private readonly IIssueConsumerStorage issueConsumerStorage;
        private readonly IFileTracker fileTracker;
        private readonly IThreadHandling threadHandling;
        private readonly ISonarErrorListDataSource sonarErrorDataSource;

        public string LastAnalysisFilePath { get; private set; }
        public IEnumerable<AnalysisLanguage> DetectedLanguages { get; }
        internal /* for testing */ IssuesSnapshotFactory Factory { get; }

        public TextBufferIssueTracker(
            TaggerProvider provider,
            ITextDocument document,
            IEnumerable<AnalysisLanguage> detectedLanguages,
            ISonarErrorListDataSource sonarErrorDataSource,
            IVsAwareAnalysisService vsAwareAnalysisService,
            IVsProjectInfoProvider vsProjectInfoProvider,
            IIssueConsumerFactory issueConsumerFactory,
            IIssueConsumerStorage issueConsumerStorage,
            IFileTracker fileTracker,
            IThreadHandling threadHandling,
            ILogger logger)
        {
            Provider = provider;
            textBuffer = document.TextBuffer;

            this.sonarErrorDataSource = sonarErrorDataSource;
            this.vsAwareAnalysisService = vsAwareAnalysisService;
            this.vsProjectInfoProvider = vsProjectInfoProvider;
            this.issueConsumerFactory = issueConsumerFactory;
            this.issueConsumerStorage = issueConsumerStorage;
            this.fileTracker = fileTracker;
            this.threadHandling = threadHandling;
            this.logger = logger;

            this.document = document;
            LastAnalysisFilePath = document.FilePath;
            DetectedLanguages = detectedLanguages;
            Factory = new IssuesSnapshotFactory(LastAnalysisFilePath);

            document.FileActionOccurred += SafeOnFileActionOccurred;

            sonarErrorDataSource.AddFactory(Factory);
            Provider.AddIssueTracker(this);

            InitializeAnalysisStateAsync().Forget();
        }

        private void SafeOnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            // Handles callback from VS. Suppress non-critical errors to prevent them
            // propagating to VS, which would display a dialogue and disable the extension.
            try
            {
                switch (e.FileActionType)
                {
                    // TODO by https://sonarsource.atlassian.net/browse/SLVS-2310 Request analysis can be removed and replaced by InitializeAnalysisStateAsync
                    case FileActionTypes.ContentSavedToDisk:
                        {
                            Provider.OnDocumentSaved(document.FilePath, document.TextBuffer.CurrentSnapshot.GetText(), DetectedLanguages);
                            break;
                        }
                    case FileActionTypes.DocumentRenamed:
                        {
                            Provider.OnOpenDocumentRenamed(e.FilePath, LastAnalysisFilePath, DetectedLanguages);
                            LastAnalysisFilePath = e.FilePath;
                            break;
                        }
                    default:
                        return;
                }
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Analysis_ErrorTriggeringAnalysis, ex);
            }
        }

        private void SnapToNewSnapshot(IIssuesSnapshot newSnapshot)
        {
            // Tell our factory to snap to a new snapshot.
            Factory.UpdateSnapshot(newSnapshot);

            sonarErrorDataSource.RefreshErrorList(Factory);
        }

        public void RequestAnalysis()
        {
            try
            {
                CancelForFile(LastAnalysisFilePath);
                var analysisSnapshot = UpdateAnalysisState();
                CreateIssueConsumerAsync(analysisSnapshot).Forget();
                vsAwareAnalysisService.RequestAnalysis(analysisSnapshot);
            }
            catch (NotSupportedException ex)
            {
                // Display a simple user-friendly message for options we know are not supported.
                // See https://github.com/SonarSource/sonarlint-visualstudio/pull/2212
                logger.WriteLine(Strings.Analysis_NotSupported, ex.Message);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Strings.Analysis_ErrorTriggeringAnalysis, ex);
            }
        }

        private AnalysisSnapshot UpdateAnalysisState()
        {
            LastAnalysisFilePath = document.FilePath; // Refresh the stored file path in case the document has been renamed
            var analysisSnapshot = new AnalysisSnapshot(LastAnalysisFilePath, document.TextBuffer.CurrentSnapshot);
            NotifyFileTracker(analysisSnapshot.TextSnapshot);
            return analysisSnapshot;
        }

        private void NotifyFileTracker(ITextSnapshot snapshot) => fileTracker.AddFiles(new SourceFile(LastAnalysisFilePath, content: snapshot.GetText()));

        private async Task InitializeAnalysisStateAsync()
        {
            var analysisSnapshot = UpdateAnalysisState();
            await CreateIssueConsumerAsync(analysisSnapshot);
        }

        private void CancelForFile(string filePath) => issueConsumerStorage.Remove(filePath);

        private async Task<IIssueConsumer> CreateIssueConsumerAsync(AnalysisSnapshot analysisSnapshot)
        {
            var (projectName, projectGuid) = await vsProjectInfoProvider.GetDocumentProjectInfoAsync(analysisSnapshot.FilePath);
            var issueConsumer = issueConsumerFactory.Create(document, analysisSnapshot.FilePath, analysisSnapshot.TextSnapshot, projectName, projectGuid, SnapToNewSnapshot);
            issueConsumerStorage.Set(analysisSnapshot.FilePath, issueConsumer);
            await threadHandling.RunOnBackgroundThread(() => ClearErrorList(analysisSnapshot.FilePath, issueConsumer));

            return issueConsumer;
        }

        private static void ClearErrorList(string filePath, IIssueConsumer issueConsumer)
        {
            issueConsumer.SetIssues(filePath, []);
            issueConsumer.SetHotspots(filePath, []);
        }

        public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans) => [];

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        public void Dispose()
        {
            CancelForFile(LastAnalysisFilePath);
            document.FileActionOccurred -= SafeOnFileActionOccurred;
            textBuffer.Properties.RemoveProperty(TaggerProvider.SingletonManagerPropertyCollectionKey);
            sonarErrorDataSource.RemoveFactory(Factory);
            Provider.OnDocumentClosed(this);
        }
    }
}
