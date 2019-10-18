/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.CFamily;

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
        private Mock<IAnalyzerController> mockAnalyzerController;
        private IAnalyzerController analyzerController;
        private Mock<ILogger> mockLogger;

        private TaggerProvider provider;

        private IContentType jsContentType;

        private DummyTextDocumentFactoryService dummyDocumentFactoryService;

        [TestInitialize]
        public void SetUp()
        {
            // minimal setup to create a tagger

            mockAnalyzerController = new Mock<IAnalyzerController>();
            mockAnalyzerController.Setup(x => x.IsAnalysisSupported(It.IsAny<IEnumerable<SonarLanguage>>())).Returns(true);
            analyzerController = this.mockAnalyzerController.Object;

            var mockTableManagerProvider = new Mock<ITableManagerProvider>();
            mockTableManagerProvider.Setup(t => t.GetTableManager(StandardTables.ErrorsTable))
                .Returns(new Mock<ITableManager>().Object);
            var tableManagerProvider = mockTableManagerProvider.Object;

            var mockContentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            mockContentTypeRegistryService.Setup(c => c.ContentTypes).Returns(Enumerable.Empty<IContentType>());
            var contentTypeRegistryService = mockContentTypeRegistryService.Object;

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

            var mockFileExtensionRegistryService = new Mock<IFileExtensionRegistryService>();
            var fileExtensionRegistryService = mockFileExtensionRegistryService.Object;

            var mockJsContentType = new Mock<IContentType>();
            mockJsContentType.Setup(c => c.IsOfType(It.IsAny<string>())).Returns(false);
            mockJsContentType.Setup(c => c.IsOfType("JavaScript")).Returns(true);
            this.jsContentType = mockJsContentType.Object;

            dummyDocumentFactoryService = new DummyTextDocumentFactoryService();

            mockLogger = new Mock<ILogger>();

            var sonarLanguageRecognizer = new SonarLanguageRecognizer(contentTypeRegistryService, fileExtensionRegistryService);
            var mockSettingsFileMonitor = new Mock<ISingleFileMonitor>();

            this.provider = new TaggerProvider(tableManagerProvider, dummyDocumentFactoryService, analyzerController, serviceProvider,
                sonarLanguageRecognizer, mockLogger.Object, mockSettingsFileMonitor.Object);
        }

        [TestMethod]
        public void CreateTagger_should_create_tracker_for_js_when_analysis_is_supported()
        {
            CreateTagger(jsContentType).Should().NotBeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_when_analysis_is_not_supported()
        {
            mockAnalyzerController.Setup(x => x.IsAnalysisSupported(It.IsAny<IEnumerable<SonarLanguage>>())).Returns(false);

            CreateTagger(jsContentType).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_null_for_not_js()
        {
            var doc = CreateMockedDocument("foo.java");

            CreateTaggerForDocument(doc).Should().BeNull();
        }

        [TestMethod]
        public void CreateTagger_should_return_new_tagger_for_already_tracked_file()
        {
            var doc1 = CreateMockedDocument("doc1.js", jsContentType);

            var tagger1 = CreateTaggerForDocument(doc1);
            tagger1.Should().NotBeNull();

            var tagger2 = CreateTaggerForDocument(doc1);
            tagger2.Should().NotBeNull();

            // Taggers should be different but tracker should be the same
            tagger1.Should().NotBeSameAs(tagger2);
            tagger1.IssueTracker.Should().BeSameAs(tagger2.IssueTracker);
        }

        [TestMethod]
        public void CreateTagger_close_last_tagger_should_unregister_tracker()
        {
            var doc1 = CreateMockedDocument("doc1.js", jsContentType);

            var tracker1 = CreateTaggerForDocument(doc1);
            provider.ActiveTrackersForTesting.Count().Should().Be(1);

            var tracker2 = CreateTaggerForDocument(doc1);
            provider.ActiveTrackersForTesting.Count().Should().Be(1);

            // Remove one tagger -> tracker should still be registered
            tracker1.Dispose();
            provider.ActiveTrackersForTesting.Count().Should().Be(1);

            // Remove the last tagger -> tracker should be unregistered
            tracker2.Dispose();
            provider.ActiveTrackersForTesting.Count().Should().Be(0);
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

            tagger1.IssueTracker.Should().NotBeSameAs(tagger2.IssueTracker);
            tagger1.Should().NotBe(tagger2);
        }

        [TestMethod]
        public void Should_propagate_new_tracker_factories_to_existing_sink_managers()
        {
            var mockTableDataSink1 = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink1.Object);

            var mockTableDataSink2 = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink2.Object);

            CreateTagger(jsContentType);
            var trackers = provider.ActiveTrackersForTesting.ToArray();
            trackers.Count().Should().Be(1); // sanity check
            
            // factory of new tracker is propagated to all existing sink managers
            mockTableDataSink1.Verify(s => s.AddFactory(trackers[0].Factory, false));
            mockTableDataSink2.Verify(s => s.AddFactory(trackers[0].Factory, false));
        }

        [TestMethod]
        public void Should_not_crash_when_sink_subscriber_gone()
        {
            var mockTableDataSink1 = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink1.Object);

            var mockTableDataSink2 = new Mock<ITableDataSink>(MockBehavior.Strict);
            var sinkManager = provider.Subscribe(mockTableDataSink2.Object);

            sinkManager.Dispose();

            CreateTagger(jsContentType);

            var trackers = provider.ActiveTrackersForTesting.ToArray();
            trackers.Count().Should().Be(1); // sanity check

            // factory of new tracker is propagated to all existing sink managers
            mockTableDataSink1.Verify(s => s.AddFactory(trackers[0].Factory, false));
        }

        [TestMethod]
        public void Should_propagate_existing_tracker_factories_to_new_sink_managers()
        {
            var doc1 = CreateMockedDocument("foo.js", jsContentType);
            CreateTaggerForDocument(doc1);

            var doc2 = CreateMockedDocument("bar.js", jsContentType);
            CreateTaggerForDocument(doc2);

            var trackers = provider.ActiveTrackersForTesting.ToArray();
            trackers.Count().Should().Be(2); // sanity check

            var mockTableDataSink = new Mock<ITableDataSink>();
            provider.Subscribe(mockTableDataSink.Object);

            // factories of existing trackers are propagated to new sink manager
            mockTableDataSink.Verify(s => s.AddFactory(trackers[0].Factory, false));
            mockTableDataSink.Verify(s => s.AddFactory(trackers[1].Factory, false));
        }

        private IssueTagger CreateTagger(IContentType bufferContentType = null)
        {
            var doc = CreateMockedDocument("anyname", bufferContentType);
            return CreateTaggerForDocument(doc);
        }

        private IssueTagger CreateTaggerForDocument(ITextDocument document)
        {
            var mockTextDataModel = new Mock<ITextDataModel>();
            mockTextDataModel.Setup(x => x.DocumentBuffer).Returns(document.TextBuffer);
            var textDataModel = mockTextDataModel.Object;

            var mockTextView = new Mock<ITextView>();
            mockTextView.Setup(t => t.TextBuffer).Returns(document.TextBuffer);
            mockTextView.Setup(t => t.TextDataModel).Returns(textDataModel);

            ITextView textView = mockTextView.Object;

            return provider.CreateTagger<IErrorTag>(textView, document.TextBuffer) as IssueTagger;
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
            private Dictionary<ITextBuffer, ITextDocument> bufferToDocMapping = new Dictionary<ITextBuffer, ITextDocument>();

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
