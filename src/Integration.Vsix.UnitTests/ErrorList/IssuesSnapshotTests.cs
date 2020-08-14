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

            snapshot = new IssuesSnapshot("MyProject", path, 1, new List<IssueMarker>() { marker });
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
            issue.RuleKey = "javascript:S123";
            GetValue(StandardTableKeyNames.HelpLink).Should().Be("https://rules.sonarsource.com/javascript/RSPEC-123");
            issue.RuleKey = "javascript:SOMETHING";
            GetValue(StandardTableKeyNames.HelpLink).Should().Be("https://rules.sonarsource.com/javascript/RSPEC-SOMETHING");
            issue.RuleKey = "c:456";
            GetValue(StandardTableKeyNames.HelpLink).Should().Be("https://rules.sonarsource.com/c/RSPEC-456");
            issue.RuleKey = "cpp:789";
            GetValue(StandardTableKeyNames.HelpLink).Should().Be("https://rules.sonarsource.com/cpp/RSPEC-789");
            issue.RuleKey = "php:101112";
            GetValue(StandardTableKeyNames.HelpLink).Should().Be("https://rules.sonarsource.com/php/RSPEC-101112");
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

        private object GetValue(string columnName)
        {
            object content;
            snapshot.TryGetValue(0, columnName, out content).Should().BeTrue();
            return content;
        }
    }
}
