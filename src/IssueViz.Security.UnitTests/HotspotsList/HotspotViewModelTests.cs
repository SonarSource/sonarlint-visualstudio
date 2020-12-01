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

using System.ComponentModel;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotViewModelTests
    {
        [TestMethod]
        public void Ctor_RegisterToHotspotPropertyChangedEvent()
        {
            var hotspot = new Mock<IAnalysisIssueVisualization>();
            hotspot.SetupAdd(x => x.PropertyChanged += null);

            new HotspotViewModel(hotspot.Object);

            hotspot.VerifyAdd(x => x.PropertyChanged += It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromHotspotPropertyChangedEvent()
        {
            var hotspot = new Mock<IAnalysisIssueVisualization>();
            hotspot.SetupRemove(x => x.PropertyChanged -= null);

            var testSubject = new HotspotViewModel(hotspot.Object);
            testSubject.Dispose();

            hotspot.VerifyRemove(x => x.PropertyChanged -= It.IsAny<PropertyChangedEventHandler>(), Times.Once);
        }

        [TestMethod]
        public void HotspotPropertyChanged_UnknownProperty_NoPropertyChangedEvent()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var hotspot = new Mock<IAnalysisIssueVisualization>();

            var testSubject = new HotspotViewModel(hotspot.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            hotspot.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs("some dummy property"));

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void HotspotPropertyChanged_SpanProperty_RaisesPropertyChangedForLineAndColumn()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var hotspot = new Mock<IAnalysisIssueVisualization>();

            var testSubject = new HotspotViewModel(hotspot.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            hotspot.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.Span)));

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(IHotspotViewModel.Line))),
                Times.Once);

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(IHotspotViewModel.Column))),
                Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Line_NoSpan_ReturnsHotspotStartLine(bool spanIsNull)
        {
            const int originalLineNumber = 123;
            var issueViz = CreateIssueVizWithoutSpan(spanIsNull, originalLineNumber: originalLineNumber);

            var testSubject = new HotspotViewModel(issueViz);
            testSubject.Line.Should().Be(originalLineNumber);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Column_NoSpan_ReturnsOneBasedHotspotStartLineOffset(bool spanIsNull)
        {
            const int originalColumnNumber = 456;
            var issueViz = CreateIssueVizWithoutSpan(spanIsNull, originalColumnNumber: originalColumnNumber);

            var testSubject = new HotspotViewModel(issueViz);
            testSubject.Column.Should().Be(originalColumnNumber + 1);
        }

        [TestMethod]
        public void Line_HasSpan_ReturnsOneBasedSpanStartLine()
        {
            const int lineNumber = 12;
            var issueViz = CreateIssueVizWithSpan(lineNumber: lineNumber);

            var testSubject = new HotspotViewModel(issueViz);
            testSubject.Line.Should().Be(lineNumber + 1);
        }

        [TestMethod]
        public void Column_HasSpan_ReturnsOneBasedSpanStartLine()
        {
            const int columnNumber = 15;
            var issueViz = CreateIssueVizWithSpan(columnNumber: columnNumber);

            var testSubject = new HotspotViewModel(issueViz);
            testSubject.Column.Should().Be(columnNumber + 1);
        }

        [TestMethod]
        public void DisplayPath_HotspotHasNoFilePath_ReturnsServerPath()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.ServerFilePath).Returns("\\some\\server\\path.cs");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns((string)null);
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            var testSubject = new HotspotViewModel(issueViz.Object);
            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void DisplayPath_HotspotHasFilePath_ReturnsFilePath()
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns("c:\\some\\local\\path.cs");

            var testSubject = new HotspotViewModel(issueViz.Object);
            testSubject.DisplayPath.Should().Be("path.cs");
        }

        [TestMethod]
        public void CategoryDisplayName_SecurityCategoryIsKnown_ReturnsFriendlyName()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.Rule.SecurityCategory).Returns("auth");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            var testSubject = new HotspotViewModel(issueViz.Object);
            testSubject.CategoryDisplayName.Should().Be("Authentication");
        }

        [TestMethod]
        public void CategoryDisplayName_SecurityCategoryIsUnknown_ReturnsEmptyString()
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.Rule.SecurityCategory).Returns("some dummy category");

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            var testSubject = new HotspotViewModel(issueViz.Object);
            testSubject.CategoryDisplayName.Should().BeEmpty();
        }

        [TestMethod]
        public void HotspotPropertyChanged_CurrentFilePathProperty_RaisesPropertyChangedForDisplayPath()
        {
            var eventHandler = new Mock<PropertyChangedEventHandler>();
            var hotspot = new Mock<IAnalysisIssueVisualization>();

            var testSubject = new HotspotViewModel(hotspot.Object);
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            hotspot.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IAnalysisIssueVisualization.CurrentFilePath)));

            eventHandler.Verify(x => x(testSubject,
                    It.Is((PropertyChangedEventArgs args) => args.PropertyName == nameof(IHotspotViewModel.DisplayPath))),
                Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        private static IAnalysisIssueVisualization CreateIssueVizWithoutSpan(bool spanIsNull, int originalLineNumber = 123, int originalColumnNumber = 456)
        {
            var hotspot = new Mock<IAnalysisIssueBase>();
            hotspot.SetupGet(x => x.StartLine).Returns(originalLineNumber);
            hotspot.SetupGet(x => x.StartLineOffset).Returns(originalColumnNumber);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);
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
