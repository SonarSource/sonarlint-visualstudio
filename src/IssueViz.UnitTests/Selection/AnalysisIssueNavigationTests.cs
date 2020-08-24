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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Selection
{
    [TestClass]
    public class AnalysisIssueNavigationTests
    {
        private Mock<IAnalysisIssueSelectionService> selectionServiceMock;

        private AnalysisIssueNavigation testSubject;

        [TestInitialize]
        public void TesInitialize()
        {
            selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();

            testSubject = new AnalysisIssueNavigation(selectionServiceMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var selectionService = Mock.Of<IAnalysisIssueSelectionService>();
            var selectionServiceExport = MefTestHelpers.CreateExport<IAnalysisIssueSelectionService>(selectionService);

            MefTestHelpers.CheckTypeCanBeImported<AnalysisIssueNavigation, IAnalysisIssueNavigation>(null, new[] { selectionServiceExport });
        }

        [TestMethod]
        public void GotoNextLocation_NoCurrentFlow_NoNavigation()
        {
            SetupCurrentFlow((IAnalysisIssueFlowVisualization) null);
            SetupCurrentLocation(CreateLocation(1, true));

            testSubject.GotoNextLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousLocation_NoCurrentFlow_NoNavigation()
        {
            SetupCurrentFlow((IAnalysisIssueFlowVisualization)null);
            SetupCurrentLocation(CreateLocation(1, true));

            testSubject.GotoPreviousLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextLocation_NoCurrentLocation_NoNavigation()
        {
            SetupCurrentFlow(CreateLocation(1, true));
            SetupCurrentLocation(null);

            testSubject.GotoNextLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousLocation_NoCurrentLocation_NoNavigation()
        {
            SetupCurrentFlow(CreateLocation(1, true));
            SetupCurrentLocation(null);

            testSubject.GotoPreviousLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextLocation_FlowHasOnlyOneLocation_NoNavigation()
        {
            var location = CreateLocation(1, true);

            SetupCurrentFlow(location);
            SetupCurrentLocation(location);

            testSubject.GotoNextLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousLocation_FlowHasOnlyOneLocation_NoNavigation()
        {
            var location = CreateLocation(1, true);

            SetupCurrentFlow(location);
            SetupCurrentLocation(location);

            testSubject.GotoPreviousLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextLocation_CurrentLocationIsLast_NoNavigation()
        {
            var firstLocation = CreateLocation(1, true);
            var lastLocation = CreateLocation(2, true);

            SetupCurrentFlow(firstLocation, lastLocation);
            SetupCurrentLocation(lastLocation);

            testSubject.GotoNextLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextLocation_CurrentLocationIsLastNavigableLocation_NoNavigation()
        {
            var firstLocation = CreateLocation(1, true);
            var secondLocation = CreateLocation(2, true);
            var lastLocation = CreateLocation(3, false);

            SetupCurrentFlow(firstLocation, secondLocation, lastLocation);
            SetupCurrentLocation(secondLocation);

            testSubject.GotoNextLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousLocation_CurrentLocationIsFirst_NoNavigation()
        {
            var firstLocation = CreateLocation(1, true);
            var lastLocation = CreateLocation(2, true);

            SetupCurrentFlow(firstLocation, lastLocation);
            SetupCurrentLocation(firstLocation);

            testSubject.GotoPreviousLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousLocation_CurrentLocationIsFirstNavigableLocation_NoNavigation()
        {
            var firstLocation = CreateLocation(1, false);
            var secondLocation = CreateLocation(2, true);
            var lastLocation = CreateLocation(3, true);

            SetupCurrentFlow(firstLocation, secondLocation, lastLocation);
            SetupCurrentLocation(secondLocation);

            testSubject.GotoPreviousLocation();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextLocation_NavigatesToNextNavigableLocation()
        {
            var locations = new[]
            {
                CreateLocation(1, true),
                CreateLocation(2, true),
                CreateLocation(3, false), 
                CreateLocation(4, true),
                CreateLocation(5, true)
            };

            var currentLocation = locations[1];
            var expectedNavigation = locations[3];

            SetupCurrentFlow(locations);
            SetupCurrentLocation(currentLocation);

            testSubject.GotoNextLocation();

            VerifyNavigation(expectedNavigation);
        }

        [TestMethod]
        public void GotoPreviousLocation_NavigatesToPreviousNavigableLocation()
        {
            var locations = new[]
            {
                CreateLocation(1, true),
                CreateLocation(2, true),
                CreateLocation(3, false),
                CreateLocation(4, true),
                CreateLocation(5, true)
            };

            var currentLocation = locations[3];
            var expectedNavigation = locations[1];

            SetupCurrentFlow(locations);
            SetupCurrentLocation(currentLocation);

            testSubject.GotoPreviousLocation();

            VerifyNavigation(expectedNavigation);
        }

        private void SetupCurrentLocation(IAnalysisIssueLocationVisualization location)
        {
            selectionServiceMock.Setup(x => x.SelectedLocation).Returns(location);
        }

        private void SetupCurrentFlow(IAnalysisIssueFlowVisualization flow)
        {
            selectionServiceMock.Setup(x => x.SelectedFlow).Returns(flow);
        }

        private void SetupCurrentFlow(params IAnalysisIssueLocationVisualization[] locations)
        {
            var flow = new Mock<IAnalysisIssueFlowVisualization>();
            flow.Setup(x => x.Locations).Returns(locations);

            selectionServiceMock.Setup(x => x.SelectedFlow).Returns(flow.Object);
        }

        private IAnalysisIssueLocationVisualization CreateLocation(int stepNumber, bool isNavigable = true)
        {
            var location = new Mock<IAnalysisIssueLocationVisualization>();
            location.Setup(x => x.StepNumber).Returns(stepNumber);
            location.Setup(x => x.IsNavigable).Returns(isNavigable);

            return location.Object;
        }

        private void VerifyNoNavigation()
        {
            selectionServiceMock.Verify(x => x.Select(It.IsAny<IAnalysisIssueLocationVisualization>()), Times.Never);
        }

        private void VerifyNavigation(IAnalysisIssueLocationVisualization expectedNavigation)
        {
            selectionServiceMock.Verify(x => x.Select(expectedNavigation), Times.Once);
        }
    }
}
