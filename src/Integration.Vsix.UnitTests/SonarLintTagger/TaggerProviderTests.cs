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
        private IContentType jsContentType;
        private DummyTextDocumentFactoryService dummyDocumentFactoryService;

        private TaggerProvider testSubject;
        private Mock<IAnalysisRequestHandlersStore> analysisRequestHandlersStore;
        private Mock<IAnalysisRequestHandlerFactory> analysisRequestHandlerFactory;

        [TestInitialize]
        public void SetUp()
        {
            mockAnalyzerController = new Mock<IAnalyzerController>();
            mockAnalyzerController.Setup(x => x.IsAnalysisSupported(It.IsAny<IEnumerable<AnalysisLanguage>>())).Returns(true);
            analyzerController = this.mockAnalyzerController.Object;

            var mockContentTypeRegistryService = new Mock<IContentTypeRegistryService>();
            mockContentTypeRegistryService.Setup(c => c.ContentTypes).Returns(Enumerable.Empty<IContentType>());
            var contentTypeRegistryService = mockContentTypeRegistryService.Object;

            var mockFileExtensionRegistryService = new Mock<IFileExtensionRegistryService>();
            var fileExtensionRegistryService = mockFileExtensionRegistryService.Object;

            var mockJsContentType = new Mock<IContentType>();
            mockJsContentType.Setup(c => c.IsOfType(It.IsAny<string>())).Returns(false);
            mockJsContentType.Setup(c => c.IsOfType("JavaScript")).Returns(true);
            this.jsContentType = mockJsContentType.Object;

            dummyDocumentFactoryService = new DummyTextDocumentFactoryService();

            var sonarLanguageRecognizer = new SonarLanguageRecognizer(contentTypeRegistryService, fileExtensionRegistryService);

            analysisRequestHandlersStore = new Mock<IAnalysisRequestHandlersStore>();
            analysisRequestHandlerFactory = new Mock<IAnalysisRequestHandlerFactory>();

            testSubject = new TaggerProvider(dummyDocumentFactoryService, analyzerController,
                sonarLanguageRecognizer, analysisRequestHandlersStore.Object, analysisRequestHandlerFactory.Object);
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

            analysisRequestHandlersStore.Verify(x=> x.Add(tagger1), Times.Once());
            analysisRequestHandlersStore.VerifyNoOtherCalls();
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

            tagger1.Should().NotBe(tagger2);

            analysisRequestHandlersStore.Verify(x => x.Add(tagger1), Times.Once());
            analysisRequestHandlersStore.Verify(x => x.Add(tagger2), Times.Once());
            analysisRequestHandlersStore.VerifyNoOtherCalls();
        }

        private ITagger<IErrorTag> CreateTagger(IContentType bufferContentType = null)
        {
            var doc = CreateMockedDocument("anyname", bufferContentType);
            return CreateTaggerForDocument(doc);
        }

        private IAnalysisRequestHandler CreateTaggerForDocument(ITextDocument document)
        {
            analysisRequestHandlerFactory
                .Setup(x => x.Create(document, It.IsAny<IEnumerable<AnalysisLanguage>>()))
                .Returns(Mock.Of<IAnalysisRequestHandler>());

            var mockTextDataModel = new Mock<ITextDataModel>();
            mockTextDataModel.Setup(x => x.DocumentBuffer).Returns(document.TextBuffer);

            return testSubject.CreateTagger<IErrorTag>(document.TextBuffer) as IAnalysisRequestHandler;
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
