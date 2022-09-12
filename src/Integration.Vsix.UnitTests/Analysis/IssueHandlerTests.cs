// TODO - duncanp

///*
// * SonarLint for Visual Studio
// * Copyright (C) 2016-2022 SonarSource SA
// * mailto:info AT sonarsource DOT com
// *
// * This program is free software; you can redistribute it and/or
// * modify it under the terms of the GNU Lesser General Public
// * License as published by the Free Software Foundation; either
// * version 3 of the License, or (at your option) any later version.
// *
// * This program is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// * Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public License
// * along with this program; if not, write to the Free Software Foundation,
// * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// */

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Microsoft.VisualStudio.Text;
//using Moq;
//using SonarLint.VisualStudio.Core.Suppression;
//using SonarLint.VisualStudio.Integration.Vsix;
//using SonarLint.VisualStudio.Integration.Vsix.Analysis;
//using SonarLint.VisualStudio.IssueVisualization.Models;
//using static SonarLint.VisualStudio.Integration.Vsix.Analysis.IssueConsumerFactory;

//namespace SonarLint.VisualStudio.Integration.UnitTests.Analysis
//{
//    internal class IssueHandlerTests
//    {
//        [TestMethod]
//        public void WhenNewIssuesAreFound_FilterIsApplied_NewSnapshotIsPublished()
//        {
//            var inputIssues = new[]
//            {
//                CreateIssue("S111", startLine: 1, endLine: 1),
//                CreateIssue("S222", startLine: 2, endLine: 2)
//            };

//            var issuesToReturnFromFilter = new[]
//            {
//                CreateIssue("xxx", startLine: 3, endLine: 3)
//            };

//            var issuesFilter = CreateIssuesFilter(out var issuesPassedToFilter, issuesToReturnFromFilter);

//            var testSubject = CreateTestSubject(issuesFilter: issuesFilter);

//            // Act
//            testSubject.HandleNewIssues(inputIssues);

//            // Assert
//            // Check the snapshot has changed
//            testSubject.Factory.CurrentSnapshot.AnalysisRunId.Should().NotBe(originalId);

//            // Check the expected issues were passed to the filter
//            issuesPassedToFilter.Count.Should().Be(2);
//            issuesPassedToFilter.Should().BeEquivalentTo(inputIssues, c => c.WithStrictOrdering());

//            CheckErrorListRefreshWasRequestedOnce(testSubject.Factory);

//            // Check the post-filter issues
//            testSubject.Factory.CurrentSnapshot.Issues.Count().Should().Be(1);
//            testSubject.Factory.CurrentSnapshot.Issues.First().Issue.RuleKey.Should().Be("xxx");

//            testSubject.Factory.CurrentSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectName, out var projectName).Should().BeTrue();
//            projectName.Should().Be("MyProject");

//            testSubject.Factory.CurrentSnapshot.TryGetValue(0, StandardTableKeyNames.ProjectGuid, out var projectGuid).Should().BeTrue();
//            projectGuid.Should().Be(projectGuid);
//        }

//        [TestMethod]
//        public void WhenNewIssuesAreFound_AndFilterRemovesAllIssues_ListenersAreUpdated()
//        {
//            mockSonarErrorDataSource.Invocations.Clear();

//            // Arrange
//            var filteredIssue = CreateIssue("single issue", startLine: 1, endLine: 1);

//            var issuesToReturnFromFilter = Enumerable.Empty<IAnalysisIssueVisualization>();
//            var issuesFilter = CreateIssuesFilter(out var capturedFilterInput, issuesToReturnFromFilter);

//            // Act
//            testSubject.HandleNewIssues(new[] { filteredIssue });

//            // Assert
//            // Check the expected issues were passed to the filter
//            capturedFilterInput.Count.Should().Be(1);
//            capturedFilterInput[0].Should().Be(filteredIssue);

//            CheckErrorListRefreshWasRequestedOnce(testSubject.Factory);

//            // Check there are no issues
//            testSubject.Factory.CurrentSnapshot.Issues.Count().Should().Be(0);
//        }

//        [TestMethod]
//        public void WhenNewFileLevelIssuesAreFound_IssueNotFiltered_ListenersAreUpdated()
//        {
//            mockSonarErrorDataSource.Invocations.Clear();

//            // Arrange
//            var issues = new[] { CreateFileLevelIssue("File Level Issue") };

