/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
    private Mock<IVsAwareAnalysisService> mockAnalysisService;
    private Mock<ITextSnapshot> mockTextSnapshot;
    private Mock<ITextBuffer> mockDocumentTextBuffer;
    private Mock<ITextDocument> mockedJavascriptDocumentFooJs;
    private Mock<ISonarErrorListDataSource> mockSonarErrorDataSource;
    private Mock<IFileTracker> mockFileTracker;
    private TaggerProvider taggerProvider;
    private TextBufferIssueTracker testSubject;

    [TestInitialize]
    public void SetUp()
    {
        mockSonarErrorDataSource = new Mock<ISonarErrorListDataSource>();
        mockAnalysisService = new Mock<IVsAwareAnalysisService>();
        mockFileTracker = new Mock<IFileTracker>();
        taggerProvider = CreateTaggerProvider();
        mockTextSnapshot = CreateTextSnapshotMock();
        mockDocumentTextBuffer = CreateTextBufferMock(mockTextSnapshot);
        mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js", mockDocumentTextBuffer);
        javascriptLanguage = [AnalysisLanguage.Javascript];

        testSubject = CreateTestSubject(mockAnalysisService);
    }

    private TextBufferIssueTracker CreateTestSubject(Mock<IVsAwareAnalysisService> vsAnalysisService = null)
    {
        logger = new TestLogger();
        vsAnalysisService ??= new Mock<IVsAwareAnalysisService>();

        return new TextBufferIssueTracker(taggerProvider,
            mockedJavascriptDocumentFooJs.Object, javascriptLanguage,
            mockSonarErrorDataSource.Object, vsAnalysisService.Object, 
            mockFileTracker.Object, logger);
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
        CheckFactoryWasRegisteredWithDataSource(testSubject.Factory, Times.Once());

        taggerProvider.ActiveTrackersForTesting.Should().BeEquivalentTo(testSubject);

        mockedJavascriptDocumentFooJs.VerifyAdd(x => x.FileActionOccurred += It.IsAny<EventHandler<TextDocumentFileActionEventArgs>>(), Times.Once);

        // Note: the test subject isn't responsible for adding the entry to the buffer.Properties
        // - that's done by the TaggerProvider.
    }

    [TestMethod]
    public void Ctor_CreatesFactoryWithCorrectData()
    {
        var issuesSnapshot = testSubject.Factory.CurrentSnapshot;
        issuesSnapshot.Should().NotBeNull();

        issuesSnapshot.AnalyzedFilePath.Should().Be(mockedJavascriptDocumentFooJs.Object.FilePath);
        issuesSnapshot.Issues.Count().Should().Be(0);
    }

    private static void SetUpAnalysisThrows(Mock<IVsAwareAnalysisService> mockAnalysisService, Exception exception)
    {
        mockAnalysisService.Setup(x => x.RequestAnalysis(It.IsAny<ITextDocument>(),
                It.IsAny<AnalysisSnapshot>(),
                It.IsAny<IEnumerable<AnalysisLanguage>>(),
                It.IsAny<SnapshotChangedHandler>(),
                It.IsAny<IAnalyzerOptions>()))
            .Throws(exception);
    }

    [TestMethod]
    public void Dispose_CleansUpEventsAndRegistrations()
    {
        // Sanity checks
        var singletonManager = new SingletonDisposableTaggerManager<IErrorTag>(null);
        mockDocumentTextBuffer.Object.Properties.AddProperty(TaggerProvider.SingletonManagerPropertyCollectionKey, singletonManager);
        VerifySingletonManagerExists(mockDocumentTextBuffer.Object);
        CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, Times.Never());

        // Act
        testSubject.Dispose();
        
        mockAnalysisService.Verify(x => x.CancelForFile(mockedJavascriptDocumentFooJs.Object.FilePath));

        VerifySingletonManagerDoesNotExist(mockDocumentTextBuffer.Object);

        CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, Times.Once());

        taggerProvider.ActiveTrackersForTesting.Should().BeEmpty();

        mockedJavascriptDocumentFooJs.VerifyRemove(x => x.FileActionOccurred -= It.IsAny<EventHandler<TextDocumentFileActionEventArgs>>(),
            Times.Once);
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

    private void CheckFactoryWasRegisteredWithDataSource(IssuesSnapshotFactory factory, Times times)
    {
        mockSonarErrorDataSource.Verify(x => x.AddFactory(factory), times);
    }

    private void CheckFactoryWasUnregisteredFromDataSource(IssuesSnapshotFactory factory, Times times)
    {
        mockSonarErrorDataSource.Verify(x => x.RemoveFactory(factory), times);
    }

    private TaggerProvider CreateTaggerProvider()
    {
        var tableManagerProviderMock = new Mock<ITableManagerProvider>();
        tableManagerProviderMock.Setup(t => t.GetTableManager(StandardTables.ErrorsTable))
            .Returns(new Mock<ITableManager>().Object);

        var textDocFactoryServiceMock = new Mock<ITextDocumentFactoryService>();

        var languageRecognizer = Mock.Of<ISonarLanguageRecognizer>();
        
        var serviceProvider = new ConfigurableServiceProvider();
        serviceProvider.RegisterService(typeof(IVsStatusbar), Mock.Of<IVsStatusbar>());

        var mockAnalysisRequester = new Mock<IAnalysisRequester>();

        var mockAnalysisScheduler = new Mock<IScheduler>();
        mockAnalysisScheduler.Setup(x => x.Schedule(It.IsAny<string>(), It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
            .Callback((string file, Action<CancellationToken> analyze, int timeout) => analyze(CancellationToken.None));

        var sonarErrorListDataSource = mockSonarErrorDataSource.Object;
        var textDocumentFactoryService = textDocFactoryServiceMock.Object;
        var vsAwareAnalysisService = mockAnalysisService.Object;
        var analysisRequester = mockAnalysisRequester.Object;
        var provider = new TaggerProvider(sonarErrorListDataSource, textDocumentFactoryService, 
            serviceProvider, languageRecognizer, vsAwareAnalysisService, analysisRequester, Mock.Of<ITaggableBufferIndicator>(), mockFileTracker.Object, logger);
        return provider;
    }
    
    
    private static Mock<ITextSnapshot>CreateTextSnapshotMock()
    {
        var textSnapshot = new Mock<ITextSnapshot>();
        textSnapshot.Setup(x => x.GetText()).Returns(TextContent);
        return textSnapshot;
    }


    private static Mock<ITextBuffer> CreateTextBufferMock(Mock<ITextSnapshot> textSnapshot)
    {
        // Text buffer with a properties collection and current snapshot
        var mockTextBuffer = new Mock<ITextBuffer>();

        var dummyProperties = new PropertyCollection();
        mockTextBuffer.Setup(p => p.Properties).Returns(dummyProperties);
        
        mockTextBuffer.Setup(p => p.CurrentSnapshot).Returns(textSnapshot.Object);

        return mockTextBuffer;
    }

    private static Mock<ITextDocument> CreateDocumentMock(string fileName, Mock<ITextBuffer> textBufferMock)
    {
        var mockTextDocument = new Mock<ITextDocument>();
        mockTextDocument.Setup(d => d.FilePath).Returns(fileName);
        mockTextDocument.Setup(d => d.TextBuffer).Returns(textBufferMock.Object);
        mockTextDocument.Setup(d => d.Encoding).Returns(Encoding.UTF8);
        mockTextDocument.SetupAdd(d => d.FileActionOccurred += (sender, args) => { });

        return mockTextDocument;
    }

    #region Triggering analysis tests

    [TestMethod]
    public void WhenFileIsSaved_AnalysisIsRequested()
    {
        mockAnalysisService.Invocations.Clear();
        mockTextSnapshot.Invocations.Clear();

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisRequestedWithDefaultOptions(false);

        // Dispose and raise -> analysis not requested
        testSubject.Dispose();
        mockAnalysisService.Invocations.Clear();
        mockTextSnapshot.Invocations.Clear();

        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisNotRequested();
    }

    [TestMethod]
    public void WhenFileIsLoaded_AnalysisIsNotRequested()
    {
        mockAnalysisService.Invocations.Clear();
        mockTextSnapshot.Invocations.Clear();

        // Act
        RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisNotRequested();

        // Sanity check (that the test setup is correct and that events are actually being handled)
        RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
        VerifyAnalysisRequestedWithDefaultOptions(false);
    }

    private static void RaiseFileSavedEvent(Mock<ITextDocument> mockDocument)
    {
        var args = new TextDocumentFileActionEventArgs(mockDocument.Object.FilePath, DateTime.UtcNow, FileActionTypes.ContentSavedToDisk);
        mockDocument.Raise(x => x.FileActionOccurred += null, args);
    }

    private static void RaiseFileLoadedEvent(Mock<ITextDocument> mockDocument)
    {
        var args = new TextDocumentFileActionEventArgs(mockDocument.Object.FilePath, DateTime.UtcNow, FileActionTypes.ContentLoadedFromDisk);
        mockDocument.Raise(x => x.FileActionOccurred += null, args);
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
        mockAnalysisService.Invocations.Clear();

        var analyzerOptions = Mock.Of<IAnalyzerOptions>();
        testSubject.RequestAnalysis(analyzerOptions);

        
        VerifyAnalysisRequested(analyzerOptions);
    }
    
    [TestMethod]
    public void RequestAnalysis_DocumentRenamed_CancelsForPreviousFilePath()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.Invocations.Clear();
        mockedJavascriptDocumentFooJs.SetupSequence(x => x.FilePath)
            .Returns("newFoo.js");

        var analyzerOptions = Mock.Of<IAnalyzerOptions>();
        testSubject.RequestAnalysis(analyzerOptions);

        mockAnalysisService.Verify(x => x.CancelForFile("foo.js"));
        mockAnalysisService.Verify(x => x.RequestAnalysis(It.IsAny<ITextDocument>(),
            It.Is<AnalysisSnapshot>(x => x.FilePath == "newFoo.js"),
            It.IsAny<IEnumerable<AnalysisLanguage>>(),
            It.IsAny<SnapshotChangedHandler>(),
            It.IsAny<IAnalyzerOptions>()));
    }

    [TestMethod]
    public void RequestAnalysis_NonCriticalException_IsSuppressed()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.Invocations.Clear();

        SetUpAnalysisThrows(mockAnalysisService, new InvalidOperationException());

        var act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
        act.Should().NotThrow();

        logger.AssertPartialOutputStringExists("[Analysis] Error triggering analysis: ");
    }

    [TestMethod]
    public void RequestAnalysis_NotSupportedException_IsSuppressedAndLogged()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.Invocations.Clear();

        SetUpAnalysisThrows(mockAnalysisService, new NotSupportedException("This is not supported"));

        var act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
        act.Should().NotThrow();

        logger.AssertOutputStringExists("[Analysis] Unable to analyze: This is not supported");
    }

    [TestMethod]
    public void RequestAnalysis_CriticalException_IsNotSuppressed()
    {
        // Clear the invocations that occurred during construction
        mockAnalysisService.Invocations.Clear();

        SetUpAnalysisThrows(mockAnalysisService, new DivideByZeroException("this is a test"));

        var act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
        act.Should().Throw<DivideByZeroException>()
            .WithMessage("this is a test");
    }
    
    private void VerifyAnalysisRequested(IAnalyzerOptions analyzerOptions)
    {
        var textDocument = mockedJavascriptDocumentFooJs.Object;
        mockAnalysisService.Verify(x => x.CancelForFile(textDocument.FilePath));
        mockTextSnapshot.Verify(x => x.GetText());
        mockFileTracker.Verify(x => x.AddFiles(new SourceFile(textDocument.FilePath, null, TextContent)));
        mockAnalysisService.Verify(x => x.RequestAnalysis(
            textDocument,
            new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot, textDocument.Encoding),
            javascriptLanguage,
            It.IsAny<SnapshotChangedHandler>(),
            analyzerOptions));
    }
    
    private void VerifyAnalysisRequestedWithDefaultOptions(bool isOnOpen)
    {
        var textDocument = mockedJavascriptDocumentFooJs.Object;
        mockAnalysisService.Verify(x => x.CancelForFile(textDocument.FilePath));
        mockTextSnapshot.Verify(x => x.GetText());
        mockFileTracker.Verify(x => x.AddFiles(new SourceFile(textDocument.FilePath, null, TextContent)));
        mockAnalysisService.Verify(x => x.RequestAnalysis(
            textDocument,
            new AnalysisSnapshot(textDocument.FilePath, textDocument.TextBuffer.CurrentSnapshot, textDocument.Encoding),
            javascriptLanguage,
            It.IsAny<SnapshotChangedHandler>(),
            It.Is<IAnalyzerOptions>(o => o.IsOnOpen == isOnOpen)));
    }

    private void VerifyAnalysisNotRequested()
    {
        mockAnalysisService.Verify(x => x.CancelForFile(It.IsAny<string>()), Times.Never);
        mockTextSnapshot.Verify(x => x.GetText(), Times.Never);
        mockAnalysisService.Verify(x =>
                x.RequestAnalysis(It.IsAny<ITextDocument>(),
                    It.IsAny<AnalysisSnapshot>(),
                    It.IsAny<IEnumerable<AnalysisLanguage>>(),
                    It.IsAny<SnapshotChangedHandler>(),
                    It.IsAny<IAnalyzerOptions>()),
            Times.Never);
    }

    #endregion RequestAnalysis
}
