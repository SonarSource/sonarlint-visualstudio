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

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests
{
    [TestClass]
    public class AnalysisIssueSelectionEventsTests
    {
        private AnalysisIssueSelectionEvents testSubject;

        [TestInitialize]
        public void TestInitialize()
        {
            testSubject = new AnalysisIssueSelectionEvents();
        }

        [TestMethod]
        public void SetSelectedIssue_NoSubscribers_NoException()
        {
            Action act = () => testSubject.SelectedIssue = Mock.Of<IAnalysisIssue>();

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SetSelectedFlow_NoSubscribers_NoException()
        {
            Action act = () => testSubject.SelectedFlow = Mock.Of<IAnalysisIssueFlow>();

            act.Should().NotThrow();
        }

        [TestMethod]
        public void SetSelectedLocation_NoSubscribers_NoException()
        {
            Action act = () => testSubject.SelectedLocation = Mock.Of<IAnalysisIssueLocation>();

            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_HasSubscribers_RaisesSelectedIssueChangedEvent(bool isNewIssueNull)
        {
            var eventHandler = new Mock<EventHandler<IssueChangedEventArgs>>();

            testSubject.SelectedIssueChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            var expectedIssue = isNewIssueNull ? null : Mock.Of<IAnalysisIssue>();
            testSubject.SelectedIssue = expectedIssue;

            eventHandler.Verify(x => x(testSubject, It.Is((IssueChangedEventArgs args) => args.Issue == expectedIssue)), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedFlow_HasSubscribers_RaisesSelectedFlowChangedEvent(bool isNewFlowNull)
        {
            var eventHandler = new Mock<EventHandler<FlowChangedEventArgs>>();

            testSubject.SelectedFlowChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            var expectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlow>();
            testSubject.SelectedFlow = expectedFlow;

            eventHandler.Verify(x => x(testSubject, It.Is((FlowChangedEventArgs args) => args.Flow == expectedFlow)), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedLocation_HasSubscribers_RaisesSelectedLocationChangedEvent(bool isNewLocationNull)
        {
            var eventHandler = new Mock<EventHandler<LocationChangedEventArgs>>();

            testSubject.SelectedLocationChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            var expectedLocation = isNewLocationNull ? null : Mock.Of<IAnalysisIssueLocation>();
            testSubject.SelectedLocation = expectedLocation;

            eventHandler.Verify(x => x(testSubject, It.Is((LocationChangedEventArgs args) => args.Location == expectedLocation)), Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedIssue_ReturnsValue(bool isNewIssueNull)
        {
            testSubject.SelectedIssue.Should().BeNull();

            var expectedIssue = isNewIssueNull ? null : Mock.Of<IAnalysisIssue>();

            testSubject.SelectedIssue = expectedIssue;

            testSubject.SelectedIssue.Should().Be(expectedIssue);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedFlow_ReturnsValue(bool isNewFlowNull)
        {
            testSubject.SelectedFlow.Should().BeNull();

            var expectedFlow = isNewFlowNull ? null : Mock.Of<IAnalysisIssueFlow>();

            testSubject.SelectedFlow = expectedFlow;

            testSubject.SelectedFlow.Should().Be(expectedFlow);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetSelectedLocation_ReturnsValue(bool isNewLocationNull)
        {
            testSubject.SelectedLocation.Should().BeNull();

            var expectedLocation = isNewLocationNull ? null : Mock.Of<IAnalysisIssueLocation>();

            testSubject.SelectedLocation = expectedLocation;

            testSubject.SelectedLocation.Should().Be(expectedLocation);
        }

        [TestMethod]
        public void Dispose_SelectedIssueChangedHasSubscribers_RemovesSubscribers()
        {
            var eventHandler = new Mock<EventHandler<IssueChangedEventArgs>>();

            testSubject.SelectedIssueChanged += eventHandler.Object;
            testSubject.Dispose();

            testSubject.SelectedIssue = Mock.Of<IAnalysisIssue>();

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_SelectedFlowChangedHasSubscribers_RemovesSubscribers()
        {
            var eventHandler = new Mock<EventHandler<FlowChangedEventArgs>>();

            testSubject.SelectedFlowChanged += eventHandler.Object;
            testSubject.Dispose();

            testSubject.SelectedFlow = Mock.Of<IAnalysisIssueFlow>();

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_SelectedLocationChangedHasSubscribers_RemovesSubscribers()
        {
            var eventHandler = new Mock<EventHandler<LocationChangedEventArgs>>();

            testSubject.SelectedLocationChanged += eventHandler.Object;
            testSubject.Dispose();

            testSubject.SelectedLocation = Mock.Of<IAnalysisIssueLocation>();

            eventHandler.VerifyNoOtherCalls();
        }
    }
}
