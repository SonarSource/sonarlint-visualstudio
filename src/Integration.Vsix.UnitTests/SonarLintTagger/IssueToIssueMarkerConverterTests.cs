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

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IssueToIssueMarkerConverterTests
    {
        [DataTestMethod]
        [DataRow(100, 1)]
        [DataRow(101, 100)]
        public void Convert_IssueLineOutsideSnapshot_ReturnsNull(int issueStartLine, int bufferLineCount)
        {
            // Arrange
            var issue = new DummyAnalysisIssue { StartLine = issueStartLine};
            var mockSnapshot = CreateMockTextSnapshot(bufferLineCount, "unimportant");

            var testSubject = new IssueToIssueMarkerConverter();

            // Act and assert
            testSubject.Convert(issue, mockSnapshot.Object)
                .Should().BeNull();
        }

        [DataTestMethod]
        [DataRow(2, 100)]
        [DataRow(100, 100)]
        public void Convert_IssueLineInSnapshot_ReturnsFilterableIssue(int issueStartLine, int bufferLineCount)
        {
            var issue = new DummyAnalysisIssue { StartLine = issueStartLine };
            var mockSnapshot = CreateMockTextSnapshot(bufferLineCount, "some text");
            var testSubject = new IssueToIssueMarkerConverter();

            // Act
            var actual = testSubject.Convert(issue, mockSnapshot.Object);

            // Assert
            actual.Should().NotBeNull();

            actual.WholeLineText.Should().Be("some text");
            actual.LineHash.Should().Be(ChecksumCalculator.Calculate("some text"));
        }

        [TestMethod]
        public void Convert_FileLevelIssue_ReturnsFilterableIssue()
        {
            // Arrange
            var issue = new DummyAnalysisIssue { StartLine = 0 };
            var mockSnapshot = CreateMockTextSnapshot(10, "anything");
            var testSubject = new IssueToIssueMarkerConverter();

            // Act
            var actual = testSubject.Convert(issue, mockSnapshot.Object);

            // Assert
            actual.Should().NotBeNull();

            actual.WholeLineText.Should().BeNull();
            actual.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void Convert_NullIssues_Throws()
        {
            // Arrange
            var mockSnapshot = new Mock<ITextSnapshot>();
            var testSubject = new IssueToIssueMarkerConverter();
            Action act = () => testSubject.Convert(null, mockSnapshot.Object);

            // Act and assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issue");
        }

        [TestMethod]
        public void Convert_NullTextSnapshot_Throws()
        {
            // Arrange
            var testSubject = new IssueToIssueMarkerConverter();
            Action act = () => testSubject.Convert(Mock.Of<IAnalysisIssue>(), null);

            // Act and assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("textSnapshot");
        }

        private static Mock<ITextSnapshot> CreateMockTextSnapshot(int lineCount, string textToReturn)
        {
            var mockSnapshotLine = new Mock<ITextSnapshotLine>();
            mockSnapshotLine.Setup(x => x.GetText()).Returns(textToReturn);

            var mockSnapshot = new Mock<ITextSnapshot>();
            mockSnapshot.Setup(x => x.LineCount).Returns(lineCount);
            mockSnapshot.Setup(x => x.GetLineFromLineNumber(It.IsAny<int>()))
                .Returns(mockSnapshotLine.Object);

            return mockSnapshot;
        }
    }
}
