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

using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests
{
    [TestClass]
    public class IssueVizDisplayPositionCalculatorTests
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Line_NoSpan_ReturnsIssueVizStartLine(bool spanIsNull)
        {
            const int originalLineNumber = 123;
            var issueViz = CreateIssueVizWithoutSpan(spanIsNull, originalLineNumber: originalLineNumber);

            var testSubject = CreateTestSubject();
            testSubject.GetLine(issueViz).Should().Be(originalLineNumber);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Column_NoSpan_ReturnsOneBasedIssueVizStartLineOffset(bool spanIsNull)
        {
            const int originalColumnNumber = 456;
            var issueViz = CreateIssueVizWithoutSpan(spanIsNull, originalColumnNumber: originalColumnNumber);

            var testSubject = CreateTestSubject();
            testSubject.GetColumn(issueViz).Should().Be(originalColumnNumber + 1);
        }

        [TestMethod]
        public void Line_HasSpan_ReturnsOneBasedSpanStartLine()
        {
            const int lineNumber = 12;
            var issueViz = CreateIssueVizWithSpan(lineNumber: lineNumber);

            var testSubject = CreateTestSubject();
            testSubject.GetLine(issueViz).Should().Be(lineNumber + 1);
        }

        [TestMethod]
        public void Column_HasSpan_ReturnsOneBasedSpanStartLine()
        {
            const int columnNumber = 15;
            var issueViz = CreateIssueVizWithSpan(columnNumber: columnNumber);

            var testSubject = CreateTestSubject();
            testSubject.GetColumn(issueViz).Should().Be(columnNumber + 1);
        }

        private static IIssueVizDisplayPositionCalculator CreateTestSubject() =>
            new IssueVizDisplayPositionCalculator();

        private static IAnalysisIssueVisualization CreateIssueVizWithoutSpan(bool spanIsNull, int originalLineNumber = 123, int originalColumnNumber = 456)
        {
            var issue = new Mock<IAnalysisIssueBase>();
            issue.SetupGet(x => x.PrimaryLocation.TextRange.StartLine).Returns(originalLineNumber);
            issue.SetupGet(x => x.PrimaryLocation.TextRange.StartLineOffset).Returns(originalColumnNumber);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(issue.Object);
            issueViz.SetupProperty(x => x.Span);
            issueViz.Object.Span = spanIsNull ? (SnapshotSpan?)null : new SnapshotSpan();

            return issueViz.Object;
        }

        private static IAnalysisIssueVisualization CreateIssueVizWithSpan(int lineNumber = 1, int columnNumber = 2)
        {
            const int lineStartPosition = 10;
            var spanStartPosition = columnNumber + lineStartPosition;

            var mockTextSnap = new Mock<ITextSnapshot>();
            mockTextSnap.Setup(t => t.Length).Returns(50);

            var mockTextSnapLine = new Mock<ITextSnapshotLine>();
            mockTextSnapLine.Setup(l => l.LineNumber).Returns(lineNumber);
            mockTextSnapLine.Setup(l => l.Start).Returns(new SnapshotPoint(mockTextSnap.Object, lineStartPosition));

            mockTextSnap.Setup(t => t.GetLineFromPosition(spanStartPosition)).Returns(mockTextSnapLine.Object);

            var span = new SnapshotSpan(new SnapshotPoint(mockTextSnap.Object, spanStartPosition), new SnapshotPoint(mockTextSnap.Object, spanStartPosition + 1));

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupProperty(x => x.Span);
            issueViz.Object.Span = span;

            return issueViz.Object;
        }
    }
}
