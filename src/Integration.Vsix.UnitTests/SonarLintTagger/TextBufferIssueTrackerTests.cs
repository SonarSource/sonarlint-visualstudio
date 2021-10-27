﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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
using EnvDTE80;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LanguageDetection;
using SonarLint.VisualStudio.IssueVisualization.Models;

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
        private Mock<IIssuesFilter> issuesFilter;
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
            issuesFilter = new Mock<IIssuesFilter>();
            taggerProvider = CreateTaggerProvider();
            mockDocumentTextBuffer = CreateTextBufferMock();
            mockedJavascriptDocumentFooJs = CreateDocumentMock("foo.js", mockDocumentTextBuffer.Object);
            javascriptLanguage = new[] { AnalysisLanguage.Javascript };
            projectGuid = Guid.NewGuid();

            testSubject = CreateTextBufferIssueTracker();
        }

        private TextBufferIssueTracker CreateTextBufferIssueTracker(IVsSolution5 vsSolution = null)
        {
            logger = new TestLogger();
            vsSolution ??= Mock.Of<IVsSolution5>();

            return new TextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, issuesFilter.Object,
                mockSonarErrorDataSource.Object, Mock.Of<IAnalysisIssueVisualizationConverter>(), vsSolution, logger);
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
        public void Ctor_FailsToRetrieveProjectGuid_NonCriticalException_ExceptionCaught()
        {
            var mockVsSolution = new Mock<IVsSolution5>();
            mockVsSolution.Setup(x => x.GetGuidOfProjectFile("MyProject.csproj"))
                .Throws(new Exception("this is a test"));

            Action act = () => CreateTextBufferIssueTracker(mockVsSolution.Object);

            act.Should().NotThrow();
        }

        [TestMethod]
        public void Ctor_FailsToRetrieveProjectGuid_CriticalException_ExceptionThrown()
        {
            var mockVsSolution = new Mock<IVsSolution5>();
            mockVsSolution.Setup(x => x.GetGuidOfProjectFile("MyProject.csproj"))
                .Throws(new StackOverflowException("this is a test"));

            Action act = () => CreateTextBufferIssueTracker(mockVsSolution.Object);

            act.Should().ThrowExactly<StackOverflowException>();
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
                new[] {AnalysisLanguage.Javascript}, It.IsAny<IIssueConsumer>(),
                null /* no expecting any options when a new tagger is added */,
                It.IsAny<CancellationToken>()), Times.Once);
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
                It.IsAny<IIssueConsumer>(), It.IsAny<IAnalyzerOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }
        private void CheckAnalysisWasRequested()
        {
            mockAnalyzerController.Verify(x => x.ExecuteAnalysis(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<AnalysisLanguage>>(),
                It.IsAny<IIssueConsumer>(), It.IsAny<IAnalyzerOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        #endregion

        #region Processing analysis results tests

        [TestMethod]
        public void WhenNewIssuesAreFound_FilterIsApplied_ListenersAreUpdated()
        {
            // Arrange
            // Use the test version of the text buffer to bypass the span translation code
            testSubject = new TestableTextBufferIssueTracker(taggerProvider.dte, taggerProvider,
                mockedJavascriptDocumentFooJs.Object, javascriptLanguage, issuesFilter.Object,
                mockSonarErrorDataSource.Object, Mock.Of<IAnalysisIssueVisualizationConverter>(), 
                Mock.Of<IVsSolution5>(), logger);

            mockSonarErrorDataSource.Invocations.Clear();

            var originalId = testSubject.Factory.CurrentSnapshot.AnalysisRunId;

            var inputIssues = new[]
            {
                CreateIssue("S111", startLine: 1, endLine: 1),
                CreateIssue("S222", startLine: 2, endLine: 2)
            };

            var issuesToReturnFromFilter = new[]
            {
                CreateIssue("xxx", startLine: 3, endLine: 3)
            };

            SetupIssuesFilter(out var issuesPassedToFilter, issuesToReturnFromFilter);

            // Act
            testSubject.HandleNewIssues(inputIssues);

            // Assert
            // Check the snapshot has changed
            testSubject.Factory.CurrentSnapshot.AnalysisRunId.Should().NotBe(originalId);

            // Check the expected issues were passed to the filter
            issuesPassedToFilter.Count.Should().Be(2);
            issuesPassedToFilter.Should().BeEquivalentTo(inputIssues, c => c.WithStrictOrdering());

            CheckErrorListRefreshWasRequestedOnce(testSubject.Factory);

            // Check the post-filter issues
            testSubject.Factory.CurrentSnapshot.Issues.Count().Should().Be(1);
            testSubject.Factory.CurrentSnapshot.Issues.First().Issue.RuleKey.Should().Be("xxx");

            testSubject.Factory.CurrentSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, out var projectName).Should().BeTrue();
            projectName.Should().Be("MyProject");

            testSubject.Factory.CurrentSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectGuid, out var projectGuid).Should().BeTrue();
            projectGuid.Should().Be(projectGuid);
        }

        [TestMethod]
        public void WhenNewIssuesAreFound_AndFilterRemovesAllIssues_ListenersAreUpdated()
        {
            mockSonarErrorDataSource.Invocations.Clear();

            // Arrange
            var filteredIssue = CreateIssue("single issue", startLine: 1, endLine: 1);

            var issuesToReturnFromFilter = Enumerable.Empty<IAnalysisIssueVisualization>();
            SetupIssuesFilter(out var capturedFilterInput, issuesToReturnFromFilter);

            // Act
            testSubject.HandleNewIssues(new [] { filteredIssue });

            // Assert
            // Check the expected issues were passed to the filter
            capturedFilterInput.Count.Should().Be(1);
            capturedFilterInput[0].Should().Be(filteredIssue);

            CheckErrorListRefreshWasRequestedOnce(testSubject.Factory);

            // Check there are no issues
            testSubject.Factory.CurrentSnapshot.Issues.Count().Should().Be(0);
        }

        [TestMethod]
        public void WhenNoIssuesAreFound_ListenersAreUpdated()
        {
            mockSonarErrorDataSource.Invocations.Clear();

            // Arrange
            SetupIssuesFilter(out var capturedFilterInput, Enumerable.Empty<IFilterableIssue>());

            // Act
            testSubject.HandleNewIssues(Enumerable.Empty<IAnalysisIssueVisualization>());

            // Assert
            capturedFilterInput.Should().BeEmpty();

            CheckErrorListRefreshWasRequestedOnce(testSubject.Factory);

            testSubject.Factory.CurrentSnapshot.Issues.Count().Should().Be(0);
        }

        private void SetupIssuesFilter(out IList<IFilterableIssue> capturedFilterInput,
            IEnumerable<IFilterableIssue> optionalDataToReturn = null /* if null then the supplied input will be returned */)
        {
            var captured = new List<IFilterableIssue>();
            issuesFilter.Setup(x => x.Filter(It.IsAny<IEnumerable<IFilterableIssue>>()))
                .Callback((IEnumerable<IFilterableIssue> inputIssues) => captured.AddRange(inputIssues))
                .Returns(optionalDataToReturn ?? captured);
            capturedFilterInput = captured;
        }

        private void CheckErrorListRefreshWasRequestedOnce(IssuesSnapshotFactory factory)
        {
            mockSonarErrorDataSource.Verify(x => x.RefreshErrorList(factory), Times.Once);
        }

        private static IAnalysisIssueVisualization CreateIssue(string ruleKey, int startLine, int endLine)
        {
            var issue = new DummyAnalysisIssue
            {
                RuleKey = ruleKey,
                StartLine = startLine,
                EndLine = endLine
            };

            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(issue);
            issueVizMock.Setup(x => x.Location).Returns(issue);
            issueVizMock.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());
            issueVizMock.SetupProperty(x => x.Span);
            issueVizMock.Object.Span = new SnapshotSpan(CreateMockTextSnapshot(1000, "any line text").Object, 0, 1);

            return issueVizMock.Object;
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

        [TestMethod]
        public void WhenNewIssuesAreFound_FileHasNoAssociatedProject_NoExceptionIsThrown()
        {
            mockSolution.Reset();
            mockSolution
                .Setup(x => x.FindProjectItem(mockedJavascriptDocumentFooJs.Name))
                .Returns((ProjectItem)null);

            testSubject = CreateTextBufferIssueTracker();

            Action act = () => testSubject.HandleNewIssues(new List<IAnalysisIssueVisualization>());
            act.Should().NotThrow();
        }

        #endregion

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

            var provider = new TaggerProvider(mockSonarErrorDataSource.Object, textDocFactoryServiceMock.Object, issuesFilter.Object, mockAnalyzerController.Object,
                serviceProvider, languageRecognizer, mockAnalysisRequester.Object, Mock.Of<IAnalysisIssueVisualizationConverter>(), Mock.Of<ITaggableBufferIndicator>(), logger, mockAnalysisScheduler.Object);
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

        // We can't mock the span translation code (to difficult to mock),
        // so this subclass by-passes it.
        // See https://github.com/SonarSource/sonarlint-visualstudio/issues/1522
        private class TestableTextBufferIssueTracker : TextBufferIssueTracker
        {
            public TestableTextBufferIssueTracker(DTE2 dte, TaggerProvider provider, ITextDocument document,
                IEnumerable<AnalysisLanguage> detectedLanguages, IIssuesFilter issuesFilter,
                ISonarErrorListDataSource sonarErrorDataSource, IAnalysisIssueVisualizationConverter converter, IVsSolution5 vsSolution, ILogger logger)
                : base(dte, provider, document, detectedLanguages, issuesFilter, sonarErrorDataSource, converter, vsSolution, logger)
            { }

            protected override IEnumerable<IAnalysisIssueVisualization> TranslateSpans(IEnumerable<IAnalysisIssueVisualization> issues, ITextSnapshot activeSnapshot)
            {
                // Just pass-through the supplied issues
                return issues;
            }
        }
    }
}
