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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Models
{
    [TestClass]

    public class AnalysisIssueVisualizationExtensionsTests
    {
        [TestMethod]
        public void GetAllLocations_NoSecondaryLocations_ListWithOnlyPrimaryLocation()
        {
            var issue = new Mock<IAnalysisIssueVisualization>();
            issue.Setup(x => x.Flows).Returns(Enumerable.Empty<IAnalysisIssueFlowVisualization>().ToList());

            var locations = issue.Object.GetAllLocations();
            locations.Should().BeEquivalentTo(issue.Object);
        }

        [TestMethod]
        public void GetAllLocations_HasSecondaryLocations_ListWithPrimaryAndLocations()
        {
            var location1 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var flow1 = new Mock<IAnalysisIssueFlowVisualization>();
            flow1.Setup(x => x.Locations).Returns(new[] { location1 });

            var location2 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var location3 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var flow2 = new Mock<IAnalysisIssueFlowVisualization>();
            flow2.Setup(x => x.Locations).Returns(new[] { location2, location3 });

            var issue = new Mock<IAnalysisIssueVisualization>();
            issue.Setup(x => x.Flows).Returns(new[] { flow1.Object, flow2.Object });

            var expectedLocations = new[] { issue.Object, location1, location2, location3 };
            var locations = issue.Object.GetAllLocations();

            locations.Should().BeEquivalentTo(expectedLocations, c => c.WithStrictOrdering());
        }
    }
}
