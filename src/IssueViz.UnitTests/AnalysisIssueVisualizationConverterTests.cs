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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests
{
    [TestClass]
    public class AnalysisIssueVisualizationConverterTests
    {
        private AnalysisIssueVisualizationConverter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new AnalysisIssueVisualizationConverter();
        }

        [TestMethod]
        public void Convert_IssueHasNoFlows_ReturnsIssueVisualizationWithoutFlows()
        {
            var flows = Array.Empty<IAnalysisIssueFlow>();
            var issue = CreateIssue(flows);

            var expectedIssueVisualization = new AnalysisIssueVisualization(Array.Empty<IAnalysisIssueFlowVisualization>(), issue);

            var actualIssueVisualization = testSubject.Convert(issue);

            AssertConversion(expectedIssueVisualization, actualIssueVisualization);
        }

        [TestMethod]
        public void Convert_IssueHasOneFlowWithOneLocation_ReturnsIssueVisualizationWithFlowAndLocation()
        {
            var location = CreateLocation();
            var flow = CreateFlow(location);
            var issue = CreateIssue(flow);

            var expectedLocationVisualization = new AnalysisIssueLocationVisualization(1, location);
            var expectedFlowVisualization = new AnalysisIssueFlowVisualization(1, new[] { expectedLocationVisualization }, flow);
            var expectedIssueVisualization = new AnalysisIssueVisualization(new[] {expectedFlowVisualization}, issue);

            var actualIssueVisualization = testSubject.Convert(issue);

            AssertConversion(expectedIssueVisualization, actualIssueVisualization);
        }

        [TestMethod]
        public void Convert_IssueHasTwoFlowsWithTwoLocation_ReturnsIssueVisualizationWithFlowsAndLocations()
        {
            var firstFlowFirstLocation = CreateLocation();
            var firstFlowSecondLocation = CreateLocation();
            var firstFlow = CreateFlow(firstFlowFirstLocation, firstFlowSecondLocation);

            var secondFlowFirstLocation = CreateLocation();
            var secondFlowSecondLocation = CreateLocation();
            var secondFlow = CreateFlow(secondFlowFirstLocation, secondFlowSecondLocation);

            var issue = CreateIssue(firstFlow, secondFlow);

            var expectedFirstFlowFirstLocationVisualization = new AnalysisIssueLocationVisualization(1, firstFlowFirstLocation);
            var expectedFirstFlowSecondLocationVisualization = new AnalysisIssueLocationVisualization(2, firstFlowSecondLocation);
            var expectedFirstFlowVisualization = new AnalysisIssueFlowVisualization(1, new[] { expectedFirstFlowFirstLocationVisualization, expectedFirstFlowSecondLocationVisualization }, firstFlow);

            var expectedSecondFlowFirstLocationVisualization = new AnalysisIssueLocationVisualization(1, secondFlowFirstLocation);
            var expectedSecondFlowSecondLocationVisualization = new AnalysisIssueLocationVisualization(2, secondFlowSecondLocation);
            var expectedSecondFlowVisualization = new AnalysisIssueFlowVisualization(2, new[] { expectedSecondFlowFirstLocationVisualization, expectedSecondFlowSecondLocationVisualization }, secondFlow);

            var expectedIssueVisualization = new AnalysisIssueVisualization(new[] { expectedFirstFlowVisualization, expectedSecondFlowVisualization }, issue);

            var actualIssueVisualization = testSubject.Convert(issue);

            AssertConversion(expectedIssueVisualization, actualIssueVisualization);
        }

        private void AssertConversion(IAnalysisIssueVisualization expectedIssueVisualization, IAnalysisIssueVisualization actualIssueVisualization)
        {
            actualIssueVisualization.Issue.Should().Be(expectedIssueVisualization.Issue);

            actualIssueVisualization.Flows.Should().BeEquivalentTo(expectedIssueVisualization.Flows, c=> c.WithStrictOrdering());
        }

        private static IAnalysisIssue CreateIssue(params IAnalysisIssueFlow[] flows)
        {
            return new AnalysisIssue(
                Guid.NewGuid().ToString(),
                AnalysisIssueSeverity.Blocker,
                AnalysisIssueType.Bug,
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().ToString(),
                flows
            );
        }

        private static IAnalysisIssueFlow CreateFlow(params IAnalysisIssueLocation[] locations)
        {
           return new AnalysisIssueFlow(locations);
        }

        private static IAnalysisIssueLocation CreateLocation()
        {
            return new AnalysisIssueLocation(
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().ToString()
            );
        }
    }
}
