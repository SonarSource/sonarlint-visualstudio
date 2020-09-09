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
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor
{
    [TestClass]
    public class FileRenamesEventHandlerTests
    {
        private FileRenamesEventHandler testSubject;
        private Mock<IIssueLocationStore> locationsStoreMock;
        private Mock<IVsTrackProjectDocuments2> trackProjectDocumentsMock;
        private uint cookie;

        [TestInitialize]
        public void TestInitialize()
        {
            locationsStoreMock = new Mock<IIssueLocationStore>();
            trackProjectDocumentsMock = new Mock<IVsTrackProjectDocuments2>();

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(x => x.GetService(typeof(SVsTrackProjectDocuments)))
                .Returns(trackProjectDocumentsMock.Object);

            testSubject = new FileRenamesEventHandler(serviceProviderMock.Object, locationsStoreMock.Object);

            cookie = 0;
            trackProjectDocumentsMock.Setup(x => x.AdviseTrackProjectDocumentsEvents(testSubject, out cookie));
        }

        [TestMethod]
        public void Ctor_RegisterToAdviseTrackProjectDocumentsEvents()
        {
            trackProjectDocumentsMock.Verify(x => x.AdviseTrackProjectDocumentsEvents(testSubject, out cookie), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromAdviseTrackProjectDocumentsEvents()
        {
            trackProjectDocumentsMock.Verify(x=> x.UnadviseTrackProjectDocumentsEvents(It.IsAny<uint>()), Times.Never);

            testSubject.Dispose();

            trackProjectDocumentsMock.Verify(x => x.UnadviseTrackProjectDocumentsEvents(cookie), Times.Once);
        }

        [TestMethod]
        public void AfterDocumentsRenamed_NoLocationsInChangedDocument_NoChanges()
        {
            SetupLocationsInFile("old name1", Array.Empty<IAnalysisIssueLocationVisualization>());
            SetupLocationsInFile("old name2", Array.Empty<IAnalysisIssueLocationVisualization>());

            var renamedFiles = new Dictionary<string, string>
            {
                {"old name1", "new name1"},
                {"old name2", "new name2"}
            };

            RaiseDocumentsRenamed(renamedFiles);

            locationsStoreMock.Verify(x=> x.GetLocations("old name1"), Times.Once);
            locationsStoreMock.Verify(x=> x.GetLocations("old name2"), Times.Once);

            locationsStoreMock.Verify(x=> x.GetLocations("new name1"), Times.Never);
            locationsStoreMock.Verify(x=> x.GetLocations("new name2"), Times.Never);

            locationsStoreMock.Verify(x=> x.Refresh(It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        [TestMethod]
        public void AfterDocumentsRenamed_OneChangedDocumentWithLocations_LocationPathsUpdated()
        {
            var location1 = CreateLocation("old name");
            var location2 = CreateLocation("old name");
            SetupLocationsInFile("old name", location1, location2);

            var renamedFiles = new Dictionary<string, string>
            {
                {"old name", "new name"}
            };

            RaiseDocumentsRenamed(renamedFiles);

            location1.CurrentFilePath.Should().Be("new name");
            location2.CurrentFilePath.Should().Be("new name");

            locationsStoreMock.Verify(x => x.GetLocations("old name"), Times.Once);
            locationsStoreMock.Verify(x => x.GetLocations("new name"), Times.Never);
            locationsStoreMock.Verify(x => x.Refresh(new[] {"old name"}), Times.Once());
        }

        [TestMethod]
        public void AfterDocumentsRenamed_TwoChangedDocumentsWithLocations_LocationPathsUpdated()
        {
            var location1 = CreateLocation("old name1");
            var location2 = CreateLocation("old name2");

            SetupLocationsInFile("old name1", location1);
            SetupLocationsInFile("old name2", location2);

            var renamedFiles = new Dictionary<string, string>
            {
                {"old name1", "new name1"},
                {"old name2", "new name2"}
            };

            RaiseDocumentsRenamed(renamedFiles);

            location1.CurrentFilePath.Should().Be("new name1");
            location2.CurrentFilePath.Should().Be("new name2");

            locationsStoreMock.Verify(x => x.GetLocations("old name1"), Times.Once);
            locationsStoreMock.Verify(x => x.GetLocations("old name2"), Times.Once);

            locationsStoreMock.Verify(x => x.GetLocations("new name1"), Times.Never);
            locationsStoreMock.Verify(x => x.GetLocations("new name2"), Times.Never);

            locationsStoreMock.Verify(x => x.Refresh(new[] { "old name1", "old name2" }), Times.Once());
        }

        private IAnalysisIssueLocationVisualization CreateLocation(string filePath)
        {
            var location = new Mock<IAnalysisIssueLocationVisualization>();
            location.SetupProperty(x => x.CurrentFilePath);
            location.Object.CurrentFilePath = filePath;

            return location.Object;
        }

        private void SetupLocationsInFile(string filePath, params IAnalysisIssueLocationVisualization[] locations)
        {
            locationsStoreMock
                .Setup(x => x.GetLocations(filePath))
                .Returns(locations);
        }

        private void RaiseDocumentsRenamed(IDictionary<string, string> oldNewFilePaths)
        {
            (testSubject as IVsTrackProjectDocumentsEvents2).OnAfterRenameFiles(0, 0, null, new int[0],
                oldNewFilePaths.Keys.ToArray(), oldNewFilePaths.Values.ToArray(), null);
        }
    }
}
