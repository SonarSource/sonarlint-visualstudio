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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.Vsix;
using SonarLint.VisualStudio.Integration.Vsix.ErrorList;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.Integration.UnitTests.ErrorList
{
    [TestClass]
    public class SnapshotFactoryTests
    {
        [TestMethod]
        public void HandleFileRenames_NoReferencesToToRenamedFile_NoChanges()
        {
            var location = CreateLocation("some file1");
            var snapshot = CreateIssuesSnapshot("some file2", location);

            var testSubject = new IssuesSnapshotFactory(snapshot.Object);

            var result = testSubject.HandleFileRenames(new Dictionary<string, string>{{ "old file", "new file" } });
            result.Should().BeFalse();

            testSubject.CurrentSnapshot.Should().Be(snapshot.Object);
            location.CurrentFilePath.Should().Be("some file1");

            snapshot.Verify(x=> x.IncrementVersion(), Times.Never);
            snapshot.Verify(x=> x.CreateUpdatedSnapshot(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void HandleFileRenames_OnlyAnalyzedFileRenamed_SnapshotUpdated()
        {
            var location1 = CreateLocation("file1");
            var location2 = CreateLocation("file2");
            var snapshot = CreateIssuesSnapshot("file3", location1, location2);

            var renames = new Dictionary<string, string>
            {
                {"file3", "new file3"}
            };

            var updatedSnapshot = CreateIssuesSnapshot("new file3");
            snapshot.Setup(x => x.CreateUpdatedSnapshot("new file3")).Returns(updatedSnapshot.Object);

            var testSubject = new IssuesSnapshotFactory(snapshot.Object);

            var result = testSubject.HandleFileRenames(renames);
            result.Should().BeTrue();

            testSubject.CurrentSnapshot.Should().Be(updatedSnapshot.Object);

            location1.CurrentFilePath.Should().Be("file1");
            location2.CurrentFilePath.Should().Be("file2");

            snapshot.Verify(x => x.IncrementVersion(), Times.Never);
            updatedSnapshot.Verify(x => x.IncrementVersion(), Times.Never);
        }

        [TestMethod]
        public void HandleFileRenames_AnalyzedFileAndLocationsRenamed_SnapshotUpdated()
        {
            var location1 = CreateLocation("file1");
            var location2 = CreateLocation("file2");
            var snapshot = CreateIssuesSnapshot("file3", location1, location2);

            var renames = new Dictionary<string, string>
            {
                {"file1", "new file1"},
                {"file3", "new file3"}
            };

            var updatedSnapshot = CreateIssuesSnapshot("new file3");
            snapshot.Setup(x => x.CreateUpdatedSnapshot("new file3")).Returns(updatedSnapshot.Object);

            var testSubject = new IssuesSnapshotFactory(snapshot.Object);

            var result = testSubject.HandleFileRenames(renames);
            result.Should().BeTrue();

            testSubject.CurrentSnapshot.Should().Be(updatedSnapshot.Object);

            location1.CurrentFilePath.Should().Be("new file1");
            location2.CurrentFilePath.Should().Be("file2");

            snapshot.Verify(x => x.IncrementVersion(), Times.Never);
            updatedSnapshot.Verify(x => x.IncrementVersion(), Times.Never);
        }

        [TestMethod]
        public void OnFilesRenamed_OnlyLocationsRenamed_SnapshotUpdated()
        {
            var location1 = CreateLocation("file1");
            var location2 = CreateLocation("file2");
            var snapshot = CreateIssuesSnapshot("file3", location1, location2);

            var renames = new Dictionary<string, string>
            {
                {"file1", "new file1"}
            };

            var updatedSnapshot = CreateIssuesSnapshot("file3");
            snapshot.Setup(x => x.CreateUpdatedSnapshot("file3")).Returns(updatedSnapshot.Object);

            var testSubject = new IssuesSnapshotFactory(snapshot.Object);

            var result = testSubject.HandleFileRenames(renames);
            result.Should().BeTrue();

            testSubject.CurrentSnapshot.Should().Be(updatedSnapshot.Object);

            location1.CurrentFilePath.Should().Be("new file1");
            location2.CurrentFilePath.Should().Be("file2");

            snapshot.Verify(x => x.IncrementVersion(), Times.Never);
            updatedSnapshot.Verify(x => x.IncrementVersion(), Times.Never);
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
