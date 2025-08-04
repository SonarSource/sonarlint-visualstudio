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
using SonarLint.VisualStudio.Integration.CSharpVB;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;


internal record AnalysisSnapshot(string FilePath, ITextSnapshot TextSnapshot);

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
internal sealed class TextBufferIssueTracker : IIssueTracker, ITagger<IErrorTag>
{
    private readonly ITextDocument document;
    private readonly ISonarLintRoslynAnalyzer roslynAnalyzer;
    private readonly IIssueConsumerFactory issueConsumerFactory;
    private readonly IIssueConsumerStorage issueConsumerStorage;
    private readonly IAnalysisStopwatchService analysisStopwatchService;
    private readonly ILogger logger;
    private readonly IThreadHandling threadHandling;
    private readonly ISonarErrorListDataSource sonarErrorDataSource;
    private readonly ITextBuffer textBuffer;
    private readonly IVsProjectInfoProvider vsProjectInfoProvider;
    internal /* for testing */ TaggerProvider Provider { get; }
    internal /* for testing */ IssuesSnapshotFactory Factory { get; }

    private CancellationTokenSource analysisCancellation;

    public TextBufferIssueTracker(
        TaggerProvider provider,
        ITextDocument document,
        ISonarLintRoslynAnalyzer roslynAnalyzer,
        IEnumerable<AnalysisLanguage> detectedLanguages,
        ISonarErrorListDataSource sonarErrorDataSource,
        IVsProjectInfoProvider vsProjectInfoProvider,
        IIssueConsumerFactory issueConsumerFactory,
        IIssueConsumerStorage issueConsumerStorage,
        IAnalysisStopwatchService analysisStopwatchService,
        ILogger logger,
        IThreadHandling threadHandling)
    {
        Provider = provider;
        textBuffer = document.TextBuffer;

        this.sonarErrorDataSource = sonarErrorDataSource;
        this.vsProjectInfoProvider = vsProjectInfoProvider;
        this.issueConsumerFactory = issueConsumerFactory;
        this.issueConsumerStorage = issueConsumerStorage;
        this.analysisStopwatchService = analysisStopwatchService;
        this.logger = logger;
        this.threadHandling = threadHandling;
        logger.ForContext(nameof(TextBufferIssueTracker));

        this.document = document;
        this.roslynAnalyzer = roslynAnalyzer;
        LastAnalysisFilePath = document.FilePath;
        DetectedLanguages = detectedLanguages;
        Factory = new IssuesSnapshotFactory(LastAnalysisFilePath);

        var textBuffer2 = textBuffer as ITextBuffer2;
        textBuffer2.ChangedOnBackground += (sender, args) =>
        {
            InitializeAnalysisState();
        };

        document.FileActionOccurred += SafeOnFileActionOccurred;

        sonarErrorDataSource.AddFactory(Factory);
        Provider.AddIssueTracker(this);

        InitializeAnalysisState();
    }

    public string LastAnalysisFilePath { get; private set; }
    public IEnumerable<AnalysisLanguage> DetectedLanguages { get; }

    public void UpdateAnalysisState()
    {
        try
        {
            RemoveIssueConsumer(LastAnalysisFilePath);
            InitializeAnalysisState(true);
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(Strings.Analysis_ErrorUpdatingAnalysisState, ex);
        }
    }

    public string GetText() => document.TextBuffer.CurrentSnapshot.GetText();

    public void Dispose()
    {
        RemoveIssueConsumer(LastAnalysisFilePath);
        document.FileActionOccurred -= SafeOnFileActionOccurred;
        textBuffer.Properties.RemoveProperty(TaggerProvider.SingletonManagerPropertyCollectionKey);
        sonarErrorDataSource.RemoveFactory(Factory);
        Provider.OnDocumentClosed(this);
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
                        UpdateAnalysisState();
                        Provider.OnDocumentSaved(document.FilePath, GetText(), DetectedLanguages);
                        break;
                    }
                case FileActionTypes.DocumentRenamed:
                    {
                        var oldFilePath = LastAnalysisFilePath;
                        LastAnalysisFilePath = e.FilePath;
                        UpdateAnalysisState();
                        Provider.OnOpenDocumentRenamed(e.FilePath, oldFilePath, DetectedLanguages);
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

    private void SnapToNewSnapshot(IIssuesSnapshot newSnapshot)
    {
        // Tell our factory to snap to a new snapshot.
        Factory.UpdateSnapshot(newSnapshot);

        sonarErrorDataSource.RefreshErrorList(Factory);
    }

    private AnalysisSnapshot GetAnalysisSnapshot() => new(LastAnalysisFilePath, document.TextBuffer.CurrentSnapshot);

    private void InitializeAnalysisState(bool forceAnalysis = false)
    {
        analysisStopwatchService.Current = (Stopwatch.StartNew(), DateTime.Now);
        analysisCancellation?.Cancel();
        var cts = analysisCancellation = new CancellationTokenSource();
        var analysisSnapshot = GetAnalysisSnapshot();
        var issueConsumer = CreateIssueConsumer(analysisSnapshot);
        var analysisFilePath = LastAnalysisFilePath;

        threadHandling.RunOnBackgroundThread(async () =>
            {
                if (!forceAnalysis)
                {
                    await Task.Delay(500, cts.Token);
                }

                var analysisIssues = await roslynAnalyzer.AnalyzeAsync(analysisFilePath, cts.Token);
                if (analysisIssues == null)
                {
                    return;
                }
                issueConsumer.SetIssues(analysisFilePath, analysisIssues);
            })
        .Forget();
    }

    private void RemoveIssueConsumer(string filePath) => issueConsumerStorage.Remove(filePath);

    private IIssueConsumer CreateIssueConsumer(AnalysisSnapshot analysisSnapshot)
    {
        var (projectName, projectGuid) = vsProjectInfoProvider.GetDocumentProjectInfo(analysisSnapshot.FilePath);
        var issueConsumer = issueConsumerFactory.Create(document, analysisSnapshot.FilePath, analysisSnapshot.TextSnapshot, projectName, projectGuid, SnapToNewSnapshot);
        issueConsumerStorage.Set(analysisSnapshot.FilePath, issueConsumer);
        ClearErrorList(analysisSnapshot.FilePath, issueConsumer);
        return issueConsumer;
    }

    private static void ClearErrorList(string filePath, IIssueConsumer issueConsumer)
    {
        issueConsumer.SetIssues(filePath, []);
        issueConsumer.SetHotspots(filePath, []);
    }
}
