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
            suppliedArgs.AffectedFiles.Should().BeEquivalentTo("file1.txt", "file2.txt");
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

        private static SnapshotFactory CreateFactoryWithLocationVizs(string filePathToMatch, params IAnalysisIssueLocationVisualization[] locVixsToReturn)
        {
            var snapshotMock = new Mock<IIssuesSnapshot>();
            snapshotMock.Setup(x => x.GetLocationsVizsForFile(filePathToMatch))
                .Returns(locVixsToReturn);

            return new SnapshotFactory(snapshotMock.Object);
        }

        private static SnapshotFactory CreateFactoryAndSnapshotWithSpecifiedFiles(params string[] filePaths)
        {
            var snapshotMock = new Mock<IIssuesSnapshot>();
            snapshotMock.Setup(x => x.FilesInSnapshot).Returns(filePaths);

            return new SnapshotFactory(snapshotMock.Object);
        }

        private static SonarErrorListDataSource CreateTestSubject()
        {
            var managerMock = new Mock<ITableManager>();
            var providerMock = new Mock<ITableManagerProvider>();
            providerMock.Setup(x => x.GetTableManager(StandardTables.ErrorsTable)).Returns(managerMock.Object);

            return new SonarErrorListDataSource(providerMock.Object);
        }

        private static void CheckSnapshotGetLocationsCalled(SnapshotFactory factory)
        {
            var snapshotMock = ((Moq.IMocked<IIssuesSnapshot>)factory.CurrentSnapshot).Mock;
            snapshotMock.Verify(x => x.GetLocationsVizsForFile(It.IsAny<string>()), Times.Once);
        }
    }
}
