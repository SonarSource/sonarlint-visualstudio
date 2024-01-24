/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
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
            var issue = CreateIssue();

            var locations = issue.GetAllLocations();
            locations.Should().BeEquivalentTo(issue);
        }

        [TestMethod]
        public void GetAllLocations_HasSecondaryLocations_ListWithPrimaryAndLocations()
        {
            var location1 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var flow1 = CreateFlow(location1);

            var location2 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var location3 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var flow2 = CreateFlow(location2, location3);

            var issue = CreateIssue(flow1, flow2);

            var expectedLocations = new[] { issue, location1, location2, location3 };
            var locations = issue.GetAllLocations();

            locations.Should().BeEquivalentTo(expectedLocations, c => c.WithStrictOrdering());
        }

        [TestMethod]
        public void GetSecondaryLocations_NoSecondaryLocations_EmptyList()
        {
            var issue = CreateIssue();

            var locations = issue.GetSecondaryLocations();

            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetSecondaryLocations_HasSecondaryLocations_ListWithLocations()
        {
            var location1 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var flow1 = CreateFlow(location1);

            var location2 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var location3 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var flow2 = CreateFlow(location2, location3);

            var issue = CreateIssue(flow1, flow2);

            var locations = issue.GetSecondaryLocations();

            locations.Should().BeEquivalentTo(location1, location2, location3);
        }

        private IAnalysisIssueFlowVisualization CreateFlow(params IAnalysisIssueLocationVisualization[] locations)
        {
            var flow = new Mock<IAnalysisIssueFlowVisualization>();
            flow.Setup(x => x.Locations).Returns(locations);

            return flow.Object;
        }

        private IAnalysisIssueVisualization CreateIssue(params IAnalysisIssueFlowVisualization[] flows)
        {
            var issue = new Mock<IAnalysisIssueVisualization>();
            issue.Setup(x => x.Flows).Returns(flows);

            return issue.Object;
        }
    }
}
