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
        private readonly IEnumerable<IssueMarker> ValidMarkerList = new IssueMarker[] { CreateMarker() };

        [TestMethod]
        public void Construction_CreateNew_SetsProperties()
        {
            var testSubject = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);

            testSubject.AnalysisRunId.Should().NotBe(Guid.Empty);
            testSubject.VersionNumber.Should().BeGreaterOrEqualTo(0);
            GetProjectName(testSubject).Should().BeEquivalentTo(ValidProjectName);
            GetFilePath(testSubject).Should().BeEquivalentTo(ValidFilePath);
            testSubject.IssueMarkers.Should().BeEquivalentTo(ValidMarkerList);
        }


        [TestMethod]
        public void Construction_CreateNew_SetsUniqueId()
        {
            var snapshot1 = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);
            var snapshot2 = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);

            snapshot1.AnalysisRunId.Should().NotBe(snapshot2.AnalysisRunId);
            snapshot2.VersionNumber.Should().BeGreaterThan(snapshot1.VersionNumber);
        }

        [TestMethod]
        public void Construction_UpdateFilePath_PreservesIdAndUpdatesVersion()
        {
            var original = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);
            var revised = original.CreateUpdatedSnapshot("new path");

            revised.AnalysisRunId.Should().Be(original.AnalysisRunId);
            revised.VersionNumber.Should().BeGreaterThan(original.VersionNumber);
            GetFilePath(original).Should().Be(ValidFilePath);
            GetFilePath(revised).Should().Be("new path");

            // Other properties
            revised.IssueMarkers.Should().BeEquivalentTo(original.IssueMarkers);
            GetProjectName(revised).Should().Be(GetProjectName(original));
        }

        [TestMethod]
        public void Construction_UpdateIssues_PreservesIdAndUpdatesVersion()
        {
            // Both snapshots must have same number of issues for this test case
            var originalIssues = new IssueMarker[] { CreateMarker("hash1") };
            var newIssues = new IssueMarker[] { CreateMarker("hash2") };

            // Sanity check
            originalIssues.Should().NotBeEquivalentTo(newIssues);

            var original = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, originalIssues);
            var revised = original.CreateUpdatedSnapshot(newIssues);

            revised.AnalysisRunId.Should().Be(original.AnalysisRunId);
            revised.VersionNumber.Should().BeGreaterThan(original.VersionNumber);
            revised.IssueMarkers.Should().BeEquivalentTo(newIssues);

            // Unchanged
            GetProjectName(revised).Should().Be(GetProjectName(original));
            GetFilePath(revised).Should().Be(GetFilePath(original));
        }

        [TestMethod]
        public void Construction_ReviseIssues_NumberOfIssuesChanged_Throws()
        {
            var originalIssues = new IssueMarker[] { CreateMarker(), CreateMarker() };
            var newIssues = new IssueMarker[] { CreateMarker() };

            var original = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, originalIssues);

            Action act = () => original.CreateUpdatedSnapshot(newIssues);

            act.Should().ThrowExactly<ArgumentException>()
                .And.ParamName.Should().Be("updatedIssueMarkers");
        }

        [TestMethod]
        public void IndexOf_SameSnapshotId_ReturnExpectedIndex()
        {
            var original = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);
            var revised = original.CreateUpdatedSnapshot("unimportant change");

            // Should be able to map issues between two snapshots with the same snapshot id
            original.IndexOf(999, revised)
                .Should().Be(999);
        }

        [TestMethod]
        public void IndexOf_DifferentSnapshotId_ReturnMinusOne()
        {
            var snapshot1 = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);
            var snapshot2 = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);

            // Should not be able to map issues between two snapshots with different snapshot ids
            snapshot1.IndexOf(999, snapshot2)
                .Should().Be(IndexOf_NotFoundResult);
        }

        [TestMethod]
        public void IndexOf_NotAnIssuesSnapshot_ReturnsMinusOne()
        {
            var original = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);

            original.IndexOf(999, Mock.Of<ITableEntriesSnapshot>())
                .Should().Be(IndexOf_NotFoundResult);
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

        private static IssueMarker CreateMarker(string ruleKey = "rule key") =>
            new IssueMarker(CreateIssueViz(new DummyAnalysisIssue { RuleKey = ruleKey }), new SnapshotSpan());

        private static IAnalysisIssueVisualization CreateIssueViz(IAnalysisIssue issue)
        {
            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(issue);

            issueVizMock.SetupProperty(x => x.Span);
            return issueVizMock.Object;
        }
    }
}
