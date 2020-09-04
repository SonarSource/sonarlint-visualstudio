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
using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests
{
    [TestClass]
    public class AnalysisIssueVisualizationConverterTests
    {
        private Mock<IIssueSpanCalculator> issueSpanCalculatorMock;
        private ITextSnapshot textSnapshotMock;

        private AnalysisIssueVisualizationConverter testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            issueSpanCalculatorMock = new Mock<IIssueSpanCalculator>();
            textSnapshotMock = Mock.Of<ITextSnapshot>();

            testSubject = new AnalysisIssueVisualizationConverter(issueSpanCalculatorMock.Object);
        }

        [TestMethod]
        public void Convert_EmptySpan_Null()
        {
            var issue = CreateIssue(Path.GetRandomFileName(), new SnapshotSpan());

            var result = testSubject.Convert(issue, textSnapshotMock);

            result.Should().BeNull();
        }

        [TestMethod]
        public void Convert_IssueHasNoFlows_IssueVisualizationWithoutFlows()
        {
            var flows = Array.Empty<IAnalysisIssueFlow>();
            var issueSpan = CreateNonEmptySpan();
            var issue = CreateIssue(Path.GetRandomFileName(), issueSpan, flows);

            var expectedIssueVisualization = new AnalysisIssueVisualization(Array.Empty<IAnalysisIssueFlowVisualization>(), issue, issueSpan);

            var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

            AssertConversion(expectedIssueVisualization, actualIssueVisualization);
        }

        [TestMethod]
        public void Convert_IssueHasOneFlow_IssueVisualizationWithFlow()
        {
            var location = CreateLocation(Path.GetRandomFileName());
            var flow = CreateFlow(location);

            var issueSpan = CreateNonEmptySpan();
            var issue = CreateIssue(Path.GetRandomFileName(), issueSpan, flow);

            var expectedLocationVisualization = new AnalysisIssueLocationVisualization(1, location);
            var expectedFlowVisualization = new AnalysisIssueFlowVisualization(1, new[] { expectedLocationVisualization }, flow);
            var expectedIssueVisualization = new AnalysisIssueVisualization(new[] {expectedFlowVisualization}, issue, issueSpan);

            var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

            AssertConversion(expectedIssueVisualization, actualIssueVisualization);
        }

        [TestMethod]
        public void Convert_IssueHasTwoFlows_IssueVisualizationWithTwoFlows()
        {
            var firstFlowFirstLocation = CreateLocation(Path.GetRandomFileName());
            var firstFlowSecondLocation = CreateLocation(Path.GetRandomFileName());
            var firstFlow = CreateFlow(firstFlowFirstLocation, firstFlowSecondLocation);

            var secondFlowFirstLocation = CreateLocation(Path.GetRandomFileName());
            var secondFlowSecondLocation = CreateLocation(Path.GetRandomFileName());
            var secondFlow = CreateFlow(secondFlowFirstLocation, secondFlowSecondLocation);

            var issueSpan = CreateNonEmptySpan();
            var issue = CreateIssue(Path.GetRandomFileName(), issueSpan, firstFlow, secondFlow);

            var expectedFirstFlowFirstLocationVisualization = new AnalysisIssueLocationVisualization(1, firstFlowFirstLocation);
            var expectedFirstFlowSecondLocationVisualization = new AnalysisIssueLocationVisualization(2, firstFlowSecondLocation);
            var expectedFirstFlowVisualization = new AnalysisIssueFlowVisualization(1, new[] { expectedFirstFlowFirstLocationVisualization, expectedFirstFlowSecondLocationVisualization }, firstFlow);

            var expectedSecondFlowFirstLocationVisualization = new AnalysisIssueLocationVisualization(1, secondFlowFirstLocation);
            var expectedSecondFlowSecondLocationVisualization = new AnalysisIssueLocationVisualization(2, secondFlowSecondLocation);
            var expectedSecondFlowVisualization = new AnalysisIssueFlowVisualization(2, new[] { expectedSecondFlowFirstLocationVisualization, expectedSecondFlowSecondLocationVisualization }, secondFlow);

            var expectedIssueVisualization = new AnalysisIssueVisualization(new[] { expectedFirstFlowVisualization, expectedSecondFlowVisualization }, issue, issueSpan);

            var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

            AssertConversion(expectedIssueVisualization, actualIssueVisualization);
        }

        [TestMethod]
        public void Convert_IssueHasLocationsInDifferentFiles_CalculatesSpanForLocationsInSameFile()
        {
            var issueFilePath = Path.GetRandomFileName();

            var locationInSameFile = CreateLocation(issueFilePath);
            var locationInAnotherFile = CreateLocation(Path.GetRandomFileName());
            var flow = CreateFlow(locationInSameFile, locationInAnotherFile);

            var issueSpan = CreateNonEmptySpan();
            var issue = CreateIssue(issueFilePath, issueSpan, flow);

            var locationSpan = CreateNonEmptySpan();
            SetupSpanCalculator(locationInSameFile, locationSpan);

            var expectedLocation1 = new AnalysisIssueLocationVisualization(1, locationInSameFile) {Span = locationSpan};
            var expectedLocation2 = new AnalysisIssueLocationVisualization(2, locationInAnotherFile) {Span = null};
            var expectedFlow = new AnalysisIssueFlowVisualization(1, new[] {expectedLocation1, expectedLocation2}, flow);
            var expectedIssueVisualization = new AnalysisIssueVisualization(new[] { expectedFlow }, issue, issueSpan);

            var actualIssueVisualization = testSubject.Convert(issue, textSnapshotMock);

            AssertConversion(expectedIssueVisualization, actualIssueVisualization);
        }

        private void AssertConversion(IAnalysisIssueVisualization expectedIssueVisualization, IAnalysisIssueVisualization actualIssueVisualization)
        {
            actualIssueVisualization.Issue.Should().Be(expectedIssueVisualization.Issue);

            actualIssueVisualization.Flows.Should().BeEquivalentTo(expectedIssueVisualization.Flows, c=> c.WithStrictOrdering());
        }

        private IAnalysisIssue CreateIssue(string filePath, SnapshotSpan span, params IAnalysisIssueFlow[] flows)
        {
            var issue = new AnalysisIssue(
                Guid.NewGuid().ToString(),
                AnalysisIssueSeverity.Blocker,
                AnalysisIssueType.Bug,
                Guid.NewGuid().ToString(),
                filePath,
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().ToString(),
                flows
            );

            SetupSpanCalculator(issue, span);

            return issue;
        }

        private static IAnalysisIssueFlow CreateFlow(params IAnalysisIssueLocation[] locations)
        {
           return new AnalysisIssueFlow(locations);
        }

        private static IAnalysisIssueLocation CreateLocation(string filePath)
        {
            return new AnalysisIssueLocation(
                Guid.NewGuid().ToString(),
                filePath,
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().GetHashCode(),
                Guid.NewGuid().ToString()
            );
        }

        private SnapshotSpan CreateNonEmptySpan()
        {
            var mockTextSnapshot = new Mock<ITextSnapshot>();
            mockTextSnapshot.SetupGet(x => x.Length).Returns(20);

            return new SnapshotSpan(mockTextSnapshot.Object, new Span(0, 10));
        }

        private void SetupSpanCalculator(IAnalysisIssueLocation issueLocation, SnapshotSpan nonEmptySpan)
        {
            issueSpanCalculatorMock.Setup(x => x.CalculateSpan(issueLocation, textSnapshotMock)).Returns(nonEmptySpan);
        }
    }
}
