/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.TableControls;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IssuesSnapshotTests_GetValue
    {
        private IssuesSnapshot snapshot;
        private DummyAnalysisIssue issue;
        private IAnalysisIssueVisualization issueViz;
        private Guid projectGuid;

        [TestInitialize]
        public void SetUp()
        {
            const string path = "foo.js";
            issue = CreateIssue(path);
            projectGuid = Guid.NewGuid();

            var mockTextSnap = new Mock<ITextSnapshot>();
            mockTextSnap.Setup(t => t.Length).Returns(50);

            var mockTextSnapLine = new Mock<ITextSnapshotLine>();
            mockTextSnapLine.Setup(l => l.LineNumber).Returns(12);
            mockTextSnapLine.Setup(l => l.Start).Returns(new SnapshotPoint(mockTextSnap.Object, 10));

            mockTextSnap.Setup(t => t.GetLineFromPosition(25)).Returns(mockTextSnapLine.Object);
            var textSnap = mockTextSnap.Object;

            issueViz = CreateIssueViz(issue, new SnapshotSpan(new SnapshotPoint(textSnap, 25), new SnapshotPoint(textSnap, 27)));
            snapshot = CreateIssueSnapshot("MyProject", projectGuid, path, new List<IAnalysisIssueVisualization> { issueViz });
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void Count_ReturnsNumberOfIssues(int numberOfIssues)
        {
            var analysisIssueVisualizations = Enumerable.Repeat(issueViz, numberOfIssues);

            var testSubject = new IssuesSnapshot("MyProject", Guid.Empty, "foo.js", analysisIssueVisualizations);
            testSubject.Count.Should().Be(numberOfIssues);
        }

        [TestMethod]
        public void TryCreateDetailsStringContent_ReturnsIssueMessage()
        {
            snapshot.TryCreateDetailsStringContent(0, out var content);
            content.Should().Be("This is dangerous");
        }

        [TestMethod]
        public void GetValue_UnknownColumn_False()
        {
            AssertGetValueReturnsFalse(columnName: "asdsdgdsgrgddfgfg");
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(999)]
        public void GetValue_IndexOutOfRange_False(int index)
        {
            AssertGetValueReturnsFalse(index);
        }

        [DataRow(StandardTableKeyNames.Line)]
        [DataRow(StandardTableKeyNames.Column)]
        [TestMethod]
        public void GetValue_IssueFileLevel_ContentIsNull(string keyName)
        {
            string path = "foo.js";

            var analysisIssue = CreateIssue(path, true);

            var analysisIssueViz = CreateIssueViz(analysisIssue, new SnapshotSpan());

            var issueSnapshot = CreateIssueSnapshot("FileLevel", Guid.NewGuid(), path, new[] { analysisIssueViz });

            object content;
            issueSnapshot.TryGetValue(0, keyName, out content).Should().BeTrue();
            content.Should().BeNull();
        }

        [TestMethod]
        public void GetValue_IssueHasNoSpan_False()
        {
            issueViz.Span = null;

            AssertGetValueReturnsFalse();
        }

        [TestMethod]
        public void GetValue_IssueHasEmptySpan_False()
        {
            issueViz.InvalidateSpan();

            AssertGetValueReturnsFalse();
        }

        [TestMethod]
        public void Ctor_BaseIssueIsNotAnalysisIssue_InvalidCastException()
        {
            var issue = new Mock<IAnalysisIssueBase>();
            issue.Setup(i => i.PrimaryLocation).Returns(() =>
            {
                return new DummyAnalysisIssueLocation
                {
                    TextRange = new DummyTextRange()
                };
            });

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupGet(x => x.Issue).Returns(issue.Object);

            Action act = () => new IssuesSnapshot("test", projectGuid, "test.cpp", new[] { issueViz.Object });
            act.Should().Throw<InvalidCastException>();
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
            GetValue(StandardTableKeyNames.DocumentName).Should().Be(issue.PrimaryLocation.FilePath);
        }

        [TestMethod]
        public void GetValue_Message()
        {
            GetValue(StandardTableKeyNames.Text).Should().Be(issue.PrimaryLocation.Message);
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
        public void GetValue_ProjectGuid()
        {
            GetValue(StandardTableKeyNames.ProjectGuid).Should().Be(projectGuid);
        }

        [TestMethod]
        public void GetValue_Issue()
        {
            GetValue(SonarLintTableControlConstants.IssueVizColumnName).Should().BeSameAs(issueViz);
        }

        [TestMethod]
        public void GetValue_SuppressionState_Is_SuppressionState()
        {
            issueViz.IsSuppressed = true;
            GetValue(StandardTableKeyNames.SuppressionState).Should().BeSameAs(Boxes.SuppressionState.Suppressed);
            issueViz.IsSuppressed = false;
            GetValue(StandardTableKeyNames.SuppressionState).Should().BeSameAs(Boxes.SuppressionState.Active);
        }

        private object GetValue(string columnName)
        {
            object content;
            snapshot.TryGetValue(0, columnName, out content).Should().BeTrue();
            return content;
        }

        private void AssertGetValueReturnsFalse(int index = 0, string columnName = StandardTableKeyNames.Line)
        {
            object content;
            snapshot.TryGetValue(index, columnName, out content).Should().BeFalse();
            content.Should().BeNull();
        }

        private static IAnalysisIssueVisualization CreateIssueViz(IAnalysisIssue issue, SnapshotSpan snapshotSpan)
        {
            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(issue);
            issueVizMock.Setup(x => x.Location).Returns(new DummyAnalysisIssueLocation { FilePath = "any.txt" });
            issueVizMock.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());
            issueVizMock.SetupProperty(x => x.Span);
            issueVizMock.Object.Span = snapshotSpan;
            issueVizMock.SetupProperty(x => x.IsSuppressed);

            return issueVizMock.Object;
        }

        private DummyAnalysisIssue CreateIssue(string path, bool isFileLevel = false)
        {
            var analysisIssue = new DummyAnalysisIssue
            {
                PrimaryLocation = CreateIssueLocation(path, !isFileLevel),
                RuleKey = "javascript:123",
                Severity = AnalysisIssueSeverity.Blocker,
            };

            return analysisIssue;
        }

        private static DummyAnalysisIssueLocation CreateIssueLocation(string path, bool hasTextRange = true)
        {
            var issueLocation = new DummyAnalysisIssueLocation
            {
                FilePath = path,
                Message = "This is dangerous"
            };

            if (hasTextRange)
            {
                issueLocation.TextRange = new DummyTextRange();
            }
            return issueLocation;
        }

        private static IssuesSnapshot CreateIssueSnapshot(string projectName, Guid projectGuid, string filePath, IEnumerable<IAnalysisIssueVisualization> issues)
        {
            return new IssuesSnapshot(projectName, projectGuid, filePath, issues);
        }
    }
}
