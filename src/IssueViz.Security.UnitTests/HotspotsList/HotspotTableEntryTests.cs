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

        private static object GetValue(IAnalysisIssue issue, string column)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue);

            new HotspotTableEntry(issueViz.Object).TryGetValue(column, out object value);
            return value;
        }
    }
}
