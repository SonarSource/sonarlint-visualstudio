/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.ComponentModel.Composition.Primitives;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Initialization;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger;

[TestClass]
public class TaggerProviderTests
{
    private ISonarErrorListDataSource mockSonarErrorDataSource;
    private TestLogger logger;
    private ISonarLanguageRecognizer mockSonarLanguageRecognizer;
    private ITaggableBufferIndicator mockTaggableBufferIndicator;

    private TaggerProvider testSubject;

    private DummyTextDocumentFactoryService dummyDocumentFactoryService;
    private IServiceProvider serviceProvider;
    private IAnalysisRequester mockAnalysisRequester;
    private IVsProjectInfoProvider vsProjectInfoProvider;
    private IIssueConsumerFactory issueConsumerFactory;
    private IIssueConsumerStorage issueConsumerStorage;
    private IAnalyzer analyzer;
    private IInitializationProcessorFactory initializationProcessorFactory;
    private ICanonicalFilePathProvider canonicalFilePathProvider;
    private IThreadHandling threadHandling;
    private IFileStateManager fileStateManager;

    private static readonly AnalysisLanguage[] DetectedLanguagesJsTs = [AnalysisLanguage.TypeScript, AnalysisLanguage.Javascript];

    [TestInitialize]
    public void SetUp()
    {
        // minimal setup to create a tagger

        mockSonarErrorDataSource = Substitute.For<ISonarErrorListDataSource>();

        var mockServiceProvider = Substitute.For<IServiceProvider>();
        serviceProvider = mockServiceProvider;

        dummyDocumentFactoryService = new DummyTextDocumentFactoryService();

        logger = new TestLogger();

        mockSonarLanguageRecognizer = Substitute.For<ISonarLanguageRecognizer>();

        mockTaggableBufferIndicator = Substitute.For<ITaggableBufferIndicator>();
        mockTaggableBufferIndicator.IsTaggable(Arg.Any<ITextBuffer>()).Returns(true);

        mockAnalysisRequester = Substitute.For<IAnalysisRequester>();

        vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        analyzer = Substitute.For<IAnalyzer>();

        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();

        fileStateManager = Substitute.For<IFileStateManager>();
        canonicalFilePathProvider = Substitute.For<ICanonicalFilePathProvider>();
        canonicalFilePathProvider.GetCanonicalPath(Arg.Any<string>()).Returns(info => info.Arg<string>());

        testSubject = CreateAndInitializeTestSubject();
    }

    #region MEF tests

    [TestMethod]
    public void CheckIsSingletonMefComponent() => MefTestHelpers.CheckIsSingletonMefComponent<TaggerProvider>();

    [TestMethod]
    public void MefCtor_CheckIsExported_TaggerProvider() => MefTestHelpers.CheckTypeCanBeImported<TaggerProvider, ITaggerProvider>(GetRequiredExports());

    [TestMethod]
    public void MefCtor_CheckIsExported_DocumentEvents() => MefTestHelpers.CheckTypeCanBeImported<TaggerProvider, IDocumentTracker>(GetRequiredExports());

    [TestMethod]
    public void MefCtor_Check_SameInstanceExported() => MefTestHelpers.CheckMultipleExportsReturnSameInstance<TaggerProvider, ITaggerProvider, IDocumentTracker>(GetRequiredExports());

    private static Export[] GetRequiredExports() =>
    [
        MefTestHelpers.CreateExport<ISonarErrorListDataSource>(),
        MefTestHelpers.CreateExport<ITextDocumentFactoryService>(),
        MefTestHelpers.CreateExport<SVsServiceProvider>(),
        MefTestHelpers.CreateExport<ISonarLanguageRecognizer>(),
        MefTestHelpers.CreateExport<IAnalysisRequester>(),
        MefTestHelpers.CreateExport<IVsProjectInfoProvider>(),
        MefTestHelpers.CreateExport<IIssueConsumerFactory>(),
        MefTestHelpers.CreateExport<IIssueConsumerStorage>(),
        MefTestHelpers.CreateExport<ITaggableBufferIndicator>(),
        MefTestHelpers.CreateExport<IFileStateManager>(),
        MefTestHelpers.CreateExport<IAnalyzer>(),
        MefTestHelpers.CreateExport<ILogger>(),
        MefTestHelpers.CreateExport<IInitializationProcessorFactory>(),
        MefTestHelpers.CreateExport<ICanonicalFilePathProvider>(),
        MefTestHelpers.CreateExport<IThreadHandling>(),
    ];

