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

using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class SnapshotFactoryTests
    {
        [TestMethod]
        public void HandleFileRenames_NoReferencesToToRenamedFile_NoChanges()
        {
            var location = CreateLocation("some other file1");
            var snapshot = CreateIssuesSnapshot("some other file2", location);

            var testSubject = new SnapshotFactory(snapshot.Object);

            var result = testSubject.HandleFileRename("old file", "new file");
            result.Should().BeFalse();

            testSubject.CurrentSnapshot.Should().Be(snapshot.Object);
            location.CurrentFilePath.Should().Be("some other file1");
            snapshot.Verify(x=> x.IncrementVersion(), Times.Never);
            snapshot.Verify(x=> x.CreateUpdatedSnapshot(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HandleFileRenames_RenameAnalyzedFile_LocationsRenamedAndSnapshotUpdated()
        {
            var location1 = CreateLocation("old file");
            var location2 = CreateLocation("some other file");
            var snapshot = CreateIssuesSnapshot("old file", location1, location2);

            var expectedUpdatedSnapshot = CreateIssuesSnapshot("new file");
            snapshot.Setup(x => x.CreateUpdatedSnapshot("new file")).Returns(expectedUpdatedSnapshot.Object);

            var testSubject = new SnapshotFactory(snapshot.Object);

            var result = testSubject.HandleFileRename("old file", "new file");
            result.Should().BeTrue();

            testSubject.CurrentSnapshot.Should().Be(expectedUpdatedSnapshot.Object);
            location1.CurrentFilePath.Should().Be("new file");
            location2.CurrentFilePath.Should().Be("some other file");
            snapshot.Verify(x => x.IncrementVersion(), Times.Never);
            expectedUpdatedSnapshot.Verify(x => x.IncrementVersion(), Times.Never);
        }

        [TestMethod]
        public void OnFilesRenamed_RenamedSecondaryLocationFile_LocationsRenamedAndSnapshotVersionIncremented()
        {
            var location1 = CreateLocation("old file");
            var location2 = CreateLocation("some other file");
            var snapshot = CreateIssuesSnapshot("some other file", location1, location2);

            var testSubject = new SnapshotFactory(snapshot.Object);

            var result = testSubject.HandleFileRename("old file", "new file");
            result.Should().BeTrue();

            testSubject.CurrentSnapshot.Should().Be(snapshot.Object);
            location1.CurrentFilePath.Should().Be("new file");
            location2.CurrentFilePath.Should().Be("some other file");
            snapshot.Verify(x => x.IncrementVersion(), Times.Once);
            snapshot.Verify(x => x.CreateUpdatedSnapshot(It.IsAny<string>()), Times.Never);
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
    }
}
