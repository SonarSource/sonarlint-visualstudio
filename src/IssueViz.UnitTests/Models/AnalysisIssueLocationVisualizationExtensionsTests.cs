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
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Models
{
    [TestClass]
    public class AnalysisIssueLocationVisualizationExtensionsTests
    {
        [TestMethod]
        public void IsNavigable_NoSpan_True()
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.SetupGet(x => x.Span).Returns((SnapshotSpan?) null);

            var result = locationViz.Object.IsNavigable();
            result.Should().BeTrue();
        }

        [TestMethod]
        public void IsNavigable_EmptySpan_False()
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.SetupGet(x => x.Span).Returns(new SnapshotSpan());

            var result = locationViz.Object.IsNavigable();
            result.Should().BeFalse();
        }

        [TestMethod]
        public void IsNavigable_NonEmptySpan_True()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(x => x.Length).Returns(20);
            var nonEmptySpan = new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 10));

            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.SetupGet(x => x.Span).Returns(nonEmptySpan);

            var result = locationViz.Object.IsNavigable();
            result.Should().BeTrue();
        }
    }
}
