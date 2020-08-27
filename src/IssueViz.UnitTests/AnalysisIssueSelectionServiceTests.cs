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
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests
{
    [TestClass]
    public class AnalysisIssueSelectionServiceTests
    {
        private Mock<IVsMonitorSelection> monitorSelectionMock;

        private AnalysisIssueSelectionService testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            monitorSelectionMock = new Mock<IVsMonitorSelection>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsShellMonitorSelection)))
                .Returns(monitorSelectionMock.Object);

            testSubject = new AnalysisIssueSelectionService(serviceProviderMock.Object);
        }

        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<AnalysisIssueSelectionService, IAnalysisIssueSelectionService>(null,
                new[]
                {
                    MefTestHelpers.CreateExport<SVsServiceProvider>(Mock.Of<IServiceProvider>())
                });
        }

        [TestMethod]
        public void SetSelectedIssue_IssueIsNull_UiContextIsHidden()
        {
            var cookie = SetupContextMock();

            testSubject.SelectedIssue = null;

            monitorSelectionMock.Verify(x=> x.SetCmdUIContext(cookie, 0), Times.Once);
        }

        [TestMethod]
        public void SetSelectedIssue_IssueIsNotNull_UiContextIsShown()
        {
            var cookie = SetupContextMock();

            testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            monitorSelectionMock.Verify(x => x.SetCmdUIContext(cookie, 1), Times.Once);
        }

        [TestMethod]
        public void SetSelectedIssue_FailsToGetContext_NoException()
        { 
            SetupContextMock(VSConstants.E_FAIL);

            Action act = () => testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            act.Should().NotThrow();

            monitorSelectionMock.Verify(x => x.SetCmdUIContext(It.IsAny<uint>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public void SetSelectedIssue_NoSubscribers_NoException()
        {
            Action act = () => testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SetSelectedFlow_NoSubscribers_NoException()
        {
            Action act = () => testSubject.SelectedFlow = Mock.Of<IAnalysisIssueFlowVisualization>();

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SetSelectedLocation_NoSubscribers_NoException()
        {
            Action act = () => testSubject.SelectedLocation = Mock.Of<IAnalysisIssueLocationVisualization>();

            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelIssue(bool isNewIssueNull)
        {
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
            testSubject.SelectedLocation.Should().BeNull();

            var expectedLocation = isNewLocationNull ? null : Mock.Of<IAnalysisIssueLocationVisualization>();

            testSubject.SelectedLocation = expectedLocation;

            testSubject.SelectedLocation.Should().Be(expectedLocation);
        }

        [TestMethod]
        public void Dispose_HasSubscribers_RemovesSubscribers()
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            testSubject.Dispose();

            testSubject.SelectedIssue = Mock.Of<IAnalysisIssueVisualization>();

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectedFlowChanged_ChangesSelectedLocation()
        {
            var eventHandler = new Mock<EventHandler<SelectionChangedEventArgs>>();
            testSubject.SelectionChanged += eventHandler.Object;

            // Set flow to value
            var firstFlow = GetFlowWithLocation(out var firstFlowFirstLocation);
            testSubject.SelectedFlow = firstFlow;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == firstFlowFirstLocation)), Times.Once());
            testSubject.SelectedLocation.Should().Be(firstFlowFirstLocation);

            // Set flow to null
            testSubject.SelectedFlow = null;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedLocation == null)), Times.Once());
            testSubject.SelectedLocation.Should().BeNull();

            // Set flow to a different value
            var secondFlow = GetFlowWithLocation(out var secondFlowFirstLocation);
            testSubject.SelectedFlow = secondFlow;

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
            testSubject.SelectedIssue = firstIssue;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == firstIssueFirstFlow)), Times.Once());
            testSubject.SelectedFlow.Should().Be(firstIssueFirstFlow);

            // Set issue to null
            testSubject.SelectedIssue = null;

            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == null)), Times.Once());
            testSubject.SelectedFlow.Should().BeNull();

            // Set issue to different value
            var secondIssue = GetIssueWithFlow(out var secondIssueFirstFlow);
            testSubject.SelectedIssue = secondIssue;
            eventHandler.Verify(x => x(testSubject, It.Is((SelectionChangedEventArgs args) => args.SelectedFlow == secondIssueFirstFlow)), Times.Once());
            testSubject.SelectedFlow.Should().Be(secondIssueFirstFlow);

            eventHandler.VerifyNoOtherCalls();
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

        private uint SetupContextMock(int result = VSConstants.S_OK)
        {
            uint cookie = 0;

            monitorSelectionMock
                .Setup(x => x.GetCmdUIContextCookie(ref It.Ref<Guid>.IsAny, out cookie))
                .Returns(result);

            return cookie;
        }
    }
}
