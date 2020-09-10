/*
 * SonarQube Client
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
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.TableControls
{
    [TestClass]
    public class VSTableEventProcessorProcesserTests
    {
        private Mock<IWpfTableControl> mockTable;
        private Mock<ITableEntryHandle> mockSelectedItem;
        private Mock<ITableEntriesSnapshot> mockSnapshot;

        private Mock<IIssueTablesSelectionMonitor> mockMonitor;
        private VSTableEventProcessorProvider.EventProcessor testSubject;

        // Note: this must be a field as we are using it as an out parameter
        private int ValidRowIndex = 999;

        [TestInitialize]
        public void TestInitialize()
        {
            mockTable = new Mock<IWpfTableControl>();
            mockSelectedItem = new Mock<ITableEntryHandle>();
            mockSnapshot = new Mock<ITableEntriesSnapshot>();

            mockMonitor = new Mock<IIssueTablesSelectionMonitor>();

            testSubject = new VSTableEventProcessorProvider.EventProcessor(mockTable.Object, mockMonitor.Object);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void SelectionChanged_TooManyOrTooFewSelectedItems_PassesNullToMonitor(int numberOfSelectedItems)
        {
            // Set up the first check to fail
            SetSelectedEntryCount(numberOfSelectedItems);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(null);

            SanityCheckSnapshotNotAccessed();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetSnapshotReturnsFalse_PassesNullToMonitor()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);

            // Set up TryGetSnapshot so it fails
            SetupTryGetSnapshot(resultToReturn: false);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(null);

            SanityCheckSnapshotNotAccessed();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsFalse_PassesNullToMonitor()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            // Set up TryGetValue so it fails
            SetupTryGetValue(objectToReturn: null, false);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueButObjectIsNull_PassesNullToMonitor()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            // Set up TryGetValue so it succeed but returns null
            SetupTryGetValue(objectToReturn: null, true);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueButValueIsNotIssue_PassesNullToMonitor()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            // Set up preceeding checks to succeed but not return IAnalysisIssueVisualization
            SetupTryGetValue(objectToReturn: new object(), true);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueAndValueIsIssueButWithoutSecondaryLocations_PassesNullToMonitor()
        {
            // Set up all checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            var issueWithoutLocations = new Mock<IAnalysisIssueVisualization>();
            issueWithoutLocations.Setup(x => x.Flows).Returns(Array.Empty<IAnalysisIssueFlowVisualization>());

            SetupTryGetValue(issueWithoutLocations, true);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueAndValueIsIssueWithSecondaryLocations_PassesIssueToMonitor()
        {
            // Set up all checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            var flowWithLocation = new Mock<IAnalysisIssueFlowVisualization>();
            flowWithLocation.Setup(x => x.Locations).Returns(new[] {Mock.Of<IAnalysisIssueLocationVisualization>()});

            var issueWithLocations = new Mock<IAnalysisIssueVisualization>();
            issueWithLocations.Setup(x => x.Flows).Returns(new[] {flowWithLocation.Object});

            SetupTryGetValue(issueWithLocations.Object, true);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(issueWithLocations.Object);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_NonCriticalExceptionIsSuppressed()
        {
            mockTable.Setup(x => x.SelectedEntries).Throws(new InvalidOperationException());

            CheckDoesNotThrowOrCallMonitor(SimulatePostProcessEvent);
        }

        [TestMethod]
        public void SelectionChanged_CriticalExceptionIsNotSuppressed()
        {
            mockTable.Setup(x => x.SelectedEntries).Throws(new StackOverflowException());

            Action act = SimulatePostProcessEvent;

            act.Should().ThrowExactly<StackOverflowException>();
            CheckMonitorIsNotCalled();
        }

        [TestMethod]
        public void UnusedEvent_DoNotCallMonitor()
        {
            // ITableControlEventProcessor.PostprocessSelectionChanged should be the only event we handle

            var tableControlProcess = ((ITableControlEventProcessor)testSubject);

            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.KeyDown(null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.KeyUp(null));

            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessDragEnter(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessDragOver(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessDrop(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessGiveFeedback(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseDown(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseEnter(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseLeave(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseLeftButtonDown(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseLeftButtonUp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseMove(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseRightButtonDown(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseRightButtonUp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseUp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessMouseWheel(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessNavigate(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessNavigateToHelp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PostprocessQueryContinueDrag(null, null));

            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessDragEnter(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessDragOver(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessDrop(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessGiveFeedback(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseDown(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseEnter(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseLeave(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseLeftButtonDown(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseLeftButtonUp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseMove(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseRightButtonDown(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseRightButtonUp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseUp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessMouseWheel(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessNavigate(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessNavigateToHelp(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessQueryContinueDrag(null, null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreprocessSelectionChanged(null));

            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreviewKeyDown(null));
            CheckDoesNotThrowOrCallMonitor(() => tableControlProcess.PreviewKeyUp(null));
        }

        private void SetSelectedEntryCount(int numberOfEntries)
        {
            var dummySelectedEntries = new ITableEntryHandle[numberOfEntries];
            mockTable.Setup(x => x.SelectedEntries).Returns(dummySelectedEntries);
        }

        private void SetupTryGetSnapshot(bool resultToReturn)
        {
            var snapshotToReturn = mockSnapshot.Object;
            mockSelectedItem.Setup(x => x.TryGetSnapshot(out snapshotToReturn, out ValidRowIndex))
                .Returns(resultToReturn);

            mockTable.Setup(x => x.SelectedEntry).Returns(mockSelectedItem.Object);
        }

        private void SetupTryGetValue(object objectToReturn, bool resultToReturn) =>
            mockSnapshot.Setup(x => x.TryGetValue(ValidRowIndex, SonarLintTableControlConstants.IssueVizColumnName, out objectToReturn))
                .Returns(resultToReturn);

        private void SimulatePostProcessEvent()
        {
            ((ITableControlEventProcessor)testSubject).PostprocessSelectionChanged(new TableSelectionChangedEventArgs(null));
        }

        private void CheckMonitorCalledOnce(IAnalysisIssueVisualization expected)
        {
            mockMonitor.Verify(x => x.SelectionChanged(expected), Times.Once);
            mockMonitor.VerifyNoOtherCalls();
        }

        private void CheckMonitorIsNotCalled()
        {
            mockMonitor.Verify(x => x.SelectionChanged(It.IsAny<IAnalysisIssueVisualization>()), Times.Never);
            mockMonitor.VerifyNoOtherCalls();
        }

        // Sanity check that a test didn't get as far as using the snapshot
        private void SanityCheckSnapshotNotAccessed() =>
            mockSnapshot.Invocations.Count.Should().Be(0);

        // Sanity check that a test at least got as far as using the snapshot
        private void SanityCheckSnapshotAccessedOnce() =>
            mockSnapshot.Invocations.Count.Should().Be(1);

        private void CheckDoesNotThrowOrCallMonitor(Action act)
        {
            act.Should().NotThrow();
            CheckMonitorIsNotCalled();
        }
    }
}
