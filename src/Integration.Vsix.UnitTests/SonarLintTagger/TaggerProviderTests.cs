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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.SonarLintTagger;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class TaggerProviderTests
    {
        private Mock<IAnalyzerController> mockAnalyzerController;
        private IAnalyzerController analyzerController;
        private Mock<ILogger> mockLogger;
        private IContentType jsContentType;
        private DummyTextDocumentFactoryService dummyDocumentFactoryService;
        private Mock<IIssueTrackerFactory> issueTrackerFactory;

        private TaggerProvider testSubject;

        [TestInitialize]
        public void SetUp()
        {
            mockAnalyzerController = new Mock<IAnalyzerController>();
            mockAnalyzerController.Setup(x => x.IsAnalysisSupported(It.IsAny<IEnumerable<AnalysisLanguage>>())).Returns(true);
            analyzerController = this.mockAnalyzerController.Object;

            var mockContentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            mockContentTypeRegistryService.Setup(c => c.ContentTypes).Returns(Enumerable.Empty<IContentType>());
            var contentTypeRegistryService = mockContentTypeRegistryService.Object;

            var mockServiceProvider = new Mock<IServiceProvider>();
            var serviceProvider = mockServiceProvider.Object;

            var mockFileExtensionRegistryService = new Mock<IFileExtensionRegistryService>();
            var fileExtensionRegistryService = mockFileExtensionRegistryService.Object;

            var mockJsContentType = new Mock<IContentType>();
            mockJsContentType.Setup(c => c.IsOfType(It.IsAny<string>())).Returns(false);
            mockJsContentType.Setup(c => c.IsOfType("JavaScript")).Returns(true);
            this.jsContentType = mockJsContentType.Object;

            dummyDocumentFactoryService = new DummyTextDocumentFactoryService();

            mockLogger = new Mock<ILogger>();

            var sonarLanguageRecognizer = new SonarLanguageRecognizer(contentTypeRegistryService, fileExtensionRegistryService);
            var mockAnalysisRequester = new Mock<IAnalysisRequester>();
            issueTrackerFactory = new Mock<IIssueTrackerFactory>();

            testSubject = new TaggerProvider(issueTrackerFactory.Object, dummyDocumentFactoryService, analyzerController, serviceProvider,
                sonarLanguageRecognizer, mockAnalysisRequester.Object, mockLogger.Object);
        }

        [TestMethod]
        public void CreateTagger_should_create_tracker_for_js_when_analysis_is_supported()
        {
            CreateTagger(jsContentType).Should().NotBeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_when_analysis_is_not_supported()
        {
            mockAnalyzerController.Setup(x => x.IsAnalysisSupported(It.IsAny<IEnumerable<AnalysisLanguage>>())).Returns(false);

            CreateTagger(jsContentType).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_not_js()
        {
            var doc = CreateMockedDocument("foo.java");

            CreateTaggerForDocument(doc).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_same_tagger_for_already_tracked_file()
        {
            var doc1 = CreateMockedDocument("doc1.js", jsContentType);

            var tagger1 = CreateTaggerForDocument(doc1);
            tagger1.Should().NotBeNull();

            var tagger2 = CreateTaggerForDocument(doc1);
            tagger2.Should().NotBeNull();

            tagger1.Should().BeSameAs(tagger2);
            testSubject.ActiveTrackersForTesting.Count().Should().Be(1);
        }

        [TestMethod]
        public void CreateTagger_close_tagger_should_unregister_tracker()
        {
            var doc1 = CreateMockedDocument("doc1.js", jsContentType);

            var tracker = CreateTaggerForDocument(doc1);
            testSubject.ActiveTrackersForTesting.Count().Should().Be(1);

            // Remove the tagger -> tracker should be unregistered
            Mock.Get(tracker).Raise(x => x.Disposed += null, EventArgs.Empty);
            testSubject.ActiveTrackersForTesting.Count().Should().Be(0);
        }

        [TestMethod]
        public void CreateTagger_tracker_should_be_distinct_per_file()
        {
            var doc1 = CreateMockedDocument("foo.js", jsContentType);
            var tagger1 = CreateTaggerForDocument(doc1);
            tagger1.Should().NotBeNull();

            var doc2 = CreateMockedDocument("bar.js", jsContentType);
            var tagger2 = CreateTaggerForDocument(doc2);
            tagger2.Should().NotBeNull();

            testSubject.ActiveTrackersForTesting.Count().Should().Be(2);
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

        private IIssueTracker[] CreateMockedIssueTrackers(params string[] filePaths) =>
            filePaths.Select(x => CreateMockedIssueTracker(x)).ToArray();

        private static IIssueTracker CreateMockedIssueTracker(string filePath)
        {
            var mock = new Mock<IIssueTracker>();
            mock.Setup(x => x.FilePath).Returns(filePath);
            return mock.Object;
        }

        private ITagger<IErrorTag> CreateTagger(IContentType bufferContentType = null)
        {
            var doc = CreateMockedDocument("anyname", bufferContentType);
            return CreateTaggerForDocument(doc);
        }

        private IIssueTracker CreateTaggerForDocument(ITextDocument document)
        {
            issueTrackerFactory
                .Setup(x => x.Create(document, It.IsAny<IEnumerable<AnalysisLanguage>>()))
                .Returns(Mock.Of<IIssueTracker>());

            var mockTextDataModel = new Mock<ITextDataModel>();
            mockTextDataModel.Setup(x => x.DocumentBuffer).Returns(document.TextBuffer);

            return testSubject.CreateTagger<IErrorTag>(document.TextBuffer) as IIssueTracker;
        }

        private ITextDocument CreateMockedDocument(string fileName, IContentType bufferContentType = null)
        {
            // Text buffer with a properties collection and current snapshot
            var mockTextBuffer = new Mock<ITextBuffer>();
            mockTextBuffer.Setup(b => b.ContentType).Returns(bufferContentType);
            ITextBuffer textBuffer = mockTextBuffer.Object;

            var dummyProperties = new PropertyCollection();
            mockTextBuffer.Setup(p => p.Properties).Returns(dummyProperties);

            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.Length).Returns(0);
            mockTextBuffer.Setup(x => x.CurrentSnapshot).Returns(mockSnapshot.Object);

            // Create the document and associate the buffer with the it
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(d => d.FilePath).Returns(fileName);
            mockTextDocument.Setup(d => d.Encoding).Returns(Encoding.UTF8);

            mockTextDocument.Setup(x => x.TextBuffer).Returns(textBuffer);

            // Register the buffer-to-doc mapping for the factory service
            dummyDocumentFactoryService.RegisterDocument(mockTextDocument.Object);

            return mockTextDocument.Object;
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
