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
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.IssueVisualization.TableControls;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.TableControls
{
    [TestClass]
    public class VSTableEventProcessorProcesserTests
    {
        private Mock<IWpfTableControl> mockTable;
        private Mock<ITableEntryHandle> mockSelectedItem;
        private Mock<ITableEntriesSnapshot> mockSnapshot;
        private Mock<IIssueSelectionService> mockSelectionService;
        private VSTableEventProcessorProvider.EventProcessor testSubject;

        // Note: this must be a field as we are using it as an out parameter
        private int ValidRowIndex = 999;

        [TestInitialize]
        public void TestInitialize()
        {
            mockTable = new Mock<IWpfTableControl>();
            mockSelectedItem = new Mock<ITableEntryHandle>();
            mockSnapshot = new Mock<ITableEntriesSnapshot>();
            mockSelectionService = new Mock<IIssueSelectionService>();

            testSubject = new VSTableEventProcessorProvider.EventProcessor(mockTable.Object, mockSelectionService.Object);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void SelectionChanged_TooManyOrTooFewSelectedItems_PassesNullToSelectionService(int numberOfSelectedItems)
        {
            // Set up the first check to fail
            SetSelectedEntryCount(numberOfSelectedItems);

            SimulatePostProcessEvent();

            CheckSelectionServiceCalledOnce(null);

            SanityCheckSnapshotNotAccessed();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetSnapshotReturnsFalse_PassesNullToSelectionService()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);

            // Set up TryGetSnapshot so it fails
            SetupTryGetSnapshot(resultToReturn: false);

            SimulatePostProcessEvent();

            CheckSelectionServiceCalledOnce(null);

            SanityCheckSnapshotNotAccessed();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsFalse_PassesNullToSelectionService()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            // Set up TryGetValue so it fails
            SetupTryGetValue(objectToReturn: null, false);

            SimulatePostProcessEvent();

            CheckSelectionServiceCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueButObjectIsNull_PassesNullToSelectionService()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            // Set up TryGetValue so it succeed but returns null
            SetupTryGetValue(objectToReturn: null, true);

            SimulatePostProcessEvent();

            CheckSelectionServiceCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueButValueIsNotIssue_PassesNullToSelectionService()
        {
            // Set up preceeding checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            // Set up preceeding checks to succeed but not return IAnalysisIssueVisualization
            SetupTryGetValue(objectToReturn: new object(), true);

            SimulatePostProcessEvent();

            CheckSelectionServiceCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueAndValueIsIssue_PassesIssueToSelectionService()
        {
            // Set up all checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            var issue = Mock.Of<IAnalysisIssueVisualization>();

            SetupTryGetValue(issue, true);

            SimulatePostProcessEvent();

            CheckSelectionServiceCalledOnce(issue);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_NonCriticalExceptionIsSuppressed()
        {
            mockTable.Setup(x => x.SelectedEntries).Throws(new InvalidOperationException());

            CheckDoesNotThrowOrCallSelectionService(SimulatePostProcessEvent);
        }

        [TestMethod]
        public void SelectionChanged_CriticalExceptionIsNotSuppressed()
        {
            mockTable.Setup(x => x.SelectedEntries).Throws(new StackOverflowException());

            Action act = SimulatePostProcessEvent;

            act.Should().ThrowExactly<StackOverflowException>();
            CheckSelectionServiceIsNotCalled();
        }

        [TestMethod]
        public void UnusedEvent_DoNotCallMonitor()
        {
            // ITableControlEventProcessor.PostprocessSelectionChanged should be the only event we handle

            var tableControlProcess = ((ITableControlEventProcessor)testSubject);

            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.KeyDown(null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.KeyUp(null));

            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessDragEnter(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessDragOver(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessDrop(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessGiveFeedback(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseDown(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseEnter(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseLeave(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseLeftButtonDown(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseLeftButtonUp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseMove(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseRightButtonDown(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseRightButtonUp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseUp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessMouseWheel(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessNavigate(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessNavigateToHelp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PostprocessQueryContinueDrag(null, null));

            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessDragEnter(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessDragLeave(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessDragOver(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessDrop(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessGiveFeedback(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseDown(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseEnter(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseLeave(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseLeftButtonDown(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseLeftButtonUp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseMove(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseRightButtonDown(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseRightButtonUp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseUp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessMouseWheel(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessNavigate(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessNavigateToHelp(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessQueryContinueDrag(null, null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreprocessSelectionChanged(null));

            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreviewKeyDown(null));
            CheckDoesNotThrowOrCallSelectionService(() => tableControlProcess.PreviewKeyUp(null));
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

        private void CheckSelectionServiceCalledOnce(IAnalysisIssueVisualization expected)
        {
            mockSelectionService.VerifySet(x => x.SelectedIssue = expected, Times.Once);
            mockSelectionService.VerifyNoOtherCalls();
        }

        private void CheckSelectionServiceIsNotCalled()
        {
            mockSelectionService.VerifySet(x => x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never);
            mockSelectionService.VerifyNoOtherCalls();
        }

        // Sanity check that a test didn't get as far as using the snapshot
        private void SanityCheckSnapshotNotAccessed() =>
            mockSnapshot.Invocations.Count.Should().Be(0);

        // Sanity check that a test at least got as far as using the snapshot
        private void SanityCheckSnapshotAccessedOnce() =>
            mockSnapshot.Invocations.Count.Should().Be(1);

        private void CheckDoesNotThrowOrCallSelectionService(Action act)
        {
            act.Should().NotThrow();
            CheckSelectionServiceIsNotCalled();
        }
    }
}
