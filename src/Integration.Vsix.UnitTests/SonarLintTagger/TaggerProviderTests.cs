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

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Text;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /*
     * Note: the TextBufferIssueTracker and TaggerProvider are tightly coupled so it isn't possible
     * to test them completely independently without substantial refactoring.
     * These unit tests are dependent on both classes behaving correctly.
     */

    [TestClass]
    public class TaggerProviderTests
    {
        private Mock<ISonarErrorListDataSource> mockSonarErrorDataSource;
        private Mock<IAnalyzerController> mockAnalyzerController;
        private TestLogger logger;
        private Mock<IScheduler> mockAnalysisScheduler;
        private Mock<ISonarLanguageRecognizer> mockSonarLanguageRecognizer;
        private Mock<ITaggableBufferIndicator> mockTaggableBufferIndicator;

        private TaggerProvider provider;

        private DummyTextDocumentFactoryService dummyDocumentFactoryService;

        [TestInitialize]
        public void SetUp()
        {
            // minimal setup to create a tagger

            mockSonarErrorDataSource = new Mock<ISonarErrorListDataSource>();

            mockAnalyzerController = new Mock<IAnalyzerController>();

            var mockProject = new Mock<Project>();
            mockProject.Setup(p => p.Name).Returns("MyProject");
            var project = mockProject.Object;

            var mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.Setup(s => s.ContainingProject).Returns(project);
            var projectItem = mockProjectItem.Object;

            var mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>())).Returns(projectItem);
            var solution = mockSolution.Object;

            var mockDTE = new Mock<DTE2>();
            mockDTE.Setup(d => d.Solution).Returns(solution);
            var dte = mockDTE.Object;

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(SDTE))).Returns(dte);
            mockServiceProvider.Setup(s=> s.GetService(typeof(SVsSolution))).Returns(Mock.Of<IVsSolution5>());

            var serviceProvider = mockServiceProvider.Object;

            dummyDocumentFactoryService = new DummyTextDocumentFactoryService();

            logger = new TestLogger();

            mockSonarLanguageRecognizer = new Mock<ISonarLanguageRecognizer>();

            mockTaggableBufferIndicator = new Mock<ITaggableBufferIndicator>();
            mockTaggableBufferIndicator.Setup(x => x.IsTaggable(It.IsAny<ITextBuffer>())).Returns(true);

            var mockAnalysisRequester = new Mock<IAnalysisRequester>();

            mockAnalysisScheduler = new Mock<IScheduler>();
            mockAnalysisScheduler.Setup(x => x.Schedule(It.IsAny<string>(), It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Callback((string file, Action<CancellationToken> analyze, int timeout) => analyze(CancellationToken.None));

            var issueConsumerFactory = new Mock<IIssueConsumerFactory>();
            issueConsumerFactory.Setup(x => x.Create(It.IsAny<ITextDocument>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<SnapshotChangedHandler>()))
                .Returns(Mock.Of<IIssueConsumer>());

            this.provider = new TaggerProvider(mockSonarErrorDataSource.Object, dummyDocumentFactoryService, mockAnalyzerController.Object, serviceProvider,
                mockSonarLanguageRecognizer.Object, mockAnalysisRequester.Object, mockTaggableBufferIndicator.Object, issueConsumerFactory.Object, logger, mockAnalysisScheduler.Object, new NoOpThreadHandler());
        }


        #region MEF tests

        [TestMethod]
        public void CheckIsSingletonMefComponent()
            => MefTestHelpers.CheckIsSingletonMefComponent<TaggerProvider>();

        [TestMethod]
        public void MefCtor_CheckIsExported_TaggerProvider()
            => MefTestHelpers.CheckTypeCanBeImported<TaggerProvider, ITaggerProvider>(GetRequiredExports());

        [TestMethod]
        public void MefCtor_CheckIsExported_DocumentEvents()
            => MefTestHelpers.CheckTypeCanBeImported<TaggerProvider, IDocumentEvents>(GetRequiredExports());

        [TestMethod]
        public void MefCtor_Check_SameInstanceExported()
            => MefTestHelpers.CheckMultipleExportsReturnSameInstance<TaggerProvider, ITaggerProvider, IDocumentEvents>(GetRequiredExports());

        private static Export[] GetRequiredExports() => new[]
        {
            MefTestHelpers.CreateExport<ISonarErrorListDataSource>(),
            MefTestHelpers.CreateExport<ITextDocumentFactoryService>(),
            MefTestHelpers.CreateExport<IAnalyzerController>(),
            MefTestHelpers.CreateExport<SVsServiceProvider>(),
            MefTestHelpers.CreateExport<ISonarLanguageRecognizer>(),
            MefTestHelpers.CreateExport<IAnalysisRequester>(),
            MefTestHelpers.CreateExport<ITaggableBufferIndicator>(),
            MefTestHelpers.CreateExport<IIssueConsumerFactory>(),
            MefTestHelpers.CreateExport<ILogger>(),
            MefTestHelpers.CreateExport<IScheduler>(),
            MefTestHelpers.CreateExport<IThreadHandling>()
        };

        #endregion MEF tests

        [TestMethod]
        public void CreateTagger_should_create_tracker_when_analysis_is_supported()
        {
            var doc = CreateMockedDocument("anyname", isDetectable: true);
            var tagger = CreateTaggerForDocument(doc);

            tagger.Should().NotBeNull();

            VerifyCheckedAnalysisIsSupported();
            VerifyAnalysisWasRequested();
            mockAnalyzerController.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_when_analysis_is_not_supported()
        { 
            var doc = CreateMockedDocument("anyname", isDetectable: false);
            var tagger = CreateTaggerForDocument(doc);

            tagger.Should().BeNull();

            VerifyCheckedAnalysisIsSupported();
            mockAnalyzerController.VerifyNoOtherCalls();
        }


        [TestMethod]
        public void CreateTagger_should_return_null_when_buffer_is_not_taggable()
        {
            var doc = CreateMockedDocument("anyname", isDetectable: true);
            mockTaggableBufferIndicator.Setup(x => x.IsTaggable(doc.TextBuffer)).Returns(false);;

            var tagger = CreateTaggerForDocument(doc);

            tagger.Should().BeNull();

            mockTaggableBufferIndicator.Verify(x=> x.IsTaggable(doc.TextBuffer), Times.Once);
            mockTaggableBufferIndicator.Verify(x=> x.IsTaggable(It.IsAny<ITextBuffer>()), Times.Once);
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
        public void RequestAnalysis_Should_NotThrow_When_AnalysisFails()
        {
            mockAnalysisScheduler
                .Setup(x => x.Schedule("doc1.js", It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Throws<Exception>();

            Action act = () =>
                provider.RequestAnalysis("doc1.js", "", new[] { AnalysisLanguage.CFamily }, null, null);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void RequestAnalysis_NotSupportedException_IsLoggedWithoutCallStack()
        {
            var ex = new NotSupportedException("thrown in a test");

            mockAnalysisScheduler
                .Setup(x => x.Schedule("doc1.js", It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Throws(ex);

            provider.RequestAnalysis("doc1.js", "", new[] { AnalysisLanguage.CFamily }, null, null);

            // Note: checking for an exact string here to be sure that the call stack is not included
            logger.AssertOutputStringExists("Unable to analyze: thrown in a test");
            logger.AssertPartialOutputStringDoesNotExist(ex.ToString());
        }

        [TestMethod]
        public void RequestAnalysis_InvalidOperationException_IsLoggedWithCallStack()
        {
            var ex = new InvalidOperationException("thrown in a test");

            mockAnalysisScheduler
                .Setup(x => x.Schedule("doc1.js", It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Throws(ex);

            provider.RequestAnalysis("doc1.js", "", new[] { AnalysisLanguage.CFamily }, null, null);

            logger.AssertPartialOutputStringExists(ex.ToString());
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
                    "file1.txt",
                    "D:\\BBB\\FILE3.xxx" // match should be case-insensitive
                });

            actual.Should().BeEquivalentTo(trackers[0], trackers[2]);
        }

        [TestMethod]
        public void FilterIssueTrackersByPath_WithPaths_AllMatched_AllTrackersReturned()
        {
            var trackers = CreateMockedIssueTrackers("file1.txt", "c:\\aaa\\file2.cpp", "d:\\bbb\\file3.xxx");

            var actual = TaggerProvider.FilterIssuesTrackersByPath(trackers,
                new string[]
                {
                    "unmatchedFile1.cs",
                    "file1.txt",
                    "c:\\aaa\\file2.cpp",
                    "unmatchedfile2.cpp",
                    "d:\\bbb\\file3.xxx"
                });

            actual.Should().BeEquivalentTo(trackers);
        }

        [TestMethod]
        [DataRow(-1, TaggerProvider.DefaultAnalysisTimeoutMs)]
        [DataRow(0, TaggerProvider.DefaultAnalysisTimeoutMs)]
        [DataRow(1, 1)]
        [DataRow(999, 999)]
        public void AnalysisTimeout(int envSettingsResponse, int expectedTimeout)
        {
            var envSettingsMock = new Mock<IEnvironmentSettings>();
            envSettingsMock.Setup(x => x.AnalysisTimeoutInMs()).Returns(envSettingsResponse);

            TaggerProvider.GetAnalysisTimeoutInMilliseconds(envSettingsMock.Object).Should().Be(expectedTimeout);
        }

        [TestMethod]
        public void AnalysisTimeoutInMilliseconds_NoEnvironmentSettings_DefaultTimeout()
        {
            TaggerProvider.GetAnalysisTimeoutInMilliseconds().Should().Be(TaggerProvider.DefaultAnalysisTimeoutMs);
        }

        private IIssueTracker[] CreateMockedIssueTrackers(params string[] filePaths) =>
            filePaths.Select(x => CreateMockedIssueTracker(x)).ToArray();

        private static IIssueTracker CreateMockedIssueTracker(string filePath)
        {
            var mock = new Mock<IIssueTracker>();
            mock.Setup(x => x.FilePath).Returns(filePath);
            return mock.Object;
        }

        private ITagger<IErrorTag> CreateTaggerForDocument(ITextDocument document)
        {
            var mockTextDataModel = new Mock<ITextDataModel>();
            mockTextDataModel.Setup(x => x.DocumentBuffer).Returns(document.TextBuffer);

            return provider.CreateTagger<IErrorTag>(document.TextBuffer);
        }

        private ITextDocument CreateMockedDocument(string fileName, bool isDetectable = true)
        {
            var bufferContentType = Mock.Of<IContentType>();

            // Text buffer with a properties collection and current snapshot
            var mockTextBuffer = new Mock<ITextBuffer>();
            mockTextBuffer.Setup(b => b.ContentType).Returns(bufferContentType);

            var dummyProperties = new PropertyCollection();
            mockTextBuffer.Setup(p => p.Properties).Returns(dummyProperties);

            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.Length).Returns(0);
            mockTextBuffer.Setup(x => x.CurrentSnapshot).Returns(mockSnapshot.Object);

            // Create the document and associate the buffer with the it
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(d => d.FilePath).Returns(fileName);
            mockTextDocument.Setup(d => d.Encoding).Returns(Encoding.UTF8);

            mockTextDocument.Setup(x => x.TextBuffer).Returns(mockTextBuffer.Object);

            // Register the buffer-to-doc mapping for the factory service
            dummyDocumentFactoryService.RegisterDocument(mockTextDocument.Object);

            var analysisLanguages = isDetectable ? new[] { AnalysisLanguage.Javascript } : Enumerable.Empty<AnalysisLanguage>();

            SetupDetectedLanguages(fileName, bufferContentType, analysisLanguages);

            mockAnalyzerController.Setup(x => x.IsAnalysisSupported(analysisLanguages)).Returns(isDetectable);

            return mockTextDocument.Object;
        }

        private void SetupDetectedLanguages(string fileName, IContentType bufferContentType, IEnumerable<AnalysisLanguage> detectedLanguages)
        {
            mockSonarLanguageRecognizer
                .Setup(x => x.Detect(fileName, bufferContentType))
                .Returns(detectedLanguages);
        }

        private static void VerifySingletonManagerDoesNotExist(ITextBuffer buffer) =>
            FindSingletonManagerInPropertyCollection(buffer).Should().BeNull();

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

        private void VerifyCheckedAnalysisIsSupported()
        {
            mockAnalyzerController.Verify(x => x.IsAnalysisSupported(It.IsAny<IEnumerable<AnalysisLanguage>>()), Times.Once);
        }

        private void VerifyAnalysisWasRequested()
        {
            mockAnalyzerController.Verify(
                x => x.ExecuteAnalysis("anyname", "utf-8", It.IsAny<IEnumerable<AnalysisLanguage>>(),
                    It.IsAny<IIssueConsumer>(), null, CancellationToken.None), Times.Once);
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

            ITextDocument ITextDocumentFactoryService.CreateAndLoadTextDocument(string filePath, IContentType contentType, Encoding encoding, out bool characterSubstitutionsOccurred)
            {
                throw new NotImplementedException();
            }

            ITextDocument ITextDocumentFactoryService.CreateAndLoadTextDocument(string filePath, IContentType contentType, bool attemptUtf8Detection, out bool characterSubstitutionsOccurred)
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
}
