/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.ConnectedMode.Suppressions;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using static SonarLint.VisualStudio.Integration.Vsix.Analysis.IssueConsumerFactory;
using static SonarLint.VisualStudio.Integration.Vsix.Analysis.IssueConsumerFactory.IssueHandler;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class IssueHandlerTests
    {
        [TestMethod]
        public void HandleNewIssues_UpdatedSnapshotAndHotspotStoreHaveExpectedValues()
        {
            var hotspotStoreMock = new Mock<ILocalHotspotsStoreUpdater>();

            var hotspot = CreateIssue("S112", startLine: 1, endLine: 1, isHotspot: true);
            var issue = CreateIssue("S111", startLine: 1, endLine: 1);
            var inputIssues = new[]
            {
                issue,
                hotspot,
            };

            var notificationHandler = new SnapshotChangeHandler();

            var expectedGuid = Guid.NewGuid();
            const string expectedProjectName = "my project name";
            const string expectedFilePath = "c:\\aaa\\file.txt";
            
            var testSubject = CreateTestSubject(notificationHandler.OnSnapshotChanged,
                expectedProjectName, expectedGuid, expectedFilePath, localHotspotsStoreUpdater:hotspotStoreMock.Object);

            // Act
            testSubject.HandleNewIssues(inputIssues);

            // Assert
            notificationHandler.InvocationCount.Should().Be(1);

            // Check the updated issues
            VerifyHotspotsAdded(hotspotStoreMock, expectedFilePath, new []{ hotspot });
            
            notificationHandler.UpdatedSnapshot.Issues.Count().Should().Be(1);
            notificationHandler.UpdatedSnapshot.Issues.Should().BeEquivalentTo(new []{issue});

            notificationHandler.UpdatedSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, out var actualProjectName).Should().BeTrue();
            actualProjectName.Should().Be(expectedProjectName);

            notificationHandler.UpdatedSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectGuid, out var actualProjectGuid).Should().BeTrue();
            actualProjectGuid.Should().Be(expectedGuid);

            notificationHandler.UpdatedSnapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, out var actualFilePath).Should().BeTrue();
            actualFilePath.Should().Be(expectedFilePath);

            notificationHandler.UpdatedSnapshot.AnalyzedFilePath.Should().Be(expectedFilePath);
        }

        [TestMethod]
        public void HandleNewIssues_IssuesGetMatchesIsCalled()
        {
            var inputIssues = new[]
            {
                CreateIssue("S111", startLine: 1, endLine: 1),
                CreateIssue("S222", startLine: 2, endLine: 2)
            };

            var notificationHandler = new SnapshotChangeHandler();
            var suppressedIssueMatcher = new Mock<ISuppressedIssueMatcher>();

            var testSubject = CreateTestSubject(notificationHandler.OnSnapshotChanged, suppressedIssueMatcher.Object);

            // Act
            testSubject.HandleNewIssues(inputIssues);

            // Assert

            foreach (var issue in inputIssues)
            {
                suppressedIssueMatcher.Verify(x => x.SuppressionExists(issue), Times.Once());
            }

            notificationHandler.InvocationCount.Should().Be(1);

            // Check the updated issues
            notificationHandler.UpdatedSnapshot.Issues.Count().Should().Be(2);
            notificationHandler.UpdatedSnapshot.Issues.Should().BeEquivalentTo(inputIssues);
        }

        [TestMethod]
        public void HandleNewIssues_SpansAreTranslated()
        {
            var hotspotStoreMock = new Mock<ILocalHotspotsStoreUpdater>();

            var inputIssues = new[]
            {
                CreateIssue("xxx", startLine: 1, endLine: 1),
                CreateIssue("xxxx", startLine: 3, endLine: 3, isHotspot: true)
            };

            var issuesToReturnFromTranslator = new[]
            {
                CreateIssue("yyy", startLine: 2, endLine: 2),
                CreateIssue("yyyy", startLine: 4, endLine: 4, isHotspot: true)
            };

            var notificationHandler = new SnapshotChangeHandler();

            const string filePath = "any.txt";
            var textDocument = CreateValidTextDocument(filePath);

            var translator = new Mock<TranslateSpans>();
            translator.Setup(x => x.Invoke(It.IsAny<IEnumerable<IAnalysisIssueVisualization>>(), textDocument.TextBuffer.CurrentSnapshot))
                .Returns(issuesToReturnFromTranslator);

            var testSubject = CreateTestSubject(notificationHandler: notificationHandler.OnSnapshotChanged,
                translator: translator.Object, textDocument: textDocument,
                localHotspotsStoreUpdater: hotspotStoreMock.Object);

            // Act
            testSubject.HandleNewIssues(inputIssues);

            // Assert
            translator.VerifyAll();
            notificationHandler.InvocationCount.Should().Be(1);
            notificationHandler.UpdatedSnapshot.Issues.Should().BeEquivalentTo(issuesToReturnFromTranslator.First());
            
            VerifyHotspotsAdded(hotspotStoreMock, filePath, new []{ issuesToReturnFromTranslator.Last()});
        }

        [TestMethod]
        public void HandleNewIssues_HasFileLevelIssue_IssueIsReturned()
        {
            // Arrange
            var inputIssues = new[] { CreateFileLevelIssue("File Level Issue") };

            var notificationHandler = new SnapshotChangeHandler();
            var testSubject = CreateTestSubject(notificationHandler.OnSnapshotChanged);

            // Act
            testSubject.HandleNewIssues(inputIssues);

            // Assert
            notificationHandler.InvocationCount.Should().Be(1);
            notificationHandler.UpdatedSnapshot.Issues.Count().Should().Be(1);
        }

        [TestMethod]
        public void HandleNewIssues_NoIssues_NoIssuesReturned()
        {
            // Arrange
            var inputIssues = Enumerable.Empty<IAnalysisIssueVisualization>();

            var notificationHandler = new SnapshotChangeHandler();
            var testSubject = CreateTestSubject(notificationHandler.OnSnapshotChanged);

            // Act
            testSubject.HandleNewIssues(inputIssues);

            // Assert
            notificationHandler.InvocationCount.Should().Be(1);
            notificationHandler.UpdatedSnapshot.Issues.Count().Should().Be(0);
        }

        [TestMethod]
        public void HandleNewIssues_SomeSuppressedIssues_IssuesGetMarkedCorrectly()
        {
            // Arrange
            var issue1 = CreateIssue("xxx", startLine: 1, endLine: 1);
            var issue2 = CreateIssue("xxx2", startLine: 2, endLine: 2);
            var issue3 = CreateIssue("xxx3", startLine: 3, endLine: 3);
            var issue4 = CreateIssue("xxx4", startLine: 4, endLine: 4);

            var issues = new[] { issue1, issue2, issue3, issue4 };
            var suppressedIssueMatcher = new Mock<ISuppressedIssueMatcher>();
            suppressedIssueMatcher.Setup(x => x.SuppressionExists(issue1)).Returns(true);
            suppressedIssueMatcher.Setup(x => x.SuppressionExists(issue4)).Returns(true);

            var notificationHandler = new SnapshotChangeHandler();
            var testSubject = CreateTestSubject(notificationHandler.OnSnapshotChanged, suppressedIssueMatcher: suppressedIssueMatcher.Object);

            // Act
            testSubject.HandleNewIssues(issues);

            // Assert
            notificationHandler.InvocationCount.Should().Be(1);
            notificationHandler.UpdatedSnapshot.Issues.Count().Should().Be(4);

            issue1.IsSuppressed = true;

            issue1.IsSuppressed.Should().BeTrue();
            issue2.IsSuppressed.Should().BeFalse();
            issue3.IsSuppressed.Should().BeFalse();
            issue4.IsSuppressed.Should().BeTrue();
        }
        
        private static void VerifyHotspotsAdded(Mock<ILocalHotspotsStoreUpdater> hotspotStoreMock, string filePath,
            IAnalysisIssueVisualization[] expectedHotspots)
        {
            hotspotStoreMock.Verify(store => store.UpdateForFile(filePath,
                It.Is<IEnumerable<IAnalysisIssueVisualization>>(hotspots =>
                    hotspots.SequenceEqual(expectedHotspots))), Times.Once);
        }

        private static IAnalysisIssueVisualization CreateIssue(string ruleKey, int startLine, int endLine, bool isHotspot = false)
        {
            var issue = new DummyAnalysisIssue
            {
                RuleKey = ruleKey,
                PrimaryLocation = new DummyAnalysisIssueLocation
                {
                    TextRange = new DummyTextRange
                    {
                        StartLine = startLine,
                        EndLine = endLine,
                    }
                },
                Type = isHotspot ? AnalysisIssueType.SecurityHotspot : AnalysisIssueType.CodeSmell
            };

            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(issue);
            issueVizMock.Setup(x => x.Location).Returns(issue.PrimaryLocation);
            issueVizMock.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());
            issueVizMock.SetupProperty(x => x.Span);
            issueVizMock.SetupProperty(x => x.IsSuppressed);
            issueVizMock.Object.Span = new SnapshotSpan(CreateMockTextSnapshot(1000, "any line text").Object, 0, 1);

            return issueVizMock.Object;
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

        private static IAnalysisIssueVisualization CreateFileLevelIssue(string ruleKey)
        {
            var issue = new DummyAnalysisIssue
            {
                RuleKey = ruleKey,
                PrimaryLocation = new DummyAnalysisIssueLocation { TextRange = null }
            };

            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(issue);
            issueVizMock.Setup(x => x.Location).Returns(issue.PrimaryLocation);
            issueVizMock.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());

            return issueVizMock.Object;
        }

        private static ITextDocument CreateValidTextDocument(string filePath)
        {
            var document = new Mock<ITextDocument>();
            var buffer = new Mock<ITextBuffer>();
            var snapshot = new Mock<ITextSnapshot>();

            document.Setup(x => x.FilePath).Returns(filePath);
            document.Setup(x => x.TextBuffer).Returns(buffer.Object);
            buffer.Setup(x => x.CurrentSnapshot).Returns(snapshot.Object);

            return document.Object;
        }

        private static IssueHandler CreateTestSubject(SnapshotChangedHandler notificationHandler,
            ISuppressedIssueMatcher suppressedIssueMatcher = null, 
            TranslateSpans translator = null,
            ITextDocument textDocument = null,
            ILocalHotspotsStoreUpdater localHotspotsStoreUpdater = null)
            => CreateTestSubject(notificationHandler, "any project name", Guid.NewGuid(),
                suppressedIssueMatcher, translator, textDocument, localHotspotsStoreUpdater);

        private static IssueHandler CreateTestSubject(SnapshotChangedHandler notificationHandler,
            string projectName,
            Guid projectGuid,
            string filePath,
            ILocalHotspotsStoreUpdater localHotspotsStoreUpdater = null)
            => CreateTestSubject(notificationHandler, projectName, projectGuid, null, null, CreateValidTextDocument(filePath), localHotspotsStoreUpdater);

        private static IssueHandler CreateTestSubject(SnapshotChangedHandler notificationHandler,
            string projectName, Guid projectGuid,
            ISuppressedIssueMatcher suppressedIssueMatcher = null,
            TranslateSpans translator = null,
            ITextDocument textDocument = null,
            ILocalHotspotsStoreUpdater localHotspotsStoreUpdater = null)
        {
            suppressedIssueMatcher ??= Mock.Of<ISuppressedIssueMatcher>();
            translator ??= PassthroughSpanTranslator;
            textDocument ??= CreateValidTextDocument("any");

            var testSubject = new IssueHandler(
                textDocument,
                projectName,
                projectGuid,                
                suppressedIssueMatcher,
                notificationHandler,
                localHotspotsStoreUpdater ?? Mock.Of<ILocalHotspotsStoreUpdater>(),
                // Override the un-testable "TranslateSpans" behaviour
                translator);

            return testSubject;
        }

        private static IAnalysisIssueVisualization[] PassthroughSpanTranslator(IEnumerable<IAnalysisIssueVisualization> issues, ITextSnapshot activeSnapshot)
            => issues.ToArray();

        internal class SnapshotChangeHandler
        {
            public IIssuesSnapshot UpdatedSnapshot { get; private set; }

            public int InvocationCount { get; private set; }

            public void OnSnapshotChanged(IIssuesSnapshot newSnapshot)
            {
                InvocationCount++;
                UpdatedSnapshot = newSnapshot;
            }
        }
    }
}