//            var issuesFilter = CreateIssuesFilter(out var capturedFilterInput, issues);

//            // Act
//            testSubject.HandleNewIssues(issues);

//            // Assert
//            // Check the expected issues were passed to the filter
//            capturedFilterInput.Count.Should().Be(1);
//            capturedFilterInput.Should().BeEquivalentTo(issues);

//            CheckErrorListRefreshWasRequestedOnce(testSubject.Factory);

//            testSubject.Factory.CurrentSnapshot.Issues.Count().Should().Be(1);
//        }

//        [TestMethod]
//        public void WhenNoIssuesAreFound_ListenersAreUpdated()
//        {
//            mockSonarErrorDataSource.Invocations.Clear();

//            // Arrange
//            var issuesFilter = CreateIssuesFilter(out var capturedFilterInput, Enumerable.Empty<IFilterableIssue>());

//            // Act
//            testSubject.HandleNewIssues(Enumerable.Empty<IAnalysisIssueVisualization>());

//            // Assert
//            capturedFilterInput.Should().BeEmpty();

//            CheckErrorListRefreshWasRequestedOnce(testSubject.Factory);

//            testSubject.Factory.CurrentSnapshot.Issues.Count().Should().Be(0);
//        }

//        private Mock<IIssuesFilter> CreateIssuesFilter(out IList<IFilterableIssue> capturedFilterInput,
//            IEnumerable<IFilterableIssue> optionalDataToReturn = null /* if null then the supplied input will be returned */)
//        {
//            var issuesFilter = new Mock<IIssuesFilter>();
//            var captured = new List<IFilterableIssue>();
//            issuesFilter.Setup(x => x.Filter(It.IsAny<IEnumerable<IFilterableIssue>>()))
//                .Callback((IEnumerable<IFilterableIssue> inputIssues) => captured.AddRange(inputIssues))
//                .Returns(optionalDataToReturn ?? captured);
//            capturedFilterInput = captured;
//            return issuesFilter;
//        }

//        private void CheckSnapshotWasPublishedOnce(Mock<PublishSnapshot> publishSnapshot, IIssuesSnapshot expectedSnapshot)
//        {
//            publishSnapshot.Verify(x => x.Invoke(expectedSnapshot), Times.Once);
//        }

//        private static IAnalysisIssueVisualization CreateIssue(string ruleKey, int startLine, int endLine)
//        {
//            var issue = new DummyAnalysisIssue
//            {
//                RuleKey = ruleKey,
//                PrimaryLocation = new DummyAnalysisIssueLocation
//                {
//                    TextRange = new DummyTextRange
//                    {
//                        StartLine = startLine,
//                        EndLine = endLine,
//                    }
//                }
//            };

//            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
//            issueVizMock.Setup(x => x.Issue).Returns(issue);
//            issueVizMock.Setup(x => x.Location).Returns(issue.PrimaryLocation);
//            issueVizMock.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());
//            issueVizMock.SetupProperty(x => x.Span);
//            issueVizMock.Object.Span = new SnapshotSpan(CreateMockTextSnapshot(1000, "any line text").Object, 0, 1);

//            return issueVizMock.Object;
//        }

//        private static IAnalysisIssueVisualization CreateFileLevelIssue(string ruleKey)
//        {
//            var issue = new DummyAnalysisIssue
//            {
//                RuleKey = ruleKey,
//                PrimaryLocation = new DummyAnalysisIssueLocation { TextRange = null }
//            };

//            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
//            issueVizMock.Setup(x => x.Issue).Returns(issue);
//            issueVizMock.Setup(x => x.Location).Returns(issue.PrimaryLocation);
//            issueVizMock.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());

//            return issueVizMock.Object;
//        }

//        private static IssueHandler CreateTestSubject(ITextDocument textDocument = null,
//            string projectName = "any",
//            Guid projectGuid,
//            IIssuesFilter issuesFilter = null,
//            PublishSnapshot publishSnapshot = null)
//        {
//            issuesFilter ??= Mock.Of<IIssuesFilter>();
//            publishSnapshot ??= Mock.Of<PublishSnapshot>();

//            var testSubject = new IssueHandler(textDocument, projectName, projectGuid, issuesFilter, publishSnapshot);

//            return testSubject;
//        }
//    }
//}
