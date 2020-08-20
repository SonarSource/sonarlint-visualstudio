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
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IssuesSnapshotTests
    {
        private const string ValidProjectName = "aproject";
        private const string ValidFilePath = "c:\\file.txt";
        private readonly IEnumerable<IssueMarker> ValidMarkerList = new IssueMarker[] { CreateMarker() };

        private IssuesSnapshot snapshot;
        private DummyAnalysisIssue issue;

        [TestInitialize]
        public void SetUp()
        {
            var path = "foo.js";
            issue = new DummyAnalysisIssue()
            {
                FilePath = path,
                Message = "This is dangerous",
                RuleKey = "javascript:123",
                Severity = AnalysisIssueSeverity.Blocker,
            };

            var mockTextSnap = new Mock<ITextSnapshot>();
            mockTextSnap.Setup(t => t.Length).Returns(50);

            var mockTextSnapLine = new Mock<ITextSnapshotLine>();
            mockTextSnapLine.Setup(l => l.LineNumber).Returns(12);
            mockTextSnapLine.Setup(l => l.Start).Returns(new SnapshotPoint(mockTextSnap.Object, 10));

            mockTextSnap.Setup(t => t.GetLineFromPosition(25)).Returns(mockTextSnapLine.Object);
            var textSnap = mockTextSnap.Object;

            var marker = new IssueMarker(issue, new SnapshotSpan(new SnapshotPoint(textSnap, 25), new SnapshotPoint(textSnap, 27)), "whole line text", "line hash");

            snapshot = IssuesSnapshot.CreateNew("MyProject", path, new List<IssueMarker>() { marker });
        }

        [TestMethod]
        public void GetValue_Line()
        {
            GetValue(StandardTableKeyNames.Line).Should().Be(12);
        }

        [TestMethod]
        public void GetValue_Column()
        {
            GetValue(StandardTableKeyNames.Column).Should().Be(25 - 10);
        }

        [TestMethod]
        public void GetValue_Path()
        {
            GetValue(StandardTableKeyNames.DocumentName).Should().Be(issue.FilePath);
        }

        [TestMethod]
        public void GetValue_Message()
        {
            GetValue(StandardTableKeyNames.Text).Should().Be(issue.Message);
        }

        [TestMethod]
        public void GetValue_ErrorCode()
        {
            GetValue(StandardTableKeyNames.ErrorCode).Should().Be(issue.RuleKey);
        }

        [TestMethod]
        public void GetValue_Severity()
        {
            GetValue(StandardTableKeyNames.ErrorSeverity).Should().NotBeNull();
        }

        [TestMethod]
        public void GetValue_BuildTool()
        {
            GetValue(StandardTableKeyNames.BuildTool).Should().Be("SonarLint");
        }

        [TestMethod]
        public void GetValue_ErrorRank_Other()
        {
            GetValue(StandardTableKeyNames.ErrorRank).Should().Be(ErrorRank.Other);
        }

        [TestMethod]
        public void GetValue_ErrorCategory_Is_CodeSmell_By_Default()
        {
            GetValue(StandardTableKeyNames.ErrorCategory).Should().Be("Blocker Code Smell");
        }

        [TestMethod]
        public void GetValue_ErrorCategory_Is_Issue_Type()
        {
            issue.Type = AnalysisIssueType.Bug;
            issue.Severity = AnalysisIssueSeverity.Blocker;
            GetValue(StandardTableKeyNames.ErrorCategory).Should().Be("Blocker Bug");
            issue.Type = AnalysisIssueType.CodeSmell;
            GetValue(StandardTableKeyNames.ErrorCategory).Should().Be("Blocker Code Smell");
            issue.Type = AnalysisIssueType.Vulnerability;
            GetValue(StandardTableKeyNames.ErrorCategory).Should().Be("Blocker Vulnerability");
        }

        [TestMethod]
        public void GetValue_ErrorCodeToolTip()
        {
            issue.RuleKey = "javascript:123";
            GetValue(StandardTableKeyNames.ErrorCodeToolTip).Should().Be("Open description of rule javascript:123");
        }

        [TestMethod]
        public void GetValue_HelpLink()
        {
            issue.RuleKey = "javascript:123";
            GetValue(StandardTableKeyNames.HelpLink).Should().Be("https://rules.sonarsource.com/javascript/RSPEC-123");
        }

        [TestMethod]
        public void GetValue_ProjectName()
        {
            GetValue(StandardTableKeyNames.ProjectName).Should().Be("MyProject");
        }

        [TestMethod]
        public void GetValue_Issue()
        {
            GetValue(SonarLintTableControlConstants.IssueColumnName).Should().BeSameAs(issue);
        }

        [TestMethod]
        public void Construction_CreateNew_SetsProperties()
        {
            var snapshot = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);

            snapshot.AnalysisRunId.Should().NotBe(Guid.Empty);
            snapshot.VersionNumber.Should().BeGreaterOrEqualTo(0);
            GetProjectName(snapshot).Should().BeEquivalentTo(ValidProjectName);
            GetFilePath(snapshot).Should().BeEquivalentTo(ValidFilePath);
            snapshot.IssueMarkers.Should().BeEquivalentTo(ValidMarkerList);
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
            GetFilePath(revised).Should().NotBe(ValidFilePath);
            GetFilePath(revised).Should().Be("new path");

            // Unchanged
            revised.IssueMarkers.Should().BeEquivalentTo(original.IssueMarkers);
            GetProjectName(revised).Should().Be(GetProjectName(original));
        }

        [TestMethod]
        public void Construction_UpdateIssues_PreservesIdAndUpdatesVersion()
        {
            // Both snapshots must have same number of issues for this test case
            var originalIssues = new IssueMarker[] { CreateMarker("hash1") };
            var newIssues = new IssueMarker[] { CreateMarker("hash2") };

            var original = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, originalIssues);
            var revised = original.CreateUpdatedSnapshot(newIssues);

            revised.AnalysisRunId.Should().Be(original.AnalysisRunId);
            revised.VersionNumber.Should().BeGreaterThan(original.VersionNumber);
            revised.IssueMarkers.Should().NotBeEquivalentTo(originalIssues);

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
                .Should().Be(-1);
        }

        [TestMethod]
        public void IndexOf_NotAnIssuesSnapshot_ReturnsMinusOne()
        {
            var original = IssuesSnapshot.CreateNew(ValidProjectName, ValidFilePath, ValidMarkerList);

            original.IndexOf(999, Mock.Of<ITableEntriesSnapshot>())
                .Should().Be(-1);
        }

        private object GetValue(string columnName)
        {
            object content;
            snapshot.TryGetValue(0, columnName, out content).Should().BeTrue();
            return content;
        }

        private static string GetProjectName(IssuesSnapshot snapshot) =>
            GetValue<string>(snapshot, StandardTableKeyNames.ProjectName);

        private static string GetFilePath(IssuesSnapshot snapshot) =>
            GetValue<string>(snapshot, StandardTableKeyNames.DocumentName);

        private static T GetValue<T>(IssuesSnapshot snapshot, string columnName)
        {
            object content;
            snapshot.TryGetValue(0, columnName, out content).Should().BeTrue();

            content.Should().BeOfType<T>();
            return (T)content;
        }

        private static IssueMarker CreateMarker(string lineHash = "dummy hash") =>
            new IssueMarker(Mock.Of<IAnalysisIssue>(), new SnapshotSpan(), "dummy text", lineHash);
    }
}
