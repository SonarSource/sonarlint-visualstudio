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

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    /*
     * Note: the TextBufferIssueTracker and TaggerProvider are tightly coupled so it isn't possible
     * to test them completely independently without substantial refactoring.
     * These unit tests are dependent on both classes behaving correctly.
     */

    [TestClass]
    public class TextBufferIssueTrackerTests
    {
        private Mock<ISonarErrorListDataSource> mockSonarErrorDataSource;
        private TaggerProvider taggerProvider;
        private Mock<ITextBuffer> mockDocumentTextBuffer;
        private Mock<ITextDocument> mockedJavascriptDocumentFooJs;
        private Mock<IVsAwareAnalysisService> mockAnalysisService;
        private AnalysisLanguage[] javascriptLanguage = [AnalysisLanguage.Javascript];
        private TextBufferIssueTracker testSubject;
        private TestLogger logger;

        [TestInitialize]
        public void SetUp()
        {
            mockSonarErrorDataSource = new Mock<ISonarErrorListDataSource>();
            taggerProvider = CreateTaggerProvider();
            mockDocumentTextBuffer = CreateTextBufferMock();
            mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js", mockDocumentTextBuffer);
            javascriptLanguage = [AnalysisLanguage.Javascript];
            mockAnalysisService = new Mock<IVsAwareAnalysisService>();
            
            testSubject = CreateTestSubject(mockAnalysisService);
        }

        private TextBufferIssueTracker CreateTestSubject(Mock<IVsAwareAnalysisService> vsAnalysisService = null)
        {
            logger = new TestLogger();
            vsAnalysisService ??= new Mock<IVsAwareAnalysisService>();

            return new TextBufferIssueTracker(taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage,
                mockSonarErrorDataSource.Object, vsAnalysisService.Object, logger);
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
            mockAnalysisService.Setup(x => x.RequestAnalysis(It.IsAny<string>(), It.IsAny<ITextDocument>(), It.IsAny<IEnumerable<AnalysisLanguage>>(),
                    It.IsAny<SnapshotChangedHandler>(), It.IsAny<IAnalyzerOptions>()))
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

            VerifySingletonManagerDoesNotExist(mockDocumentTextBuffer.Object);

            CheckFactoryWasUnregisteredFromDataSource(testSubject.Factory, Times.Once());

            taggerProvider.ActiveTrackersForTesting.Should().BeEmpty();

            mockedJavascriptDocumentFooJs.VerifyRemove(x => x.FileActionOccurred -= It.IsAny<EventHandler<TextDocumentFileActionEventArgs>>(), Times.Once);
        }

        private static void VerifySingletonManagerDoesNotExist(ITextBuffer buffer) =>
            FindSingletonManagerInPropertyCollection(buffer).Should().BeNull();

        private static void VerifySingletonManagerExists(ITextBuffer buffer) =>
            FindSingletonManagerInPropertyCollection(buffer).Should().NotBeNull();

        private static SingletonDisposableTaggerManager<IErrorTag> FindSingletonManagerInPropertyCollection(ITextBuffer buffer)
        {
            buffer.Properties.TryGetProperty<SingletonDisposableTaggerManager<IErrorTag>>(TaggerProvider.SingletonManagerPropertyCollectionKey, out var propertyValue);
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

        #region Triggering analysis tests

        [TestMethod]
        public void WhenFileIsSaved_AnalysisIsRequested()
        {
            mockAnalysisService.Invocations.Clear();

            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            VerifyAnalysisRequested(null);

            // Dispose and raise -> analysis not requested
            testSubject.Dispose();
            mockAnalysisService.Invocations.Clear();

            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            VerifyAnalysisNotRequested();
        }

        [TestMethod]
        public void WhenFileIsLoaded_AnalysisIsNotRequested()
        {
            mockAnalysisService.Invocations.Clear();

            // Act
            RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);
            VerifyAnalysisNotRequested();

            // Sanity check (that the test setup is correct and that events are actually being handled)
            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            VerifyAnalysisRequested(null);
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
            VerifyAnalysisRequested(null);
        }

        [TestMethod]
        public void RequestAnalysis_IssueConsumerIsCreatedAndCleared()
        {
            // Clear the invocations that occurred during construction
            mockAnalysisService.Invocations.Clear();

            var analyzerOptions = Mock.Of<IAnalyzerOptions>();
            testSubject.RequestAnalysis(analyzerOptions);

            VerifyAnalysisRequested(analyzerOptions);
        }

        [TestMethod]
        public void RequestAnalysis_NonCriticalException_IsSuppressed()
        {
            // Clear the invocations that occurred during construction
            mockAnalysisService.Invocations.Clear();
            
            SetUpAnalysisThrows(mockAnalysisService, new InvalidOperationException());

            Action act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
            act.Should().NotThrow();
        }

        [TestMethod]
        public void RequestAnalysis_CriticalException_IsNotSuppressed()
        {
            // Clear the invocations that occurred during construction
            mockAnalysisService.Invocations.Clear();
            
            SetUpAnalysisThrows(mockAnalysisService, new DivideByZeroException("this is a test"));

            Action act = () => testSubject.RequestAnalysis(Mock.Of<IAnalyzerOptions>());
            act.Should().Throw<DivideByZeroException>()
                .And.Message.Should().Be("this is a test");
        }

        private void VerifyAnalysisRequested(IAnalyzerOptions analyzerOptions)
        {
            var textDocument = mockedJavascriptDocumentFooJs.Object;
            mockAnalysisService.Verify(x => x.RequestAnalysis(
                textDocument.FilePath,
                textDocument,
                javascriptLanguage,
                It.IsAny<SnapshotChangedHandler>(),
                analyzerOptions));
        }

        private void VerifyAnalysisNotRequested()
        {
            mockAnalysisService.Verify(x => 
                    x.RequestAnalysis(It.IsAny<string>(),
                        It.IsAny<ITextDocument>(),
                        It.IsAny<IEnumerable<AnalysisLanguage>>(),
                        It.IsAny<SnapshotChangedHandler>(),
                        It.IsAny<IAnalyzerOptions>()),
                Times.Never);
        }
        
        #endregion RequestAnalysis
        
        private TaggerProvider CreateTaggerProvider()
        {
            var tableManagerProviderMock = new Mock<ITableManagerProvider>();
            tableManagerProviderMock.Setup(t => t.GetTableManager(StandardTables.ErrorsTable))
                .Returns(new Mock<ITableManager>().Object);
        
            var textDocFactoryServiceMock = new Mock<ITextDocumentFactoryService>();
        
            var languageRecognizer = Mock.Of<ISonarLanguageRecognizer>();
        
            // // DTE object setup
            // var mockProject = new Mock<Project>();
            // mockProject.Setup(p => p.Name).Returns("MyProject");
            // mockProject.Setup(p => p.FileName).Returns("MyProject.csproj");
            // var project = mockProject.Object;
            //
            // var mockProjectItem = new Mock<ProjectItem>();
            // mockProjectItem.Setup(s => s.ContainingProject).Returns(project);
            // var projectItem = mockProjectItem.Object;
            //
            // mockSolution = new Mock<Solution>();
            // mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>()))
            //     .Returns(projectItem);
            //
            // var mockDTE = new Mock<DTE2>();
            // mockDTE.Setup(d => d.Solution).Returns(mockSolution.Object);
            //
            // var mockVsSolution = new Mock<IVsSolution5>();
            // mockVsSolution.Setup(x => x.GetGuidOfProjectFile("MyProject.csproj"))
            //     .Returns(projectGuid);
        
            var serviceProvider = new ConfigurableServiceProvider();
            // serviceProvider.RegisterService(typeof(SDTE), mockDTE.Object);
            serviceProvider.RegisterService(typeof(IVsStatusbar), Mock.Of<IVsStatusbar>());
            // serviceProvider.RegisterService(typeof(SVsSolution), mockVsSolution.Object);
        
            var mockAnalysisRequester = new Mock<IAnalysisRequester>();
        
            var mockAnalysisScheduler = new Mock<IScheduler>();
            mockAnalysisScheduler.Setup(x => x.Schedule(It.IsAny<string>(), It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Callback((string file, Action<CancellationToken> analyze, int timeout) => analyze(CancellationToken.None));
        
            var provider = new TaggerProvider(mockSonarErrorDataSource.Object, textDocFactoryServiceMock.Object, Mock.Of<IAnalyzerController>(),
                serviceProvider, languageRecognizer, mockAnalysisRequester.Object, Mock.Of<ITaggableBufferIndicator>(), logger);
            return provider;
        }

        private static Mock<ITextBuffer> CreateTextBufferMock()
        {
            // Text buffer with a properties collection and current snapshot
            var mockTextBuffer = new Mock<ITextBuffer>();
        
            var dummyProperties = new PropertyCollection();
            mockTextBuffer.Setup(p => p.Properties).Returns(dummyProperties);
        
            return mockTextBuffer;
        }

        private static Mock<ITextDocument> CreateDocumentMock(string fileName, Mock<ITextBuffer> textBufferMock)
        {
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(d => d.FilePath).Returns(fileName);
            mockTextDocument.Setup(d => d.TextBuffer).Returns(textBufferMock.Object);
            mockTextDocument.SetupAdd(d => d.FileActionOccurred += (sender, args) => { });

            return mockTextDocument;
        }

        // private static Mock<ITextSnapshot> CreateMockTextSnapshot(int lineCount, string textToReturn)
        // {
        //     var mockSnapshotLine = new Mock<ITextSnapshotLine>();
        //     mockSnapshotLine.Setup(x => x.GetText()).Returns(textToReturn);
        //
        //     var mockSnapshot = new Mock<ITextSnapshot>();
        //     mockSnapshot.Setup(x => x.Length).Returns(9999);
        //     mockSnapshot.Setup(x => x.LineCount).Returns(lineCount);
        //     mockSnapshot.Setup(x => x.GetLineFromLineNumber(It.IsAny<int>()))
        //         .Returns(mockSnapshotLine.Object);
        //
        //     return mockSnapshot;
        // }
    }
}
