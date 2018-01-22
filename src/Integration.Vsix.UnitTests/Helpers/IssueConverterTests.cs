/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using SonarLint.VisualStudio.Integration.Vsix.Helpers;

namespace SonarLint.VisualStudio.Integration.UnitTests.SonarLintTagger
{
    [TestClass]
    public class IssueConverterTests
    {
        [TestMethod]
        public void ToMarker_Calculates_Span_Positions()
        {
            const int issueStartLine = 3;
            const int issueEndLine = 4;

            // Arrange
            var issue = new Sonarlint.Issue
            {
                StartLine = issueStartLine,
                StartLineOffset = 10,
                EndLine = issueEndLine,
                EndLineOffset = 20,
            };

            var textSnapshotMock = new Mock<ITextSnapshot>();

            var startLineMock = new Mock<ITextSnapshotLine>();
            startLineMock.SetupGet(x => x.Start)
                .Returns(() => new SnapshotPoint(textSnapshotMock.Object, 35));

            var endLineMock = new Mock<ITextSnapshotLine>();
            endLineMock.SetupGet(x => x.Start)
                .Returns(() => new SnapshotPoint(textSnapshotMock.Object, 47));

            textSnapshotMock
                .SetupGet(x => x.Length)
                .Returns(1000); // some big number to avoid ArgumentOutOfRange exceptions
            textSnapshotMock
                .Setup(x => x.GetLineFromLineNumber(issueStartLine - 1))
                .Returns(() => startLineMock.Object);
            textSnapshotMock
                .Setup(x => x.GetLineFromLineNumber(issueEndLine - 1))
                .Returns(() => endLineMock.Object);

            // Act
            var result = new IssueConverter()
                .ToMarker(issue, textSnapshotMock.Object);

            // Assert
            result.Should().NotBeNull();
            result.Span.Start.Position.Should().Be(45); // first SnapshotPoint.Position + issue.StartLineOffset
            result.Span.End.Position.Should().Be(67); // second SnapshotPoint.Position + issue.EndLineOffset
        }
    }
}