    #endregion MEF tests

    [TestMethod]
    public void Ctor_InitializesInCorrectOrder() =>
        Received.InOrder(() =>
        {
            initializationProcessorFactory.Create<TaggerProvider>(Arg.Is<IReadOnlyCollection<IRequireInitialization>>(x => x.Count == 0), Arg.Any<Func<IThreadHandling, Task>>());
            testSubject.InitializationProcessor.InitializeAsync();
            threadHandling.RunOnUIThreadAsync(Arg.Any<Action>());
            mockAnalysisRequester.AnalysisRequested += Arg.Any<EventHandler<EventArgs>>();
            testSubject.InitializationProcessor.InitializeAsync(); // called by testinitialize
        });

    [TestMethod]
    public void CreateTagger_should_create_tracker_when_tagger_is_created()
    {
        var doc = CreateMockedDocument("anyname");
        var tagger = CreateTaggerForDocument(doc);

        tagger.Should().NotBeNull();

        VerifyFileOpenedAndClosedTimes(1, 0);
    }

    [TestMethod]
    public void CreateTagger_should_return_null_when_buffer_is_not_taggable()
    {
        var doc = CreateMockedDocument("anyname");
        mockTaggableBufferIndicator.IsTaggable(doc.TextBuffer).Returns(false);

        var tagger = CreateTaggerForDocument(doc);

        tagger.Should().BeNull();

        mockTaggableBufferIndicator.Received(1).IsTaggable(doc.TextBuffer);
        mockTaggableBufferIndicator.Received(1).IsTaggable(Arg.Any<ITextBuffer>());
    }

    [TestMethod]
    public void CreateTagger_SameDocument_ShouldUseSameSingletonManager()
    {
        var doc1 = CreateMockedDocument("doc1.js");
        var buffer = doc1.TextBuffer;

        VerifySingletonManagerDoesNotExist(buffer);

        // 1. Create first tagger for doc
        var tagger1 = CreateTaggerForDocument(doc1);
        var firstRequestManager = VerifySingletonManagerExists(buffer);

        // 2. Create second tagger for doc
        var tagger2 = CreateTaggerForDocument(doc1);
        var secondRequestManager = VerifySingletonManagerExists(buffer);

        firstRequestManager.Should().BeSameAs(secondRequestManager);
    }

    [TestMethod]
    public void CreateTagger_DifferentDocuments_ShouldUseDifferentSingletonManagers()
    {
        var doc1 = CreateMockedDocument("doc1.js");
        var doc2 = CreateMockedDocument("doc2.js");

        // 1. Create tagger for first doc
        var tagger1 = CreateTaggerForDocument(doc1);
        var doc1Manager = VerifySingletonManagerExists(doc1.TextBuffer);

        // 2. Create tagger for second doc
        var tagger2 = CreateTaggerForDocument(doc2);
        var doc2Manager = VerifySingletonManagerExists(doc2.TextBuffer);

        doc1Manager.Should().NotBeSameAs(doc2Manager);
    }

    [TestMethod]
    public void CreateTagger_should_return_new_tagger_for_already_tracked_file()
    {
        var doc1 = CreateMockedDocument("doc1.js");

        var tagger1 = CreateTaggerForDocument(doc1);
        tagger1.Should().NotBeNull();

        var tagger2 = CreateTaggerForDocument(doc1);
        tagger2.Should().NotBeNull();

        // Taggers should be different but tracker should be the same
        tagger1.Should().NotBeSameAs(tagger2);
        VerifyFileOpenedAndClosedTimes(1, 0);
    }

