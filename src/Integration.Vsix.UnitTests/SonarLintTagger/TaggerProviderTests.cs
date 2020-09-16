/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;
using SonarLint.VisualStudio.IssueVisualization.Models;

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
        private IAnalyzerController analyzerController;
        private Mock<ILogger> mockLogger;
        private Mock<IScheduler> mockAnalysisScheduler;
        private Mock<ISonarLanguageRecognizer> mockSonarLanguageRecognizer;

        private TaggerProvider provider;

        private DummyTextDocumentFactoryService dummyDocumentFactoryService;

        [TestInitialize]
        public void SetUp()
        {
            // minimal setup to create a tagger

            mockSonarErrorDataSource = new Mock<ISonarErrorListDataSource>();

            mockAnalyzerController = new Mock<IAnalyzerController>();
            mockAnalyzerController.Setup(x => x.IsAnalysisSupported(It.IsAny<IEnumerable<AnalysisLanguage>>())).Returns(true);
            analyzerController = this.mockAnalyzerController.Object;

            var mockProject = new Mock<Project>();
            mockProject.Setup(p => p.Name).Returns("MyProject");
            var project = mockProject.Object;

            var mockProjectItem = new Mock<ProjectItem>();
            mockProjectItem.Setup(s => s.ContainingProject).Returns(project);
            var projectItem = mockProjectItem.Object;

            var mockSolution = new Mock<Solution>();
            mockSolution.Setup(s => s.FindProjectItem(It.IsAny<string>())).Returns(projectItem);
            var solution = mockSolution.Object;

            var mockDTE = new Mock<DTE>();
            mockDTE.Setup(d => d.Solution).Returns(solution);
            var dte = mockDTE.Object;

            var mockServiceProvider = new Mock<IServiceProvider>();
            mockServiceProvider.Setup(s => s.GetService(typeof(DTE))).Returns(dte);
            var serviceProvider = mockServiceProvider.Object;

            dummyDocumentFactoryService = new DummyTextDocumentFactoryService();

            mockLogger = new Mock<ILogger>();

            mockSonarLanguageRecognizer = new Mock<ISonarLanguageRecognizer>();
            var mockAnalysisRequester = new Mock<IAnalysisRequester>();

            mockAnalysisScheduler = new Mock<IScheduler>();
            mockAnalysisScheduler.Setup(x => x.Schedule(It.IsAny<string>(), It.IsAny<Action<CancellationToken>>(), It.IsAny<int>()))
                .Callback((string file, Action<CancellationToken> analyze, int timeout) => analyze(CancellationToken.None));

            var issuesFilter = new Mock<IIssuesFilter>();
            this.provider = new TaggerProvider(mockSonarErrorDataSource.Object, dummyDocumentFactoryService, issuesFilter.Object, analyzerController, serviceProvider,
                mockSonarLanguageRecognizer.Object, mockAnalysisRequester.Object, Mock.Of<IAnalysisIssueVisualizationConverter>(), mockLogger.Object, mockAnalysisScheduler.Object);
        }

        [TestMethod]
        public void CreateTagger_should_create_tracker_when_analysis_is_supported()
        {
            var doc = CreateMockedDocument("anyname", isDetectable: true);
            var tagger = CreateTaggerForDocument(doc);

            tagger.Should().NotBeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_when_analysis_is_not_supported()
        {
            var doc = CreateMockedDocument("anyname", isDetectable: false);
            var tagger = CreateTaggerForDocument(doc);

            tagger.Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_SameDocument_ShouldUseSameSingletonManager()
        {
            var doc1 = CreateMockedDocument("doc1.js");
            var buffer = doc1.TextBuffer;

            CheckPresenceOfSingletonManagerInPropertyCollection(buffer, false);

            // 1. Create first tagger for doc
            var tagger1 = CreateTaggerForDocument(doc1);
            var firstRequestManger = CheckPresenceOfSingletonManagerInPropertyCollection(buffer, true);

            // 1. Create second tagger for doc
            var tagger2 = CreateTaggerForDocument(doc1);
            var secondRequestManager = CheckPresenceOfSingletonManagerInPropertyCollection(buffer, true);

            firstRequestManger.Should().BeSameAs(secondRequestManager);
        }

        [TestMethod]
        public void CreateTagger_DifferentDocuments_ShouldUseDifferentSingletonManagers()
        {
            var doc1 = CreateMockedDocument("doc1.js");
            var doc2 = CreateMockedDocument("doc2.js");

            // 1. Create tagger for first doc
            var tagger1 = CreateTaggerForDocument(doc1);
            var doc1RequestManager = CheckPresenceOfSingletonManagerInPropertyCollection(doc1.TextBuffer, true);

            // 2. Create tagger for second doc
            var tagger2 = CreateTaggerForDocument(doc2);
            var doc2RequestManager = CheckPresenceOfSingletonManagerInPropertyCollection(doc2.TextBuffer, true);

            doc1RequestManager.Should().NotBeSameAs(doc2RequestManager);
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
        public void CreateTagger_close_last_tagger_should_unregister_tracker()
        {
            var doc1 = CreateMockedDocument("doc1.js");

            var tracker1 = CreateTaggerForDocument(doc1);
            provider.ActiveTrackersForTesting.Count().Should().Be(1);

            var tracker2 = CreateTaggerForDocument(doc1);
            provider.ActiveTrackersForTesting.Count().Should().Be(1);

            // Remove one tagger -> tracker should still be registered
            ((IDisposable)tracker1).Dispose();
            provider.ActiveTrackersForTesting.Count().Should().Be(1);

            // Remove the last tagger -> tracker should be unregistered
            ((IDisposable)tracker2).Dispose();
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

            SetupDetectedLanguages(fileName, bufferContentType,
                isDetectable ? new[] {AnalysisLanguage.Javascript} : Enumerable.Empty<AnalysisLanguage>());

            return mockTextDocument.Object;
        }

        private void SetupDetectedLanguages(string fileName, IContentType bufferContentType, IEnumerable<AnalysisLanguage> detectedLanguages)
        {
            mockSonarLanguageRecognizer
                .Setup(x => x.Detect(fileName, bufferContentType))
                .Returns(detectedLanguages);
        }

        private SingletonDisposableTaggerManager<IErrorTag> CheckPresenceOfSingletonManagerInPropertyCollection(ITextBuffer buffer, bool shouldExist)
        {
            buffer.Properties.TryGetProperty<SingletonDisposableTaggerManager<IErrorTag>>(TaggerProvider.SingletonManagerPropertyCollectionKey, out var propertyValue)
                .Should().Be(shouldExist);
            return propertyValue;
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
