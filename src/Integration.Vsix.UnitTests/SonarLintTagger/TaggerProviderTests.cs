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

using System.ComponentModel.Composition.Primitives;
using System.Text;
using Microsoft.VisualStudio.Shell;
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
public class TaggerProviderTests
{
    private const int AnalysisTimeout = 1000;
    private ISonarErrorListDataSource mockSonarErrorDataSource;
    private TestLogger logger;
    private ISonarLanguageRecognizer mockSonarLanguageRecognizer;
    private ITaggableBufferIndicator mockTaggableBufferIndicator;

    private TaggerProvider provider;

    private DummyTextDocumentFactoryService dummyDocumentFactoryService;
    private IServiceProvider serviceProvider;
    private IAnalysisRequester mockAnalysisRequester;
    private IFileTracker mockFileTracker;
    private IVsProjectInfoProvider vsProjectInfoProvider;
    private IIssueConsumerFactory issueConsumerFactory;
    private IIssueConsumerStorage issueConsumerStorage;
    private IAnalyzer analyzer;

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

        mockFileTracker = Substitute.For<IFileTracker>();

        vsProjectInfoProvider = Substitute.For<IVsProjectInfoProvider>();
        issueConsumerFactory = Substitute.For<IIssueConsumerFactory>();
        issueConsumerStorage = Substitute.For<IIssueConsumerStorage>();
        analyzer = Substitute.For<IAnalyzer>();

