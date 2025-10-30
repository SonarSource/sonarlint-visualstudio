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
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.Vsix;

internal interface IDocumentTrackerUpdater
{
    void OnDocumentOpened(IFileState fileState);

    void OnOpenDocumentRenamed(IFileState fileState, string oldFilePath);

    void OnDocumentSaved(IFileState fileState);

    void OnDocumentUpdated(IFileState document);

    void OnDocumentClosed(IFileState fileState);
}

/// <summary>
///     Factory for the <see cref="ITagger{T}" />. There will be one instance of this class/VS session.
/// </summary>
/// <remarks>
///     See the README.md in this folder for more information
/// </remarks>
[Export(typeof(ITaggerProvider))]
[Export(typeof(IDocumentTracker))]
[TagType(typeof(IErrorTag))]
[ContentType("text")]
[TextViewRole(PredefinedTextViewRoles.Document)]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TaggerProvider : ITaggerProvider, IRequireInitialization, IDocumentTracker, IDocumentTrackerUpdater, IDisposable
{
    internal static readonly Type SingletonManagerPropertyCollectionKey = typeof(SingletonDisposableTaggerManager<IErrorTag>);
    private readonly IAnalysisRequester analysisRequester;
    private readonly IFileStateManager fileStateManager;
    private readonly IIssueConsumerFactory issueConsumerFactory;
    private readonly IIssueConsumerStorage issueConsumerStorage;

    private readonly ISonarLanguageRecognizer languageRecognizer;
    private readonly ILogger logger;
    private readonly IThreadHandling threadHandling;

    private readonly ISonarErrorListDataSource sonarErrorDataSource;
    private readonly ITaggableBufferIndicator taggableBufferIndicator;
    private readonly ITextDocumentFactoryService textDocumentFactoryService;
    private readonly IVsProjectInfoProvider vsProjectInfoProvider;

    private bool disposed;

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
        IFileStateManager fileStateManager,
        IAnalyzer analyzer,
        ILogger logger,
        IInitializationProcessorFactory initializationProcessorFactory,
        IThreadHandling threadHandling)
    {
        this.sonarErrorDataSource = sonarErrorDataSource;
        this.textDocumentFactoryService = textDocumentFactoryService;
        this.vsProjectInfoProvider = vsProjectInfoProvider;
        this.issueConsumerFactory = issueConsumerFactory;
        this.issueConsumerStorage = issueConsumerStorage;
        this.languageRecognizer = languageRecognizer;
        this.analysisRequester = analysisRequester;
        this.taggableBufferIndicator = taggableBufferIndicator;
        this.fileStateManager = fileStateManager;
        this.logger = logger;
        this.threadHandling = threadHandling;

        InitializationProcessor = initializationProcessorFactory.CreateAndStart<TaggerProvider>(
            [],
            _ => threadHandling.RunOnUIThreadAsync(() =>
            {
                analysisRequester.AnalysisRequested += OnAnalysisRequested;
            }));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        if (InitializationProcessor.IsFinalized)
        {
            analysisRequester.AnalysisRequested -= OnAnalysisRequested;
        }

        disposed = true;
    }

    public IInitializationProcessor InitializationProcessor { get; }

    private void OnAnalysisRequested(object sender, EventArgs args) =>
        threadHandling.RunOnBackgroundThread(() => fileStateManager.AnalyzeAllOpenFiles())
            .Forget();

    #region IViewTaggerProvider members

    /// <summary>
    ///     Create a tagger that will track SonarLint issues on the view/buffer combination.
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

        // We only want one TBIT per buffer and we don't want it be disposed until
        // it is not being used by any tag aggregators, so we're wrapping it in a SingletonDisposableTaggerManager
        var singletonTaggerManager = buffer.Properties.GetOrCreateSingletonProperty(SingletonManagerPropertyCollectionKey,
            () => new SingletonDisposableTaggerManager<IErrorTag>(_ => InternalCreateTextBufferIssueTracker(textDocument)));

        var tagger = singletonTaggerManager.CreateTagger(buffer);
        return tagger as ITagger<T>;
    }

    private TextBufferIssueTracker InternalCreateTextBufferIssueTracker(ITextDocument textDocument) =>
        new(
            this,
            textDocument,
            languageRecognizer,
            sonarErrorDataSource,
            vsProjectInfoProvider,
            issueConsumerFactory,
            issueConsumerStorage,
            logger);

    #endregion IViewTaggerProvider members

    #region IDocumentTracker methods

    public event EventHandler<DocumentEventArgs> DocumentClosed;
    public event EventHandler<DocumentEventArgs> DocumentOpened;
    public event EventHandler<DocumentEventArgs> DocumentSaved;
    public event EventHandler<DocumentRenamedEventArgs> OpenDocumentRenamed;

    public Document[] GetOpenDocuments() => fileStateManager.GetOpenDocuments();

    public void OnDocumentOpened(IFileState fileState) =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            fileStateManager.Opened(fileState);
            // The lifetime of an issue tracker is tied to a single document. A document is opened, then a tracker is created.
            DocumentOpened?.Invoke(this, new DocumentEventArgs(new Document(fileState.FilePath, fileState.DetectedLanguages)));
        }).Forget();

    public void OnOpenDocumentRenamed(IFileState fileState, string oldFilePath) =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            fileStateManager.Renamed(fileState);
            OpenDocumentRenamed?.Invoke(this, new DocumentRenamedEventArgs(new Document(fileState.FilePath, fileState.DetectedLanguages), oldFilePath));
        }).Forget();

    public void OnDocumentSaved(IFileState fileState) =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            fileStateManager.ContentSaved(fileState);
            DocumentSaved?.Invoke(this, new DocumentEventArgs(new Document(fileState.FilePath, fileState.DetectedLanguages)));
        }).Forget();

    public void OnDocumentUpdated(IFileState document) =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            fileStateManager.ContentChanged(document);
        }).Forget();

    public void OnDocumentClosed(IFileState fileState) =>
        threadHandling.RunOnBackgroundThread(() =>
        {
            fileStateManager.Closed(fileState);
            // The lifetime of an issue tracker is tied to a single document. A tracker is removed when
            // it is no longer needed i.e. the document has been closed.
            DocumentClosed?.Invoke(this, new DocumentEventArgs(new Document(fileState.FilePath, fileState.DetectedLanguages)));
        }).Forget();

    #endregion IDocumentTracker methods
}
