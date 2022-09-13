/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2022 SonarSource SA
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
using SonarLint.VisualStudio.Core.Suppression;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using static SonarLint.VisualStudio.Integration.Vsix.Analysis.IssueConsumerFactory;

namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
{
    [TestClass]
    public class IssueHandlerTests
    {
        [TestMethod]
        public void WhenIssuesAreFound_FilterIsApplied_NewSnapshotIsPublished()
        {
            var inputIssues = new[]
            {
                CreateIssue("S111", startLine: 1, endLine: 1),
                CreateIssue("S222", startLine: 2, endLine: 2)
            };
            var issuesToReturnFromFilter = new[]

            {
                CreateIssue("xxx", startLine: 3, endLine: 3)
            };

            var issuesFilter = CreateIssuesFilter(out var issuesPassedToFilter, issuesToReturnFromFilter);
            var publisher = new SnapshotPublisher();

            var expectedGuid = Guid.NewGuid();
            const string expectedProjectName = "my project name";
            const string expectedFilePath = "c:\\aaa\\file.txt";
            
            var testSubject = CreateTestSubject(issuesFilter.Object, publisher.PublishSnapshot,
                expectedProjectName, expectedGuid, expectedFilePath);

            // Act
            testSubject.HandleNewIssues(inputIssues);

            // Assert
            publisher.InvocationCount.Should().Be(1);

            // Check the expected issues were passed to the filter
            issuesPassedToFilter.Count.Should().Be(2);
            issuesPassedToFilter.Should().BeEquivalentTo(inputIssues, c => c.WithStrictOrdering());

            // Check the publish issues
            publisher.PublishedSnapshot.Issues.Count().Should().Be(1);
            publisher.PublishedSnapshot.Issues.First().Issue.RuleKey.Should().Be("xxx");

            publisher.PublishedSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, out var actualProjectName).Should().BeTrue();
            actualProjectName.Should().Be(expectedProjectName);

            publisher.PublishedSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectGuid, out var actualProjectGuid).Should().BeTrue();
            actualProjectGuid.Should().Be(expectedGuid);

            publisher.PublishedSnapshot.TryGetValue(0, StandardTableKeyNames.DocumentName, out var actualFilePath).Should().BeTrue();
            actualFilePath.Should().Be(expectedFilePath);

            publisher.PublishedSnapshot.AnalyzedFilePath.Should().Be(expectedFilePath);
        }

        [TestMethod]
        public void WhenNewIssuesAreFound_AndFilterRemovesAllIssues_ListenersAreUpdated()
        {
            // Arrange
            var filteredIssue = CreateIssue("single issue", startLine: 1, endLine: 1);

            var issuesToReturnFromFilter = Enumerable.Empty<IAnalysisIssueVisualization>();
            var issuesFilter = CreateIssuesFilter(out var capturedFilterInput, issuesToReturnFromFilter);

            var publisher = new SnapshotPublisher();
            var testSubject = CreateTestSubject(issuesFilter.Object, publisher.PublishSnapshot);

            // Act
            testSubject.HandleNewIssues(new[] { filteredIssue });

            // Assert
            // Check the expected issues were passed to the filter
            capturedFilterInput.Count.Should().Be(1);
            capturedFilterInput[0].Should().Be(filteredIssue);

            publisher.InvocationCount.Should().Be(1);
            publisher.PublishedSnapshot.Issues.Count().Should().Be(0);
        }

        [TestMethod]
        public void WhenNewFileLevelIssuesAreFound_IssueNotFiltered_ListenersAreUpdated()
        {
            // Arrange
            var issues = new[] { CreateFileLevelIssue("File Level Issue") };

            var issuesFilter = CreateIssuesFilter(out var capturedFilterInput, issues);
            var publisher = new SnapshotPublisher();
            var testSubject = CreateTestSubject(issuesFilter.Object, publisher.PublishSnapshot);

            // Act
            testSubject.HandleNewIssues(issues);

            // Assert
            // Check the expected issues were passed to the filter
            capturedFilterInput.Count.Should().Be(1);
            capturedFilterInput.Should().BeEquivalentTo(issues);

            publisher.InvocationCount.Should().Be(1);
            publisher.PublishedSnapshot.Issues.Count().Should().Be(1);
        }

        [TestMethod]
        public void WhenNoIssuesAreFound_ListenersAreUpdated()
        {
            // Arrange
            var issuesFilter = CreateIssuesFilter(out var capturedFilterInput, Enumerable.Empty<IFilterableIssue>());
            var publisher = new SnapshotPublisher();
            var testSubject = CreateTestSubject(issuesFilter.Object, publisher.PublishSnapshot);

            // Act
            testSubject.HandleNewIssues(Enumerable.Empty<IAnalysisIssueVisualization>());

            // Assert
            capturedFilterInput.Should().BeEmpty();
            publisher.InvocationCount.Should().Be(1);
            publisher.PublishedSnapshot.Issues.Count().Should().Be(0);
        }

        private Mock<IIssuesFilter> CreateIssuesFilter(out IList<IFilterableIssue> capturedFilterInput,
            IEnumerable<IFilterableIssue> optionalDataToReturn = null /* if null then the supplied input will be returned */)
        {
            var issuesFilter = new Mock<IIssuesFilter>();
            var captured = new List<IFilterableIssue>();
            issuesFilter.Setup(x => x.Filter(It.IsAny<IEnumerable<IFilterableIssue>>()))
                .Callback((IEnumerable<IFilterableIssue> inputIssues) => captured.AddRange(inputIssues))
                .Returns(optionalDataToReturn ?? captured);
            capturedFilterInput = captured;
            return issuesFilter;
        }

        private static IAnalysisIssueVisualization CreateIssue(string ruleKey, int startLine, int endLine)
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
                }
            };

            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(issue);
            issueVizMock.Setup(x => x.Location).Returns(issue.PrimaryLocation);
            issueVizMock.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());
            issueVizMock.SetupProperty(x => x.Span);
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

        private static IssueHandler CreateTestSubject(IIssuesFilter issuesFilter,
            PublishSnapshot publishSnapshot)
            => CreateTestSubject(issuesFilter, publishSnapshot, "any project name", Guid.NewGuid(), "any file.txt");

        private static IssueHandler CreateTestSubject(IIssuesFilter issuesFilter,
            PublishSnapshot publishSnapshot, string projectName, Guid projectGuid, string filePath)
        {
            issuesFilter ??= Mock.Of<IIssuesFilter>();
            publishSnapshot ??= Mock.Of<PublishSnapshot>();

            var testSubject = new IssueHandler(
                CreateValidTextDocument(filePath),
                projectName,
                projectGuid,                
                issuesFilter,
                publishSnapshot,
                // Override the un-testable "TranslateSpans" behaviour
                PassthroughSpanTranslator);

            return testSubject;
        }

        private static IEnumerable<IAnalysisIssueVisualization> PassthroughSpanTranslator(IEnumerable<IAnalysisIssueVisualization> issues, ITextSnapshot activeSnapshot)
            => issues;

        internal class SnapshotPublisher
        {
            public IIssuesSnapshot PublishedSnapshot { get; private set; }

            public int InvocationCount { get; private set; }

            public void PublishSnapshot(IIssuesSnapshot newSnapshot)
            {
                InvocationCount++;
                PublishedSnapshot = newSnapshot;
            }
        }
    }
}
