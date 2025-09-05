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
using SonarLint.VisualStudio.Core.Synchronization;
using SonarLint.VisualStudio.Infrastructure.VS.Initialization;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class TextBufferIssueTrackerTests
{
    private const string TextContent = "file content";

    private AnalysisLanguage[] javascriptLanguage = [AnalysisLanguage.Javascript];
    private TestLogger logger;
    private ITextSnapshot mockTextSnapshot;
    private ITextBuffer mockDocumentTextBuffer;
    private ITextDocument mockedJavascriptDocumentFooJs;
    private ISonarErrorListDataSource mockSonarErrorDataSource;
    private IFileTracker mockFileTracker;
    private TaggerProvider taggerProvider;
    private TextBufferIssueTracker testSubject;
    private IVsProjectInfoProvider vsProjectInfoProvider;
    private IIssueConsumerFactory issueConsumerFactory;
    private IIssueConsumerStorage issueConsumerStorage;
    private IIssueConsumer issueConsumer;
    private ITaskExecutorWithDebounceFactory taskExecutorWithDebounceFactory;
    private ITaskExecutorWithDebounce taskExecutorWithDebounce;

    [TestInitialize]
    public void SetUp()
    {
        mockSonarErrorDataSource = Substitute.For<ISonarErrorListDataSource>();
        mockFileTracker = Substitute.For<IFileTracker>();
        vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        issueConsumer = Substitute.For<IIssueConsumer>();
        taggerProvider = CreateTaggerProvider();
        mockTextSnapshot = CreateTextSnapshotMock();
        mockDocumentTextBuffer = CreateTextBufferMock(mockTextSnapshot);
        mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js", mockDocumentTextBuffer);
        javascriptLanguage = [AnalysisLanguage.Javascript];
        taskExecutorWithDebounceFactory = Substitute.For<ITaskExecutorWithDebounceFactory>();
        taskExecutorWithDebounce = Substitute.For<ITaskExecutorWithDebounce>();
        taskExecutorWithDebounceFactory.Create(Arg.Any<TimeSpan>()).Returns(taskExecutorWithDebounce);
        MockIssueConsumerFactory(mockedJavascriptDocumentFooJs, issueConsumer);

        testSubject = CreateTestSubject(mockedJavascriptDocumentFooJs);
    }

    [TestMethod]
    [Description("TextBufferIssueTracker is no longer used as a real tagger and therefore should not produce any tags")]
    public void GetTags_EmptyArray() => testSubject.GetTags(null).Should().BeEmpty();

    [TestMethod]
    public void Ctor_SetsContext()
    {
        var mockedLogger = Substitute.For<ILogger>();

        _ = CreateTestSubject(mockedJavascriptDocumentFooJs, mockedLogger);

        mockedLogger.Received(1).ForContext(nameof(TextBufferIssueTracker));
    }

    [TestMethod]
    public void Ctor_RegistersEventsTrackerAndFactory()
    {
        CheckFactoryWasRegisteredWithDataSource(testSubject.Factory, times: 1);

        taggerProvider.ActiveTrackersForTesting.Should().BeEquivalentTo(testSubject);

        mockedJavascriptDocumentFooJs.Received(1).FileActionOccurred += Arg.Any<EventHandler<TextDocumentFileActionEventArgs>>();
        ((ITextBuffer2)mockDocumentTextBuffer).Received(1).ChangedOnBackground += Arg.Any<EventHandler<TextContentChangedEventArgs>>();

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

    [TestMethod]
    public void Ctor_UpdatesAnalysisStateAndCreatesIssueConsumer()
    {
        var textDocument = CreateDocumentMock("foo.ts", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer);
        var projectInfo = MockGetDocumentProjectInfo(textDocument.FilePath);

        _ = CreateTestSubject(textDocument);

        VerifyCreateIssueConsumerWasCalled(textDocument, projectInfo, consumer, new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    [TestMethod]
    public void GetText_ReturnExpectedSnapshotText() => testSubject.GetText().Should().Be(TextContent);

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

        issueConsumerStorage.Received().Remove(mockedJavascriptDocumentFooJs.FilePath);

        VerifySingletonManagerDoesNotExist(mockDocumentTextBuffer);

        CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, times: 1);

        taggerProvider.ActiveTrackersForTesting.Should().BeEmpty();

        mockedJavascriptDocumentFooJs.Received(1).FileActionOccurred -= Arg.Any<EventHandler<TextDocumentFileActionEventArgs>>();
        ((ITextBuffer2)mockDocumentTextBuffer).Received(1).ChangedOnBackground -= Arg.Any<EventHandler<TextContentChangedEventArgs>>();
    }

    [TestMethod]
    public void Dispose_RaisesEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        taggerProvider.DocumentClosed += eventHandler;

        testSubject.Dispose();

        eventHandler.Received(1).Invoke(taggerProvider, Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == mockedJavascriptDocumentFooJs.FilePath));
    }

    [TestMethod]
    public void WhenFileIsSaved_DocumentSavedEventIsRaised()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        taggerProvider.DocumentSaved += eventHandler;

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);

        eventHandler.Received(1).Invoke(taggerProvider,
            Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == mockedJavascriptDocumentFooJs.FilePath && x.Document.DetectedLanguages == javascriptLanguage));
    }

    [TestMethod]
    public void WhenFileIsSaved_AnalysisSnapshotIsUpdated()
    {
        var textDocument = CreateDocumentMock("foo1.css", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer);
        var projectInfo = MockGetDocumentProjectInfo(textDocument.FilePath);
        CreateTestSubject(textDocument);
        ClearIssueConsumerCalls();

        RaiseFileSavedEvent(textDocument);

        VerifyCreateIssueConsumerWasCalled(textDocument, projectInfo, consumer, new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    [TestMethod]
    public void WhenFileIsLoaded_EventsAreNotRaised()
    {
        var renamedEventHandler = Substitute.For<EventHandler<DocumentRenamedEventArgs>>();
        var savedEventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        taggerProvider.OpenDocumentRenamed += renamedEventHandler;

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
    public void WhenFileIsRenamed_OpenDocumentRenamedEventIsRaised()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentRenamedEventArgs>>();
        taggerProvider.OpenDocumentRenamed += eventHandler;
        var newFilePath = "renamedFile.js";

        RaiseFileRenamedEvent(mockedJavascriptDocumentFooJs, newFilePath);

        eventHandler.Received(1).Invoke(taggerProvider, Arg.Is<DocumentRenamedEventArgs>(x =>
            x.Document.FullPath == newFilePath && x.OldFilePath == mockedJavascriptDocumentFooJs.FilePath && x.Document.DetectedLanguages == javascriptLanguage));
    }

    [TestMethod]
    public void WhenFileIsRenamed_AnalysisSnapshotIsUpdated()
    {
        var newFilePath = "renamedFile.js";
        var textDocument = CreateDocumentMock("foo1.css", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer, fileName: newFilePath);
        var projectInfo = MockGetDocumentProjectInfo(newFilePath);
        CreateTestSubject(textDocument);
        ClearIssueConsumerCalls();

        RaiseFileRenamedEvent(textDocument, newFilePath);

        VerifyCreateIssueConsumerWasCalled(textDocument, projectInfo, consumer, new AnalysisSnapshot(newFilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    private static void RaiseFileSavedEvent(ITextDocument mockDocument) => RaiseFileEvent(mockDocument, FileActionTypes.ContentSavedToDisk);

    private static void RaiseFileLoadedEvent(ITextDocument mockDocument) => RaiseFileEvent(mockDocument, FileActionTypes.ContentLoadedFromDisk);

    private static void RaiseFileRenamedEvent(ITextDocument mockDocument, string newFilePath) => RaiseFileEvent(mockDocument, FileActionTypes.DocumentRenamed, newFilePath);

    private static void RaiseFileEvent(ITextDocument textDocument, FileActionTypes actionType, string filePath = null)
    {
        var args = new TextDocumentFileActionEventArgs(filePath ?? textDocument.FilePath, DateTime.UtcNow, actionType);
        textDocument.FileActionOccurred += Raise.EventWith(null, args);
    }

    [TestMethod]
    public void UpdateAnalysisState_CancelsForPreviousFilePath()
    {
        mockedJavascriptDocumentFooJs.FilePath.Returns("newFoo.js");

        testSubject.UpdateAnalysisState();

        issueConsumerStorage.Received().Remove("foo.js");
    }

    [TestMethod]
    public void UpdateAnalysisState_ProjectInformationReturned_CreatesIssueConsumerCorrectly()
    {
        var textDocument = CreateDocumentMock("foo1.css", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer);
        var projectInfo = MockGetDocumentProjectInfo(textDocument.FilePath);

        CreateTestSubject(textDocument).UpdateAnalysisState();

        VerifyCreateIssueConsumerWasCalled(textDocument, projectInfo, consumer, new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    [TestMethod]
    public void UpdateAnalysisState_NoProjectInformation_CreatesIssueConsumerCorrectly()
    {
        var textDocument = CreateDocumentMock("foo2.css", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer);
        MockGetDocumentProjectInfo(default);

        CreateTestSubject(textDocument).UpdateAnalysisState();

        VerifyCreateIssueConsumerWasCalled(textDocument, (default, Guid.Empty), consumer, new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    [TestMethod]
    public void UpdateAnalysisState_ClearsErrorList()
    {
        var textDocument = mockedJavascriptDocumentFooJs;

        CreateTestSubject(textDocument).UpdateAnalysisState();

        issueConsumer.Received().SetIssues(textDocument.FilePath, []);
        issueConsumer.Received().SetHotspots(textDocument.FilePath, []);
    }

    [TestMethod]
    public void UpdateAnalysisState_NonCriticalException_IsSuppressed()
    {
        SetUpIssueConsumerStorageThrows(new InvalidOperationException());

        var act = () => testSubject.UpdateAnalysisState();

        act.Should().NotThrow();
        logger.AssertPartialOutputStringExists(string.Format(Strings.Analysis_ErrorUpdatingAnalysisState, string.Empty));
    }

    [TestMethod]
    public void UpdateAnalysisState_CriticalException_IsNotSuppressed()
    {
        SetUpIssueConsumerStorageThrows(new DivideByZeroException("this is a test"));

        var act = () => testSubject.UpdateAnalysisState();

        act.Should().Throw<DivideByZeroException>()
            .WithMessage("this is a test");
    }

    [TestMethod]
    public void OnTextBufferChangedOnBackground_UpdatesAnalysisState()
    {
        var eventHandler = SubscribeToDocumentSaved();
        var newContent = "new content";
        var newSnapshot = CreateTextSnapshotMock(newContent);
        var newAnalysisSnapshot = new AnalysisSnapshot(mockedJavascriptDocumentFooJs.FilePath, newSnapshot);
        MockTaskExecutorWithDebounce();
        CreateTestSubject(mockedJavascriptDocumentFooJs);

        RaiseTextBufferChangedOnBackground(currentTextBuffer: mockDocumentTextBuffer, newSnapshot);

        VerifyAnalysisStateUpdated(mockedJavascriptDocumentFooJs, newAnalysisSnapshot, eventHandler, newContent);
    }

    private void SetUpIssueConsumerStorageThrows(Exception exception) => issueConsumerStorage.When(x => x.Remove(Arg.Any<string>())).Do(x => throw exception);

    private static void VerifySingletonManagerDoesNotExist(ITextBuffer buffer) => FindSingletonManagerInPropertyCollection(buffer).Should().BeNull();

    private static void VerifySingletonManagerExists(ITextBuffer buffer) => FindSingletonManagerInPropertyCollection(buffer).Should().NotBeNull();

    private static SingletonDisposableTaggerManager<IErrorTag> FindSingletonManagerInPropertyCollection(ITextBuffer buffer)
    {
        buffer.Properties.TryGetProperty<SingletonDisposableTaggerManager<IErrorTag>>(TaggerProvider.SingletonManagerPropertyCollectionKey,
            out var propertyValue);
        return propertyValue;
    }

    private void CheckFactoryWasRegisteredWithDataSource(IssuesSnapshotFactory factory, int times) => mockSonarErrorDataSource.Received(times).AddFactory(factory);

    private void CheckFactoryWasUnregisteredFromDataSource(IssuesSnapshotFactory factory, int times) => mockSonarErrorDataSource.Received(times).RemoveFactory(factory);

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
        var analyzer = Substitute.For<IAnalyzer>();

        var sonarErrorListDataSource = mockSonarErrorDataSource;
        var textDocumentFactoryService = textDocFactoryServiceMock;
        var analysisRequester = mockAnalysisRequester;
        var provider = new TaggerProvider(sonarErrorListDataSource, textDocumentFactoryService,
            serviceProvider, languageRecognizer, analysisRequester, vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage, Mock.Of<ITaggableBufferIndicator>(),
            mockFileTracker, analyzer, logger, new InitializationProcessorFactory(Substitute.For<IAsyncLockFactory>(), new NoOpThreadHandler(), new TestLogger()), taskExecutorWithDebounceFactory);
        return provider;
    }

    private static ITextSnapshot CreateTextSnapshotMock(string content = TextContent)
    {
        var textSnapshot = Substitute.For<ITextSnapshot>();
        textSnapshot.GetText().Returns(content);
        return textSnapshot;
    }

    private static ITextBuffer CreateTextBufferMock(ITextSnapshot textSnapshot)
    {
        // Text buffer with a properties collection and current snapshot
        var mockTextBuffer = Substitute.For<ITextBuffer2>();

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

    private void MockIssueConsumerFactory(ITextDocument document, IIssueConsumer issueConsumer, string fileName = null) =>
        issueConsumerFactory
            .Create(document,
                fileName ?? document.FilePath,
                Arg.Any<ITextSnapshot>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<SnapshotChangedHandler>())
            .Returns(issueConsumer);

    private (string projectName, Guid projectGuid) MockGetDocumentProjectInfo(string filePath)
    {
        var projectInfo = (projectName: "project123", projectGuid: Guid.NewGuid());
        MockGetDocumentProjectInfo(filePath, projectInfo);
        return projectInfo;
    }

    private void MockGetDocumentProjectInfo(string filePath, (string projectName, Guid projectGuid) projectInfo) => vsProjectInfoProvider.GetDocumentProjectInfo(filePath).Returns(projectInfo);

    private void VerifyCreateIssueConsumerWasCalled(
        ITextDocument document,
        (string projectName, Guid projectGuid) projectInfo,
        IIssueConsumer issueConsumerToVerify,
        AnalysisSnapshot analysisSnapshot)
    {
        vsProjectInfoProvider.Received().GetDocumentProjectInfo(analysisSnapshot.FilePath);
        issueConsumerFactory.Received().Create(document, analysisSnapshot.FilePath, analysisSnapshot.TextSnapshot, projectInfo.projectName, projectInfo.projectGuid, Arg.Any<SnapshotChangedHandler>());
        issueConsumerStorage.Received().Set(analysisSnapshot.FilePath, issueConsumerToVerify);
    }

    private TextBufferIssueTracker CreateTestSubject(ITextDocument textDocument)
    {
        logger = new TestLogger();

        return CreateTestSubject(textDocument, logger);
    }

    private TextBufferIssueTracker CreateTestSubject(ITextDocument textDocument, ILogger logger) =>
        new(taggerProvider,
            textDocument, javascriptLanguage,
            mockSonarErrorDataSource, vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage, taskExecutorWithDebounce, logger);

    private void ClearIssueConsumerCalls()
    {
        issueConsumerStorage.ClearReceivedCalls();
        issueConsumerFactory.ClearReceivedCalls();
        vsProjectInfoProvider.ClearReceivedCalls();
    }

    private static void RaiseTextBufferChangedOnBackground(ITextBuffer currentTextBuffer, ITextSnapshot newTextSnapshot)
    {
        var args = new TextContentChangedEventArgs(Substitute.For<ITextSnapshot>(), newTextSnapshot, EditOptions.DefaultMinimalChange, null);
        ((ITextBuffer2)currentTextBuffer).ChangedOnBackground += Raise.EventWith(null, args);
    }

    private EventHandler<DocumentEventArgs> SubscribeToDocumentSaved()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        taggerProvider.DocumentUpdated += eventHandler;
        return eventHandler;
    }

    private void MockTaskExecutorWithDebounce() =>
        taskExecutorWithDebounce.When(x => x.DebounceAsync(Arg.Any<Action>())).Do(callInfo =>
        {
            var action = callInfo.Arg<Action>();
            action();
        });

    private void VerifyAnalysisStateUpdated(
        ITextDocument textDocument,
        AnalysisSnapshot newAnalysisSnapshot,
        EventHandler<DocumentEventArgs> eventHandler,
        string newContent)
    {
        issueConsumerStorage.Received().Remove(textDocument.FilePath);
        vsProjectInfoProvider.Received().GetDocumentProjectInfo(newAnalysisSnapshot.FilePath);
        issueConsumerFactory.Received().Create(textDocument, newAnalysisSnapshot.FilePath, newAnalysisSnapshot.TextSnapshot, Arg.Any<string>(), Arg.Any<Guid>(),
            Arg.Any<SnapshotChangedHandler>());
        issueConsumerStorage.Received().Set(textDocument.FilePath, Arg.Any<IIssueConsumer>());
        issueConsumer.Received().SetIssues(textDocument.FilePath, []);
        issueConsumer.Received().SetHotspots(textDocument.FilePath, []);
        eventHandler.Received().Invoke(taggerProvider,
            Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == textDocument.FilePath
                                           && x.Document.DetectedLanguages == javascriptLanguage
                                           && x.Content == newContent));
    }
}
