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
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Selection
{
    [TestClass]
    public class IssueSelectionServiceTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<IssueSelectionService, IIssueSelectionService>(null,
                new[]
                {
                    MefTestHelpers.CreateExport<IAnalysisIssueSelectionService>(Mock.Of<IAnalysisIssueSelectionService>())
                });
        }

        [TestMethod]
        public void Ctor_RegisterToFlowStepSelectionEvent()
        {
            var flowStepSelectionService = new Mock<IAnalysisIssueSelectionService>();
            flowStepSelectionService.SetupAdd(x => x.SelectionChanged += null);

            CreateTestSubject(flowStepSelectionService.Object);

            flowStepSelectionService.VerifyAdd(x=> x.SelectionChanged += It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
            flowStepSelectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromFlowStepSelectionEvent()
        {
            var flowStepSelectionService = new Mock<IAnalysisIssueSelectionService>();

            var testSubject = CreateTestSubject(flowStepSelectionService.Object);

            flowStepSelectionService.Reset();
            flowStepSelectionService.SetupRemove(x => x.SelectionChanged -= null);

            testSubject.Dispose();

            flowStepSelectionService.VerifyRemove(x => x.SelectionChanged -= It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once);
            flowStepSelectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_IssueIsSet(bool isSetToNull)
        {
            var testSubject = CreateTestSubject();

            var issueViz = isSetToNull ? null : CreateIssueViz();
            testSubject.SelectedIssue = issueViz;

            testSubject.SelectedIssue.Should().Be(issueViz);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_NoSubscribersToSelectionChanged_NoException(bool isSetToNull)
        {
            var testSubject = CreateTestSubject();

            var issueViz = isSetToNull ? null : CreateIssueViz();

            Action act = () => testSubject.SelectedIssue = issueViz;
            act.Should().NotThrow();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_SameValue_SubscribersNotNotified(bool isSetToNull)
        {
            var oldSelection = isSetToNull ? null : CreateIssueViz();

            var testSubject = CreateTestSubject();
            
            testSubject.SelectedIssue = oldSelection;

            var callCount = 0;
            testSubject.SelectedIssueChanged += (sender, args) => { callCount++; };

            testSubject.SelectedIssue = oldSelection;

            callCount.Should().Be(0);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_DifferentValue_HasSubscribersToSelectionChanged_SubscribersNotified(bool isSetToNull)
        {
            var testSubject = CreateTestSubject();

            var oldSelection = isSetToNull ? CreateIssueViz() : null;
            var newSelection = isSetToNull ? null : CreateIssueViz();

            testSubject.SelectedIssue = oldSelection;

            var callCount = 0;
            testSubject.SelectedIssueChanged += (sender, args) => { callCount++; };
            
            testSubject.SelectedIssue = newSelection;

            callCount.Should().Be(1);
        }

        [TestMethod]
        public void SetSelectedIssue_IssueHasSecondaryLocations_FlowStepSelectionIsSet()
        {
            var issueViz = CreateIssueViz(Mock.Of<IAnalysisIssueLocationVisualization>());

            var flowStepSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var testSubject = CreateTestSubject(flowStepSelectionService.Object);

            flowStepSelectionService.Reset();

            testSubject.SelectedIssue = issueViz;

            flowStepSelectionService.VerifySet(x => x.SelectedIssue = issueViz, Times.Once);
        }

        [TestMethod]
        public void SetSelectedIssue_IssueHasNoSecondaryLocations_FlowStepSelectionIsCleared()
        {
            var issueViz = CreateIssueViz();

            var flowStepSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var testSubject = CreateTestSubject(flowStepSelectionService.Object);

            flowStepSelectionService.Reset();

            testSubject.SelectedIssue = issueViz;

            flowStepSelectionService.VerifySet(x => x.SelectedIssue = null, Times.Once);
        }

        [TestMethod]
        public void SetSelectedIssue_IssueIsNull_FlowStepSelectionIsCleared()
        {
            var flowStepSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var testSubject = CreateTestSubject(flowStepSelectionService.Object);

            var oldSelection = CreateIssueViz();
            testSubject.SelectedIssue = oldSelection;

            flowStepSelectionService.Reset();
            flowStepSelectionService.SetupGet(x => x.SelectedIssue).Returns(oldSelection);

            testSubject.SelectedIssue = null;

            flowStepSelectionService.VerifySet(x => x.SelectedIssue = null, Times.Once);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void FlowStepSelectionChanges_SelectedIssueIsSetAndSubscribersNotified(bool isSetToNull)
        {
            var flowStepSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var testSubject = CreateTestSubject(flowStepSelectionService.Object);

            var oldSelection = isSetToNull ? CreateIssueViz() : null;
            var newSelection = isSetToNull ? null : CreateIssueViz();

            testSubject.SelectedIssue = oldSelection;

            var callCount = 0;
            testSubject.SelectedIssueChanged += (sender, args) => callCount++;

            SetFlowStepSelection(flowStepSelectionService, newSelection);

            testSubject.SelectedIssue.Should().Be(newSelection);
            callCount.Should().Be(1);
        }

        [TestMethod]
        [Description("Verify that there is no infinite loop")]
        public void FlowStepSelectionChanges_FlowStepSelectionServiceNotCalledAgain()
        {
            var flowStepSelectionService = new Mock<IAnalysisIssueSelectionService>();
            var testSubject = CreateTestSubject(flowStepSelectionService.Object);

            var newSelection = CreateIssueViz(Mock.Of<IAnalysisIssueLocationVisualization>());

            SetFlowStepSelection(flowStepSelectionService, newSelection);

            testSubject.SelectedIssue.Should().Be(newSelection);

            flowStepSelectionService.VerifySet(x=> x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never);
        }

        private IAnalysisIssueVisualization CreateIssueViz(params IAnalysisIssueLocationVisualization[] locationVizs)
        {
            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.SetupGet(x => x.Locations).Returns(locationVizs);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupGet(x => x.Flows).Returns(new[] {flowViz.Object});

            return issueViz.Object;
        }

        private IssueSelectionService CreateTestSubject(IAnalysisIssueSelectionService flowStepSelectionService = null)
        {
            flowStepSelectionService ??= Mock.Of<IAnalysisIssueSelectionService>();

            return new IssueSelectionService(flowStepSelectionService);
        }

        private void SetFlowStepSelection(Mock<IAnalysisIssueSelectionService> flowStepSelectionService, IAnalysisIssueVisualization selectedIssue)
        {
            flowStepSelectionService.SetupGet(x => x.SelectedIssue).Returns(selectedIssue);

            flowStepSelectionService.Raise(x => x.SelectionChanged += null, null,
                new SelectionChangedEventArgs(SelectionChangeLevel.Issue,
                    selectedIssue,
                    null,
                    null));
        }
    }
}
