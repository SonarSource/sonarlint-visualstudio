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
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Suppression;
using SonarLint.VisualStudio.Integration.Vsix;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IssueToFilterableIssueConverterTests
    {
        [DataTestMethod]
        [DataRow(100, 1)]
        [DataRow(101, 100)]
        public void CreateFilterableIssue_IssueLineOutsideSnapshot_ReturnsNull(int issueLine, int bufferLineCount)
        {
            // Arrange
            var issue = new Sonarlint.Issue {StartLine = issueLine};
            var mockSnapshot = CreateMockTextSnapshot(bufferLineCount, "unimportant");

            // Act and assert
            IssueToFilterableIssueConverter.CreateFilterableIssue(issue, mockSnapshot.Object)
                .Should().BeNull();
        }

        [DataTestMethod]
        [DataRow(2, 100)]
        [DataRow(100, 100)]
        public void CreateFilterableIssue_IssueLineInSnapshot_ReturnsFilterableIssue(int issueLine, int bufferLineCount)
        {
            var issue = new Sonarlint.Issue { StartLine = issueLine };
            var mockSnapshot = CreateMockTextSnapshot(bufferLineCount, "some text");

            // Act
            var actual = IssueToFilterableIssueConverter.CreateFilterableIssue(issue, mockSnapshot.Object);

            // Assert
            actual.Should().BeOfType(typeof(DaemonIssueAdapter));

            var adapterIssue = (DaemonIssueAdapter)actual;
            adapterIssue.SonarLintIssue.Should().BeSameAs(issue);

            actual.WholeLineText.Should().Be("some text");
            actual.LineHash.Should().Be(ChecksumCalculator.Calculate("some text"));
        }

        [TestMethod]
        public void CreateFilterableIssue_FileLevelIssue_ReturnsFilterableIssue()
        {
            // Arrange
            var issue = new Sonarlint.Issue { StartLine = 0 };
            var mockSnapshot = CreateMockTextSnapshot(10, "anything");

            // Act
            var actual = IssueToFilterableIssueConverter.CreateFilterableIssue(issue, mockSnapshot.Object);

            // Assert
            actual.Should().BeOfType(typeof(DaemonIssueAdapter));
            var adapterIssue = (DaemonIssueAdapter)actual;
            adapterIssue.SonarLintIssue.Should().BeSameAs(issue);

            actual.StartLine.Should().Be(0);
            actual.WholeLineText.Should().BeNull();
            actual.LineHash.Should().BeNull();
        }

        [TestMethod]
        public void Convert_NullIssues_Throws()
        {
            // Arrange
            var mockSnapshot = new Mock<ITextSnapshot>();
            Action act = () => IssueToFilterableIssueConverter.Convert(null, mockSnapshot.Object);

            // Act and assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issues");
        }

        [TestMethod]
        public void Convert_NullTextSnapshot_Throws()
        {
            // Arrange
            Action act = () => IssueToFilterableIssueConverter.Convert(Enumerable.Empty<Issue>(), null);

            // Act and assert
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("textSnapshot");
        }

        [TestMethod]
        public void Convert_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var mockSnapshot = new Mock<ITextSnapshot>();

            // Act and assert
            IssueToFilterableIssueConverter.Convert(Enumerable.Empty<Issue>(), mockSnapshot.Object)
                .Should().BeEmpty();
        }

        [TestMethod]
        public void Convert_MultipleItemsIncludingOutOfRangeIssues_InRangeIssuesReturned()
        {
            // Arrange
            const int maxLineNumber = 5;
            var fileIssue = new Issue { StartLine = 0 };
            var validIssue1 = new Issue { StartLine = 2 };
            var outOfRangeIssue = new Issue { StartLine = maxLineNumber + 1 };
            var validIssue2 = new Issue { StartLine = maxLineNumber };

            var input = new[] { fileIssue, validIssue1, outOfRangeIssue, null, validIssue2 };

            var mockSnapshot = CreateMockTextSnapshot(maxLineNumber, "anything");

            // Act
            var actual = IssueToFilterableIssueConverter.Convert(input, mockSnapshot.Object);

            // Assert
            actual.Count().Should().Be(3);

            var adapterIssues = actual.OfType<DaemonIssueAdapter>().ToArray();
            adapterIssues[0].SonarLintIssue.Should().BeSameAs(fileIssue);
            adapterIssues[1].SonarLintIssue.Should().BeSameAs(validIssue1);
            adapterIssues[2].SonarLintIssue.Should().BeSameAs(validIssue2);
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
