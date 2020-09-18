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
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Selection
{
    [TestClass, Ignore]
    public class IssueFlowStepNavigatorTests
    {
        private Mock<IAnalysisIssueSelectionService> selectionServiceMock;
        private Mock<ILocationNavigator> locationNavigatorMock;

        private IssueFlowStepNavigator testSubject;

        [TestInitialize]
        public void TesInitialize()
        {
            selectionServiceMock = new Mock<IAnalysisIssueSelectionService>();
            locationNavigatorMock = new Mock<ILocationNavigator>();

            testSubject = new IssueFlowStepNavigator(selectionServiceMock.Object, locationNavigatorMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            var selectionServiceExport = MefTestHelpers.CreateExport<IAnalysisIssueSelectionService>(Mock.Of<IAnalysisIssueSelectionService>());
            var locationNavigatorExport = MefTestHelpers.CreateExport<ILocationNavigator>(Mock.Of<ILocationNavigator>());

            MefTestHelpers.CheckTypeCanBeImported<IssueFlowStepNavigator, IIssueFlowStepNavigator>(null, new[]
            {
                selectionServiceExport,
                locationNavigatorExport
            });
        }

        [TestMethod]
        public void GotoNextNavigableFlowStep_NoCurrentFlow_NoNavigation()
        {
            SetupCurrentFlow((IAnalysisIssueFlowVisualization) null);
            SetupCurrentLocation(CreateLocation());

            testSubject.GotoNextNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousNavigableFlowStep_NoCurrentFlow_NoNavigation()
        {
            SetupCurrentFlow((IAnalysisIssueFlowVisualization)null);
            SetupCurrentLocation(CreateLocation());

            testSubject.GotoPreviousNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextNavigableFlowStep_NoCurrentLocation_NoNavigation()
        {
            SetupCurrentFlow(CreateLocation());
            SetupCurrentLocation(null);

            testSubject.GotoNextNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousNavigableFlowStep_NoCurrentLocation_NoNavigation()
        {
            SetupCurrentFlow(CreateLocation());
            SetupCurrentLocation(null);

            testSubject.GotoPreviousNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextNavigableFlowStep_FlowHasOnlyOneLocation_NoNavigation()
        {
            var location = CreateLocation();

            SetupCurrentFlow(location);
            SetupCurrentLocation(location);

            testSubject.GotoNextNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousNavigableFlowStep_FlowHasOnlyOneLocation_NoNavigation()
        {
            var location = CreateLocation();

            SetupCurrentFlow(location);
            SetupCurrentLocation(location);

            testSubject.GotoPreviousNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextNavigableFlowStep_CurrentLocationIsLast_NoNavigation()
        {
            var firstLocation = CreateLocation(1);
            var lastLocation = CreateLocation(2);

            SetupCurrentFlow(firstLocation, lastLocation);
            SetupCurrentLocation(lastLocation);

            testSubject.GotoNextNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousNavigableFlowStep_CurrentLocationIsFirst_NoNavigation()
        {
            var firstLocation = CreateLocation(1);
            var lastLocation = CreateLocation(2);

            SetupCurrentFlow(firstLocation, lastLocation);
            SetupCurrentLocation(firstLocation);

            testSubject.GotoPreviousNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextNavigableFlowStep_CurrentLocationIsLastNavigableLocation_NoNavigation()
        {
            var locations = new[]
            {
                CreateLocation(1, isMarkedAsNavigable:true),
                CreateLocation(2, isMarkedAsNavigable:true),
                CreateLocation(3, isMarkedAsNavigable:false),
                CreateLocation(4, isMarkedAsNavigable:true, isTrulyNavigable:false)
            };

            var currentLocation = locations[1];

            SetupCurrentFlow(locations);
            SetupCurrentLocation(currentLocation);

            testSubject.GotoNextNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoPreviousNavigableFlowStep_CurrentLocationIsFirstNavigableLocation_NoNavigation()
        {
            var locations = new[]
            {
                CreateLocation(1, isMarkedAsNavigable:true, isTrulyNavigable:false),
                CreateLocation(2, isMarkedAsNavigable:false),
                CreateLocation(3, isMarkedAsNavigable:true),
                CreateLocation(4, isMarkedAsNavigable:true)
            };

            var currentLocation = locations[2];

            SetupCurrentFlow(locations);
            SetupCurrentLocation(currentLocation);

            testSubject.GotoPreviousNavigableFlowStep();

            VerifyNoNavigation();
        }

        [TestMethod]
        public void GotoNextNavigableFlowStep_NavigatesToNextNavigableLocation()
        {
            var locations = new[]
            {
                CreateLocation(1, isMarkedAsNavigable:true),
                CreateLocation(2, isMarkedAsNavigable:true), // current
                CreateLocation(3, isMarkedAsNavigable:false), 
                CreateLocation(4, isMarkedAsNavigable:true, isTrulyNavigable:false),
                CreateLocation(5, isMarkedAsNavigable:true), // expected
                CreateLocation(6, isMarkedAsNavigable:true)
            };

            var currentLocation = locations[1];
            var expectedNavigation = locations[4];

            SetupCurrentFlow(locations);
            SetupCurrentLocation(currentLocation);

            testSubject.GotoNextNavigableFlowStep();

            VerifyNavigation(expectedNavigation);
        }

        [TestMethod]
        public void GotoPreviousNavigableFlowStep_NavigatesToPreviousNavigableLocation()
        {
            var locations = new[]
            {
                CreateLocation(1, isMarkedAsNavigable:true),
                CreateLocation(2, isMarkedAsNavigable:true), // expected
                CreateLocation(3, isMarkedAsNavigable:false),
                CreateLocation(4, isMarkedAsNavigable:true, isTrulyNavigable:false),
                CreateLocation(5, isMarkedAsNavigable:true), // current
                CreateLocation(6, isMarkedAsNavigable:true)
            };

            var currentLocation = locations[4];
            var expectedNavigation = locations[1];

            SetupCurrentFlow(locations);
            SetupCurrentLocation(currentLocation);

            testSubject.GotoPreviousNavigableFlowStep();

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

        private IAnalysisIssueLocationVisualization CreateLocation(int stepNumber = 1, bool isMarkedAsNavigable = true, bool isTrulyNavigable = true)
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.Location).Returns(Mock.Of<IAnalysisIssueLocation>());
            locationViz.Setup(x => x.StepNumber).Returns(stepNumber);

            if (isMarkedAsNavigable)
            {
                locationViz.Setup(x => x.Span).Returns((SnapshotSpan?) null);
            }
            else
            {
                locationViz.Setup(x => x.Span).Returns(new SnapshotSpan());
            }

            locationNavigatorMock.Setup(x => x.TryNavigate(locationViz.Object)).Returns(isTrulyNavigable);

            return locationViz.Object;
        }

        private void VerifyNoNavigation()
        {
            selectionServiceMock.VerifySet(x => x.SelectedLocation = It.IsAny<IAnalysisIssueLocationVisualization>(),
                Times.Never);
        }

        private void VerifyNavigation(IAnalysisIssueLocationVisualization expectedNavigation)
        {
            selectionServiceMock.VerifySet(x => x.SelectedLocation = expectedNavigation, Times.Once);
        }
    }
}
