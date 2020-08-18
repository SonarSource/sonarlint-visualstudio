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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class ISpanCalculatorTests
    {
        [TestMethod]
        public void ToMarker_Calculates_Span_Positions()
        {
            // Arrange
            var issue = new DummyAnalysisIssueLocation
            {
                StartLine = 3,
                StartLineOffset = 10,
                EndLine = 4,
                EndLineOffset = 20,
            };

            var firstLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 2,
                LineLength = 10,
                LineStartPosition = 35
            };

            var secondLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 3,
                LineLength = 25,
                LineStartPosition = 47
            };

            var textSnapshotMock = CreateSnapshotMock(firstLine, secondLine);

            // Act
            var result = new IssueSpanCalculator()
                .CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.Should().NotBeNull();
            result.Start.Position.Should().Be(45); // firstLine.LineStartPosition + issue.StartLineOffset
            result.End.Position.Should().Be(67); // secondLine.LineStartPosition + issue.EndLineOffset
        }

        [TestMethod]
        public void ToMarker_EndLineIsZero()
        {
            // If issue.EndLine is zero the whole of the start line should be selected

            // Arrange
            var issue = new DummyAnalysisIssueLocation
            {
                StartLine = 22,
                StartLineOffset = 2,
                EndLine = 0,
                EndLineOffset = 0,
            };

            var firstLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 21,
                LineLength = 34,
                LineStartPosition = 103
            };

            // The second VS line shouldn't be used in this case
            var secondLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 25,
                LineLength = 100,
                LineStartPosition = 1010
            };

            var textSnapshotMock = CreateSnapshotMock(firstLine, secondLine);

            // Act
            var result = new IssueSpanCalculator()
                .CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.Should().NotBeNull();
            result.Start.Position.Should().Be(103); // firstLine.LineStartPosition. Ignore issue.StartLineOffset in this case
            result.End.Position.Should().Be(137); // firstLine.LineStartPosition +  firstLine.LineLength
        }

        [TestMethod]
        public void ToMarker_StartPositionExceedsSnapshotLength()
        {
            // These values were taken from a real analysis issue
            // Rule "cpp:S113 - no newline at end of file" returns an offset after the end of the file.

            // Arrange
            var issue = new DummyAnalysisIssueLocation
            {
                StartLine = 53,
                StartLineOffset = 2,
                EndLine = 53,
                EndLineOffset = 3,
            };

            var firstLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 52,
                LineLength = 1,
                LineStartPosition = 1599
            };

            // Second line should not be used in this case
            var secondLine = new VSLineDescription
            {
                ZeroBasedLineNumber = -999,
                LineLength = -999,
                LineStartPosition = -999
            };

            var textSnapshotMock = CreateSnapshotMock(firstLine, secondLine, 1600);

            // Act
            var result = new IssueSpanCalculator()
                .CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.Should().NotBeNull();
            result.Start.Position.Should().Be(1599); // firstLine.LineStartPosition. Ignore offset because that will take us beyond the end of file
            result.End.Position.Should().Be(1600); // snapshot length
        }

        [TestMethod]
        public void ToMarker_EndPositionExceedsSnapshotLength()
        {
            // Defensive: handle the issue end position being beyond the end of the snapshot
            // (we have not seen this in practice so far)

            // Arrange
            var issue = new DummyAnalysisIssueLocation
            {
                StartLine = 53,
                StartLineOffset = 2,
                EndLine = 53,
                EndLineOffset = 12,
            };

            // The issue is on a single line in this case, but the issue end position
            // is beyond the end of the line.
            var vsLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 52,
                LineLength = 10,
                LineStartPosition = 1599
            };

            var textSnapshotMock = CreateSnapshotMock(vsLine, vsLine, 1602);

            // Act
            var result = new IssueSpanCalculator()
                .CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.Should().NotBeNull();
            result.Start.Position.Should().Be(1601); // vsLine.LineStartPosition + issue.StartLineOffset
            result.End.Position.Should().Be(1602); // snapshot length
        }


        private class VSLineDescription
        {
            public int ZeroBasedLineNumber { get; set; }
            public int LineStartPosition { get; set; }
            public int LineLength { get; set; }
        }

        private Mock<ITextSnapshot> CreateSnapshotMock(
            VSLineDescription startLine, VSLineDescription endLine, int snapShotLength = 10000)
        {
            var textSnapshotMock = new Mock<ITextSnapshot>();

            var startLineMock = CreateLineMock(textSnapshotMock.Object, startLine);
            var endLineMock = CreateLineMock(textSnapshotMock.Object, endLine);

            textSnapshotMock
                .SetupGet(x => x.Length)
                .Returns(snapShotLength);
            textSnapshotMock
                .Setup(x => x.GetLineFromLineNumber(startLine.ZeroBasedLineNumber))
                .Returns(() => startLineMock);
            textSnapshotMock
                .Setup(x => x.GetLineFromLineNumber(endLine.ZeroBasedLineNumber))
                .Returns(() => endLineMock);

            return textSnapshotMock;
        }

        private ITextSnapshotLine CreateLineMock(ITextSnapshot textSnapshot,
            VSLineDescription firstLine)
        {
            var startLineMock = new Mock<ITextSnapshotLine>();
            startLineMock.SetupGet(x => x.Start)
                .Returns(() => new SnapshotPoint(textSnapshot, firstLine.LineStartPosition));
            startLineMock.SetupGet(x => x.Length)
                    .Returns(() => new SnapshotPoint(textSnapshot, firstLine.LineLength));

            return startLineMock.Object;
        }
    }
}
