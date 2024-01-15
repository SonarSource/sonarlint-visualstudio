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

using System;
using FluentAssertions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests
{
    [TestClass]
    public class AnalysisIssueSelectionServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<AnalysisIssueSelectionService, IAnalysisIssueSelectionService>(
                MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>()),
                MefTestHelpers.CreateExport<IIssueSelectionService>());
        }

        [TestMethod]
        public void Ctor_RegisterToIssueSelectionEvent()
        {
            var selectionService = new Mock<IIssueSelectionService>();
            selectionService.SetupAdd(x => x.SelectedIssueChanged += null);

            CreateTestSubject(selectionService: selectionService.Object);

            selectionService.VerifyAdd(x => x.SelectedIssueChanged += It.IsAny<EventHandler>(), Times.Once);
            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromIssueSelectionEvent()
        {
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            selectionService.Reset();
            selectionService.SetupRemove(x => x.SelectedIssueChanged -= null);

            testSubject.Dispose();

            selectionService.VerifyRemove(x => x.SelectedIssueChanged -= It.IsAny<EventHandler>(), Times.Once);
            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SetSelectedIssue_IssueIsNull_UiContextIsHidden()
        {
            var monitorSelection = new Mock<IVsMonitorSelection>();
            var cookie = SetupContextMock(monitorSelection);

            var testSubject = CreateTestSubject(monitorSelection: monitorSelection.Object);

            testSubject.SelectedIssue = null;

            monitorSelection.Verify(x=> x.SetCmdUIContext(cookie, 0), Times.Once);
        }

        [TestMethod]
        public void SetSelectedIssue_IssueIsNotNull_UiContextIsShown()
        {
            var monitorSelection = new Mock<IVsMonitorSelection>();
            var cookie = SetupContextMock(monitorSelection);

            var testSubject = CreateTestSubject(monitorSelection: monitorSelection.Object);

            testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            monitorSelection.Verify(x => x.SetCmdUIContext(cookie, 1), Times.Once);
        }

        [TestMethod]
        public void SetSelectedIssue_FailsToGetContext_NoException()
        {
            var monitorSelection = new Mock<IVsMonitorSelection>();
            SetupContextMock(monitorSelection, VSConstants.E_FAIL);

            var testSubject = CreateTestSubject(monitorSelection: monitorSelection.Object);

            Action act = () => testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            act.Should().NotThrow();

            monitorSelection.Verify(x => x.SetCmdUIContext(It.IsAny<uint>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void SetSelectedIssue_NoSubscribers_NoException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SetSelectedFlow_NoSubscribers_NoException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.SelectedFlow = Mock.Of<IAnalysisIssueFlowVisualization>();

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SetSelectedLocation_NoSubscribers_NoException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.SelectedLocation = Mock.Of<IAnalysisIssueLocationVisualization>();

            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelIssue(bool isNewIssueNull)
        {
            var testSubject = CreateTestSubject();

            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            var expectedIssue = isNewIssueNull ? null : Mock.Of<IAnalysisIssueVisualization>();
            testSubject.SelectedIssue = expectedIssue;

            eventHandler.Verify(
                x => x(testSubject,
                    It.Is((SelectionChangedEventArgs args) => args.SelectedIssue == expectedIssue &&
                                                              args.SelectionChangeLevel == SelectionChangeLevel.Issue)),
                Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedFlow_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelFlow(bool isNewFlowNull)
        {
            var testSubject = CreateTestSubject();

            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            var expectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlowVisualization>();
            testSubject.SelectedFlow = expectedFlow;

            eventHandler.Verify(
                x => x(testSubject,
                    It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == expectedFlow &&
                                                              args.SelectionChangeLevel == SelectionChangeLevel.Flow)),
                Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedLocation_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelLocation(bool isNewLocationNull)
        {
            var testSubject = CreateTestSubject();

            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            var expectedLocation = isNewLocationNull ? null : Mock.Of<IAnalysisIssueLocationVisualization>();
            testSubject.SelectedLocation = expectedLocation;

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
            var testSubject = CreateTestSubject();

            testSubject.SelectedIssue.Should().BeNull();

            var expectedIssue = isNewIssueNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            testSubject.SelectedIssue = expectedIssue;

            testSubject.SelectedIssue.Should().Be(expectedIssue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedFlow_ReturnsValue(bool isNewFlowNull)
        {
            var testSubject = CreateTestSubject();

            testSubject.SelectedFlow.Should().BeNull();

            var expectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlowVisualization>();

            testSubject.SelectedFlow = expectedFlow;

            testSubject.SelectedFlow.Should().Be(expectedFlow);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedLocation_ReturnsValue(bool isNewLocationNull)
        {
            var testSubject = CreateTestSubject();

            testSubject.SelectedLocation.Should().BeNull();

            var expectedLocation = isNewLocationNull ? null : Mock.Of<IAnalysisIssueLocationVisualization>();

            testSubject.SelectedLocation = expectedLocation;

            testSubject.SelectedLocation.Should().Be(expectedLocation);
        }

        [TestMethod]
        public void Dispose_HasSubscribers_RemovesSubscribers()
        {
            var testSubject = CreateTestSubject();

            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            testSubject.Dispose();

            testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedFlowChanged_ChangesSelectedLocation()
        {
            var testSubject = CreateTestSubject();

            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            // Set flow to value
            var firstFlowFirstLocation = Mock.Of<IAnalysisIssueLocationVisualization>();
            var firstFlow = CreateFlow(firstFlowFirstLocation);
            testSubject.SelectedFlow = firstFlow;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == firstFlowFirstLocation)), Times.Once());
            testSubject.SelectedLocation.Should().Be(firstFlowFirstLocation);

            // Set flow to null
            testSubject.SelectedFlow = null;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == null)), Times.Once());
            testSubject.SelectedLocation.Should().BeNull();

            // Set flow to a different value
            var secondFlowFirstLocation = Mock.Of<IAnalysisIssueLocationVisualization>();
            var secondFlow = CreateFlow(secondFlowFirstLocation);
            testSubject.SelectedFlow = secondFlow;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == secondFlowFirstLocation)), Times.Once());
            testSubject.SelectedLocation.Should().Be(secondFlowFirstLocation);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedIssueChanged_ChangesSelectedFlow()
        {
            var testSubject = CreateTestSubject();

            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            // Set issue to value
            var firstIssueFirstFlow = CreateFlow();
            var firstIssue = CreateIssue(firstIssueFirstFlow);
            testSubject.SelectedIssue = firstIssue;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == firstIssueFirstFlow)), Times.Once());
            testSubject.SelectedFlow.Should().Be(firstIssueFirstFlow);

            // Set issue to null
            testSubject.SelectedIssue = null;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == null)), Times.Once());
            testSubject.SelectedFlow.Should().BeNull();

            // Set issue to different value
            var secondIssueFirstFlow = CreateFlow();
            var secondIssue = CreateIssue(secondIssueFirstFlow);
            testSubject.SelectedIssue = secondIssue;
            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == secondIssueFirstFlow)), Times.Once());
            testSubject.SelectedFlow.Should().Be(secondIssueFirstFlow);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IssueSelectionServiceEvent_IssueHasSecondaryLocations_SelectedIssueIsSet()
        {
            var location = Mock.Of<IAnalysisIssueLocationVisualization>();
            var flow = CreateFlow(location);
            var issue = CreateIssue(flow);

            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            RaiseSelectedIssueChangedEvent(selectionService, issue);

            testSubject.SelectedIssue.Should().Be(issue);
            testSubject.SelectedFlow.Should().Be(flow);
            testSubject.SelectedLocation.Should().Be(location);
        }

        [TestMethod]
        public void IssueSelectionServiceEvent_IssueHasNoSecondaryLocations_SelectedIssueIsCleared()
        {
            var issue = CreateIssue();

            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var oldSelection = Mock.Of<IAnalysisIssueVisualization>();
            testSubject.SelectedIssue = oldSelection;

            testSubject.SelectedIssue.Should().Be(oldSelection);

            RaiseSelectedIssueChangedEvent(selectionService, issue);

            testSubject.SelectedIssue.Should().BeNull();
            testSubject.SelectedFlow.Should().BeNull();
            testSubject.SelectedLocation.Should().BeNull();
        }

        [TestMethod]
        public void IssueSelectionServiceEvent_IssueIsNull_SelectedIssueIsCleared()
        {
            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var oldSelection = Mock.Of<IAnalysisIssueVisualization>();
            testSubject.SelectedIssue = oldSelection;

            testSubject.SelectedIssue.Should().Be(oldSelection);

            RaiseSelectedIssueChangedEvent(selectionService, null);

            testSubject.SelectedIssue.Should().BeNull();
            testSubject.SelectedFlow.Should().BeNull();
            testSubject.SelectedLocation.Should().BeNull();
        }

        private void RaiseSelectedIssueChangedEvent(Mock<IIssueSelectionService> selectionService, IAnalysisIssueVisualization issue)
        {
            selectionService.Setup(x => x.SelectedIssue).Returns(issue);
            selectionService.Raise(x => x.SelectedIssueChanged += null, null, EventArgs.Empty);
        }

        private IAnalysisIssueVisualization CreateIssue(params IAnalysisIssueFlowVisualization[] flows)
        {
            var issue = new Mock<IAnalysisIssueVisualization>();
            issue.Setup(x => x.Flows).Returns(flows);

            return issue.Object;
        }

        private IAnalysisIssueFlowVisualization CreateFlow(params IAnalysisIssueLocationVisualization[] locations)
        {
            var flow = new Mock<IAnalysisIssueFlowVisualization>();
            flow.Setup(x => x.Locations).Returns(locations);

            return flow.Object;
        }

        private uint SetupContextMock(Mock<IVsMonitorSelection> monitorSelectionMock, int result = VSConstants.S_OK)
        {
            uint cookie = 0;

            monitorSelectionMock
                .Setup(x => x.GetCmdUIContextCookie(ref It.Ref<Guid>.IsAny, out cookie))
                .Returns(result);

            return cookie;
        }

        private AnalysisIssueSelectionService CreateTestSubject(
            IVsMonitorSelection monitorSelection = null,
            IIssueSelectionService selectionService = null)
        {
            monitorSelection ??= Mock.Of<IVsMonitorSelection>();
            selectionService ??= Mock.Of<IIssueSelectionService>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsShellMonitorSelection)))
                .Returns(monitorSelection);

            return new AnalysisIssueSelectionService(serviceProviderMock.Object, selectionService);
        }
    }
}
