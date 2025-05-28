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
    private ISonarErrorListDataSource mockSonarErrorDataSource;
    private IVsAwareAnalysisService mockAnalysisService;
    private TestLogger logger;
    private ISonarLanguageRecognizer mockSonarLanguageRecognizer;
    private ITaggableBufferIndicator mockTaggableBufferIndicator;

    private TaggerProvider provider;

    private DummyTextDocumentFactoryService dummyDocumentFactoryService;
    private IServiceProvider serviceProvider;
    private IAnalysisRequester mockAnalysisRequester;
    private IFileTracker mockFileTracker;

    [TestInitialize]
    public void SetUp()
    {
        // minimal setup to create a tagger

        mockSonarErrorDataSource = Substitute.For<ISonarErrorListDataSource>();
        mockAnalysisService = Substitute.For<IVsAwareAnalysisService>();

        var mockServiceProvider = Substitute.For<IServiceProvider>();
        serviceProvider = mockServiceProvider;

        dummyDocumentFactoryService = new DummyTextDocumentFactoryService();

        logger = new TestLogger();

        mockSonarLanguageRecognizer = Substitute.For<ISonarLanguageRecognizer>();

        mockTaggableBufferIndicator = Substitute.For<ITaggableBufferIndicator>();
        mockTaggableBufferIndicator.IsTaggable(Arg.Any<ITextBuffer>()).Returns(true);

        mockAnalysisRequester = Substitute.For<IAnalysisRequester>();

        mockFileTracker = Substitute.For<IFileTracker>();

        provider = CreateTestSubject();
    }

    #region MEF tests

    [TestMethod]
    public void CheckIsSingletonMefComponent() => MefTestHelpers.CheckIsSingletonMefComponent<TaggerProvider>();

    [TestMethod]
    public void MefCtor_CheckIsExported_TaggerProvider() => MefTestHelpers.CheckTypeCanBeImported<TaggerProvider, ITaggerProvider>(GetRequiredExports());

    [TestMethod]
    public void MefCtor_CheckIsExported_DocumentEvents() => MefTestHelpers.CheckTypeCanBeImported<TaggerProvider, IDocumentEvents>(GetRequiredExports());

    [TestMethod]
    public void MefCtor_Check_SameInstanceExported() => MefTestHelpers.CheckMultipleExportsReturnSameInstance<TaggerProvider, ITaggerProvider, IDocumentEvents>(GetRequiredExports());

    private static Export[] GetRequiredExports() =>
    [
        MefTestHelpers.CreateExport<ISonarErrorListDataSource>(),
        MefTestHelpers.CreateExport<ITextDocumentFactoryService>(),
        MefTestHelpers.CreateExport<SVsServiceProvider>(),
        MefTestHelpers.CreateExport<ISonarLanguageRecognizer>(),
        MefTestHelpers.CreateExport<IVsAwareAnalysisService>(),
        MefTestHelpers.CreateExport<IAnalysisRequester>(),
        MefTestHelpers.CreateExport<ITaggableBufferIndicator>(),
        MefTestHelpers.CreateExport<IFileTracker>(),
        MefTestHelpers.CreateExport<ILogger>()
    ];

    #endregion MEF tests

    [TestMethod]
    public void CreateTagger_should_create_tracker_when_analysis_is_requested()
    {
        var doc = CreateMockedDocument("anyname");
        var tagger = CreateTaggerForDocument(doc);

        tagger.Should().NotBeNull();

        VerifyAnalysisWasRequested();
        mockAnalysisService.ReceivedCalls().Should().HaveCount(2); // no other calls
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
        DocumentClosedEventArgs actualEventArgs = null;
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
        actualEventArgs.FullPath.Should().Be("doc1.js");

        void OnDocumentClosed(object sender, DocumentClosedEventArgs e)
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

    private IIssueTracker[] CreateMockedIssueTrackers(params string[] filePaths) => filePaths.Select(x => CreateMockedIssueTracker(x)).ToArray();

    private static IIssueTracker CreateMockedIssueTracker(string filePath)
    {
        var mock = Substitute.For<IIssueTracker>();
        mock.LastAnalysisFilePath.Returns(filePath);
        return mock;
    }

    private ITagger<IErrorTag> CreateTaggerForDocument(ITextDocument document)
    {
        var mockTextDataModel = Substitute.For<ITextDataModel>();
        mockTextDataModel.DocumentBuffer.Returns(document.TextBuffer);

        return provider.CreateTagger<IErrorTag>(document.TextBuffer);
    }

    private ITextDocument CreateMockedDocument(string fileName)
    {
        var bufferContentType = Mock.Of<IContentType>();

        // Text buffer with a properties collection and current snapshot
        var mockTextBuffer = Substitute.For<ITextBuffer>();
        mockTextBuffer.ContentType.Returns(bufferContentType);

        var dummyProperties = new PropertyCollection();
        mockTextBuffer.Properties.Returns(dummyProperties);

        var mockSnapshot = Substitute.For<ITextSnapshot>();
        mockSnapshot.Length.Returns(0);
        mockTextBuffer.CurrentSnapshot.Returns(mockSnapshot);

        // Create the document and associate the buffer with the it
        var mockTextDocument = Substitute.For<ITextDocument>();
        mockTextDocument.FilePath.Returns(fileName);
        mockTextDocument.Encoding.Returns(Encoding.UTF8);

        mockTextDocument.TextBuffer.Returns(mockTextBuffer);

        // Register the buffer-to-doc mapping for the factory service
        dummyDocumentFactoryService.RegisterDocument(mockTextDocument);

        var analysisLanguages = new[] { AnalysisLanguage.Javascript };

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

    private void VerifyAnalysisWasRequested()
    {
        mockAnalysisService.Received().CancelForFile(Arg.Any<string>());
        mockAnalysisService.Received().RequestAnalysis(Arg.Any<ITextDocument>(), Arg.Any<AnalysisSnapshot>(), Arg.Any<IEnumerable<AnalysisLanguage>>(), Arg.Any<SnapshotChangedHandler>(),
            Arg.Any<IAnalyzerOptions>());
    }

    private TaggerProvider CreateTestSubject() =>
        new(mockSonarErrorDataSource, dummyDocumentFactoryService, serviceProvider,
            mockSonarLanguageRecognizer, mockAnalysisService, mockAnalysisRequester,
            mockTaggableBufferIndicator, mockFileTracker, logger);

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
