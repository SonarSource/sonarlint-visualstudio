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

using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
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

            // Set up preceeding checks to succeed but not return IAnalysisIssue
            SetupTryGetValue(objectToReturn: new object(), true);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(null);

            SanityCheckSnapshotAccessedOnce();
        }

        [TestMethod]
        public void SelectionChanged_SingleItem_TryGetValueReturnsTrueAndValueIsIssue_PassesIssueToMonitor()
        {
            // Set up all checks to succeed
            SetSelectedEntryCount(1);
            SetupTryGetSnapshot(true);

            var expectedIssue = Mock.Of<IAnalysisIssue>();
            SetupTryGetValue(expectedIssue, true);

            SimulatePostProcessEvent();

            CheckMonitorCalledOnce(expectedIssue);

            SanityCheckSnapshotAccessedOnce();
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
            mockSnapshot.Setup(x => x.TryGetValue(ValidRowIndex, SonarLintTableControlConstants.IssueColumnName, out objectToReturn))
                .Returns(resultToReturn);


        private void SimulatePostProcessEvent()
        {
            ((ITableControlEventProcessor)testSubject).PostprocessSelectionChanged(new TableSelectionChangedEventArgs(null));
        }

        private void CheckMonitorCalledOnce(IAnalysisIssue expectedIssue)
        {
            mockMonitor.Verify(x => x.SelectionChanged(expectedIssue), Times.Once);
            mockMonitor.VerifyNoOtherCalls();
        }

        // Sanity check that a test didn't get as far as using the snapshot
        private void SanityCheckSnapshotNotAccessed() =>
            mockSnapshot.Invocations.Count.Should().Be(0);

        // Sanity check that a test at least got as far as using the snapshot
        private void SanityCheckSnapshotAccessedOnce() =>
            mockSnapshot.Invocations.Count.Should().Be(1);
    }
}
