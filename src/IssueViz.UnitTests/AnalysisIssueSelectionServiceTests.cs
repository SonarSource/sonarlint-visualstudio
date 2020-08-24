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
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests
{
    [TestClass]
    public class AnalysisIssueSelectionServiceTests
    {
        private Mock<ILocationNavigator> locationNavigatorMock;

        private AnalysisIssueSelectionService testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            locationNavigatorMock = new Mock<ILocationNavigator>();

            testSubject = new AnalysisIssueSelectionService(locationNavigatorMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            // Arrange
            var locationNavigatorExport = MefTestHelpers.CreateExport<ILocationNavigator>(locationNavigatorMock.Object);

            // Act & Assert
            MefTestHelpers.CheckTypeCanBeImported<AnalysisIssueSelectionService, IAnalysisIssueSelectionService>(null, new[]
            {
                locationNavigatorExport
            });
        }

        [TestMethod]
        public void SelectIssue_NoSubscribers_NoException()
        {
            Action act = () => testSubject.Select(Mock.Of<IAnalysisIssueVisualization>());

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SelectFlow_NoSubscribers_NoException()
        {
            Action act = () => testSubject.Select(Mock.Of<IAnalysisIssueFlowVisualization>());

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SelectLocation_NoSubscribers_NoException()
        {
            Action act = () => testSubject.Select(Mock.Of<IAnalysisIssueLocationVisualization>());

            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectIssue_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelIssue(bool isNewIssueNull)
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            var expectedIssue = isNewIssueNull ? null : Mock.Of<IAnalysisIssueVisualization>();
            testSubject.Select(expectedIssue);

            eventHandler.Verify(
                x => x(testSubject,
                    It.Is((SelectionChangedEventArgs args) => args.SelectedIssue == expectedIssue &&
                                                              args.SelectionChangeLevel == SelectionChangeLevel.Issue)),
                Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectFlow_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelFlow(bool isNewFlowNull)
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            var expectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.Select(expectedFlow);

            eventHandler.Verify(
                x => x(testSubject,
                    It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == expectedFlow &&
                                                              args.SelectionChangeLevel == SelectionChangeLevel.Flow)),
                Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectLocation_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelLocation(bool isNewLocationNull)
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            var expectedLocation = isNewLocationNull ? null : Mock.Of<IAnalysisIssueLocationVisualization>();
            testSubject.Select(expectedLocation);

            eventHandler.Verify(
                x => x(testSubject,
                    It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == expectedLocation &&
                                                              args.SelectionChangeLevel ==
                                                              SelectionChangeLevel.Location)), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedIssue_ReturnsValue(bool isNewIssueNull)
        {
            testSubject.SelectedIssue.Should().BeNull();

            var expectedIssue = isNewIssueNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            testSubject.Select(expectedIssue);

            testSubject.SelectedIssue.Should().Be(expectedIssue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedFlow_ReturnsValue(bool isNewFlowNull)
        {
            testSubject.SelectedFlow.Should().BeNull();

            var expectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlowVisualization>();

            testSubject.Select(expectedFlow);

            testSubject.SelectedFlow.Should().Be(expectedFlow);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedLocation_ReturnsValue(bool isNewLocationNull)
        {
            testSubject.SelectedLocation.Should().BeNull();

            var expectedLocation = isNewLocationNull ? null : Mock.Of<IAnalysisIssueLocationVisualization>();

            testSubject.Select(expectedLocation);

            testSubject.SelectedLocation.Should().Be(expectedLocation);
        }

        [TestMethod]
        public void Dispose_HasSubscribers_RemovesSubscribers()
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            testSubject.Dispose();

            testSubject.Select(Mock.Of<IAnalysisIssueVisualization>());

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedFlowChanged_ChangesSelectedLocation()
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            // Set flow to value
            var firstFlow = GetFlowWithLocation(out var firstFlowFirstLocation);
            testSubject.Select(firstFlow);

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == firstFlowFirstLocation)), Times.Once());
            testSubject.SelectedLocation.Should().Be(firstFlowFirstLocation);

            // Set flow to null
            testSubject.Select(null as IAnalysisIssueFlowVisualization);

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == null)), Times.Once());
            testSubject.SelectedLocation.Should().BeNull();

            // Set flow to a different value
            var secondFlow = GetFlowWithLocation(out var secondFlowFirstLocation);
            testSubject.Select(secondFlow);

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == secondFlowFirstLocation)), Times.Once());
            testSubject.SelectedLocation.Should().Be(secondFlowFirstLocation);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedIssueChanged_ChangesSelectedFlow()
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            // Set issue to value
            var firstIssue = GetIssueWithFlow(out var firstIssueFirstFlow);
            testSubject.Select(firstIssue);

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == firstIssueFirstFlow)), Times.Once());
            testSubject.SelectedFlow.Should().Be(firstIssueFirstFlow);

            // Set issue to null
            testSubject.Select(null as IAnalysisIssueVisualization);

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == null)), Times.Once());
            testSubject.SelectedFlow.Should().BeNull();

            // Set issue to different value
            var secondIssue = GetIssueWithFlow(out var secondIssueFirstFlow);
            testSubject.Select(secondIssue);
            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == secondIssueFirstFlow)), Times.Once());
            testSubject.SelectedFlow.Should().Be(secondIssueFirstFlow);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedFlowChanged_FlowIsNull_NoLocationNavigation()
        {
            testSubject.Select(null as IAnalysisIssueFlowVisualization);

            locationNavigatorMock.Verify(x => x.TryNavigate(It.IsAny<IAnalysisIssueLocation>()), Times.Never);
            locationNavigatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedFlowChanged_FlowIsNotNull_FlowHasNoLocations_NoLocationNavigation()
        {
            testSubject.Select(Mock.Of<IAnalysisIssueFlowVisualization>());

            locationNavigatorMock.Verify(x => x.TryNavigate(It.IsAny<IAnalysisIssueLocation>()), Times.Never);
            locationNavigatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedFlowChanged_FlowIsNotNull_FlowHasLocations_NavigatesToFlowFirstLocation()
        {
            var selectedFlow = GetFlowWithLocation(out var flowFirstLocation);
            testSubject.Select(selectedFlow);

            locationNavigatorMock.Verify(x => x.TryNavigate(flowFirstLocation.Location), Times.Once);
            locationNavigatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedLocationChanged_LocationIsNull_NoLocationNavigation()
        {
            testSubject.Select(null as IAnalysisIssueLocationVisualization);

            locationNavigatorMock.Verify(x => x.TryNavigate(It.IsAny<IAnalysisIssueLocation>()), Times.Never);
            locationNavigatorMock.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedLocationChanged_LocationIsNotNull_NavigatesToLocation()
        {
            var location = Mock.Of<IAnalysisIssueLocation>();
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.Location).Returns(location);

            testSubject.Select(locationViz.Object);

            locationNavigatorMock.Verify(x => x.TryNavigate(location), Times.Once);
            locationNavigatorMock.VerifyNoOtherCalls();
        }

        private static IAnalysisIssueFlowVisualization GetFlowWithLocation(out IAnalysisIssueLocationVisualization firstLocation)
        {
            firstLocation = Mock.Of<IAnalysisIssueLocationVisualization>();
            var otherLocation = Mock.Of<IAnalysisIssueLocationVisualization>();

            var mockFlow = new Mock<IAnalysisIssueFlowVisualization>();
            mockFlow
                .Setup(x => x.Locations)
                .Returns(new[] {firstLocation, otherLocation});

            return mockFlow.Object;
        }

        private IAnalysisIssueVisualization GetIssueWithFlow(out IAnalysisIssueFlowVisualization firstFlow)
        {
            firstFlow = Mock.Of<IAnalysisIssueFlowVisualization>();
            var otherFlow = Mock.Of<IAnalysisIssueFlowVisualization>();

            var mockIssue = new Mock<IAnalysisIssueVisualization>();
            mockIssue
                .Setup(x => x.Flows)
                .Returns(new[] { firstFlow, otherFlow });

            return mockIssue.Object;
        }
    }
}
