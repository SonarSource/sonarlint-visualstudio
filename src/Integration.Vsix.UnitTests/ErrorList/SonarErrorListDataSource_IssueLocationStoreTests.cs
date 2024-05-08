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

using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class SonarErrorListDataSource_IssueLocationStoreTests
    {
        private const string ValidPath = "valid.txt";

        [TestMethod]
        public void GetLocations_NullArg_Throws()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.GetLocations(null);
            act.Should().ThrowExactly<ArgumentNullException>("filePath");
        }

        [TestMethod]
        public void GetLocations_NoFactories_EmptyListReturned()
        {
            var testSubject = CreateTestSubject();

            var actual = testSubject.GetLocations(ValidPath);
            actual.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_FactoryButNoMatches_EmptyListReturned()
        {
            var testSubject = CreateTestSubject();

            var factory1 = CreateFactoryWithLocationVizs(ValidPath);
            testSubject.AddFactory(factory1);

            var actual = testSubject.GetLocations(ValidPath);

            actual.Should().BeEmpty();
            CheckSnapshotGetLocationsCalled(factory1);
        }

        [TestMethod]
        public void GetLocations_FactoriesWithMatches_ExpectedIssuesReturned()
        {
            var testSubject = CreateTestSubject();

            var expectedLoc1 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var expectedLoc2 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var expectedLoc3 = Mock.Of<IAnalysisIssueLocationVisualization>();

            var factory1 = CreateFactoryWithLocationVizs(ValidPath, expectedLoc1);
            var factory2 = CreateFactoryWithLocationVizs(ValidPath /* no locations */ );
            var factory3 = CreateFactoryWithLocationVizs(ValidPath, expectedLoc2, expectedLoc3);

            testSubject.AddFactory(factory1);
            testSubject.AddFactory(factory2);
            testSubject.AddFactory(factory3);

            var actual = testSubject.GetLocations(ValidPath);

            actual.Should().BeEquivalentTo(expectedLoc1, expectedLoc2, expectedLoc3);
        }

        [TestMethod]
        public void RemoveFactory_NoEventListeners_NoError()
        {
            var testSubject = CreateTestSubject();
            var factory = CreateFactoryAndSnapshotWithSpecifiedFiles("file1.txt", "file2.txt");

            Action act = () => testSubject.RemoveFactory(factory);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void RemoveFactory_HasListener_EventRaised()
        {
            var testSubject = CreateTestSubject();
            var factory = CreateFactoryAndSnapshotWithSpecifiedFiles("file1.txt", "file2.txt");
            testSubject.AddFactory(factory);

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            testSubject.RemoveFactory(factory);

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("file1.txt", "file2.txt");
        }

        [TestMethod]
        public void RemoveFactory_FactoryIsNotRegistered_EventNotRaised()
        {
            var testSubject = CreateTestSubject();
            var factory = CreateFactoryAndSnapshotWithSpecifiedFiles("any.txt");

            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => eventCount++;

            testSubject.RemoveFactory(factory);

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void RefreshErrorList_NoEventListeners_NoError()
        {
            var testSubject = CreateTestSubject();
            var factory = CreateFactoryAndSnapshotWithSpecifiedFiles("file1.txt", "file2.txt");

            Action act = () => testSubject.RefreshErrorList(factory);
            act.Should().NotThrow();
        }

        [TestMethod]
        public void RefreshErrorList_HasListener_EventRaised()
        {
            var testSubject = CreateTestSubject();
            var factory = CreateFactoryAndSnapshotWithSpecifiedFiles("file1.txt", "file2.txt");
            testSubject.AddFactory(factory);

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            testSubject.RefreshErrorList(factory);

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("file1.txt", "file2.txt");
        }

        [TestMethod]
        public void RefreshErrorList_FactoryIsNotRegistered_EventNotRaised()
        {
            var testSubject = CreateTestSubject();
            var factory = CreateFactoryAndSnapshotWithSpecifiedFiles("any.txt");

            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => eventCount++;

            testSubject.RefreshErrorList(factory);

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void Refresh_NullArg_Throws()
        {
            var testSubject = CreateTestSubject();
            Action act = () => testSubject.Refresh(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("affectedFilePaths");
        }

        [TestMethod]
        public void RefreshOnBufferChanged_NullArg_Throws()
        {
            var testSubject = CreateTestSubject();
            Action act = () => testSubject.RefreshOnBufferChanged(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("affectedFilePath");
        }

        /// <summary>
        /// Most of the behaviour of Refresh(file) and RefreshOnBufferChanged(files) is the same.
        /// This delegate is used by tests that check the common functionality
        /// i.e. there is a common internal test method that does the common setup and assertions.
        /// This delegate is passed into that common method to make a call to either 
        /// "Refresh(file)" or "RefreshOnBufferChanged(files)"
        /// </summary>
        private delegate void RefreshImplTestOperation(SonarErrorListDataSource testSubject, string filePath);

        private static void CallRefresh(SonarErrorListDataSource testSubject, string filePath)
            => testSubject.Refresh(new[] { filePath });

        private static void CallRefreshOnBufferChanged(SonarErrorListDataSource testSubject, string filePath)
        {
            // The RefreshOnBufferChanged method should never raise an IssuesChanged event, so we
            // can add it here as an invariant check in all tests
            var issueChangedHandler = CreateAndRegisterIssuesEventHandler(testSubject);

            testSubject.RefreshOnBufferChanged(filePath);

            CheckIssuesChangedEventIsNotRaised(issueChangedHandler);
        }
        [TestMethod]
        public void Refresh_FactoriesWithMatchingFilesUpdatedAndErrorListRefreshed()
            => RefreshImpl_FactoriesWithMatchingFilesUpdatedAndErrorListRefreshed(CallRefresh);

        [TestMethod]
        public void RefreshOnBufferChanged_FactoriesWithMatchingFilesUpdatedAndErrorListRefreshed()
            => RefreshImpl_FactoriesWithMatchingFilesUpdatedAndErrorListRefreshed(CallRefreshOnBufferChanged);

        private void RefreshImpl_FactoriesWithMatchingFilesUpdatedAndErrorListRefreshed(RefreshImplTestOperation testOp)
        {
            var testSubject = CreateTestSubject();

            var factoryWithMatch1 = CreateFactoryAndSnapshotWithSpecifiedFiles("match.txt");
            var factoryWithMatch2 = CreateFactoryAndSnapshotWithSpecifiedFiles("MATCH.TXT");
            var factoryWithoutMatches = CreateFactoryAndSnapshotWithSpecifiedFiles("not a match.txt");

            testSubject.AddFactory(factoryWithMatch1);
            testSubject.AddFactory(factoryWithoutMatches);
            testSubject.AddFactory(factoryWithMatch2);

            var sinkMock1 = new Mock<ITableDataSink>();
            var sinkMock2 = new Mock<ITableDataSink>();
            testSubject.Subscribe(sinkMock1.Object);
            testSubject.Subscribe(sinkMock2.Object);

            testOp(testSubject, "match.txt");

            CheckUpdateSnapshotCalled(factoryWithMatch1);
            CheckUpdateSnapshotCalled(factoryWithMatch2);
            CheckUpdateSnapshotNotCalled(factoryWithoutMatches);

            // All sinks should be notified about updates to only factories with matches
            CheckSinkNotifiedOfChangeToFactories(sinkMock1, factoryWithMatch1, factoryWithMatch2);
            CheckSinkNotifiedOfChangeToFactories(sinkMock2, factoryWithMatch1, factoryWithMatch2);
        }

        [TestMethod]
        public void RefreshImpl_SomeFactoriesWithMatchingFiles_RaisesSingleIssueChangeEventContainingExpectedFiles()
        {
            var testSubject = CreateTestSubject();

            var factoryWithMatch1 = CreateFactoryAndSnapshotWithSpecifiedFiles("match1.aaa", "match2.bbb");
            var factoryWithMatch2 = CreateFactoryAndSnapshotWithSpecifiedFiles("MATCH1.AAA", "match3.ccc"); // tests case-insensitivity
            var factoryWithMatch3 = CreateFactoryAndSnapshotWithSpecifiedFiles("not a match but still included.txt", "match4.ddd");
            var factoryWithMatch4 = CreateFactoryAndSnapshotWithSpecifiedFiles("match5.eee");
            var factoryWithoutMatches1 = CreateFactoryAndSnapshotWithSpecifiedFiles("not a match.txt");
            var factoryWithoutMatches2 = CreateFactoryAndSnapshotWithSpecifiedFiles("another not a match.txt");

            testSubject.AddFactory(factoryWithMatch1);
            testSubject.AddFactory(factoryWithoutMatches1);
            testSubject.AddFactory(factoryWithMatch2);
            testSubject.AddFactory(factoryWithoutMatches2);
            testSubject.AddFactory(factoryWithMatch3);
            testSubject.AddFactory(factoryWithMatch4);

            var issuesChangedEvent = CreateAndRegisterIssuesEventHandler(testSubject);

            // XXX.txt and YYY.ccc won't match any of the snapshot files
            testSubject.Refresh(new[] { "XXX.txt", "match1.aaa", "match2.bbb", "match3.ccc", "YYY.ccc", "match4.ddd", "match5.eee" });

            // Should be one event with all directyl and indirectly affected files
            CheckSingleIssuesChangedEvent(issuesChangedEvent, testSubject,
                    "match1.aaa", "match2.bbb", "match3.ccc", "match4.ddd", "match5.eee",
                    // this file is included because it is indirectly included i.e. is in a snapshot a file that does match
                    "not a match but still included.txt"
                    );
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Refresh_SelectedIssueIsNotInFactory_SelectedIssueNotChanged(bool hasSelectedIssue)
            => RefreshImpl_SelectedIssueIsNotInFactory_SelectedIssueNotChanged(hasSelectedIssue, CallRefresh);

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void RefreshOnBufferChanged_SelectedIssueIsNotInFactory_SelectedIssueNotChanged(bool hasSelectedIssue)
            => RefreshImpl_SelectedIssueIsNotInFactory_SelectedIssueNotChanged(hasSelectedIssue, CallRefreshOnBufferChanged);

        private void RefreshImpl_SelectedIssueIsNotInFactory_SelectedIssueNotChanged(bool hasSelectedIssue,
            RefreshImplTestOperation testOp)
        {
            var selectedIssue = hasSelectedIssue ? Mock.Of<IAnalysisIssueVisualization>() : null;
            var selectionService = CreateSelectionService(selectedIssue);

            var factory = CreateFactoryWithIssues(Mock.Of<IAnalysisIssueVisualization>());
            var testSubject = CreateTestSubject(selectionService.Object);
            testSubject.AddFactory(factory);

            testOp(testSubject, "test.txt");

            selectionService.VerifySet(x => x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never);
        }

        [TestMethod]
        public void Refresh_SelectedIssueIsInFactory_IssueIsNavigable_SelectedIssueNotChanged()
            => RefreshImpl_SelectedIssueIsInFactory_IssueIsNavigable_SelectedIssueNotChanged(CallRefresh);

        [TestMethod]
        public void RefreshOnBufferChanged_SelectedIssueIsInFactory_IssueIsNavigable_SelectedIssueNotChanged()
            => RefreshImpl_SelectedIssueIsInFactory_IssueIsNavigable_SelectedIssueNotChanged(CallRefreshOnBufferChanged);

        private void RefreshImpl_SelectedIssueIsInFactory_IssueIsNavigable_SelectedIssueNotChanged(RefreshImplTestOperation testOp)
        {
            var navigableSpan = (SnapshotSpan?) null;
            var navigableIssue = CreateIssueVizWithSpan(navigableSpan);
            var selectionService = CreateSelectionService(navigableIssue);

            var factory = CreateFactoryWithIssues(navigableIssue);
            var testSubject = CreateTestSubject(selectionService.Object);
            testSubject.AddFactory(factory);

            testOp(testSubject, "test.txt");

            selectionService.VerifySet(x => x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never);
        }

        [TestMethod]
        public void LocationsUpdated_SelectedIssueIsInFactory_IssueIsNotNavigable_SelectedIssueIsCleared()
            => RefreshImpl_SelectedIssueIsInFactory_IssueIsNotNavigable_SelectedIssueIsCleared(CallRefresh);

        [TestMethod]
        public void RefreshOnBufferChanged_SelectedIssueIsInFactory_IssueIsNotNavigable_SelectedIssueIsCleared()
            => RefreshImpl_SelectedIssueIsInFactory_IssueIsNotNavigable_SelectedIssueIsCleared(CallRefreshOnBufferChanged);

        private void RefreshImpl_SelectedIssueIsInFactory_IssueIsNotNavigable_SelectedIssueIsCleared(RefreshImplTestOperation testOp)
        {
            var nonNavigableSpan = new SnapshotSpan();
            var nonNavigableIssue = CreateIssueVizWithSpan(nonNavigableSpan);
            var selectionService = CreateSelectionService(nonNavigableIssue);

            var factory = CreateFactoryWithIssues(nonNavigableIssue);
            var testSubject = CreateTestSubject(selectionService.Object);
            testSubject.AddFactory(factory);

            testOp(testSubject, "test.txt");

            selectionService.VerifySet(x => x.SelectedIssue = null, Times.Once);
        }

        [TestMethod]
        public void Contains_NullArg_Throws()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Contains(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("issueVisualization");
        }

        [TestMethod]
        public void Contains_NoFactories_False()
        {
            var testSubject = CreateTestSubject();

            var result = testSubject.Contains(Mock.Of<IAnalysisIssueVisualization>());

            result.Should().BeFalse();
        }

        [TestMethod]
        public void Contains_HasFactories_IssueVizDoesNotExist_False()
        {
            var factory1 = new Mock<IIssuesSnapshotFactory>();
            factory1
                .SetupGet(x => x.CurrentSnapshot.Issues)
                .Returns(new[] { Mock.Of<IAnalysisIssueVisualization>() });

            var factory2 = new Mock<IIssuesSnapshotFactory>();
            factory2
                .SetupGet(x => x.CurrentSnapshot.Issues)
                .Returns(new[] { Mock.Of<IAnalysisIssueVisualization>() });

            var testSubject = CreateTestSubject();
            testSubject.AddFactory(factory1.Object);
            testSubject.AddFactory(factory2.Object);

            var result = testSubject.Contains(Mock.Of<IAnalysisIssueVisualization>());

            result.Should().BeFalse();
        }

        [TestMethod]
        public void Contains_HasFactories_IssueVizExists_True()
        {
            var factory1 = new Mock<IIssuesSnapshotFactory>();
            factory1
                .SetupGet(x => x.CurrentSnapshot.Issues)
                .Returns(new[] { Mock.Of<IAnalysisIssueVisualization>() });

            var issueViz = Mock.Of<IAnalysisIssueVisualization>();

            var factory2 = new Mock<IIssuesSnapshotFactory>();
            factory2
                .SetupGet(x => x.CurrentSnapshot.Issues)
                .Returns(new[] { issueViz });

            var testSubject = CreateTestSubject();
            testSubject.AddFactory(factory1.Object);
            testSubject.AddFactory(factory2.Object);

            var result = testSubject.Contains(issueViz);

            result.Should().BeTrue();
        }

        private static IIssuesSnapshotFactory CreateFactoryWithLocationVizs(string filePathToMatch, params IAnalysisIssueLocationVisualization[] locVixsToReturn)
        {
            var snapshotMock = new Mock<IIssuesSnapshot>();
            snapshotMock.Setup(x => x.GetLocationsVizsForFile(filePathToMatch))
                .Returns(locVixsToReturn);

            var snapshotFactory = new Mock<IIssuesSnapshotFactory>();
            snapshotFactory.Setup(x => x.CurrentSnapshot).Returns(snapshotMock.Object);

            return snapshotFactory.Object;
        }

        private static IIssuesSnapshotFactory CreateFactoryWithIssues(params IAnalysisIssueVisualization[] issues)
        {
            var snapshotMock = new Mock<IIssuesSnapshot>();
            snapshotMock.Setup(x => x.Issues).Returns(issues);

            var snapshotFactory = new Mock<IIssuesSnapshotFactory>();
            snapshotFactory.Setup(x => x.CurrentSnapshot).Returns(snapshotMock.Object);

            return snapshotFactory.Object;
        }

        private static IIssuesSnapshotFactory CreateFactoryAndSnapshotWithSpecifiedFiles(params string[] filePaths)
        {
            var snapshotMock = new Mock<IIssuesSnapshot>();
            snapshotMock.Setup(x => x.FilesInSnapshot).Returns(filePaths);
            snapshotMock.Setup(x => x.GetUpdatedSnapshot()).Returns(Mock.Of<IIssuesSnapshot>());

            var snapshotFactory = new Mock<IIssuesSnapshotFactory>();
            snapshotFactory.Setup(x => x.CurrentSnapshot).Returns(snapshotMock.Object);

            return snapshotFactory.Object;
        }

        private static SonarErrorListDataSource CreateTestSubject(IIssueSelectionService selectionService = null)
        {
            var managerMock = new Mock<ITableManager>();
            var providerMock = new Mock<ITableManagerProvider>();
            providerMock.Setup(x => x.GetTableManager(StandardTables.ErrorsTable)).Returns(managerMock.Object);

            selectionService ??= Mock.Of<IIssueSelectionService>();

            return new SonarErrorListDataSource(providerMock.Object, Mock.Of<IFileRenamesEventSource>(), selectionService);
        }

        private static void CheckSnapshotGetLocationsCalled(IIssuesSnapshotFactory factory)
        {
            var snapshotMock = ((IMocked<IIssuesSnapshot>)factory.CurrentSnapshot).Mock;
            snapshotMock.Verify(x => x.GetLocationsVizsForFile(It.IsAny<string>()), Times.Once);
        }

        private static void CheckUpdateSnapshotNotCalled(IIssuesSnapshotFactory factory)
        {
            var snapshotMock = ((IMocked<IIssuesSnapshot>)factory.CurrentSnapshot).Mock;
            snapshotMock.Verify(x => x.GetUpdatedSnapshot(), Times.Never);
        }

        private void CheckUpdateSnapshotCalled(IIssuesSnapshotFactory factory)
        {
            var snapshotMock = ((IMocked<IIssuesSnapshot>)factory.CurrentSnapshot).Mock;
            snapshotMock.Verify(x => x.GetUpdatedSnapshot(), Times.Once);

            var updatedSnapshot = factory.CurrentSnapshot.GetUpdatedSnapshot();
            updatedSnapshot.Should().NotBeNull();

            Mock.Get(factory).Verify(x=> x.UpdateSnapshot(updatedSnapshot), Times.Once);
        }

        private void CheckSinkNotifiedOfChangeToFactories(Mock<ITableDataSink> sinkMock, params IIssuesSnapshotFactory[] factories)
        {
            foreach (var factory in factories)
            {
                sinkMock.Verify(x => x.FactorySnapshotChanged(factory), Times.Once);
            }
            sinkMock.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Exactly(factories.Length));
        }

        private static Mock<EventHandler<IssuesChangedEventArgs>> CreateAndRegisterIssuesEventHandler(SonarErrorListDataSource testSubject)
        {
            var issuesChangedHandler = new Mock<EventHandler<IssuesChangedEventArgs>>();
            testSubject.IssuesChanged += issuesChangedHandler.Object;
            return issuesChangedHandler;
        }

        private static void CheckIssuesChangedEventIsNotRaised(Mock<EventHandler<IssuesChangedEventArgs>> issuesChangedEvent)
            => issuesChangedEvent.Invocations.Should().HaveCount(0);

        private static void CheckSingleIssuesChangedEvent(Mock<EventHandler<IssuesChangedEventArgs>> issuesChangedEvent,
            SonarErrorListDataSource testSubject,
            params string[] expectedFileNames)
        {
            issuesChangedEvent.Invocations.Should().HaveCount(1);
            issuesChangedEvent.Invocations[0].Arguments[0].Should().BeSameAs(testSubject);

            var actualEventArgs = issuesChangedEvent.Invocations[0].Arguments[1] as IssuesChangedEventArgs;
            actualEventArgs.Should().NotBeNull();
            actualEventArgs.AnalyzedFiles.Should().BeEquivalentTo(expectedFileNames);
        }
        
        private Mock<IIssueSelectionService> CreateSelectionService(IAnalysisIssueVisualization selectedIssueViz)
        {
            var selectionService = new Mock<IIssueSelectionService>();
            selectionService.Setup(x => x.SelectedIssue).Returns(selectedIssueViz);

            return selectionService;
        }

        private IAnalysisIssueVisualization CreateIssueVizWithSpan(SnapshotSpan? span)
        {
            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Span).Returns(span);

            return issueViz.Object;
        }
    }
}
