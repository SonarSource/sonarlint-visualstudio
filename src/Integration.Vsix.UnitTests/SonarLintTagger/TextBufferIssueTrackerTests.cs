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
using System.Linq;
using System.Text;
using EnvDTE;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    /*
     * Note: the TextBufferIssueTracker and TaggerProvider are tightly coupled so it isn't possible
     * to test them completely independently without substantial refactoring.
     * These unit tests are dependent on both classes behaving correctly.
     */

    [TestClass]
    public class TextBufferIssueTrackerTests
    {
        private Mock<ISonarLintDaemon> daemonMock;

        private TaggerProvider taggerProvider;
        private Mock<ITextDocument> mockedJavascriptDocumentFooJs;
        private SonarLanguage[] javascriptLanguage = new[] { SonarLanguage.Javascript };

        [TestInitialize]
        public void SetUp()
        {
            daemonMock = new Mock<ISonarLintDaemon>();
            taggerProvider = CreateTaggerProvider();
            mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js");
            javascriptLanguage = new[] { SonarLanguage.Javascript };
        }

        #region Triggering analysis tests

        [TestMethod]
        public void WhenTaggerIsRegistered_AnalysisIsRequested()
        {
            // Arrange
            var testSubject = new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, new TestLogger());

            // 1. No tagger -> analysis not requested
            CheckAnalysisWasNotRequested();

            // 2. Add a tagger -> analysis requested
            var tagger = new IssueTagger(testSubject);
            daemonMock.Verify(x => x.RequestAnalysis("foo.js", "utf-8", "js", testSubject), Times.Once);
        }

        [TestMethod]
        public void WhenFileRenamed_FileNameIsUpdated_AndAnalysisIsNotRequested()
        {
            // Arrange
            var testSubject = new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, new TestLogger());

            var errorListSink = RegisterNewErrorListSink();
            testSubject.Factory.CurrentSnapshot.VersionNumber.Should().Be(0); // sanity check

            // Act
            RaiseRenameEvent(mockedJavascriptDocumentFooJs, "newPath.js");

            // Assert
            testSubject.FilePath.Should().Be("newPath.js");

            // Check the snapshot was updated and the error list notified
            testSubject.Factory.CurrentSnapshot.VersionNumber.Should().Be(1);
            CheckSinkNotified(errorListSink, 1);

            CheckAnalysisWasNotRequested();
        }

        [TestMethod]
        public void WhenFileIsSaved_ButNoTaggers_AnalysisIsNotRequested()
        {
            // Arrange
            var testSubject = new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, new TestLogger());

            CheckAnalysisWasNotRequested();

            // Act
            RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);

            // Assert
            CheckAnalysisWasNotRequested();
        }

        [TestMethod]
        public void WhenFileIsSaved_AnalysisIsRequested_ButOnlyIfATaggerIsRegistered()
        {
            // Arrange
            var testSubject = new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, new TestLogger());

            // 1. No tagger -> analysis not requested
            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            CheckAnalysisWasNotRequested();

            // 2. Add a tagger and raise -> analysis requested
            var tagger = new IssueTagger(testSubject);
            daemonMock.Invocations.Clear();

            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            daemonMock.Verify(x => x.RequestAnalysis("foo.js", "utf-8", "js", testSubject), Times.Once);

            // 3. Unregister tagger and raise -> analysis not requested
            tagger.Dispose();
            daemonMock.Invocations.Clear();

            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            CheckAnalysisWasNotRequested();
        }

        [TestMethod]
        public void WhenFileIsLoaded_AnalysisIsNotRequested()
        {
            // Arrange
            var testSubject = new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, new TestLogger());

            var tagger = new IssueTagger(testSubject);
            daemonMock.Invocations.Clear();

            // Act
            RaiseFileLoadedEvent(mockedJavascriptDocumentFooJs);
            CheckAnalysisWasNotRequested();

            // Sanity check (that the test setup is correct and that events are actually being handled)
            RaiseFileSavedEvent(mockedJavascriptDocumentFooJs);
            daemonMock.Verify(x => x.RequestAnalysis("foo.js", "utf-8", "js", testSubject), Times.Once);
        }

        private static void RaiseRenameEvent(Mock<ITextDocument> mockDocument, string newFileName)
        {
            var args = new TextDocumentFileActionEventArgs(newFileName, DateTime.UtcNow, FileActionTypes.DocumentRenamed);
            mockDocument.Raise(x => x.FileActionOccurred += null, args);
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
            daemonMock.Verify(x => x.RequestAnalysis(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IIssueConsumer>()), Times.Never);
        }

        #endregion

        #region Processing analysis results tests

        [TestMethod]
        public void WhenNewIssuesAreFound_ButForIncorrectFile_ListenersAreNotUpdated()
        {
            // Arrange
            var issues = new[] { new Sonarlint.Issue { RuleKey = "S123", StartLine = 1, EndLine = 1 } };

            // Add a couple of error list listeners
            var errorListSinkMock1 = RegisterNewErrorListSink();
            var errorListSinkMock2 = RegisterNewErrorListSink();

            var testSubject = new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, new TestLogger());
            var tagger = new IssueTagger(testSubject);

            // Act
            using (new AssertIgnoreScope())
            {
                ((IIssueConsumer)testSubject).Accept("aRandomFile.xxx", issues);
            }

            // Assert
            CheckSinkNotNotified(errorListSinkMock1);
            CheckSinkNotNotified(errorListSinkMock2);
        }

        [TestMethod]
        public void WhenNewIssuesAreFound_ListenersAreUpdated()
        {
            // Arrange
            var issues = new[] {
                new Sonarlint.Issue { RuleKey = "S111", StartLine = 1, EndLine = 1 },
                // The next issue is outside the range of the new snapshot and should be ignored
                new Sonarlint.Issue { RuleKey = "S222", StartLine = 99999998, EndLine = 99999999 },
                new Sonarlint.Issue { RuleKey = "S333", StartLine = 100, EndLine = 101 }
            };

            // Add a couple of error list listeners
            var errorListSinkMock1 = RegisterNewErrorListSink();
            var errorListSinkMock2 = RegisterNewErrorListSink();

            var testSubject = new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, new TestLogger());
            var tagger = new IssueTagger(testSubject);

            // Sanity check
            testSubject.LastIssues.Should().BeNull();
            testSubject.Factory.CurrentSnapshot.VersionNumber.Should().Be(0);
            testSubject.Factory.CurrentSnapshot.IssueMarkers.Count().Should().Be(0);

            // Act
            ((IIssueConsumer)testSubject).Accept(mockedJavascriptDocumentFooJs.Object.FilePath, issues);

            // Assert
            // We can't check that the editors listeners are notified: we can't mock
            // SnapshotSpan well enough for the product code to work -> affected span
            // is always null so the taggers don't notify their listeners.
            CheckSinkNotified(errorListSinkMock1, 1);
            CheckSinkNotified(errorListSinkMock2, 1);

            testSubject.LastIssues.Should().NotBeNull();
            testSubject.Factory.CurrentSnapshot.VersionNumber.Should().Be(1);
            testSubject.Factory.CurrentSnapshot.IssueMarkers.Count().Should().Be(2);

            var actualMarkers = testSubject.Factory.CurrentSnapshot.IssueMarkers.ToArray();
            actualMarkers[0].Issue.RuleKey.Should().Be("S111");
            actualMarkers[1].Issue.RuleKey.Should().Be("S333");
        }

        private Mock<ITableDataSink> RegisterNewErrorListSink()
        {
            // Adds a new error list sink to the tagger provider.
            // Should be notified when issues change.
            var newSink = new Mock<ITableDataSink>();
            taggerProvider.Subscribe(newSink.Object);
            return newSink;
        }

        private void CheckSinkNotNotified(Mock<ITableDataSink> sinkMock)
        {
            sinkMock.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Never);
        }

        private void CheckSinkNotified(Mock<ITableDataSink> sinkMock, int count)
        {
            sinkMock.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Exactly(count));
        }

        #endregion

        private TaggerProvider CreateTaggerProvider()
        {
            var tableManagerProviderMock = new Mock<ITableManagerProvider>();
            tableManagerProviderMock.Setup(t => t.GetTableManager(StandardTables.ErrorsTable))
                .Returns(new Mock<ITableManager>().Object);

            var textDocFactoryServiceMock = new Mock<ITextDocumentFactoryService>();
            var settingsMock = new Mock<ISonarLintSettings>();

            var contentTypeRegistryServiceMock = new Mock<IContentTypeRegistryService>();
            contentTypeRegistryServiceMock.Setup(c => c.ContentTypes).Returns(Enumerable.Empty<IContentType>());
            var fileExtensionRegistryServiceMock = new Mock<IFileExtensionRegistryService>();
            var languageRecognizer = new SonarLanguageRecognizer(contentTypeRegistryServiceMock.Object, fileExtensionRegistryServiceMock.Object);


            // DTE object setup
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

            var serviceProvider = new ConfigurableServiceProvider();
            serviceProvider.RegisterService(typeof(DTE), mockDTE.Object);

            var provider = new TaggerProvider(tableManagerProviderMock.Object, textDocFactoryServiceMock.Object, daemonMock.Object,
                serviceProvider, settingsMock.Object, languageRecognizer, new TestLogger());
            return provider;
        }

        private static Mock<ITextDocument> CreateDocumentMock(string fileName)
        {
            // Text buffer with a properties collection and current snapshot
            var mockTextBuffer = new Mock<ITextBuffer>();

            var dummyProperties = new PropertyCollection();
            mockTextBuffer.Setup(p => p.Properties).Returns(dummyProperties);

            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.Length).Returns(9999);
            mockSnapshot.Setup(x => x.LineCount).Returns(1000);
            mockSnapshot.Setup(x => x.GetLineFromLineNumber(It.IsAny<int>()))
                .Returns(new Mock<ITextSnapshotLine>().Object);

            mockTextBuffer.Setup(x => x.CurrentSnapshot).Returns(mockSnapshot.Object);

            // Create the document and associate the buffer with the it
            var mockTextDocument = new Mock<ITextDocument>();
            mockTextDocument.Setup(d => d.FilePath).Returns(fileName);
            mockTextDocument.Setup(d => d.Encoding).Returns(Encoding.UTF8);

            mockTextDocument.Setup(x => x.TextBuffer).Returns(mockTextBuffer.Object);

            return mockTextDocument;
        }
    }
}
