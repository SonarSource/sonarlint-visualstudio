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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class SonarErrorListDataSource_FileRenameHandlingTests
    {
        private Mock<IFileRenamesEventSource> mockFileRenamesEventSource;
        private Mock<ITableDataSink> mockTableSync;

        [TestInitialize]
        public void TestInitialize()
        {
            mockFileRenamesEventSource = new Mock<IFileRenamesEventSource>();
            mockTableSync = new Mock<ITableDataSink>();
        }

        [TestMethod]
        public void Ctor_RegisterToFileRenamesEvent()
        {
            mockFileRenamesEventSource.SetupAdd(x => x.FilesRenamed += (sender, args) => { });

            CreateTestSubject();

            mockFileRenamesEventSource.VerifyAdd(x => x.FilesRenamed += It.IsAny<EventHandler<FilesRenamedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromFileRenamesEvent()
        {
            mockFileRenamesEventSource.SetupRemove(x => x.FilesRenamed -= (sender, args) => { });

            var testSubject = CreateTestSubject();
            testSubject.Dispose();

            mockFileRenamesEventSource.VerifyRemove(x => x.FilesRenamed -= It.IsAny<EventHandler<FilesRenamedEventArgs>>(), Times.Once);
        }

        [TestMethod]
        public void OnFilesRenamed_NoFactories_NoException()
        {
            CreateTestSubject();

            Action act = () => RaiseFilesRenamedEvent(new Dictionary<string, string> {{"old", "new"}});
            act.Should().NotThrow();
        }

        [TestMethod]
        public void OnFilesRenamed_NoReferencesToRenamedFile_NoChanges()
        {
            const string filePath = "not renamed";
            var location = CreateLocation(filePath);
            var issuesSnapshot = CreateIssuesSnapshot(filePath, location);

            var testSubject = CreateTestSubject();
            var factory = new SnapshotFactory(issuesSnapshot.Object);
            testSubject.AddFactory(factory);

            RaiseFilesRenamedEvent(new Dictionary<string, string> {{"old file", "new file"}});

            location.CurrentFilePath.Should().Be(filePath);
            factory.CurrentSnapshot.Should().Be(issuesSnapshot.Object);
            mockTableSync.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Never);
            issuesSnapshot.Verify(x => x.IncrementVersion(), Times.Never);
            issuesSnapshot.Verify(x => x.CreateUpdatedSnapshot(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void OnFilesRenamed_RenamedAnalyzedFile_LocationsRenamedAndSnapshotUpdated()
        {
            var location1 = CreateLocation("old file");
            var location2 = CreateLocation("some other file");
            var issuesSnapshot = CreateIssuesSnapshot("old file", location1, location2);

            var expectedUpdatedSnapshot = CreateIssuesSnapshot("new file");
            issuesSnapshot.Setup(x => x.CreateUpdatedSnapshot("new file")).Returns(expectedUpdatedSnapshot.Object);

            var testSubject = CreateTestSubject();
            var factory = new SnapshotFactory(issuesSnapshot.Object);
            testSubject.AddFactory(factory);

            RaiseFilesRenamedEvent(new Dictionary<string, string> { { "old file", "new file" } });

            location1.CurrentFilePath.Should().Be("new file");
            location2.CurrentFilePath.Should().Be("some other file");
            factory.CurrentSnapshot.Should().Be(expectedUpdatedSnapshot.Object);
            mockTableSync.Verify(x => x.FactorySnapshotChanged(factory), Times.Once);
            issuesSnapshot.Verify(x => x.IncrementVersion(), Times.Never);
            expectedUpdatedSnapshot.Verify(x=> x.IncrementVersion(), Times.Never);
        }

        [TestMethod]
        public void OnFilesRenamed_RenamedSecondaryLocationFile_LocationsRenamedAndSnapshotVersionIncremented()
        {
            var location1 = CreateLocation("old file");
            var location2 = CreateLocation("some other file");
            var issuesSnapshot = CreateIssuesSnapshot("some other file", location1, location2);

            var testSubject = CreateTestSubject();
            var factory = new SnapshotFactory(issuesSnapshot.Object);
            testSubject.AddFactory(factory);

            RaiseFilesRenamedEvent(new Dictionary<string, string> { { "old file", "new file" } });

            location1.CurrentFilePath.Should().Be("new file");
            location2.CurrentFilePath.Should().Be("some other file");
            factory.CurrentSnapshot.Should().Be(issuesSnapshot.Object);
            mockTableSync.Verify(x => x.FactorySnapshotChanged(factory), Times.Once);
            issuesSnapshot.Verify(x => x.IncrementVersion(), Times.Once);
            issuesSnapshot.Verify(x => x.CreateUpdatedSnapshot(It.IsAny<string>()), Times.Never);
        }

        private static Mock<IIssuesSnapshot> CreateIssuesSnapshot(string analyzedFilePath, params IAnalysisIssueLocationVisualization[] locations)
        {
            var snapshotMock = new Mock<IIssuesSnapshot>();
            snapshotMock.SetupGet(x => x.AnalyzedFilePath).Returns(analyzedFilePath);

            var locationsInFiles = locations.GroupBy(x => x.CurrentFilePath);

            foreach (var locationsInFile in locationsInFiles)
            {
                snapshotMock.Setup(x => x.GetLocationsVizsForFile(locationsInFile.Key)).Returns(locationsInFile.ToList());
            }

            return snapshotMock;
        }
     
        private IAnalysisIssueLocationVisualization CreateLocation(string filePath)
        {
            var location = new Mock<IAnalysisIssueLocationVisualization>();
            location.SetupProperty(x => x.CurrentFilePath);
            location.Object.CurrentFilePath = filePath;

            return location.Object;
        }

        private void RaiseFilesRenamedEvent(IReadOnlyDictionary<string, string> renamedFiles)
        {
            mockFileRenamesEventSource.Raise(x => x.FilesRenamed += null, It.IsAny<object>(), new FilesRenamedEventArgs(renamedFiles));
        }

        private SonarErrorListDataSource CreateTestSubject()
        {
            var tableManagerProvider = new Mock<ITableManagerProvider>();
            tableManagerProvider.Setup(x => x.GetTableManager(StandardTables.ErrorsTable))
                .Returns(Mock.Of<ITableManager>());

            var testSubject = new SonarErrorListDataSource(tableManagerProvider.Object, mockFileRenamesEventSource.Object);
            testSubject.Subscribe(mockTableSync.Object);

            return testSubject;
        }
    }
}
