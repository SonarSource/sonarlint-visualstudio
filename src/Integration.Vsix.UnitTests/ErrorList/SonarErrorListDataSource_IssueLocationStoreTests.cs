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
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

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
        public void LocationsUpdated_NullArg_Throws()
        {
            var testSubject = CreateTestSubject();
            Action act = () => testSubject.Refresh(null);
            act.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("affectedFilePaths");
        }

        [TestMethod]
        public void LocationsUpdated_FactoriesWithMatchingFilesUpdatedAndErrorListRefreshed()
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

            testSubject.Refresh(new string[] { "match.txt" });

            CheckSnapshotIncrementVersionCalled(factoryWithMatch1);
            CheckSnapshotIncrementVersionCalled(factoryWithMatch2);
            CheckSnapshotIncrementVersionNotCalled(factoryWithoutMatches);

            // All sinks should be notified about updates to only factories with matches
            CheckSinkNotifiedOfChangeToFactories(sinkMock1, factoryWithMatch1, factoryWithMatch2);
            CheckSinkNotifiedOfChangeToFactories(sinkMock2, factoryWithMatch1, factoryWithMatch2);
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

        private static IIssuesSnapshotFactory CreateFactoryAndSnapshotWithSpecifiedFiles(params string[] filePaths)
        {
            var snapshotMock = new Mock<IIssuesSnapshot>();
            snapshotMock.Setup(x => x.FilesInSnapshot).Returns(filePaths);

            var snapshotFactory = new Mock<IIssuesSnapshotFactory>();
            snapshotFactory.Setup(x => x.CurrentSnapshot).Returns(snapshotMock.Object);

            return snapshotFactory.Object;
        }

        private static SonarErrorListDataSource CreateTestSubject()
        {
            var managerMock = new Mock<ITableManager>();
            var providerMock = new Mock<ITableManagerProvider>();
            providerMock.Setup(x => x.GetTableManager(StandardTables.ErrorsTable)).Returns(managerMock.Object);

            return new SonarErrorListDataSource(providerMock.Object, Mock.Of<IFileRenamesEventSource>());
        }

        private static void CheckSnapshotGetLocationsCalled(IIssuesSnapshotFactory factory)
        {
            var snapshotMock = ((Moq.IMocked<IIssuesSnapshot>)factory.CurrentSnapshot).Mock;
            snapshotMock.Verify(x => x.GetLocationsVizsForFile(It.IsAny<string>()), Times.Once);
        }

        private static void CheckSnapshotIncrementVersionCalled(IIssuesSnapshotFactory factory)
        {
            var snapshotMock = ((Moq.IMocked<IIssuesSnapshot>)factory.CurrentSnapshot).Mock;
            snapshotMock.Verify(x => x.IncrementVersion(), Times.Once);
        }

        private static void CheckSnapshotIncrementVersionNotCalled(IIssuesSnapshotFactory factory)
        {
            var snapshotMock = ((Moq.IMocked<IIssuesSnapshot>)factory.CurrentSnapshot).Mock;
            snapshotMock.Verify(x => x.IncrementVersion(), Times.Never);
        }

        private void CheckSinkNotifiedOfChangeToFactories(Mock<ITableDataSink> sinkMock, params IIssuesSnapshotFactory[] factories)
        {
            foreach (var factory in factories)
            {
                sinkMock.Verify(x => x.FactorySnapshotChanged(factory), Times.Once);
            }
            sinkMock.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Exactly(factories.Length));
        }
    }
}
