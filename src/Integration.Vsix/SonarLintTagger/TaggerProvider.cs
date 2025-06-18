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

using System.ComponentModel.Composition;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Vsix;

/// <summary>
/// Factory for the <see cref="ITagger{T}" />. There will be one instance of this class/VS session.
/// </summary>
/// <remarks>
/// See the README.md in this folder for more information
/// </remarks>
[Export(typeof(ITaggerProvider))]
[Export(typeof(IDocumentTracker))]
[TagType(typeof(IErrorTag))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Document)]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TaggerProvider : ITaggerProvider, IDocumentTracker
{
    internal static readonly Type SingletonManagerPropertyCollectionKey = typeof(SingletonDisposableTaggerManager<IErrorTag>);
    private readonly IAnalyzer analyzer;
    private readonly IFileTracker fileTracker;
    private readonly IIssueConsumerFactory issueConsumerFactory;
    private readonly IIssueConsumerStorage issueConsumerStorage;

    private readonly ISet<IIssueTracker> issueTrackers = new HashSet<IIssueTracker>();

    private readonly ISonarLanguageRecognizer languageRecognizer;
    private readonly ILogger logger;

    private readonly object reanalysisLockObject = new();

    internal readonly ISonarErrorListDataSource sonarErrorDataSource;
    private readonly ITaggableBufferIndicator taggableBufferIndicator;
    internal readonly ITextDocumentFactoryService textDocumentFactoryService;
    private readonly IThreadHandling threadHandling;
    private readonly IVsProjectInfoProvider vsProjectInfoProvider;
    private readonly IVsStatusbar vsStatusBar;
    private Guid? lastAnalysisId;
    private CancellableJobRunner reanalysisJob;
    private StatusBarReanalysisProgressHandler reanalysisProgressHandler;

    internal IEnumerable<IIssueTracker> ActiveTrackersForTesting => issueTrackers;

    [ImportingConstructor]
    internal TaggerProvider(
        ISonarErrorListDataSource sonarErrorDataSource,
        ITextDocumentFactoryService textDocumentFactoryService,
        [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider,
        ISonarLanguageRecognizer languageRecognizer,
        IAnalysisRequester analysisRequester,
        IVsProjectInfoProvider vsProjectInfoProvider,
        IIssueConsumerFactory issueConsumerFactory,
        IIssueConsumerStorage issueConsumerStorage,
        ITaggableBufferIndicator taggableBufferIndicator,
        IFileTracker fileTracker,
        IThreadHandling threadHandling,
        IAnalyzer analyzer,
        ILogger logger)
    {
        this.sonarErrorDataSource = sonarErrorDataSource;
        this.textDocumentFactoryService = textDocumentFactoryService;

        this.vsProjectInfoProvider = vsProjectInfoProvider;
        this.issueConsumerFactory = issueConsumerFactory;
        this.issueConsumerStorage = issueConsumerStorage;
        this.languageRecognizer = languageRecognizer;
        this.taggableBufferIndicator = taggableBufferIndicator;
        this.fileTracker = fileTracker;
        this.threadHandling = threadHandling;
        this.analyzer = analyzer;
        this.logger = logger;

        vsStatusBar = serviceProvider.GetService(typeof(IVsStatusbar)) as IVsStatusbar;
        analysisRequester.AnalysisRequested += OnAnalysisRequested;
    }

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
            if (lastAnalysisId.HasValue)
            {
                analyzer.CancelAnalysis(lastAnalysisId.Value);
            }

            var filteredIssueTrackers = FilterIssuesTrackersByPath(issueTrackers, args.FilePaths).ToList();

            var operations = filteredIssueTrackers
                .Select<IIssueTracker, Action>(it => () => it.UpdateAnalysisStateAsync())
                .ToList(); // create a fixed list - the user could close a file before the reanalysis completes which would cause the enumeration to change
            var documentsToAnalyzeCount = operations.Count;
            operations.Add(() => NotifyFileTracker(filteredIssueTrackers));
            operations.Add(() => ExecuteAnalysisAsync(filteredIssueTrackers).Forget());

            reanalysisProgressHandler = new StatusBarReanalysisProgressHandler(vsStatusBar, logger);

            var message = string.Format(CultureInfo.CurrentCulture, Strings.JobRunner_JobDescription_ReaanalyzeDocs, documentsToAnalyzeCount);
            reanalysisJob = CancellableJobRunner.Start(message, operations,
                reanalysisProgressHandler, logger);
        }
    }

    private async Task ExecuteAnalysisAsync(IEnumerable<IIssueTracker> filteredIssueTrackers) =>
        lastAnalysisId = await analyzer.ExecuteAnalysis(filteredIssueTrackers.Select(x => x.LastAnalysisFilePath).ToList());

    internal /* for testing */ static IEnumerable<IIssueTracker> FilterIssuesTrackersByPath(
        IEnumerable<IIssueTracker> issueTrackers,
        IEnumerable<string> filePaths)
    {
        if (filePaths == null || !filePaths.Any())
        {
            return issueTrackers;
        }
        return issueTrackers.Where(it => filePaths.Contains(it.LastAnalysisFilePath, StringComparer.OrdinalIgnoreCase));
    }

    private void NotifyFileTracker(string filePath, string content) => fileTracker.AddFiles(CreateSourceFile(filePath, content));

    private void NotifyFileTracker(IEnumerable<IIssueTracker> filteredIssueTrackers)
    {
        var sourceFilesToUpdate = filteredIssueTrackers.Select(it => CreateSourceFile(it.LastAnalysisFilePath, it.GetText()));
        fileTracker.AddFiles(sourceFilesToUpdate.ToArray());
    }

    private static SourceFile CreateSourceFile(string filePath, string content) => new(filePath, content: content);

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

        // We only want one TBIT per buffer and we don't want it be disposed until
        // it is not being used by any tag aggregators, so we're wrapping it in a SingletonDisposableTaggerManager
        var singletonTaggerManager = buffer.Properties.GetOrCreateSingletonProperty(SingletonManagerPropertyCollectionKey,
            () => new SingletonDisposableTaggerManager<IErrorTag>(_ => InternalCreateTextBufferIssueTracker(textDocument, detectedLanguages)));

        var tagger = singletonTaggerManager.CreateTagger(buffer);
        return tagger as ITagger<T>;
    }

    private TextBufferIssueTracker InternalCreateTextBufferIssueTracker(ITextDocument textDocument, IEnumerable<AnalysisLanguage> analysisLanguages) =>
        new(
            this,
            textDocument,
            analysisLanguages,
            sonarErrorDataSource,
            vsProjectInfoProvider,
            issueConsumerFactory,
            issueConsumerStorage,
            threadHandling,
            logger);

    #endregion IViewTaggerProvider members

    #region IDocumentTracker methods

    public event EventHandler<DocumentEventArgs> DocumentClosed;
    public event EventHandler<DocumentOpenedEventArgs> DocumentOpened;
    public event EventHandler<DocumentSavedEventArgs> DocumentSaved;
    public event EventHandler<DocumentRenamedEventArgs> OpenDocumentRenamed;

    public IEnumerable<Document> GetOpenDocuments()
    {
        lock (issueTrackers)
        {
            return issueTrackers.Select(it => new Document(it.LastAnalysisFilePath, it.DetectedLanguages));
        }
    }

    public void AddIssueTracker(IIssueTracker issueTracker)
    {
        lock (issueTrackers)
        {
            issueTrackers.Add(issueTracker);
        }

        var filePath = issueTracker.LastAnalysisFilePath;
        var content = issueTracker.GetText();

        NotifyFileTracker(filePath, content);
        // The lifetime of an issue tracker is tied to a single document. A document is opened, then a tracker is created.
        DocumentOpened?.Invoke(this, new DocumentOpenedEventArgs(new Document(filePath, issueTracker.DetectedLanguages), content));
    }

    public void OnOpenDocumentRenamed(string newFilePath, string oldFilePath, IEnumerable<AnalysisLanguage> detectedLanguages) =>
        OpenDocumentRenamed?.Invoke(this, new DocumentRenamedEventArgs(new Document(newFilePath, detectedLanguages), oldFilePath));

    public void OnDocumentSaved(string fullPath, string newContent, IEnumerable<AnalysisLanguage> detectedLanguages)
    {
        NotifyFileTracker(fullPath, newContent);
        DocumentSaved?.Invoke(this, new DocumentSavedEventArgs(new Document(fullPath, detectedLanguages), newContent));
    }

    public void OnDocumentClosed(IIssueTracker issueTracker)
    {
        lock (issueTrackers)
        {
            issueTrackers.Remove(issueTracker);
        }

        // The lifetime of an issue tracker is tied to a single document. A tracker is removed when
        // it is no longer needed i.e. the document has been closed.
        DocumentClosed?.Invoke(this, new DocumentEventArgs(new Document(issueTracker.LastAnalysisFilePath, issueTracker.DetectedLanguages)));
    }

    #endregion IDocumentTracker methods
}