    [TestMethod]
    public void CreateTagger_CloseLastTagger_ShouldUnregisterTrackerAndRaiseEvent()
    {
        var doc1 = CreateMockedDocument("doc1.js");

        object actualSender = null;
        DocumentEventArgs actualEventArgs = null;
        int eventCount = 0;
        testSubject.DocumentClosed += OnDocumentClosed;

        var tracker1 = CreateTaggerForDocument(doc1);
        VerifyFileOpenedAndClosedTimes(1, 0);

        var tracker2 = CreateTaggerForDocument(doc1);
        VerifyFileOpenedAndClosedTimes(1, 0);

        // Remove one tagger -> tracker should still be registered
        ((IDisposable)tracker1).Dispose();
        VerifyFileOpenedAndClosedTimes(1, 0);
        eventCount.Should().Be(0); // no event yet...

        // Remove the last tagger -> tracker should be unregistered
        ((IDisposable)tracker2).Dispose();
        VerifyFileOpenedAndClosedTimes(1, 1);
        eventCount.Should().Be(1);
        actualSender.Should().BeSameAs(testSubject);
        actualEventArgs.Should().NotBeNull();
        actualEventArgs.Document.FullPath.Should().Be("doc1.js");

        void OnDocumentClosed(object sender, DocumentEventArgs e)
        {
            eventCount++;
            actualSender = sender;
            actualEventArgs = e;
        }
    }

    [TestMethod]
    public void CreateTagger_tracker_should_be_distinct_per_file()
    {
        var doc1 = CreateMockedDocument("foo.js");
        var tagger1 = CreateTaggerForDocument(doc1);
        tagger1.Should().NotBeNull();

        var doc2 = CreateMockedDocument("bar.js");
        var tagger2 = CreateTaggerForDocument(doc2);
        tagger2.Should().NotBeNull();

        VerifyFileOpenedAndClosedTimes(2, 0);
        tagger1.Should().NotBe(tagger2);
    }

    [TestMethod]
    public void AddIssueTracker_AddsNewFileToFileTracker()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        const string filePath = "anyname.js";
        var issueTracker = CreateMockedIssueTracker(filePath);
        issueTracker.DetectedLanguages.Returns(DetectedLanguagesJsTs);
        testSubject.DocumentOpened += eventHandler;

        testSubject.OnDocumentOpened(issueTracker);