        provider = CreateTestSubject();
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
        MefTestHelpers.CreateExport<IFileTracker>(),
        MefTestHelpers.CreateExport<IAnalyzer>(),
        MefTestHelpers.CreateExport<ILogger>()
    ];

    #endregion MEF tests

    [TestMethod]
    public void CreateTagger_should_create_tracker_when_tagger_is_created()
    {
        var doc = CreateMockedDocument("anyname");
        var tagger = CreateTaggerForDocument(doc);

        tagger.Should().NotBeNull();

        VerifyCreateIssueConsumerWasCalled(doc);
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
        provider.ActiveTrackersForTesting.Count().Should().Be(1);
    }

    [TestMethod]
    public void CreateTagger_CloseLastTagger_ShouldUnregisterTrackerAndRaiseEvent()
    {
        var doc1 = CreateMockedDocument("doc1.js");

        object actualSender = null;
        DocumentEventArgs actualEventArgs = null;
        int eventCount = 0;
        provider.DocumentClosed += OnDocumentClosed;

        var tracker1 = CreateTaggerForDocument(doc1);
        provider.ActiveTrackersForTesting.Count().Should().Be(1);

        var tracker2 = CreateTaggerForDocument(doc1);
        provider.ActiveTrackersForTesting.Count().Should().Be(1);

        // Remove one tagger -> tracker should still be registered
        ((IDisposable)tracker1).Dispose();
        provider.ActiveTrackersForTesting.Count().Should().Be(1);
        eventCount.Should().Be(0); // no event yet...

        // Remove the last tagger -> tracker should be unregistered
        ((IDisposable)tracker2).Dispose();
        provider.ActiveTrackersForTesting.Count().Should().Be(0);
        eventCount.Should().Be(1);
        actualSender.Should().BeSameAs(provider);
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
    public void DocEvents_CloseLastTagger_NoListeners_NoError()
    {
        var doc1 = CreateMockedDocument("doc1.js");

        var tracker1 = CreateTaggerForDocument(doc1);
        provider.ActiveTrackersForTesting.Count().Should().Be(1);

        ((IDisposable)tracker1).Dispose();
        provider.ActiveTrackersForTesting.Count().Should().Be(0);
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

        provider.ActiveTrackersForTesting.Count().Should().Be(2);
        tagger1.Should().NotBe(tagger2);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow(new string[] { })]
    public void FilterIssueTrackersByPath_NullOrEmptyPaths_AllTrackersReturned(string[] filePaths)
    {
        var trackers = CreateMockedIssueTrackers("any", "any2");

        var actual = TaggerProvider.FilterIssuesTrackersByPath(trackers, filePaths);

        actual.Should().BeEquivalentTo(trackers);
    }

    [TestMethod]
    public void FilterIssueTrackersByPath_WithPaths_NoMatches_EmptyListReturned()
    {
        var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp");

        var actual = TaggerProvider.FilterIssuesTrackersByPath(trackers,
            new string[] { "no matches", "file1.wrongextension" });

        actual.Should().BeEmpty();
    }

    [TestMethod]
    public void FilterIssueTrackersByPath_WithPaths_SingleMatch_SingleTrackerReturned()
    {
        var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp", "d:\\bbb\\file3.xxx");

        var actual = TaggerProvider.FilterIssuesTrackersByPath(trackers,
            new string[] { "file1.txt" });

        actual.Should().BeEquivalentTo(trackers[0]);
    }

    [TestMethod]
    public void FilterIssueTrackersByPath_WithPaths_MultipleMatches_MultipleTrackersReturned()
    {
        var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp", "d:\\bbb\\file3.xxx");

        var actual = TaggerProvider.FilterIssuesTrackersByPath(trackers,
            new string[]
            {
                "file1.txt", "D:\\BBB\\FILE3.xxx" // match should be case-insensitive
            });

        actual.Should().BeEquivalentTo(trackers[0], trackers[2]);
    }

    [TestMethod]
    public void FilterIssueTrackersByPath_WithPaths_AllMatched_AllTrackersReturned()
    {
        var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp", "d:\\bbb\\file3.xxx");

        var actual = TaggerProvider.FilterIssuesTrackersByPath(trackers,
            new string[] { "unmatchedFile1.cs", "file1.txt", "c:\\aaa\\file2.cpp", "unmatchedfile2.cpp", "d:\\bbb\\file3.xxx" });

        actual.Should().BeEquivalentTo(trackers);
    }

    [TestMethod]
    public void AddIssueTracker_RaisesEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentOpenedEventArgs>>();
        var filePath = "file1.txt";
        var content = "some text";
        var testSubject = CreateTestSubject();
        testSubject.DocumentOpened += eventHandler;
        var issueTracker = CreateMockedIssueTracker(filePath, DetectedLanguagesJsTs, content: content);

        testSubject.AddIssueTracker(issueTracker);

        eventHandler.Received(1).Invoke(testSubject, Arg.Is<DocumentOpenedEventArgs>(e =>
            e.Document.FullPath == filePath &&
            e.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void AddIssueTracker_AddsNewFileToFileTracker()
    {
        var filePath = "file1.txt";
        var content = "some text";
        var testSubject = CreateTestSubject();
        var issueTracker = CreateMockedIssueTracker(filePath, DetectedLanguagesJsTs, content: content);

        testSubject.AddIssueTracker(issueTracker);

        mockFileTracker.Received(1).AddFiles(new SourceFile(filePath, null));
    }

    [TestMethod]
    public void IssueTracker_DocumentClosed_RaiseEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentEventArgs>>();
        var fileName = "anyname.js";
        var doc = CreateMockedDocument(fileName, DetectedLanguagesJsTs);
        provider.DocumentClosed += eventHandler;

        CreateTaggerForDocument(doc);
        provider.ActiveTrackersForTesting.First().Dispose();

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<DocumentEventArgs>(x => x.Document.FullPath == fileName && x.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void IssueTracker_DocumentSaved_RaiseEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentSavedEventArgs>>();
        var fileName = "anyname.js";
        var doc = CreateMockedDocument(fileName, DetectedLanguagesJsTs);
        provider.DocumentSaved += eventHandler;

        CreateTaggerForDocument(doc);
        RaiseFileEvent(doc, FileActionTypes.ContentSavedToDisk);

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<DocumentSavedEventArgs>(x => x.Document.FullPath == fileName && x.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void IssueTracker_DocumentSaved_AddsNewFileToFileTracker()
    {
        var filePath = "anyname.js";
        string content = "new content";
        var doc = CreateMockedDocument(filePath, DetectedLanguagesJsTs, content: content);
        CreateTaggerForDocument(doc);
        mockFileTracker.ClearReceivedCalls();

        RaiseFileEvent(doc, FileActionTypes.ContentSavedToDisk);

        mockFileTracker.Received(1).AddFiles(new SourceFile(filePath, null));
    }

    [TestMethod]
    public void IssueTracker_DocumentRename_RaiseEvent()
    {
        var eventHandler = Substitute.For<EventHandler<DocumentRenamedEventArgs>>();
        var oldName = "anyname.js";
        var newName = "newName.js";
        var doc = CreateMockedDocument(oldName, DetectedLanguagesJsTs);
        provider.OpenDocumentRenamed += eventHandler;

        CreateTaggerForDocument(doc);
        RaiseFileEvent(doc, FileActionTypes.DocumentRenamed, newName);

        eventHandler.Received(1).Invoke(Arg.Any<object>(),
            Arg.Is<DocumentRenamedEventArgs>(x => x.Document.FullPath == newName && x.OldFilePath == oldName && x.Document.DetectedLanguages == DetectedLanguagesJsTs));
    }

    [TestMethod]
    public void GetOpenDocuments_ReturnsAmountOfIssueTrackers()
    {
        provider.AddIssueTracker(CreateMockedIssueTracker("myFile.js", [AnalysisLanguage.Javascript]));
        provider.AddIssueTracker(CreateMockedIssueTracker("myFile2.cs", [AnalysisLanguage.RoslynFamily]));
        provider.AddIssueTracker(CreateMockedIssueTracker("myFile3.cpp", [AnalysisLanguage.CFamily]));

        var result = provider.GetOpenDocuments().ToList();

        result.Should().HaveCount(3);
        result.Should().Contain(x => x.FullPath == "myFile.js" && x.DetectedLanguages.Contains(AnalysisLanguage.Javascript));
        result.Should().Contain(x => x.FullPath == "myFile2.cs" && x.DetectedLanguages.Contains(AnalysisLanguage.RoslynFamily));
        result.Should().Contain(x => x.FullPath == "myFile3.cpp" && x.DetectedLanguages.Contains(AnalysisLanguage.CFamily));
    }

    [TestMethod]
    public void AnalysisRequested_DisplaysCorrectNumberOfDocumentsBeingAnalyzed()
    {
        var fileName = "file.js";
        CreateTaggerForDocument(CreateMockedDocument(fileName, DetectedLanguagesJsTs));
        var analysisExecutingSignal = CreateAnalysisExecutingSignal([fileName]);

        mockAnalysisRequester.AnalysisRequested += Raise.EventWith(this, new AnalysisRequestEventArgs([fileName]));
        analysisExecutingSignal.WaitOne(AnalysisTimeout);

        var expectedMessage = string.Format(Vsix.Resources.Strings.JobRunner_JobDescription_ReaanalyzeDocs, 1);
        logger.AssertPartialOutputStringExists(expectedMessage);
    }

    [TestMethod]
    public void AnalysisRequested_CallsAnalyzerRequestAnalysis()
    {
        List<string> filesToaAnalyze = ["file.js"];
        var content = "content123";
        var analysisExecutingSignal = CreateAnalysisExecutingSignal(filesToaAnalyze);
        CreateTaggerForDocument(CreateMockedDocument(filesToaAnalyze[0], DetectedLanguagesJsTs, content));

        mockAnalysisRequester.AnalysisRequested += Raise.EventWith(this, new AnalysisRequestEventArgs(filesToaAnalyze));
        analysisExecutingSignal.WaitOne(AnalysisTimeout);

        mockFileTracker.Received(1).AddFiles(new SourceFile(filesToaAnalyze[0], new(content, Encoding.UTF8.WebName)));
        analyzer.Received(1).ExecuteAnalysis(Arg.Is<List<string>>(x => x.SequenceEqual(filesToaAnalyze)));
    }

    [TestMethod]
    public void AnalysisRequested_TwoOpenedDocuments_AddFilesToFileTrackerInBatch()
    {
        string[] sourceFiles = ["file.js", "file2.js"];
        CreateTaggerForDocument(CreateMockedDocument(sourceFiles[0], DetectedLanguagesJsTs));
        provider.AddIssueTracker(CreateMockedIssueTracker(sourceFiles[1]));
        mockFileTracker.ClearReceivedCalls();
        var analysisExecutingSignal = CreateAnalysisExecutingSignal(sourceFiles);

        mockAnalysisRequester.AnalysisRequested += Raise.EventWith(this, new AnalysisRequestEventArgs([]));
        analysisExecutingSignal.WaitOne(AnalysisTimeout);

        mockFileTracker.Received(1).AddFiles(Arg.Is<SourceFile[]>(files => files.Length == 2 &&
                                                                           files[0].FilePath == sourceFiles[0] &&
                                                                           files[1].FilePath == sourceFiles[1]));
    }

    private IIssueTracker[] CreateMockedIssueTrackers(params string[] filePaths) => filePaths.Select(CreateMockedIssueTracker).ToArray();

    private static IIssueTracker CreateMockedIssueTracker(string filePath)
    {
        var mock = Substitute.For<IIssueTracker>();
        mock.LastAnalysisFilePath.Returns(filePath);
        return mock;
    }

    private static IIssueTracker CreateMockedIssueTracker(string filePath, IEnumerable<AnalysisLanguage> analysisLanguages, string content = "")
    {
        var mock = CreateMockedIssueTracker(filePath);
        mock.DetectedLanguages.Returns(analysisLanguages);
        mock.GetText().Returns(content);
        return mock;
    }

    private ITagger<IErrorTag> CreateTaggerForDocument(ITextDocument document)
    {
        var mockTextDataModel = Substitute.For<ITextDataModel>();
        mockTextDataModel.DocumentBuffer.Returns(document.TextBuffer);

        return provider.CreateTagger<IErrorTag>(document.TextBuffer);
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

    private TaggerProvider CreateTestSubject() =>
        new(mockSonarErrorDataSource, dummyDocumentFactoryService, serviceProvider,
            mockSonarLanguageRecognizer, mockAnalysisRequester, vsProjectInfoProvider, issueConsumerFactory, issueConsumerStorage,
            mockTaggableBufferIndicator, mockFileTracker, analyzer, logger);

    private static void RaiseFileEvent(ITextDocument textDocument, FileActionTypes actionType, string filePath = null)
    {
        var args = new TextDocumentFileActionEventArgs(filePath ?? textDocument.FilePath, DateTime.UtcNow, actionType);
        textDocument.FileActionOccurred += Raise.EventWith(null, args);
    }

    private void VerifyCreateIssueConsumerWasCalled(
        ITextDocument document)
    {
        vsProjectInfoProvider.Received(1).GetDocumentProjectInfo(document.FilePath);
        issueConsumerFactory.Received(1).Create(document, document.FilePath, Arg.Any<ITextSnapshot>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<SnapshotChangedHandler>());
        issueConsumerStorage.Received(1).Set(document.FilePath, Arg.Any<IIssueConsumer>());
    }

    private ManualResetEvent CreateAnalysisExecutingSignal(IEnumerable<string> filesToAnalyze)
    {
        var manualResetEvent = new ManualResetEvent(false);
        analyzer.When(x => x.ExecuteAnalysis(Arg.Is<List<string>>(y => y.SequenceEqual(filesToAnalyze)))).Do(args =>
        {
            manualResetEvent.Set();
        });
        return manualResetEvent;
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
