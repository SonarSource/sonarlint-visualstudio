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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots
{
    [TestClass]
    public class HotspotsStoreTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            var hotspotsStoreImporter = new SingleObjectImporter<IHotspotsStore>();
            var issueLocationStoreImporter = new SingleObjectImporter<IIssueLocationStore>();
            batch.AddPart(hotspotsStoreImporter);
            batch.AddPart(issueLocationStoreImporter);

            var catalog = new TypeCatalog(typeof(HotspotsStore));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);

            hotspotsStoreImporter.Import.Should().NotBeNull();
            issueLocationStoreImporter.Import.Should().NotBeNull();

            hotspotsStoreImporter.Import.Should().BeSameAs(issueLocationStoreImporter.Import);
        }

        [TestMethod]
        public void GetOrAdd_NoExistingHotspots_HotspotAdded()
        {
            var testSubject = CreateTestSubject();

            var hotspotToAdd = CreateIssueViz(hotspotKey:"some hotspot");
            var addedHotspot = testSubject.GetOrAdd(hotspotToAdd);

            addedHotspot.Should().BeSameAs(hotspotToAdd);
            testSubject.GetAll().Count.Should().Be(1);
            testSubject.GetAll()[0].Should().BeSameAs(hotspotToAdd);
        }

        [TestMethod]
        public void GetOrAdd_NoMatchingHotspot_HotspotAdded()
        {
            var testSubject = CreateTestSubject();

            var someOtherHotspot = CreateIssueViz(hotspotKey: "some hotspot 1");
            testSubject.GetOrAdd(someOtherHotspot);

            var hotspotToAdd = CreateIssueViz(hotspotKey: "some hotspot 2");
            var addedHotspot = testSubject.GetOrAdd(hotspotToAdd);

            addedHotspot.Should().BeSameAs(hotspotToAdd);
            testSubject.GetAll().Count.Should().Be(2);
            testSubject.GetAll()[0].Should().BeSameAs(someOtherHotspot);
            testSubject.GetAll()[1].Should().BeSameAs(hotspotToAdd);
        }

        [TestMethod]
        public void GetOrAdd_HasMatchingHotspot_HotspotNotAdded()
        {
            var testSubject = CreateTestSubject();

            var firstHotspot = CreateIssueViz(hotspotKey: "some hotspot");
            var secondHotspot = CreateIssueViz(hotspotKey: "some hotspot");
            
            testSubject.GetOrAdd(firstHotspot);
            var secondAddedHotspot = testSubject.GetOrAdd(secondHotspot);

            secondAddedHotspot.Should().BeSameAs(firstHotspot);
            testSubject.GetAll().Count.Should().Be(1);
            testSubject.GetAll()[0].Should().BeSameAs(firstHotspot);
        }

        [TestMethod]
        public void Remove_HotspotRemoved()
        {
            var testSubject = CreateTestSubject();

            var firstHotspot = CreateIssueViz(hotspotKey: "some hotspot1");
            var secondHotspot = CreateIssueViz(hotspotKey: "some hotspot2");

            testSubject.GetOrAdd(firstHotspot);
            testSubject.GetOrAdd(secondHotspot);
            testSubject.Remove(firstHotspot);

            testSubject.GetAll().Count.Should().Be(1);
            testSubject.GetAll()[0].Should().BeSameAs(secondHotspot);
        }

        [TestMethod]
        public void Dispose_InnerIssueVizStoreIsDisposed()
        {
            var issueVizsStore = new Mock<IIssueVizsStore>();
            var testSubject = CreateTestSubject(issueVizsStore.Object);

            issueVizsStore.VerifyNoOtherCalls();
            
            testSubject.Dispose();

            issueVizsStore.Verify(x=> x.Dispose(), Times.Once);
            issueVizsStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetAll_ImplementationDelegatedToIssueVizsStore()
        {
            var expectedResult = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(new ObservableCollection<IAnalysisIssueVisualization>());
            var issueVizsStore = new Mock<IIssueVizsStore>();
            issueVizsStore.Setup(x => x.GetAll()).Returns(expectedResult);

            var testSubject = CreateTestSubject(issueVizsStore.Object);
            var result = testSubject.GetAll();

            result.Should().BeSameAs(expectedResult);

            issueVizsStore.VerifyAll();
            issueVizsStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IssuesChanged_ImplementationDelegatedToIssueVizsStore()
        {
            var issueVizsStore = new Mock<IIssueVizsStore>();
            issueVizsStore.SetupAdd(x => x.IssuesChanged += null);
            issueVizsStore.SetupRemove(x => x.IssuesChanged -= null);

            var testSubject = CreateTestSubject(issueVizsStore.Object);
            var eventHandler = new Mock<EventHandler<IssuesChangedEventArgs>>();

            testSubject.IssuesChanged += eventHandler.Object;
            issueVizsStore.VerifyAdd(x=> x.IssuesChanged += eventHandler.Object);
            issueVizsStore.VerifyNoOtherCalls();

            testSubject.IssuesChanged -= eventHandler.Object;
            issueVizsStore.VerifyRemove(x => x.IssuesChanged -= eventHandler.Object);
            issueVizsStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetLocations_ImplementationDelegatedToIssueVizsStore()
        {
            var expectedResult = new List<IAnalysisIssueVisualization>();
            var issueVizsStore = new Mock<IIssueVizsStore>();
            issueVizsStore.Setup(x => x.GetLocations("test.cpp")).Returns(expectedResult);

            var testSubject = CreateTestSubject(issueVizsStore.Object);
            var result = testSubject.GetLocations("test.cpp");

            result.Should().BeSameAs(expectedResult);

            issueVizsStore.VerifyAll();
            issueVizsStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Refresh_ImplementationDelegatedToIssueVizsStore()
        {
            var issueVizsStore = new Mock<IIssueVizsStore>();

            var testSubject = CreateTestSubject(issueVizsStore.Object);
            testSubject.Refresh(new List<string> {"test1.cpp", "test2.cpp"});

            issueVizsStore.Verify(x=> x.Refresh(new List<string> { "test1.cpp", "test2.cpp" }), Times.Once);
            issueVizsStore.VerifyNoOtherCalls();
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string hotspotKey)
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.HotspotKey).Returns(hotspotKey);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            return issueViz.Object;
        }

        private IHotspotsStore CreateTestSubject(IIssueVizsStore issueVizsStore = null)
        {
            return issueVizsStore == null ? new HotspotsStore() : new HotspotsStore(issueVizsStore);
        }
    }
}
