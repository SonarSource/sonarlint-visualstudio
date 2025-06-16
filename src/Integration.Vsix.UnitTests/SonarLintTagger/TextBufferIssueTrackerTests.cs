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
    private IVsAwareAnalysisService mockAnalysisService;
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
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void SetUp()
    {
        mockSonarErrorDataSource = Substitute.For<ISonarErrorListDataSource>();
        mockAnalysisService = Substitute.For<IVsAwareAnalysisService>();
        mockFileTracker = Substitute.For<IFileTracker>();
        vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        issueConsumer = Substitute.For<IIssueConsumer>();
        threadHandling = Substitute.For<IThreadHandling>();
        taggerProvider = CreateTaggerProvider();
        mockTextSnapshot = CreateTextSnapshotMock();
        mockDocumentTextBuffer = CreateTextBufferMock(mockTextSnapshot);
        mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js", mockDocumentTextBuffer);
        javascriptLanguage = [AnalysisLanguage.Javascript];
        MockIssueConsumerFactory(mockedJavascriptDocumentFooJs, issueConsumer);
        MockThreadHandling();

        testSubject = CreateTestSubject(mockedJavascriptDocumentFooJs);
    }

    private TextBufferIssueTracker CreateTestSubject(ITextDocument textDocument)
    {
        logger = new TestLogger();

        return new TextBufferIssueTracker(taggerProvider,
            textDocument, javascriptLanguage,
            mockSonarErrorDataSource, mockAnalysisService, vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage,
            mockFileTracker, threadHandling, logger);
    }

    [TestMethod]
    [Description("TextBufferIssueTracker is no longer used as a real tagger and therefore should not produce any tags")]
    public void GetTags_EmptyArray() => testSubject.GetTags(null).Should().BeEmpty();

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

    [TestMethod]
    public void Ctor_UpdatesAnalysisStateAndCreatesIssueConsumer()
    {
        var textDocument = CreateDocumentMock("foo.ts", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer);
        var projectInfo = MockGetDocumentProjectInfoAsync(textDocument.FilePath);

        _ = CreateTestSubject(textDocument);

        mockFileTracker.Received(1).AddFiles(new SourceFile(textDocument.FilePath, encoding: null, TextContent));
        VerifyCreateIssueConsumerWasCalled(textDocument, projectInfo, consumer, new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
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

        issueConsumerStorage.Received().Remove(mockedJavascriptDocumentFooJs.FilePath);

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

    private static void SetUpAnalysisThrows(IVsAwareAnalysisService mockAnalysisService, Exception exception) =>
        mockAnalysisService.When(x => x.RequestAnalysis(
            Arg.Any<AnalysisSnapshot>())).Throw(exception);

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

        var sonarErrorListDataSource = mockSonarErrorDataSource;
        var textDocumentFactoryService = textDocFactoryServiceMock;
        var vsAwareAnalysisService = mockAnalysisService;
        var analysisRequester = mockAnalysisRequester;
        var provider = new TaggerProvider(sonarErrorListDataSource, textDocumentFactoryService,
            serviceProvider, languageRecognizer, vsAwareAnalysisService, analysisRequester, vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage, Mock.Of<ITaggableBufferIndicator>(),
            mockFileTracker, threadHandling, logger);
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

    private void MockIssueConsumerFactory(ITextDocument document, IIssueConsumer issueConsumer) =>
        issueConsumerFactory
            .Create(document,
                document.FilePath,
                Arg.Any<ITextSnapshot>(),
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<SnapshotChangedHandler>())
            .Returns(issueConsumer);

    private (string projectName, Guid projectGuid) MockGetDocumentProjectInfoAsync(string filePath)
    {
        var projectInfo = (projectName: "project123", projectGuid: Guid.NewGuid());
        MockGetDocumentProjectInfoAsync(filePath, projectInfo);
        return projectInfo;
    }

    private void MockGetDocumentProjectInfoAsync(string filePath, (string projectName, Guid projectGuid) projectInfo) =>
        vsProjectInfoProvider.GetDocumentProjectInfoAsync(filePath).Returns(projectInfo);

    private void VerifyCreateIssueConsumerWasCalled(
        ITextDocument document,
        (string projectName, Guid projectGuid) projectInfo,
        IIssueConsumer issueConsumerToVerify,
        AnalysisSnapshot analysisSnapshot)
    {
        vsProjectInfoProvider.Received().GetDocumentProjectInfoAsync(document.FilePath);
        issueConsumerFactory.Received().Create(document, document.FilePath, analysisSnapshot.TextSnapshot, projectInfo.projectName, projectInfo.projectGuid, Arg.Any<SnapshotChangedHandler>());
        issueConsumerStorage.Received().Set(document.FilePath, issueConsumerToVerify);
    }

    #region Triggering analysis tests

    [TestMethod]
    public void WhenFileIsSaved_AnalysisIsNotRequested()
    {
        mockAnalysisService.ClearReceivedCalls();
        mockTextSnapshot.ClearReceivedCalls();

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);

        mockAnalysisService.DidNotReceive().RequestAnalysis(Arg.Any<AnalysisSnapshot>());
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

        RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);

        VerifyAnalysisNotRequested();
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
    public void Ctor_AnalysisNotRequestedOnCreation() => mockAnalysisService.DidNotReceive().RequestAnalysis(Arg.Any<AnalysisSnapshot>());

    [TestMethod]
    public void RequestAnalysis_CallsAnalysisService()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        testSubject.RequestAnalysis();

        VerifyAnalysisRequested();
    }

    [TestMethod]
    public void RequestAnalysis_DocumentRenamed_CancelsForPreviousFilePath()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();
        mockedJavascriptDocumentFooJs.FilePath.Returns("newFoo.js");

        testSubject.RequestAnalysis();

        issueConsumerStorage.Received().Remove("foo.js");
        mockAnalysisService.Received().RequestAnalysis(Arg.Is<AnalysisSnapshot>(x => x.FilePath == "newFoo.js"));
    }

    [TestMethod]
    public void RequestAnalysis_NonCriticalException_IsSuppressed()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        SetUpAnalysisThrows(mockAnalysisService, new InvalidOperationException());

        var act = () => testSubject.RequestAnalysis();
        act.Should().NotThrow();

        logger.AssertPartialOutputStringExists("[Analysis] Error triggering analysis: ");
    }

    [TestMethod]
    public void RequestAnalysis_NotSupportedException_IsSuppressedAndLogged()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        SetUpAnalysisThrows(mockAnalysisService, new NotSupportedException("This is not supported"));

        var act = () => testSubject.RequestAnalysis();
        act.Should().NotThrow();

        logger.AssertOutputStringExists("[Analysis] Unable to analyze: This is not supported");
    }

    [TestMethod]
    public void RequestAnalysis_CriticalException_IsNotSuppressed()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.ClearReceivedCalls();

        SetUpAnalysisThrows(mockAnalysisService, new DivideByZeroException("this is a test"));

        var act = () => testSubject.RequestAnalysis();
        act.Should().Throw<DivideByZeroException>()
            .WithMessage("this is a test");
    }

    [TestMethod]
    public void RequestAnalysis_ProjectInformationReturned_CreatesIssueConsumerCorrectly()
    {
        var textDocument = CreateDocumentMock("foo1.css", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer);
        var projectInfo = MockGetDocumentProjectInfoAsync(textDocument.FilePath);

        CreateTestSubject(textDocument).RequestAnalysis(new AnalyzerOptions());

        VerifyCreateIssueConsumerWasCalled(textDocument, projectInfo, consumer, new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    [TestMethod]
    public void RequestAnalysis_NoProjectInformation_CreatesIssueConsumerCorrectly()
    {
        var textDocument = CreateDocumentMock("foo2.css", mockDocumentTextBuffer);
        var consumer = Substitute.For<IIssueConsumer>();
        MockIssueConsumerFactory(textDocument, consumer);
        MockGetDocumentProjectInfoAsync(default);

        CreateTestSubject(textDocument).RequestAnalysis(new AnalyzerOptions());

        VerifyCreateIssueConsumerWasCalled(textDocument, (default, Guid.Empty), consumer, new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    [TestMethod]
    public void RequestAnalysis_ClearsErrorList()
    {
        var textDocument = mockedJavascriptDocumentFooJs;

        testSubject.RequestAnalysis(new AnalyzerOptions());

        threadHandling.Received().RunOnBackgroundThread(Arg.Any<Func<Task<int>>>());
        issueConsumer.Received().SetIssues(textDocument.FilePath, []);
        issueConsumer.Received().SetHotspots(textDocument.FilePath, []);
    }

    private void VerifyAnalysisRequested()
    {
        var textDocument = mockedJavascriptDocumentFooJs;
        issueConsumerStorage.Received().Remove(textDocument.FilePath);
        mockTextSnapshot.Received().GetText();
        mockFileTracker.Received().AddFiles(new SourceFile(textDocument.FilePath, null, TextContent));
        mockAnalysisService.Received().RequestAnalysis(
            new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot));
    }

    private void VerifyAnalysisNotRequested()
    {
        issueConsumerStorage.DidNotReceive().Remove(Arg.Any<string>());
        mockTextSnapshot.DidNotReceive().GetText();
        mockAnalysisService.DidNotReceive().RequestAnalysis(Arg.Any<AnalysisSnapshot>());
    }

    private void MockThreadHandling() => threadHandling.RunOnBackgroundThread(Arg.Any<Func<Task<int>>>()).Returns(info => info.Arg<Func<Task<int>>>()());

    #endregion RequestAnalysis
}
