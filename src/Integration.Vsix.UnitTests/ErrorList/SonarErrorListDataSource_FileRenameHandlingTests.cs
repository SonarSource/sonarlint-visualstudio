﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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
using FluentAssertions;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Selection;

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
            var factory = new Mock<IIssuesSnapshotFactory>();
            var testSubject = CreateTestSubject();
            testSubject.AddFactory(factory.Object);

            var fileRenames = new Dictionary<string, string> {{"old file", "new file"}};

            factory.Setup(x => x.HandleFileRenames(fileRenames)).Returns(false);

            RaiseFilesRenamedEvent(fileRenames);
       
            mockTableSync.Verify(x => x.FactorySnapshotChanged(It.IsAny<ITableEntriesSnapshotFactory>()), Times.Never);
        }

        [TestMethod]
        public void OnFilesRenamed_HasReferencesToRenamedFile_ErrorListRefreshed()
        {
            var factory = new Mock<IIssuesSnapshotFactory>();
            var testSubject = CreateTestSubject();
            testSubject.AddFactory(factory.Object);

            var fileRenames = new Dictionary<string, string> { { "old file", "new file" } };

            factory.Setup(x => x.HandleFileRenames(fileRenames)).Returns(true);

            RaiseFilesRenamedEvent(fileRenames);

            mockTableSync.Verify(x => x.FactorySnapshotChanged(factory.Object), Times.Once);
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

            var testSubject = new SonarErrorListDataSource(tableManagerProvider.Object, mockFileRenamesEventSource.Object, Mock.Of<IIssueSelectionService>());
            testSubject.Subscribe(mockTableSync.Object);

            return testSubject;
        }
    }
}
