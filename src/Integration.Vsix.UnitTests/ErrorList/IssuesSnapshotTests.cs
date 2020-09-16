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
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IssuesSnapshotTests
    {
        private const int IndexOf_NotFoundResult = -1;
        private const string ValidProjectName = "aproject";
        private const string ValidFilePath = "c:\\file.txt";
        private readonly IEnumerable<IAnalysisIssueVisualization> ValidIssueList = new[] { CreateIssue() };

        [TestMethod]
        public void Construction_CreateNew_SetsProperties()
        {
            var testSubject = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);

            testSubject.AnalysisRunId.Should().NotBe(Guid.Empty);
            testSubject.VersionNumber.Should().BeGreaterOrEqualTo(0);
            GetProjectName(testSubject).Should().BeEquivalentTo(ValidProjectName);
            GetFilePath(testSubject).Should().BeEquivalentTo(ValidFilePath);
            testSubject.Issues.Should().BeEquivalentTo(ValidIssueList);
        }

        [TestMethod]
        public void Construction_CreateNew_SetsUniqueId()
        {
            var snapshot1 = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);
            var snapshot2 = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);

            snapshot1.AnalysisRunId.Should().NotBe(snapshot2.AnalysisRunId);
            snapshot2.VersionNumber.Should().BeGreaterThan(snapshot1.VersionNumber);
        }

        [TestMethod]
        public void Construction_UpdateFilePath_PreservesIdAndUpdatesVersion()
        {
            var original = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);
            var revised = original.CreateUpdatedSnapshot("new path");

            revised.AnalysisRunId.Should().Be(original.AnalysisRunId);
            revised.VersionNumber.Should().BeGreaterThan(original.VersionNumber);
            GetFilePath(original).Should().Be(ValidFilePath);
            GetFilePath(revised).Should().Be("new path");

            // Other properties
            revised.Issues.Should().BeEquivalentTo(original.Issues);
            GetProjectName(revised).Should().Be(GetProjectName(original));
        }

        [TestMethod]
        public void Construction_CreateNew_NoLocations_FilesInSnapshotIsSetCorrectly()
        {
            var testSubject = new IssuesSnapshot(ValidProjectName, "analyzedFilePath.txt", Array.Empty<IAnalysisIssueVisualization>());
            testSubject.FilesInSnapshot.Should().BeEquivalentTo("analyzedFilePath.txt");
        }

        [TestMethod]
        public void Construction_CreateNew_FilesInSnapshotIsSetCorrectly()
        {
            var issue1 = CreateIssueWithSpecificsPaths("path1",     // primary location
                CreateFlowViz("path2"),                     // flow with one secondary location
                CreateFlowViz("path3", "path4", "PATH1"));  // flow with multiple secondary locations, including one duplicate in a different case

            var issue2 = CreateIssueWithSpecificsPaths("path5");    // new primary location, no flows

            var issue3 = CreateIssueWithSpecificsPaths("path2");    // duplicate primary location, no flows

            var issues = new[] { issue1, issue2, issue3 };

            var testSubject = new IssuesSnapshot(ValidProjectName, "analyzedFilePath.txt", issues);

            testSubject.FilesInSnapshot.Should().BeEquivalentTo("path1", "path2", "path3", "path4", "path5", "analyzedFilePath.txt");
        }

        [TestMethod]
        public void IndexOf_SameSnapshotId_ReturnExpectedIndex()
        {
            var original = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);
            var revised = original.CreateUpdatedSnapshot("unimportant change");

            // Should be able to map issues between two snapshots with the same snapshot id
            original.IndexOf(999, revised)
                .Should().Be(999);
        }

        [TestMethod]
        public void IndexOf_DifferentSnapshotId_ReturnMinusOne()
        {
            var snapshot1 = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);
            var snapshot2 = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);

            // Should not be able to map issues between two snapshots with different snapshot ids
            snapshot1.IndexOf(999, snapshot2)
                .Should().Be(IndexOf_NotFoundResult);
        }

        [TestMethod]
        public void IndexOf_NotAnIssuesSnapshot_ReturnsMinusOne()
        {
            var original = new IssuesSnapshot(ValidProjectName, ValidFilePath, ValidIssueList);

            original.IndexOf(999, Mock.Of<ITableEntriesSnapshot>())
                .Should().Be(IndexOf_NotFoundResult);
        }

        [TestMethod]
        public void GetLocationsVizForFile_NoMatches_ReturnsEmpty()
        {
            var issue1 = CreateIssueWithSpecificsPaths("path1.txt");
            var issues = new[] { issue1 };

            var testSubject = new IssuesSnapshot(ValidProjectName, ValidFilePath, issues);

            var actual = testSubject.GetLocationsVizsForFile("xxx");

            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocationsVizForFile_MatchesInPrimaryLocations_ReturnsExpected()
        {
            var issue1 = CreateIssueWithSpecificsPaths("path1.txt");
            var issue2 = CreateIssueWithSpecificsPaths("XXX.txt");
            var issue3 = CreateIssueWithSpecificsPaths("path1.txt");
            var issues = new[] { issue1, issue2, issue3 };

            var testSubject = new IssuesSnapshot(ValidProjectName, ValidFilePath, issues);

            var actual = testSubject.GetLocationsVizsForFile("path1.txt");

            actual.Should().BeEquivalentTo(issue1, issue3);
        }

        [TestMethod]
        public void GetLocationsVizForFile_MatchesInSecondaryLocations_ReturnsExpected()
        {
            var flow1 = CreateFlowViz("match.txt", "MATCH.TXT");
            var flow2 = CreateFlowViz("miss.txt", "Match.txt");
            var flow3 = CreateFlowViz("another miss.txt");

            var issue1 = CreateIssueWithSpecificsPaths("path1.txt", flow1, flow2, flow3);
            var issues = new[] { issue1 };

            var testSubject = new IssuesSnapshot(ValidProjectName, ValidFilePath, issues);

            var actual = testSubject.GetLocationsVizsForFile("match.txt");

            actual.Should().BeEquivalentTo(flow1.Locations[0], flow1.Locations[1], flow2.Locations[1]);
        }

        [TestMethod]
        public void IncrementVersion_VersionIncrement_AnalysisIdAndIssuesUnchanged()
        {
            var testSubject = new IssuesSnapshot(ValidProjectName, ValidProjectName, ValidIssueList);
            var originalVersion = testSubject.VersionNumber;
            var originalRunId = testSubject.AnalysisRunId;

            testSubject.IncrementVersion();

            testSubject.VersionNumber.Should().BeGreaterThan(originalVersion);
            testSubject.AnalysisRunId.Should().Be(originalRunId);
            testSubject.Issues.Should().BeEquivalentTo(ValidIssueList);
        }

        private static string GetProjectName(ITableEntriesSnapshot snapshot) =>
            GetValue<string>(snapshot, StandardTableKeyNames.ProjectName);

        private static string GetFilePath(ITableEntriesSnapshot snapshot) =>
            GetValue<string>(snapshot, StandardTableKeyNames.DocumentName);

        private static T GetValue<T>(ITableEntriesSnapshot snapshot, string columnName)
        {
            object content;
            snapshot.TryGetValue(0, columnName, out content).Should().BeTrue();

            content.Should().BeOfType<T>();
            return (T)content;
        }

        private static IAnalysisIssueVisualization CreateIssue(string ruleKey = "rule key") =>
            CreateIssueViz("filePath", CreateNonEmptySpan(), new DummyAnalysisIssue {RuleKey = ruleKey});

        private static IAnalysisIssueVisualization CreateIssueWithSpecificsPaths(string primaryFilePath, params IAnalysisIssueFlowVisualization[] flowVizs) =>
            CreateIssueViz(primaryFilePath, CreateNonEmptySpan(), Mock.Of<IAnalysisIssue>(), flowVizs);

        private static SnapshotSpan CreateNonEmptySpan()
        {
            var textSnapshot = new Mock<ITextSnapshot>();
            textSnapshot.SetupGet(x => x.Length).Returns(999);

            return new SnapshotSpan(textSnapshot.Object, new Span(0, 1));
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string filePath, SnapshotSpan span, IAnalysisIssue issue, params IAnalysisIssueFlowVisualization[] flowVizs)
        {
            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(issue);
            issueVizMock.Setup(x => x.Location).Returns(issue);
            issueVizMock.Setup(x => x.Flows).Returns(flowVizs);

            issueVizMock.SetupProperty(x => x.CurrentFilePath);
            issueVizMock.Object.CurrentFilePath = filePath;

            issueVizMock.SetupProperty(x => x.Span);
            issueVizMock.Object.Span = span;

            return issueVizMock.Object;
        }

        private static IAnalysisIssueFlowVisualization CreateFlowViz(params string[] locationFilePaths)
        {
            var locVizs = new List<IAnalysisIssueLocationVisualization>();

            foreach (var path in locationFilePaths)
            {
                locVizs.Add(CreateLocViz(path));
            }

            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.Setup(x => x.Locations).Returns(locVizs);
            return flowViz.Object;
        }

        private static IAnalysisIssueLocationVisualization CreateLocViz(string filePath)
        {
            var locViz = new Mock<IAnalysisIssueLocationVisualization>();
            locViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            return locViz.Object;
        }
    }
}
