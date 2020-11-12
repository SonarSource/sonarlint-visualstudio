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

using System.Windows;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.TableDataSource.CustomColumns;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotTableEntryTests
    {
        [TestMethod]
        public void Identity_ReturnsIssueViz()
        {
            var issueViz = CreateIssueViz();

            var testSubject = new HotspotTableEntry(issueViz, Mock.Of<IHotspotTableEntryWpfElementFactory>());
            var identity = testSubject.Identity;

            identity.Should().Be(issueViz);
        }

        [TestMethod]
        public void TryGetValue_ErrorCodeColumn_ReturnsHotspotRuleKey()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.RuleKey).Returns("test key");

            var value = GetValue(hotspot.Object, StandardTableColumnDefinitions.ErrorCode);
            value.Should().Be("test key");
        }

        [TestMethod]
        [DataRow(HotspotPriority.High, "High")]
        [DataRow(HotspotPriority.Medium, "Medium")]
        [DataRow(HotspotPriority.Low, "Low")]
        public void TryGetValue_PriorityColumn_ReturnsHotspotPriority(HotspotPriority priority, string expectedValue)
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.Priority).Returns(priority);

            var value = GetValue(hotspot.Object, PriorityTableColumnDefinition.ColumnName);
            value.Should().Be(expectedValue);
        }

        [TestMethod]
        public void TryGetValue_TextColumn_ReturnsHotspotMessage()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.Message).Returns("test message");

            var value = GetValue(hotspot.Object, StandardTableColumnDefinitions.Text);
            value.Should().Be("test message");
        }

        [TestMethod]
        public void TryGetValue_DocumentNameColumn_ReturnsHotspotFilePath()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.FilePath).Returns("test path");

            var value = GetValue(hotspot.Object, StandardTableColumnDefinitions.DocumentName);
            value.Should().Be("test path");
        }

        [TestMethod]
        public void TryGetValue_LineColumn_NoSpan_ReturnsHotspotStartLine()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.StartLine).Returns(123);

            var value = GetValue(hotspot.Object, StandardTableColumnDefinitions.Line);
            value.Should().Be(123);
        }

        [TestMethod]
        public void TryGetValue_LineColumn_EmptySpan_ReturnsHotspotStartLine()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.StartLine).Returns(123);

            var issueViz = CreateIssueViz(hotspot.Object);
            issueViz.Span = new SnapshotSpan();

            var result = new HotspotTableEntry(issueViz, Mock.Of<IHotspotTableEntryWpfElementFactory>()).TryGetValue(StandardTableColumnDefinitions.Line, out var value);
            result.Should().BeTrue();
            value.Should().Be(123);
        }

        [TestMethod]
        public void TryGetValue_LineColumn_HasSpan_ReturnsSpanStartLine()
        {
            const int lineNumber = 12;
            const int columnNumber = 15;
            var issueViz = CreateIssueVizWithSpan(lineNumber, columnNumber);

            var result = new HotspotTableEntry(issueViz, Mock.Of<IHotspotTableEntryWpfElementFactory>()).TryGetValue(StandardTableColumnDefinitions.Line, out var value);
            result.Should().BeTrue();
            value.Should().Be(lineNumber);
        }

        [TestMethod]
        public void TryGetValue_Column_NoSpan_ReturnsHotspotStartPosition()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.StartLineOffset).Returns(456);

            var value = GetValue(hotspot.Object, StandardTableColumnDefinitions.Column);
            value.Should().Be(456);
        }

        [TestMethod]
        public void TryGetValue_Column_EmptySpan_ReturnsHotspotStartPosition()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.StartLineOffset).Returns(456);

            var issueViz = CreateIssueViz(hotspot.Object);
            issueViz.Span = new SnapshotSpan();

            var result = new HotspotTableEntry(issueViz, Mock.Of<IHotspotTableEntryWpfElementFactory>()).TryGetValue(StandardTableColumnDefinitions.Column, out var value);
            result.Should().BeTrue();
            value.Should().Be(456);
        }

        [TestMethod]
        public void TryGetValue_Column_HasSpan_ReturnsSpanPosition()
        {
            const int lineNumber = 12;
            const int columnNumber = 15;
            var issueViz = CreateIssueVizWithSpan(lineNumber, columnNumber);

            var result = new HotspotTableEntry(issueViz, Mock.Of<IHotspotTableEntryWpfElementFactory>()).TryGetValue(StandardTableColumnDefinitions.Column, out var value);
            result.Should().BeTrue();
            value.Should().Be(columnNumber);
        }

        [TestMethod]
        public void TryGetValue_UnknownColumn_ReturnsNull()
        {
            var testSubject = new HotspotTableEntry(CreateIssueViz(), Mock.Of<IHotspotTableEntryWpfElementFactory>());

            var result = testSubject.TryGetValue("dummy column", out var content);
            result.Should().BeFalse();
            content.Should().BeNull();
        }

        [TestMethod]
        [DataRow(NavigabilityTableColumnDefinition.ColumnName)]
        [DataRow("dummy column")]
        public void TryCreateColumnContent_UnsupportedColumn_ReturnsNull(string column)
        {
            var elementFactory = new Mock<IHotspotTableEntryWpfElementFactory>();

            var testSubject = new HotspotTableEntry(CreateIssueViz(), elementFactory.Object);

            var result = testSubject.TryCreateColumnContent(column, true, out var content);
            result.Should().BeFalse();
            content.Should().BeNull();

            elementFactory.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void TryCreateColumnContent_SupportedColumn_ReturnsStyledColumn()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.SetupGet(x => x.FilePath).Returns("test path");

            var issueViz = CreateIssueViz(hotspot.Object);

            var element = new FrameworkElement();
            var elementFactory = new Mock<IHotspotTableEntryWpfElementFactory>();
            elementFactory.Setup(x => x.Create("test path")).Returns(element);

            var testSubject = new HotspotTableEntry(issueViz, elementFactory.Object);

            var result = testSubject.TryCreateColumnContent(StandardTableColumnDefinitions.DocumentName, true, out var content);
            result.Should().BeTrue();
            content.Should().NotBeNull();
            content.Should().Be(element);
        }

        private static object GetValue(IHotspot hotspot, string column)
        {
            var tryGetValue = new HotspotTableEntry(CreateIssueViz(hotspot), Mock.Of<IHotspotTableEntryWpfElementFactory>()).TryGetValue(column, out var value);
            tryGetValue.Should().BeTrue();

            return value;
        }

        private static IAnalysisIssueVisualization CreateIssueViz(IHotspot hotspot = null)
        {
            hotspot ??= Mock.Of<IHotspot>();
            var hotspotViz = new Mock<IAnalysisIssueVisualization>();
            hotspotViz.SetupGet(x => x.Issue).Returns(hotspot);

            return hotspotViz.Object;
        }

        private static IAnalysisIssueVisualization CreateIssueVizWithSpan(int lineNumber, int column)
        {
            const int lineStartPosition = 10;
            var spanStartPosition = column + lineStartPosition;

            var mockTextSnap = new Mock<ITextSnapshot>();
            mockTextSnap.Setup(t => t.Length).Returns(50);

            var mockTextSnapLine = new Mock<ITextSnapshotLine>();
            mockTextSnapLine.Setup(l => l.LineNumber).Returns(lineNumber);
            mockTextSnapLine.Setup(l => l.Start).Returns(new SnapshotPoint(mockTextSnap.Object, lineStartPosition));

            mockTextSnap.Setup(t => t.GetLineFromPosition(spanStartPosition)).Returns(mockTextSnapLine.Object);

            var span = new SnapshotSpan(new SnapshotPoint(mockTextSnap.Object, spanStartPosition), new SnapshotPoint(mockTextSnap.Object, spanStartPosition + 1));

            var issueViz = CreateIssueViz();
            Mock.Get(issueViz).SetupProperty(x => x.Span);
            issueViz.Span = span;

            return issueViz;
        }
    }
}
