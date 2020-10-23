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

using FluentAssertions;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotTableEntryTests
    {
        [TestMethod]
        public void Identity_ReturnsIssueViz()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();

            var testSubject = new HotspotTableEntry(issueViz);
            var identity = testSubject.Identity;

            identity.Should().Be(issueViz);
        }

        [TestMethod]
        public void TryGetValue_ErrorCodeColumn_ReturnsIssueRuleKey()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.RuleKey).Returns("test key");

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.ErrorCode);
            value.Should().Be("test key");
        }

        [TestMethod]
        public void TryGetValue_PriorityColumn_ReturnsConvertedIssueSeverity()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Severity).Returns(AnalysisIssueSeverity.Blocker);

            var severityToPriorityConverter = new Mock<IAnalysisSeverityToPriorityConverter>();
            severityToPriorityConverter
                .Setup(x => x.Convert(AnalysisIssueSeverity.Blocker))
                .Returns("test priority");

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.Priority, severityToPriorityConverter.Object);
            value.Should().Be("test priority");
        }

        [TestMethod]
        public void TryGetValue_DocumentNameColumn_ReturnsIssueFilePath()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.FilePath).Returns("test path");

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.DocumentName);
            value.Should().Be("test path");
        }

        [TestMethod]
        public void TryGetValue_TextColumn_ReturnsIssueMessage()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.Message).Returns("test message");

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.Text);
            value.Should().Be("test message");
        }

        [TestMethod]
        public void TryGetValue_LineColumn_NoSpan_ReturnsIssueStartLine()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.StartLine).Returns(123);

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.Line);
            value.Should().Be(123);
        }

        [TestMethod]
        public void TryGetValue_LineColumn_HasSpan_ReturnsSpanStartLine()
        {
            var issueViz = CreateIssueVizWithSpan();

            var result = new HotspotTableEntry(issueViz).TryGetValue(StandardTableColumnDefinitions.Line, out var value);
            result.Should().BeTrue();
            value.Should().Be(12);
        }

        [TestMethod]
        public void TryGetValue_Column_NoSpan_ReturnsIssueStartPosition()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.StartLineOffset).Returns(456);

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.Column);
            value.Should().Be(456);
        }

        [TestMethod]
        public void TryGetValue_Column_HasSpan_ReturnsSpanPosition()
        {
            var issueViz = CreateIssueVizWithSpan();

            var result = new HotspotTableEntry(issueViz).TryGetValue(StandardTableColumnDefinitions.Column, out var value);
            result.Should().BeTrue();
            value.Should().Be(15);
        }

        [TestMethod]
        public void TryGetValue_UnknownColumn_ReturnsNull()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());

            var result = testSubject.TryGetValue("dummy column", out var content);
            result.Should().BeFalse();
            content.Should().BeNull();
        }

        [TestMethod]
        public void TryCreateToolTip_Null()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.TryCreateToolTip(StandardTableColumnDefinitions.DocumentName, out var value);

            result.Should().BeFalse();
            value.Should().BeNull();
        }

        [TestMethod]
        public void CanSetValue_False()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.CanSetValue(StandardTableColumnDefinitions.DocumentName);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void TrySetValue_False()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.FilePath).Returns("unchanged file path");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);

            var testSubject = new HotspotTableEntry(issueViz.Object);
            var result = testSubject.TrySetValue(StandardTableColumnDefinitions.DocumentName, "test");
            result.Should().BeFalse();

            testSubject.TryGetValue(StandardTableColumnDefinitions.DocumentName, out var value);
            value.Should().Be("unchanged file path");
        }

        [TestMethod]
        public void CanCreateDetailsContent_False()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.CanCreateDetailsContent();
            result.Should().BeFalse();
        }

        [TestMethod]
        public void TryCreateDetailsContent_False()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.TryCreateDetailsContent(out var value);
            result.Should().BeFalse();
            value.Should().BeNull();
        }

        [TestMethod]
        public void TryCreateDetailsStringContent_False()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.TryCreateDetailsStringContent(out var value);
            result.Should().BeFalse();
            value.Should().BeNull();
        }

        [TestMethod]
        public void TryCreateColumnContent_False()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.TryCreateColumnContent("column", true, out var value);
            result.Should().BeFalse();
            value.Should().BeNull();
        }

        [TestMethod]
        public void TryCreateImageContent_False()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.TryCreateImageContent("column", true, out var value);
            result.Should().BeFalse();
            value.Should().BeEquivalentTo(default(ImageMoniker));
        }

        [TestMethod]
        public void TryCreateStringContent_False()
        {
            var testSubject = new HotspotTableEntry(Mock.Of<IAnalysisIssueVisualization>());
            var result = testSubject.TryCreateStringContent("column", true, true, out var value);
            result.Should().BeFalse();
            value.Should().BeNull();
        }

        private static object GetValue(IAnalysisIssue issue, string column, IAnalysisSeverityToPriorityConverter priorityConverter = null)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue);

            priorityConverter ??= Mock.Of<IAnalysisSeverityToPriorityConverter>();

            var tryGetValue = new HotspotTableEntry(issueViz.Object, priorityConverter).TryGetValue(column, out var value);
            tryGetValue.Should().BeTrue();

            return value;
        }

        private IAnalysisIssueVisualization CreateIssueVizWithSpan()
        {
            var mockTextSnap = new Mock<ITextSnapshot>();
            mockTextSnap.Setup(t => t.Length).Returns(50);

            var mockTextSnapLine = new Mock<ITextSnapshotLine>();
            mockTextSnapLine.Setup(l => l.LineNumber).Returns(12);
            mockTextSnapLine.Setup(l => l.Start).Returns(new SnapshotPoint(mockTextSnap.Object, 10));

            mockTextSnap.Setup(t => t.GetLineFromPosition(25)).Returns(mockTextSnapLine.Object);
            var textSnap = mockTextSnap.Object;

            var issueVizMock = new Mock<IAnalysisIssueVisualization>();
            issueVizMock.Setup(x => x.Issue).Returns(Mock.Of<IAnalysisIssue>());
            issueVizMock.SetupProperty(x => x.Span);
            issueVizMock.Object.Span = new SnapshotSpan(new SnapshotPoint(textSnap, 25), new SnapshotPoint(textSnap, 27));

            return issueVizMock.Object;
        }
    }
}
