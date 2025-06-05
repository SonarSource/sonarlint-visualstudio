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

using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class TextBufferIssueTrackerTests
{
    private const string TextContent = "file content";

    private AnalysisLanguage[] javascriptLanguage = [AnalysisLanguage.Javascript];
    private TestLogger logger;
    private IVsAwareAnalysisService mockAnalysisService;
    private ITextSnapshot mockTextSnapshot;
    private ITextBuffer mockDocumentTextBuffer;
    private ITextDocument mockedJavascriptDocumentFooJs;
    private ISonarErrorListDataSource mockSonarErrorDataSource;
    private IFileTracker mockFileTracker;
    private TaggerProvider taggerProvider;
    private TextBufferIssueTracker testSubject;

    [TestInitialize]
    public void SetUp()
    {
        mockSonarErrorDataSource = Substitute.For<ISonarErrorListDataSource>();
        mockAnalysisService = Substitute.For<IVsAwareAnalysisService>();
        mockFileTracker = Substitute.For<IFileTracker>();
        taggerProvider = CreateTaggerProvider();
        mockTextSnapshot = CreateTextSnapshotMock();
        mockDocumentTextBuffer = CreateTextBufferMock(mockTextSnapshot);
        mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js", mockDocumentTextBuffer);
        javascriptLanguage = [AnalysisLanguage.Javascript];

        testSubject = CreateTestSubject(mockAnalysisService);
    }

    private TextBufferIssueTracker CreateTestSubject(IVsAwareAnalysisService vsAnalysisService = null)
    {
        logger = new TestLogger();
        vsAnalysisService ??= Substitute.For<IVsAwareAnalysisService>();

        return new TextBufferIssueTracker(taggerProvider,
            mockedJavascriptDocumentFooJs, javascriptLanguage,
            mockSonarErrorDataSource, vsAnalysisService,
            mockFileTracker, logger);
    }

    [TestMethod]
    [Description("TextBufferIssueTracker is no longer used as a real tagger and therefore should not produce any tags")]
    public void GetTags_EmptyArray()
    {
        testSubject.GetTags(null).Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_RegistersEventsTrackerAndFactory()
    {
        CheckFactoryWasRegisteredWithDataSource(testSubject.Factory, times: 1);

        taggerProvider.ActiveTrackersForTesting.Should().BeEquivalentTo(testSubject);

        mockedJavascriptDocumentFooJs.Received(1).FileActionOccurred += Arg.Any<EventHandler<TextDocumentFileActionEventArgs>>();

        // Note: the test subject isn't responsible for adding the entry to the buffer.Properties
        // - that's done by the TaggerProvider.
    }

    [TestMethod]
    public void Ctor_CreatesFactoryWithCorrectData()
    {
        var issuesSnapshot = testSubject.Factory.CurrentSnapshot;
        issuesSnapshot.Should().NotBeNull();

        issuesSnapshot.AnalyzedFilePath.Should().Be(mockedJavascriptDocumentFooJs.FilePath);
        issuesSnapshot.Issues.Count().Should().Be(0);
    }

    private static void SetUpAnalysisThrows(IVsAwareAnalysisService mockAnalysisService, Exception exception)
    {
        mockAnalysisService.When(x => x.RequestAnalysis(Arg.Any<ITextDocument>(),
            Arg.Any<AnalysisSnapshot>(),
            Arg.Any<IEnumerable<AnalysisLanguage>>(),
            Arg.Any<SnapshotChangedHandler>(),
            Arg.Any<IAnalyzerOptions>())).Throw(exception);
    }

    [TestMethod]
    public void Dispose_CleansUpEventsAndRegistrations()
    {
        // Sanity checks
        var singletonManager = new SingletonDisposableTaggerManager<IErrorTag>(null);
        mockDocumentTextBuffer.Properties.AddProperty(TaggerProvider.SingletonManagerPropertyCollectionKey, singletonManager);
        VerifySingletonManagerExists(mockDocumentTextBuffer);
        CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, times: 0);

        // Act
        testSubject.Dispose();

        mockAnalysisService.Received().CancelForFile(mockedJavascriptDocumentFooJs.FilePath);

        VerifySingletonManagerDoesNotExist(mockDocumentTextBuffer);

        CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, times: 1);

        taggerProvider.ActiveTrackersForTesting.Should().BeEmpty();

        mockedJavascriptDocumentFooJs.Received(1).FileActionOccurred -= Arg.Any<EventHandler<TextDocumentFileActionEventArgs>>();
    }

    [TestMethod]
    public void Dispose_RaisesEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        taggerProvider.DocumentClosed += eventHandler;

        testSubject.Dispose();

        eventHandler.Received(1).Invoke(taggerProvider, Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == mockedJavascriptDocumentFooJs.FilePath));
    }

    private static void VerifySingletonManagerDoesNotExist(ITextBuffer buffer)
    {
        FindSingletonManagerInPropertyCollection(buffer).Should().BeNull();
    }

    private static void VerifySingletonManagerExists(ITextBuffer buffer)
    {
        FindSingletonManagerInPropertyCollection(buffer).Should().NotBeNull();
    }

    private static SingletonDisposableTaggerManager<IErrorTag> FindSingletonManagerInPropertyCollection(ITextBuffer buffer)
    {
        buffer.Properties.TryGetProperty<SingletonDisposableTaggerManager<IErrorTag>>(TaggerProvider.SingletonManagerPropertyCollectionKey,
            out var propertyValue);
        return propertyValue;
    }

    private void CheckFactoryWasRegisteredWithDataSource(IssuesSnapshotFactory factory, int times)
    {
        mockSonarErrorDataSource.Received(times).AddFactory(factory);
    }

    private void CheckFactoryWasUnregisteredFromDataSource(IssuesSnapshotFactory factory, int times)
    {
        mockSonarErrorDataSource.Received(times).RemoveFactory(factory);
    }

    private TaggerProvider CreateTaggerProvider()
    {
        var tableManagerProviderMock = Substitute.For<ITableManagerProvider>();
        tableManagerProviderMock.GetTableManager(StandardTables.ErrorsTable)
            .Returns(Substitute.For<ITableManager>());

        var textDocFactoryServiceMock = Substitute.For<ITextDocumentFactoryService>();

        var languageRecognizer = Mock.Of<ISonarLanguageRecognizer>();

        var serviceProvider = new ConfigurableServiceProvider();
        serviceProvider.RegisterService(typeof(IVsStatusbar), Mock.Of<IVsStatusbar>());

        var mockAnalysisRequester = Substitute.For<IAnalysisRequester>();

        var mockAnalysisScheduler = Substitute.For<IScheduler>();
        mockAnalysisScheduler.When(x => x.Schedule(Arg.Any<string>(), Arg.Any<Action<CancellationToken>>(), Arg.Any<int>()))
            .Do(callInfo =>
            {
                var analyze = callInfo.Arg<Action<CancellationToken>>();
                analyze(new CancellationToken());
            });

        var sonarErrorListDataSource = mockSonarErrorDataSource;
        var textDocumentFactoryService = textDocFactoryServiceMock;
        var vsAwareAnalysisService = mockAnalysisService;
        var analysisRequester = mockAnalysisRequester;
        var provider = new TaggerProvider(sonarErrorListDataSource, textDocumentFactoryService,
            serviceProvider, languageRecognizer, vsAwareAnalysisService, analysisRequester, Mock.Of<ITaggableBufferIndicator>(), mockFileTracker, logger);
        return provider;
    }

    private static ITextSnapshot CreateTextSnapshotMock()
    {
        var textSnapshot = Substitute.For<ITextSnapshot>();
        textSnapshot.GetText().Returns(TextContent);
        return textSnapshot;
    }

    private static ITextBuffer CreateTextBufferMock(ITextSnapshot textSnapshot)
    {
        // Text buffer with a properties collection and current snapshot
        var mockTextBuffer = Substitute.For<ITextBuffer>();

        var dummyProperties = new PropertyCollection();
        mockTextBuffer.Properties.Returns(dummyProperties);

        mockTextBuffer.CurrentSnapshot.Returns(textSnapshot);

        return mockTextBuffer;
    }

    private static ITextDocument CreateDocumentMock(string fileName, ITextBuffer textBufferMock)
    {
        var mockTextDocument = Substitute.For<ITextDocument>();
        mockTextDocument.FilePath.Returns(fileName);
        mockTextDocument.TextBuffer.Returns(textBufferMock);
        mockTextDocument.Encoding.Returns(Encoding.UTF8);

        return mockTextDocument;
    }

    #region Triggering analysis tests

    [TestMethod]
    public void WhenFileIsSaved_AnalysisIsRequested()
    {
        mockAnalysisService.ClearReceivedCalls();
        mockTextSnapshot.ClearReceivedCalls();

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisRequestedWithDefaultOptions(false);

        // Dispose and raise -> analysis not requested
        testSubject.Dispose();
        mockAnalysisService.ClearReceivedCalls();
        mockTextSnapshot.ClearReceivedCalls();

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisNotRequested();
    }

    [TestMethod]
    public void WhenFileIsSaved_DocumentSavedEventIsRaised()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentSavedEventArgs>>();
        taggerProvider.DocumentSaved += eventHandler;

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);

        eventHandler.Received(1).Invoke(taggerProvider,
            Arg.Is<DocumentSavedEventArgs>(x => x.Document.FullPath == mockedJavascriptDocumentFooJs.FilePath && x.Document.DetectedLanguages == javascriptLanguage));
    }

    [TestMethod]
    public void WhenFileIsLoaded_AnalysisIsNotRequested()
    {
        mockAnalysisService.ClearReceivedCalls();
        mockTextSnapshot.ClearReceivedCalls();

        // Act
        RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisNotRequested();

        // Sanity check (that the test setup is correct and that events are actually being handled)
        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisRequestedWithDefaultOptions(false);
    }

    [TestMethod]
    public void WhenFileIsLoaded_EventsAreNotRaised()
    {
        var renamedEventHandler = Substitute.For<EventHandler<DocumentRenamedEventArgs>>();
        var savedEventHandler = Substitute.For<EventHandler<DocumentSavedEventArgs>>();
        taggerProvider.OpenDocumentRenamed += renamedEventHandler;
        taggerProvider.DocumentSaved += savedEventHandler;

        RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);

        renamedEventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
        savedEventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void WhenFileIsRenamed_LastAnalysisFilePathIsUpdated()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentRenamedEventArgs>>();
        taggerProvider.OpenDocumentRenamed += eventHandler;
        var newFilePath = "newName.cs";

        RaiseFileRenamedEvent(mockedJavascriptDocumentFooJs, newFilePath);

        testSubject.LastAnalysisFilePath.Should().Be(newFilePath);
    }

    [TestMethod]
    public void WhenFileIsRenamed_AnalysisIsNotRequested()
    {
        mockAnalysisService.ClearReceivedCalls();
        mockTextSnapshot.ClearReceivedCalls();

        RaiseFileRenamedEvent(mockedJavascriptDocumentFooJs, "newFile.cs");

        VerifyAnalysisNotRequested();
    }

    [TestMethod]
    public void WhenFileIsRenamed_OpenDocumentRenamedEventIsRaised()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentRenamedEventArgs>>();
        taggerProvider.OpenDocumentRenamed += eventHandler;
        var newFilePath = "renamedFile.js";

        RaiseFileRenamedEvent(mockedJavascriptDocumentFooJs, newFilePath);

        eventHandler.Received(1).Invoke(taggerProvider, Arg.Is<DocumentRenamedEventArgs>(x =>
            x.Document.FullPath == newFilePath && x.OldFilePath == mockedJavascriptDocumentFooJs.FilePath && x.Document.DetectedLanguages == javascriptLanguage));
    }

    private static void RaiseFileSavedEvent(ITextDocument mockDocument) => RaiseFileEvent(mockDocument, FileActionTypes.ContentSavedToDisk);

    private static void RaiseFileLoadedEvent(ITextDocument mockDocument) => RaiseFileEvent(mockDocument, FileActionTypes.ContentLoadedFromDisk);

    private static void RaiseFileRenamedEvent(ITextDocument mockDocument, string newFilePath) => RaiseFileEvent(mockDocument, FileActionTypes.DocumentRenamed, newFilePath);

    private static void RaiseFileEvent(ITextDocument textDocument, FileActionTypes actionType, string filePath = null)
    {
        var args = new TextDocumentFileActionEventArgs(filePath ?? textDocument.FilePath, DateTime.UtcNow, actionType);
        textDocument.FileActionOccurred += Raise.EventWith(null, args);
    }

    #endregion

    #region RequestAnalysis

    [TestMethod]
    public void Ctor_AnalysisIsRequestedOnCreation()
    {
        VerifyAnalysisRequestedWithDefaultOptions(true);
    }

    [TestMethod]
    public void RequestAnalysis_CallsAnalysisService()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        var analyzerOptions = Mock.Of<IAnalyzerOptions>();
        testSubject.RequestAnalysis(analyzerOptions);

        VerifyAnalysisRequested(analyzerOptions);
    }

    [TestMethod]
    public void RequestAnalysis_DocumentRenamed_CancelsForPreviousFilePath()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();
        mockedJavascriptDocumentFooJs.FilePath.Returns("newFoo.js");

        var analyzerOptions = Mock.Of<IAnalyzerOptions>();
        testSubject.RequestAnalysis(analyzerOptions);

        mockAnalysisService.Received().CancelForFile("foo.js");
        mockAnalysisService.Received().RequestAnalysis(Arg.Any<ITextDocument>(),
            Arg.Is<AnalysisSnapshot>(x => x.FilePath == "newFoo.js"),
            Arg.Any<IEnumerable<AnalysisLanguage>>(),
            Arg.Any<SnapshotChangedHandler>(),
            Arg.Any<IAnalyzerOptions>());
    }

    [TestMethod]
    public void RequestAnalysis_NonCriticalException_IsSuppressed()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        SetUpAnalysisThrows(mockAnalysisService, new InvalidOperationException());

        var act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
        act.Should().NotThrow();

        logger.AssertPartialOutputStringExists("[Analysis] Error triggering analysis: ");
    }

    [TestMethod]
    public void RequestAnalysis_NotSupportedException_IsSuppressedAndLogged()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        SetUpAnalysisThrows(mockAnalysisService, new NotSupportedException("This is not supported"));

        var act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
        act.Should().NotThrow();

        logger.AssertOutputStringExists("[Analysis] Unable to analyze: This is not supported");
    }

    [TestMethod]
    public void RequestAnalysis_CriticalException_IsNotSuppressed()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        SetUpAnalysisThrows(mockAnalysisService, new DivideByZeroException("this is a test"));

        var act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
        act.Should().Throw<DivideByZeroException>()
            .WithMessage("this is a test");
    }

    private void VerifyAnalysisRequested(IAnalyzerOptions analyzerOptions)
    {
        var textDocument = mockedJavascriptDocumentFooJs;
        mockAnalysisService.Received().CancelForFile(textDocument.FilePath);
        mockTextSnapshot.Received().GetText();
        mockFileTracker.Received().AddFiles(new SourceFile(textDocument.FilePath, null, TextContent));
        mockAnalysisService.Received().RequestAnalysis(
            textDocument,
            new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot),
            javascriptLanguage,
            Arg.Any<SnapshotChangedHandler>(),
            analyzerOptions);
    }

    private void VerifyAnalysisRequestedWithDefaultOptions(bool isOnOpen)
    {
        var textDocument = mockedJavascriptDocumentFooJs;
        mockAnalysisService.Received().CancelForFile(textDocument.FilePath);
        mockTextSnapshot.Received().GetText();
        mockFileTracker.Received().AddFiles(new SourceFile(textDocument.FilePath, null, TextContent));
        mockAnalysisService.Received().RequestAnalysis(
            textDocument,
            new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot),
            javascriptLanguage,
            Arg.Any<SnapshotChangedHandler>(),
            Arg.Is<IAnalyzerOptions>(o => o.IsOnOpen == isOnOpen));
    }

    private void VerifyAnalysisNotRequested()
    {
        mockAnalysisService.DidNotReceive().CancelForFile(Arg.Any<string>());
        mockTextSnapshot.DidNotReceive().GetText();
        mockAnalysisService.DidNotReceive().RequestAnalysis(Arg.Any<ITextDocument>(),
            Arg.Any<AnalysisSnapshot>(),
            Arg.Any<IEnumerable<AnalysisLanguage>>(),
            Arg.Any<SnapshotChangedHandler>(),
            Arg.Any<IAnalyzerOptions>());
    }

    #endregion RequestAnalysis
}
