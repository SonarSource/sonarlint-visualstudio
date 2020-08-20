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
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.IssueVisualization.Editor;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    [TestClass]
    public class IssueToIssueMarkerConverterTests
    {
        [TestMethod]
        public void Convert_NullIssue_Throws()
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

        [TestMethod]
        public void Convert_NullSpan_Null()
        {
            var mockIssue = Mock.Of<IAnalysisIssue>();
            var mockSnapshot = Mock.Of<ITextSnapshot>();

            var spanCalculator = new Mock<IIssueSpanCalculator>();
            spanCalculator.Setup(x => x.CalculateSpan(mockIssue, mockSnapshot)).Returns((SnapshotSpan?) null);

            var testSubject = new IssueToIssueMarkerConverter(spanCalculator.Object);
            var issueMarker = testSubject.Convert(mockIssue, mockSnapshot);

            issueMarker.Should().BeNull();
        }

        [TestMethod]
        public void Convert_SpanIsNotNull_ReturnsMarkerWithSpan()
        {
            var mockIssue = Mock.Of<IAnalysisIssue>();
            var mockSnapshot = Mock.Of<ITextSnapshot>();
            var mockSpan = new SnapshotSpan();

            var spanCalculator = new Mock<IIssueSpanCalculator>();
            spanCalculator.Setup(x => x.CalculateSpan(mockIssue, mockSnapshot)).Returns(mockSpan);

            var testSubject = new IssueToIssueMarkerConverter(spanCalculator.Object);
            var issueMarker = testSubject.Convert(mockIssue, mockSnapshot);

            issueMarker.Should().NotBeNull();
            issueMarker.Issue.Should().Be(mockIssue);
            issueMarker.Span.Should().Be(mockSpan);
        }
    }
}
