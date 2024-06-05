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
using EnvDTE;
using EnvDTE80;
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
        private Mock<IAnalyzerController> mockAnalyzerController;
        private TaggerProvider taggerProvider;
        private Mock<ITextBuffer> mockDocumentTextBuffer;
        private Mock<ITextDocument> mockedJavascriptDocumentFooJs;
        private AnalysisLanguage[] javascriptLanguage = { AnalysisLanguage.Javascript };
        private TextBufferIssueTracker testSubject;
        private Mock<Solution> mockSolution;
        private TestLogger logger;
        private Guid projectGuid;

        [TestInitialize]
        public void SetUp()
        {
            mockSonarErrorDataSource = new Mock<ISonarErrorListDataSource>();
            mockAnalyzerController = new Mock<IAnalyzerController>();
            taggerProvider = CreateTaggerProvider();
            mockDocumentTextBuffer = CreateTextBufferMock();
            mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js", mockDocumentTextBuffer.Object);
            javascriptLanguage = new[] { AnalysisLanguage.Javascript };
            projectGuid = Guid.NewGuid();

            testSubject = CreateTextBufferIssueTracker();
        }

        private TextBufferIssueTracker CreateTextBufferIssueTracker(IVsSolution5 vsSolution = null,
            IIssueConsumerFactory issueConsumerFactory = null)
        {
            logger = new TestLogger();
            vsSolution ??= Mock.Of<IVsSolution5>();
            issueConsumerFactory ??= CreateValidIssueConsumerFactory();

            return new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage,
                mockSonarErrorDataSource.Object, vsSolution, issueConsumerFactory, logger,
                new NoOpThreadHandler());
        }

        private static IIssueConsumerFactory CreateValidIssueConsumerFactory()
        {
            var issueConsumerFactory = new Mock<IIssueConsumerFactory>();
            issueConsumerFactory.Setup(x => x.Create(It.IsAny<ITextDocument>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<SnapshotChangedHandler>()))
                .Returns(Mock.Of<IIssueConsumer>());

            return issueConsumerFactory.Object;
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

        [TestMethod]
        public void Ctor_ExceptionInInitialAnalysisRequest_IsNotPropagated()
        {
            var mockVsSolution = new Mock<IVsSolution5>();

            mockVsSolution.Setup(x => x.GetGuidOfProjectFile("MyProject.csproj"))
                // Note: even critical exceptions are not propagated, because the
                // analysis is done via RequestAnalysisAsync().Forget()
                .Throws(new StackOverflowException("this is a test"));

            Action act = () => CreateTextBufferIssueTracker(mockVsSolution.Object);

            act.Should().NotThrow();
            mockVsSolution.Verify(x => x.GetGuidOfProjectFile("MyProject.csproj"), Times.Once);
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
        public void WhenCreated_AnalysisIsRequested()
        {
            mockAnalyzerController.Verify(x => x.ExecuteAnalysis("foo.js", "utf-8",
                new[] { AnalysisLanguage.Javascript }, It.IsAny<IIssueConsumer>(),
                null /* not expecting any options when a new tagger is added */,
                It.IsAny<CancellationToken>(), It.IsAny<Guid>()), Times.Once);
        }

        [TestMethod]
        public void WhenFileIsSaved_AnalysisIsRequested()
        {
            mockAnalyzerController.Invocations.Clear();

            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            CheckAnalysisWasRequested();

            // Dispose and raise -> analysis not requested
            testSubject.Dispose();
            mockAnalyzerController.Invocations.Clear();

            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            CheckAnalysisWasNotRequested();
        }

        [TestMethod]
        public void WhenFileIsLoaded_AnalysisIsNotRequested()
        {
            mockAnalyzerController.Invocations.Clear();

            // Act
            RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);
            CheckAnalysisWasNotRequested();

            // Sanity check (that the test setup is correct and that events are actually being handled)
            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            CheckAnalysisWasRequested();
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

        private void CheckAnalysisWasNotRequested()
        {
            mockAnalyzerController.Verify(x => x.ExecuteAnalysis(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<AnalysisLanguage>>(),
                It.IsAny<IIssueConsumer>(), It.IsAny<IAnalyzerOptions>(), It.IsAny<CancellationToken>(), It.IsAny<Guid>()), Times.Never);
        }

        private void CheckAnalysisWasRequested()
        {
            mockAnalyzerController.Verify(x => x.ExecuteAnalysis(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<AnalysisLanguage>>(),
                It.IsAny<IIssueConsumer>(), It.IsAny<IAnalyzerOptions>(), It.IsAny<CancellationToken>(), It.IsAny<Guid>()), Times.Once);
        }

        #endregion

        #region File without associated project tests

        [TestMethod]
        public void WhenSubjectIsCreated_FileHasNoAssociatedProject_NoExceptionIsThrown()
        {
            mockSolution.Reset();
            mockSolution
                .Setup(x => x.FindProjectItem(mockedJavascriptDocumentFooJs.Name))
                .Returns((ProjectItem)null);

            Action act = () => CreateTextBufferIssueTracker();
            act.Should().NotThrow();
        }

        [TestMethod]
        public void WhenFileIsRenamed_FileHasNoAssociatedProject_NoExceptionIsThrown()
        {
            mockSolution.Reset();
            mockSolution
                .Setup(x => x.FindProjectItem(mockedJavascriptDocumentFooJs.Name))
                .Returns((ProjectItem)null);

            Action act = () => mockedJavascriptDocumentFooJs.Raise(x => x.FileActionOccurred += null, new TextDocumentFileActionEventArgs(
                mockedJavascriptDocumentFooJs.Name, DateTime.Now,
                FileActionTypes.DocumentRenamed));

            act.Should().NotThrow();

            logger.AssertNoOutputMessages();
        }

        #endregion

        #region RequestAnalysis

        [TestMethod]
        public void Create_AnalysisIsRequestedOnCreation()
        {
            var issueConsumer = new Mock<IIssueConsumer>();
            var issueConsumerFactory = CreateIssueConsumerFactory(issueConsumer.Object);

            testSubject = CreateTextBufferIssueTracker(issueConsumerFactory: issueConsumerFactory.Object);

            VerifyNewIssueConsumerWasCreated(issueConsumerFactory);
            VerifyExistingIssuesWereCleared(issueConsumer);
        }

        [TestMethod]
        public async Task RequestAnalysis_IssueConsumerIsCreatedAndCleared()
        {
            var issueConsumer = new Mock<IIssueConsumer>();
            var issueConsumerFactory = CreateIssueConsumerFactory(issueConsumer.Object);

            testSubject = CreateTextBufferIssueTracker(issueConsumerFactory: issueConsumerFactory.Object);

            // Clear the invocations that occurred during construction
            issueConsumer.Invocations.Clear();
            issueConsumerFactory.Invocations.Clear();

            await testSubject.RequestAnalysisAsync(Mock.Of<IAnalyzerOptions>());

            VerifyNewIssueConsumerWasCreated(issueConsumerFactory);
            VerifyExistingIssuesWereCleared(issueConsumer);
        }

        [TestMethod]
        public async Task RequestAnalysis_FileHasNoAssociatedProject_NoExceptionIsThrown()
        {
            mockSolution.Reset();
            mockSolution
                .Setup(x => x.FindProjectItem(mockedJavascriptDocumentFooJs.Name))
                .Returns((ProjectItem)null);

            var issueConsumer = new Mock<IIssueConsumer>();
            var issueConsumerFactory = CreateIssueConsumerFactory(issueConsumer.Object);

            testSubject = CreateTextBufferIssueTracker(issueConsumerFactory: issueConsumerFactory.Object);

            // Clear the invocations that occurred during construction
            issueConsumer.Invocations.Clear();
            issueConsumerFactory.Invocations.Clear();

            await testSubject.RequestAnalysisAsync(Mock.Of<IAnalyzerOptions>());

            issueConsumerFactory.Verify(x => x.Create(mockedJavascriptDocumentFooJs.Object, "{none}",
                Guid.Empty, It.IsAny<SnapshotChangedHandler>()), Times.Once);
        }

        [TestMethod]
        public void RequestAnalysis_NonCriticalException_IsSuppressed()
        {
            var issueConsumer = new Mock<IIssueConsumer>();
            var issueConsumerFactory = CreateIssueConsumerFactory(issueConsumer.Object);

            // Clear the invocations that occurred during construction
            testSubject = CreateTextBufferIssueTracker(issueConsumerFactory: issueConsumerFactory.Object);

            issueConsumer.Setup(x => x.Accept(It.IsAny<string>(), It.IsAny<IEnumerable<IAnalysisIssue>>()))
                .Throws(new InvalidOperationException("this is a test"));

            Func<Task> act = () => testSubject.RequestAnalysisAsync(Mock.Of<IAnalyzerOptions>());
            act.Should().NotThrow();
        }

        [TestMethod]
        public void RequestAnalysis_CriticalException_IsNotSuppressed()
        {
            var issueConsumer = new Mock<IIssueConsumer>();
            var issueConsumerFactory = CreateIssueConsumerFactory(issueConsumer.Object);

            // Clear the invocations that occurred during construction
            testSubject = CreateTextBufferIssueTracker(issueConsumerFactory: issueConsumerFactory.Object);

            issueConsumer.Setup(x => x.Accept(It.IsAny<string>(), It.IsAny<IEnumerable<IAnalysisIssue>>()))
                .Throws(new StackOverflowException("this is a test"));

            Func<Task> act = () => testSubject.RequestAnalysisAsync(Mock.Of<IAnalyzerOptions>());
            act.Should().Throw<StackOverflowException>()
                .And.Message.Should().Be("this is a test");
        }

        private static Mock<IIssueConsumerFactory> CreateIssueConsumerFactory(IIssueConsumer consumerToReturn = null)
        {
            consumerToReturn ??= Mock.Of<IIssueConsumer>();

            var issueConsumerFactory = new Mock<IIssueConsumerFactory>();
            issueConsumerFactory.Setup(x => x.Create(It.IsAny<ITextDocument>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<SnapshotChangedHandler>()))
                .Returns(consumerToReturn);

            return issueConsumerFactory;
        }

        private static void VerifyNewIssueConsumerWasCreated(Mock<IIssueConsumerFactory> issueConsumerFactory)
            => issueConsumerFactory.Verify(x => x.Create(It.IsAny<ITextDocument>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<SnapshotChangedHandler>()), Times.Once);

        private static void VerifyExistingIssuesWereCleared(Mock<IIssueConsumer> issueConsumer)
            // Check that issue consumer has been been cleared by passing an empty list
            => issueConsumer.Verify(x => x.Accept("foo.js", Enumerable.Empty<IAnalysisIssue>()), Times.Once);

        #endregion RequestAnalysis

        private TaggerProvider CreateTaggerProvider()
        {
            var tableManagerProviderMock = new Mock<ITableManagerProvider>();
            tableManagerProviderMock.Setup(t => t.GetTableManager(StandardTables.ErrorsTable))
                .Returns(new Mock<ITableManager>().Object);

            var textDocFactoryServiceMock = new Mock<ITextDocumentFactoryService>();

            var languageRecognizer = Mock.Of<ISonarLanguageRecognizer>();

            // DTE object setup
            var mockProject = new Mock<Project>();
            mockProject.Setup(p => p.Name).Returns("MyProject");
            mockProject.Setup(p => p.FileName).Returns("MyProject.csproj");
            var project = mockProject.Object;

            var mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.Setup(s => s.ContainingProject).Returns(project);
            var projectItem = mockProjectItem.Object;

            mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>()))
                .Returns(projectItem);

            var mockDTE = new Mock<DTE2>();
            mockDTE.Setup(d => d.Solution).Returns(mockSolution.Object);

            var mockVsSolution = new Mock<IVsSolution5>();
            mockVsSolution.Setup(x => x.GetGuidOfProjectFile("MyProject.csproj"))
                .Returns(projectGuid);

            var serviceProvider = new ConfigurableServiceProvider();
            serviceProvider.RegisterService(typeof(SDTE), mockDTE.Object);
            serviceProvider.RegisterService(typeof(IVsStatusbar), Mock.Of<IVsStatusbar>());
            serviceProvider.RegisterService(typeof(SVsSolution), mockVsSolution.Object);

            var mockAnalysisRequester = new Mock<IAnalysisRequester>();

            var mockAnalysisScheduler = new Mock<IScheduler>();
            mockAnalysisScheduler.Setup(x => x.Schedule(It.IsAny<string>(), It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Callback((string file, Action<CancellationToken> analyze, int timeout) => analyze(CancellationToken.None));

            var provider = new TaggerProvider(mockSonarErrorDataSource.Object, textDocFactoryServiceMock.Object, mockAnalyzerController.Object,
                serviceProvider, languageRecognizer, mockAnalysisRequester.Object, Mock.Of<ITaggableBufferIndicator>(), Mock.Of<IIssueConsumerFactory>(), logger, mockAnalysisScheduler.Object, new NoOpThreadHandler());
            return provider;
        }

        private static Mock<ITextBuffer> CreateTextBufferMock()
        {
            // Text buffer with a properties collection and current snapshot
            var mockTextBuffer = new Mock<ITextBuffer>();

            var dummyProperties = new PropertyCollection();
            mockTextBuffer.Setup(p => p.Properties).Returns(dummyProperties);

            var mockSnapshot = CreateMockTextSnapshot(1000, "some text");
            mockTextBuffer.Setup(x => x.CurrentSnapshot).Returns(mockSnapshot.Object);

            return mockTextBuffer;
        }

        private static Mock<ITextDocument> CreateDocumentMock(string fileName, ITextBuffer textBuffer)
        {
            // Create the document and associate the buffer with the it
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(d => d.FilePath).Returns(fileName);
            mockTextDocument.Setup(d => d.Encoding).Returns(Encoding.UTF8);
            mockTextDocument.SetupAdd(d => d.FileActionOccurred += (sender, args) => { });

            mockTextDocument.Setup(x => x.TextBuffer).Returns(textBuffer);

            return mockTextDocument;
        }

        private static Mock<ITextSnapshot> CreateMockTextSnapshot(int lineCount, string textToReturn)
        {
            var mockSnapshotLine = new Mock<ITextSnapshotLine>();
            mockSnapshotLine.Setup(x => x.GetText()).Returns(textToReturn);

            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.Length).Returns(9999);
            mockSnapshot.Setup(x => x.LineCount).Returns(lineCount);
            mockSnapshot.Setup(x => x.GetLineFromLineNumber(It.IsAny<int>()))
                .Returns(mockSnapshotLine.Object);

            return mockSnapshot;
        }
    }
}
