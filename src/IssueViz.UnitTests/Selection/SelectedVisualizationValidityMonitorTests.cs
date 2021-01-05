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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Selection
{
    [TestClass]
    public class SelectedVisualizationValidityMonitorTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<SelectedVisualizationValidityMonitor, ISelectedVisualizationValidityMonitor>(null, new[]
            {
                MefTestHelpers.CreateExport<IAnalysisIssueSelectionService>(Mock.Of<IAnalysisIssueSelectionService>()),
                MefTestHelpers.CreateExport<IIssueLocationStoreAggregator>(Mock.Of<IIssueLocationStoreAggregator>())
            });
        }

        [TestMethod]
        public void Ctor_ShouldRegisterToIssuesChangedEvent()
        {
            var locationStoreAggregator = new Mock<IIssueLocationStoreAggregator>();
            locationStoreAggregator.SetupAdd(x => x.IssuesChanged += null);

            CreateTestSubject(locationStoreAggregator.Object);

            locationStoreAggregator.VerifyAdd(x => x.IssuesChanged += It.IsAny<EventHandler<IssuesChangedEventArgs>>(), Times.Once());
            locationStoreAggregator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_ShouldUnregisterFromIssuesChangedEvent()
        {
            var locationStoreAggregator = new Mock<IIssueLocationStoreAggregator>();
            locationStoreAggregator.SetupRemove(x => x.IssuesChanged -= null);

            var testSubject = CreateTestSubject(locationStoreAggregator.Object);

            locationStoreAggregator.Invocations.Clear();

            testSubject.Dispose();

            locationStoreAggregator.VerifyRemove(x => x.IssuesChanged -= It.IsAny<EventHandler<IssuesChangedEventArgs>>(), Times.Once());
            locationStoreAggregator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IssuesChanged_NoSelectedIssueViz_NoChanges()
        {
            var selectionService = new Mock<IAnalysisIssueSelectionService>();
            SetupSelectedIssue(selectionService, null);

            var locationStoreAggregator = new Mock<IIssueLocationStoreAggregator>();

            CreateTestSubject(locationStoreAggregator.Object, selectionService.Object);

            RaiseIssuesChangedEvent(locationStoreAggregator, "someFile.cpp");

            VerifySelectedIssueNotChanged(selectionService);
        }

        [TestMethod]
        public void IssuesChanged_SelectedIssueVizIsInADifferentFile_NoChanges()
        {
            var selectionService = new Mock<IAnalysisIssueSelectionService>();
            SetupSelectedIssue(selectionService, CreateIssueViz("someFile.cpp"));

            var locationStoreAggregator = new Mock<IIssueLocationStoreAggregator>();

            CreateTestSubject(locationStoreAggregator.Object, selectionService.Object);

            RaiseIssuesChangedEvent(locationStoreAggregator, "someOtherFile.cpp");

            VerifySelectedIssueNotChanged(selectionService);
        }

        [TestMethod]
        public void IssuesChanged_SelectedIssueVizIsInAChangedFile_IssueStillExists_NoChanges()
        {
            var issueViz = CreateIssueViz("someFile.cpp");

            var selectionService = new Mock<IAnalysisIssueSelectionService>();
            SetupSelectedIssue(selectionService, issueViz);

            var locationStoreAggregator = new Mock<IIssueLocationStoreAggregator>();
            locationStoreAggregator.Setup(x => x.Contains(issueViz)).Returns(true);

            CreateTestSubject(locationStoreAggregator.Object, selectionService.Object);

            RaiseIssuesChangedEvent(locationStoreAggregator, "someFile.cpp");

            VerifySelectedIssueNotChanged(selectionService);
        }

        [TestMethod]
        public void IssuesChanged_SelectedIssueVizIsInAChangedFile_IssueDoesNotExistAnymore_IssueCleared()
        {
            var issueViz = CreateIssueViz("someFile.cpp");

            var selectionService = new Mock<IAnalysisIssueSelectionService>();
            SetupSelectedIssue(selectionService, issueViz);

            var locationStoreAggregator = new Mock<IIssueLocationStoreAggregator>();
            locationStoreAggregator.Setup(x => x.Contains(issueViz)).Returns(false);

            CreateTestSubject(locationStoreAggregator.Object, selectionService.Object);

            RaiseIssuesChangedEvent(locationStoreAggregator, "someFile.cpp");

            VerifySelectedIssueCleared(selectionService);
        }

        private SelectedVisualizationValidityMonitor CreateTestSubject(IIssueLocationStoreAggregator storeAggregator, IAnalysisIssueSelectionService selectionService = null)
        {
            selectionService ??= Mock.Of<IAnalysisIssueSelectionService>();
            return new SelectedVisualizationValidityMonitor(selectionService, storeAggregator);
        }

        private static void RaiseIssuesChangedEvent(Mock<IIssueLocationStoreAggregator> locationStoreAggregator, string filePath)
        {
            locationStoreAggregator.Raise(x => x.IssuesChanged += null, new IssuesChangedEventArgs(new[] { filePath }));
        }

        private static void SetupSelectedIssue(Mock<IAnalysisIssueSelectionService> selectionService, IAnalysisIssueVisualization selectedIssue)
        {
            selectionService.SetupGet(x => x.SelectedIssue).Returns(selectedIssue);
        }

        private static void VerifySelectedIssueNotChanged(Mock<IAnalysisIssueSelectionService> selectionService)
        {
            selectionService.VerifySet(x => x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never);
        }

        private static void VerifySelectedIssueCleared(Mock<IAnalysisIssueSelectionService> selectionService)
        {
            selectionService.VerifySet(x => x.SelectedIssue = null, Times.Once);
        }

        private IAnalysisIssueVisualization CreateIssueViz(string filePath)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.SetupGet(x => x.CurrentFilePath).Returns(filePath);

            return issueViz.Object;
        }
    }
}