        fileStateManager.Received().Opened(issueTracker);
        eventHandler.Received(1).Invoke(Arg.Any<object>(),
            Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == filePath && x.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void IssueTracker_DocumentClosed_RaiseEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        const string filePath = "anyname.js";
        var issueTracker = CreateMockedIssueTracker(filePath);
        issueTracker.DetectedLanguages.Returns(DetectedLanguagesJsTs);
        testSubject.DocumentClosed += eventHandler;

        testSubject.OnDocumentClosed(issueTracker);

        fileStateManager.Received().Closed(issueTracker);
        eventHandler.Received(1).Invoke(Arg.Any<object>(),
            Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == filePath && x.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void IssueTracker_DocumentSaved_UpdatesAnalysisStateAndRaisesEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        const string filePath = "anyname.js";
        var issueTracker = CreateMockedIssueTracker(filePath);
        issueTracker.DetectedLanguages.Returns(DetectedLanguagesJsTs);
        testSubject.DocumentSaved += eventHandler;

        testSubject.OnDocumentSaved(issueTracker);

        fileStateManager.Received().ContentSaved(issueTracker);
        eventHandler.Received(1).Invoke(Arg.Any<object>(),
            Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == filePath && x.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void IssueTracker_DocumentRename_UpdatesAnalysisStateAndRaisesEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentRenamedEventArgs>>();
        const string oldName = "anyname.js";
        const string newName = "newName.js";
        var issueTracker = CreateMockedIssueTracker(newName);
        issueTracker.DetectedLanguages.Returns(DetectedLanguagesJsTs);
        testSubject.OpenDocumentRenamed += eventHandler;

        testSubject.OnOpenDocumentRenamed(issueTracker, oldName);

        fileStateManager.Received().Renamed(issueTracker);
        eventHandler.Received(1).Invoke(Arg.Any<object>(),
            Arg.Is<DocumentRenamedEventArgs>(x => x.Document.FullPath == newName && x.OldFilePath == oldName && x.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void IssueTracker_DocumentUpdated_UpdatesAnalysisState()
    {
        var issueTracker = CreateMockedIssueTracker("any");
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        testSubject.DocumentSaved += eventHandler;

        testSubject.OnDocumentUpdated(issueTracker);

        fileStateManager.ContentChanged(issueTracker);
        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void GetOpenDocuments_ReturnsAmountOfIssueTrackers()
    {
        Document[] documents =
        [
            new("myFile.js", [AnalysisLanguage.Javascript]),
            new("myFile2.cs", [AnalysisLanguage.RoslynFamily]),
            new("myFile3.cpp", [AnalysisLanguage.CFamily])
        ];
        fileStateManager.GetOpenDocuments().Returns(documents);

        var result = testSubject.GetOpenDocuments();

        result.Should().BeEquivalentTo(documents);
    }

    [TestMethod]
    public void AnalysisRequested_CallsAnalysisQueueMultiFileAnalysis()
    {
        mockAnalysisRequester.AnalysisRequested += Raise.EventWith(this, EventArgs.Empty);

        fileStateManager.Received().AnalyzeAllOpenFiles();
    }

    [TestMethod]
    public void AnalysisRequested_Null_CallsAnalysisQueueMultiFileAnalysis()
    {
        mockAnalysisRequester.AnalysisRequested += Raise.EventWith(this, EventArgs.Empty);

        fileStateManager.Received().AnalyzeAllOpenFiles();
    }

    #region Dispose tests

    [TestMethod]
    public void Dispose_MultipleCalls_OnlyDisposesOnce()
    {
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        mockAnalysisRequester.Received(1).AnalysisRequested -= Arg.Any<EventHandler<EventArgs>>();
    }

    #endregion

    private static IFileState CreateMockedIssueTracker(string filePath)
    {
        var mock = Substitute.For<IFileState>();
        mock.FilePath.Returns(filePath);
        return mock;
    }

    private ITagger<IErrorTag> CreateTaggerForDocument(ITextDocument document)
    {
        var mockTextDataModel = Substitute.For<ITextDataModel>();
        mockTextDataModel.DocumentBuffer.Returns(document.TextBuffer);

        return testSubject.CreateTagger<IErrorTag>(document.TextBuffer);
    }

    private ITextDocument CreateMockedDocument(string fileName, IEnumerable<AnalysisLanguage> detectedLanguages = null, string content = null)
    {
        var bufferContentType = Substitute.For<IContentType>();

        // Text buffer with a properties collection and current snapshot
        var mockTextBuffer = Substitute.For<ITextBuffer>();
        mockTextBuffer.ContentType.Returns(bufferContentType);

        var dummyProperties = new PropertyCollection();
        mockTextBuffer.Properties.Returns(dummyProperties);

        var mockSnapshot = Substitute.For<ITextSnapshot>();
        mockSnapshot.Length.Returns(0);
        mockTextBuffer.CurrentSnapshot.Returns(mockSnapshot);
        mockTextBuffer.CurrentSnapshot.GetText().Returns(content);

        // Create the document and associate the buffer with the it
        var mockTextDocument = Substitute.For<ITextDocument>();
        mockTextDocument.FilePath.Returns(fileName);
        mockTextDocument.Encoding.Returns(Encoding.UTF8);

        mockTextDocument.TextBuffer.Returns(mockTextBuffer);

        // Register the buffer-to-doc mapping for the factory service
        dummyDocumentFactoryService.RegisterDocument(mockTextDocument);

        var analysisLanguages = detectedLanguages ?? [AnalysisLanguage.Javascript];

        SetupDetectedLanguages(fileName, bufferContentType, analysisLanguages);

        return mockTextDocument;
    }

    private void SetupDetectedLanguages(string fileName, IContentType bufferContentType, IEnumerable<AnalysisLanguage> detectedLanguages) =>
        mockSonarLanguageRecognizer.Detect(fileName, bufferContentType).Returns(detectedLanguages);

    private static void VerifySingletonManagerDoesNotExist(ITextBuffer buffer) => FindSingletonManagerInPropertyCollection(buffer).Should().BeNull();

    private static SingletonDisposableTaggerManager<IErrorTag> VerifySingletonManagerExists(ITextBuffer buffer)
    {
        var manager = FindSingletonManagerInPropertyCollection(buffer);
        manager.Should().NotBeNull();
        return manager;
    }

    private static SingletonDisposableTaggerManager<IErrorTag> FindSingletonManagerInPropertyCollection(ITextBuffer buffer)
    {
        buffer.Properties.TryGetProperty<SingletonDisposableTaggerManager<IErrorTag>>(TaggerProvider.SingletonManagerPropertyCollectionKey, out var propertyValue);
        return propertyValue;
    }

    private TaggerProvider CreateAndInitializeTestSubject()
    {
        initializationProcessorFactory = MockableInitializationProcessor.CreateFactory<TaggerProvider>(threadHandling, logger);
        var taggerProvider = new TaggerProvider(
            mockSonarErrorDataSource, dummyDocumentFactoryService, serviceProvider,
            mockSonarLanguageRecognizer, mockAnalysisRequester, vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage,
            mockTaggableBufferIndicator, fileStateManager, analyzer, logger, initializationProcessorFactory, canonicalFilePathProvider, threadHandling);
        taggerProvider.InitializationProcessor.InitializeAsync().GetAwaiter().GetResult();
        return taggerProvider;
    }

    private void VerifyFileOpenedAndClosedTimes(int opened, int closed)
    {
        fileStateManager.ReceivedWithAnyArgs(opened).Opened(default);
        fileStateManager.ReceivedWithAnyArgs(closed).Closed(default);
    }


    private class DummyTextDocumentFactoryService : ITextDocumentFactoryService
    {
        private readonly Dictionary<ITextBuffer, ITextDocument> bufferToDocMapping = new Dictionary<ITextBuffer, ITextDocument>();

        public void RegisterDocument(ITextDocument document)
        {
            bufferToDocMapping.Add(document.TextBuffer, document);
        }

        #region Implemented interface methods

        bool ITextDocumentFactoryService.TryGetTextDocument(ITextBuffer textBuffer, out ITextDocument textDocument)
        {
            return bufferToDocMapping.TryGetValue(textBuffer, out textDocument);
        }

        #endregion

        #region Not implemented interface methods

        event EventHandler<TextDocumentEventArgs> ITextDocumentFactoryService.TextDocumentCreated
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        event EventHandler<TextDocumentEventArgs> ITextDocumentFactoryService.TextDocumentDisposed
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        ITextDocument ITextDocumentFactoryService.CreateAndLoadTextDocument(string filePath, IContentType contentType)
        {
            throw new NotImplementedException();
        }

        ITextDocument ITextDocumentFactoryService.CreateAndLoadTextDocument(
            string filePath,
            IContentType contentType,
            Encoding encoding,
            out bool characterSubstitutionsOccurred)
        {
            throw new NotImplementedException();
        }

        ITextDocument ITextDocumentFactoryService.CreateAndLoadTextDocument(
            string filePath,
            IContentType contentType,
            bool attemptUtf8Detection,
            out bool characterSubstitutionsOccurred)
        {
            throw new NotImplementedException();
        }

        ITextDocument ITextDocumentFactoryService.CreateTextDocument(ITextBuffer textBuffer, string filePath)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
