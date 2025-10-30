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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;


internal record FileSnapshot(string FilePath, ITextSnapshot TextSnapshot);

///<summary>
///Tracks SonarLint errors for a specific buffer.
///</summary>
///<remarks>
/// <para>
/// The lifespan of this object is tied to the lifespan of the taggers on the view. On creation of the first tagger,
/// it starts tracking errors. On the disposal of the last tagger, it shuts down.
/// </para>
/// <para>
/// See the README.md in this folder for more information
/// </para>
///</remarks>
internal sealed class TextBufferIssueTracker : IFileState, ITagger<IErrorTag>
{
    private readonly ITextDocument document;
    private readonly ISonarLanguageRecognizer languageRecognizer;
    private readonly IIssueConsumerFactory issueConsumerFactory;
    private readonly IIssueConsumerStorage issueConsumerStorage;
    private readonly ILogger logger;
    private readonly ISonarErrorListDataSource sonarErrorDataSource;
    private readonly ITextBuffer textBuffer;
    private readonly IVsProjectInfoProvider vsProjectInfoProvider;
    private string projectName;
    private Guid projectGuid;

    private readonly IDocumentTrackerUpdater documentTrackerUpdater;
    internal /* for testing */ IssuesSnapshotFactory Factory { get; }

    public TextBufferIssueTracker(
        IDocumentTrackerUpdater documentTrackerUpdater,
        ITextDocument document,
        ISonarLanguageRecognizer languageRecognizer,
        ISonarErrorListDataSource sonarErrorDataSource,
        IVsProjectInfoProvider vsProjectInfoProvider,
        IIssueConsumerFactory issueConsumerFactory,
        IIssueConsumerStorage issueConsumerStorage,
        ILogger logger)
    {
        this.documentTrackerUpdater = documentTrackerUpdater;
        textBuffer = document.TextBuffer;

        this.sonarErrorDataSource = sonarErrorDataSource;
        this.vsProjectInfoProvider = vsProjectInfoProvider;
        this.issueConsumerFactory = issueConsumerFactory;
        this.issueConsumerStorage = issueConsumerStorage;
        this.logger = logger;
        logger.ForContext(nameof(TextBufferIssueTracker));

        this.document = document;
        this.languageRecognizer = languageRecognizer;
        UpdateMetadata(document.FilePath);
        Factory = new IssuesSnapshotFactory(FilePath);

        document.FileActionOccurred += SafeOnFileActionOccurred;
        if (textBuffer is ITextBuffer2 textBuffer2)
        {
            textBuffer2.ChangedOnBackground += TextBuffer_OnChangedOnBackground;
        }

        sonarErrorDataSource.AddFactory(Factory);
        this.documentTrackerUpdater.OnDocumentOpened(this);
    }

    public string FilePath { get; private set; }
    public IEnumerable<AnalysisLanguage> DetectedLanguages { get; set; }

    public void Dispose()
    {
        RemoveIssueConsumer(FilePath);
        document.FileActionOccurred -= SafeOnFileActionOccurred;
        textBuffer.Properties.RemoveProperty(TaggerProvider.SingletonManagerPropertyCollectionKey);
        if (textBuffer is ITextBuffer2 textBuffer2)
        {
            textBuffer2.ChangedOnBackground -= TextBuffer_OnChangedOnBackground;
        }
        sonarErrorDataSource.RemoveFactory(Factory);
        documentTrackerUpdater.OnDocumentClosed(this);
    }

    public IEnumerable<ITagSpan<IErrorTag>> GetTags(NormalizedSnapshotSpanCollection spans) => [];

    public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

    private void SafeOnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
    {
        // Handles callback from VS. Suppress non-critical errors to prevent them
        // propagating to VS, which would display a dialogue and disable the extension.
        try
        {
            switch (e.FileActionType)
            {
                case FileActionTypes.ContentSavedToDisk:
                    {

                        UpdateMetadata(e.FilePath);
                        documentTrackerUpdater.OnDocumentSaved(this);
                        break;
                    }
                case FileActionTypes.DocumentRenamed:
                    {
                        var oldFilePath = FilePath;
                        // workaround for issue consumer storage being filepath-based, instead of buffer based.
                        // buffer always has the latest file path, so here we have to clear the old file path, even though it's still the same buffer
                        issueConsumerStorage.Remove(oldFilePath);
                        UpdateMetadata(e.FilePath);
                        documentTrackerUpdater.OnOpenDocumentRenamed(this, oldFilePath);
                        break;
                    }
                default:
                    return;
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Strings.Analysis_ErrorOnFileAction, e.FileActionType, document.FilePath, ex);
        }
    }

    public FileSnapshot UpdateFileState()
    {
        try
        {
            RemoveIssueConsumer(FilePath);
            return InitializeAnalysisState();
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Strings.Analysis_ErrorUpdatingAnalysisState, ex);
            return null;
        }
    }

    private void SnapToNewSnapshot(IIssuesSnapshot newSnapshot)
    {
        // Tell our factory to snap to a new snapshot.
        Factory.UpdateSnapshot(newSnapshot);

        sonarErrorDataSource.RefreshErrorList(Factory);
    }

    private FileSnapshot GetAnalysisSnapshot() => new(FilePath, document.TextBuffer.CurrentSnapshot);

    private FileSnapshot InitializeAnalysisState()
    {
        var analysisSnapshot = GetAnalysisSnapshot();
        CreateIssueConsumer(analysisSnapshot);
        return analysisSnapshot;
    }

    private void UpdateMetadata(string filePath)
    {
        FilePath = filePath;
        (projectName, projectGuid) = vsProjectInfoProvider.GetDocumentProjectInfo(FilePath);
        DetectedLanguages = languageRecognizer.Detect(filePath, textBuffer.ContentType);
    }

    private void RemoveIssueConsumer(string filePath) => issueConsumerStorage.Remove(filePath);

    private void CreateIssueConsumer(FileSnapshot fileSnapshot)
    {
        var issueConsumer = issueConsumerFactory.Create(document, FilePath, fileSnapshot.TextSnapshot, projectName, projectGuid, SnapToNewSnapshot);
        issueConsumerStorage.Set(FilePath, issueConsumer);
    }

    private void TextBuffer_OnChangedOnBackground(object sender, TextContentChangedEventArgs e)
    {
        var normalizedTextChangeCollection = e.Changes;

        if (normalizedTextChangeCollection.All(x => string.IsNullOrWhiteSpace(x.NewText) && string.IsNullOrWhiteSpace(x.OldText)))
        {
            return;
        }

        documentTrackerUpdater.OnDocumentUpdated(this);
    }
}
