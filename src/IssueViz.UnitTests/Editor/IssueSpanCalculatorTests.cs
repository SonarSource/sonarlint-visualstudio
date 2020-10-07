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
using SonarLint.VisualStudio.Core.Helpers;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class IssueSpanCalculatorTests
    {
        private Mock<IChecksumCalculator> checksumCalculatorMock;

        private IssueSpanCalculator testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            checksumCalculatorMock = new Mock<IChecksumCalculator>();

            testSubject = new IssueSpanCalculator(checksumCalculatorMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueSpanCalculator, IIssueSpanCalculator>(null, new[]
            {
                MefTestHelpers.CreateExport<IChecksumCalculator>(checksumCalculatorMock.Object)
            });
        }

        [DataTestMethod]
        [DataRow(100, 1)]
        [DataRow(101, 100)]
        public void CalculateSpan_IssueStartLineIsOutsideOfSnapshot_ReturnsEmptySpan(int issueStartLine, int bufferLineCount)
        {
            // Arrange
            var issue = new DummyAnalysisIssue { StartLine = issueStartLine };
            var mockSnapshot = CreateSnapshotMock(bufferLineCount);

            // Act and assert
            var result = testSubject.CalculateSpan(issue, mockSnapshot.Object);
            result.IsEmpty.Should().BeTrue();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void CalculateSpan_IssueLineHashIsDifferent_ReturnsEmptySpan()
        {
            var issue = new DummyAnalysisIssue { StartLine = 10, LineHash = "some hash" };

            var startLine = new VSLineDescription
            {
                LineLength = 34,
                LineStartPosition = 103,
                ZeroBasedLineNumber = issue.StartLine - 1,
                Text = "unimportant"
            };

            var mockSnapshot = CreateSnapshotMock(lines: startLine);

            checksumCalculatorMock.Setup(x => x.Calculate(startLine.Text)).Returns("some other hash");

            var result = testSubject.CalculateSpan(issue, mockSnapshot.Object);
            result.IsEmpty.Should().BeTrue();

            checksumCalculatorMock.VerifyAll();
        }

        [TestMethod]
        public void CalculateSpan_IssueLinesInsideSnapshot_ReturnsSpanWithCorrectPositions()
        {
            // Arrange
            var issue = new DummyAnalysisIssueLocation
            {
                StartLine = 3,
                StartLineOffset = 10,
                EndLine = 4,
                EndLineOffset = 20,
                LineHash = "some hash"
            };

            var firstLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 2,
                LineLength = 10,
                LineStartPosition = 35,
                Text = "some text"
            };

            var secondLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 3,
                LineLength = 25,
                LineStartPosition = 47
            };

            checksumCalculatorMock.Setup(x => x.Calculate(firstLine.Text)).Returns(issue.LineHash);

            var textSnapshotMock = CreateSnapshotMock(lines: new[] {firstLine, secondLine});

            // Act
            var result = testSubject.CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.IsEmpty.Should().BeFalse();
            result.Start.Position.Should().Be(45); // firstLine.LineStartPosition + issue.StartLineOffset
            result.End.Position.Should().Be(67); // secondLine.LineStartPosition + issue.EndLineOffset
        }

        [TestMethod]
        public void CalculateSpan_EndLineIsZero_ReturnsSpanOfTheEntireStartLine()
        {
            // If issue.EndLine is zero the whole of the start line should be selected

            // Arrange
            var issue = new DummyAnalysisIssueLocation
            {
                StartLine = 22,
                StartLineOffset = 2,
                EndLine = 0,
                EndLineOffset = 0,
                LineHash = "some hash"
            };

            var firstLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 21,
                LineLength = 34,
                LineStartPosition = 103,
                Text = "some text"
            };

            // The second VS line shouldn't be used in this case
            var secondLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 25,
                LineLength = 100,
                LineStartPosition = 1010
            };

            checksumCalculatorMock.Setup(x => x.Calculate(firstLine.Text)).Returns(issue.LineHash);

            var textSnapshotMock = CreateSnapshotMock(lines: new[] {firstLine, secondLine});

            // Act
            var result = testSubject.CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.IsEmpty.Should().BeFalse();
            result.Start.Position.Should().Be(103); // firstLine.LineStartPosition. Ignore issue.StartLineOffset in this case
            result.End.Position.Should().Be(137); // firstLine.LineStartPosition +  firstLine.LineLength
        }

        [TestMethod]
        public void CalculateSpan_StartPositionExceedsSnapshotLength_ReturnsSpanWithEndOfFile()
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
                LineHash = "some hash"
            };

            var firstLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 52,
                LineLength = 1,
                LineStartPosition = 1599,
                Text = "some text"
            };

            // Second line should not be used in this case
            var secondLine = new VSLineDescription
            {
                ZeroBasedLineNumber = -999,
                LineLength = -999,
                LineStartPosition = -999
            };

            checksumCalculatorMock.Setup(x => x.Calculate(firstLine.Text)).Returns(issue.LineHash);

            var textSnapshotMock = CreateSnapshotMock(snapShotLength: 1600, lines: new[] {firstLine, secondLine});

            // Act
            var result = testSubject.CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.IsEmpty.Should().BeFalse();
            result.Start.Position.Should().Be(1599); // firstLine.LineStartPosition. Ignore offset because that will take us beyond the end of file
            result.End.Position.Should().Be(1600); // snapshot length
        }

        [TestMethod]
        public void CalculateSpan_EndPositionExceedsSnapshotLength()
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
                LineHash = "some hash"
            };

            // The issue is on a single line in this case, but the issue end position
            // is beyond the end of the line.
            var vsLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 52,
                LineLength = 10,
                LineStartPosition = 1599,
                Text = "some text"
            };

            var textSnapshotMock = CreateSnapshotMock(lines: vsLine, snapShotLength: 1602);

            checksumCalculatorMock.Setup(x => x.Calculate(vsLine.Text)).Returns(issue.LineHash);

            // Act
            var result = testSubject.CalculateSpan(issue, textSnapshotMock.Object);

            // Assert
            result.IsEmpty.Should().BeFalse();
            result.Start.Position.Should().Be(1601); // vsLine.LineStartPosition + issue.StartLineOffset
            result.End.Position.Should().Be(1602); // snapshot length
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        public void CalculateSpan_IssueDoesNotHaveLineHash_HashNotChecked(string lineHash)
        {
            var issue = new DummyAnalysisIssue
            {
                StartLine = 1,
                StartLineOffset = 0,
                EndLine = 0,
                EndLineOffset = 0,
                LineHash = lineHash
            };

            var firstLine = new VSLineDescription
            {
                ZeroBasedLineNumber = 0,
                LineLength = 10,
                LineStartPosition = 1,
                Text = "some text"
            };

            var textSnapshotMock = CreateSnapshotMock(lines: new[] { firstLine });

            // Act
            var result = testSubject.CalculateSpan(issue, textSnapshotMock.Object);

            result.IsEmpty.Should().BeFalse();

            checksumCalculatorMock.VerifyNoOtherCalls();
        }

        private class VSLineDescription
        {
            public int ZeroBasedLineNumber { get; set; }
            public int LineStartPosition { get; set; }
            public int LineLength { get; set; }
            public string Text { get; set; }
        }

        private static Mock<ITextSnapshot> CreateSnapshotMock(int bufferLineCount = 1000, int snapShotLength = 10000, params VSLineDescription[] lines)
        {
            var textSnapshotMock = new Mock<ITextSnapshot>();

            textSnapshotMock
                .SetupGet(x => x.LineCount)
                .Returns(bufferLineCount);

            textSnapshotMock
                .SetupGet(x => x.Length)
                .Returns(snapShotLength);

            foreach (var vsLineDescription in lines)
            {
                var textSnapshotLine = CreateLineMock(textSnapshotMock.Object, vsLineDescription);

                textSnapshotMock
                    .Setup(x => x.GetLineFromLineNumber(vsLineDescription.ZeroBasedLineNumber))
                    .Returns(() => textSnapshotLine);
            }

            return textSnapshotMock;
        }

        private static ITextSnapshotLine CreateLineMock(ITextSnapshot textSnapshot, VSLineDescription line)
        {
            var startLineMock = new Mock<ITextSnapshotLine>();

            startLineMock.SetupGet(x => x.Start)
                .Returns(() => new SnapshotPoint(textSnapshot, line.LineStartPosition));

            startLineMock.SetupGet(x => x.Length)
                    .Returns(() => new SnapshotPoint(textSnapshot, line.LineLength));

            startLineMock.Setup(x => x.GetText())
                .Returns(() => line.Text);

            return startLineMock.Object;
        }
    }
}
