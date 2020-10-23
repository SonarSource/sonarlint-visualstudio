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
        public void TryGetValue_LineColumn_ReturnsIssueStartLine()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.StartLine).Returns(123);

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.Line);
            value.Should().Be(123);
        }

        [TestMethod]
        public void TryGetValue_Column_ReturnsIssueStartPosition()
        {
            var issue = new Mock<IAnalysisIssue>();
            issue.SetupGet(x => x.StartLineOffset).Returns(456);

            var value = GetValue(issue.Object, StandardTableColumnDefinitions.Column);
            value.Should().Be(456);
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

        private static object GetValue(IAnalysisIssue issue, string column)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue);

            var tryGetValue = new HotspotTableEntry(issueViz.Object).TryGetValue(column, out object value);
            tryGetValue.Should().BeTrue();

            return value;
        }
    }
}
