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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
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
        public void GetAll_ReturnsReadOnlyObservableWrapper()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;
            var readOnlyWrapper = testSubject.GetAll();

            readOnlyWrapper.Should().BeAssignableTo<IReadOnlyCollection<IAnalysisIssueVisualization>>();

            var issueViz1 = CreateIssueViz("some hotspot1");
            var issueViz2 = CreateIssueViz("some hotspot2");

            testSubject.GetOrAdd(issueViz1);
            testSubject.GetOrAdd(issueViz2);

            readOnlyWrapper.Count.Should().Be(2);
            readOnlyWrapper.First().Should().Be(issueViz1);
            readOnlyWrapper.Last().Should().Be(issueViz2);

            testSubject.Remove(issueViz2);

            readOnlyWrapper.Count.Should().Be(1);
            readOnlyWrapper.First().Should().Be(issueViz1);
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
            var issueLocationStore = new Mock<IIssueLocationStore>();
            var disposable = issueLocationStore.As<IDisposable>();

            var testSubject = CreateTestSubject(issueLocationStore.Object);

            issueLocationStore.VerifyNoOtherCalls();
            
            testSubject.Dispose();

            disposable.Verify(x=> x.Dispose(), Times.Once);
            issueLocationStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void IssuesChanged_ImplementationDelegatedToIssueLocationStore()
        {
            var issueLocationStore = new Mock<IIssueLocationStore>();
            issueLocationStore.SetupAdd(x => x.IssuesChanged += null);
            issueLocationStore.SetupRemove(x => x.IssuesChanged -= null);

            var testSubject = CreateTestSubject(issueLocationStore.Object) as IIssueLocationStore;
            var eventHandler = new Mock<EventHandler<IssuesChangedEventArgs>>();

            testSubject.IssuesChanged += eventHandler.Object;
            issueLocationStore.VerifyAdd(x=> x.IssuesChanged += eventHandler.Object);
            issueLocationStore.VerifyNoOtherCalls();

            testSubject.IssuesChanged -= eventHandler.Object;
            issueLocationStore.VerifyRemove(x => x.IssuesChanged -= eventHandler.Object);
            issueLocationStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetLocations_ImplementationDelegatedToIssueLocationStore()
        {
            var expectedResult = new List<IAnalysisIssueVisualization>();
            var issueLocationStore = new Mock<IIssueLocationStore>();
            issueLocationStore.Setup(x => x.GetLocations("test.cpp")).Returns(expectedResult);

            var testSubject = CreateTestSubject(issueLocationStore.Object) as IIssueLocationStore;
            var result = testSubject.GetLocations("test.cpp");

            result.Should().BeSameAs(expectedResult);

            issueLocationStore.VerifyAll();
            issueLocationStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Refresh_ImplementationDelegatedToIssueLocationStore()
        {
            var issueLocationStore = new Mock<IIssueLocationStore>();

            var testSubject = CreateTestSubject(issueLocationStore.Object) as IIssueLocationStore;
            testSubject.Refresh(new List<string> {"test1.cpp", "test2.cpp"});

            issueLocationStore.Verify(x=> x.Refresh(new List<string> { "test1.cpp", "test2.cpp" }), Times.Once);
            issueLocationStore.VerifyNoOtherCalls();
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string hotspotKey)
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.HotspotKey).Returns(hotspotKey);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            return issueViz.Object;
        }

        private IHotspotsStore CreateTestSubject(IIssueLocationStore issueLocationStore = null)
        {
            return issueLocationStore == null ? new HotspotsStore() : new HotspotsStore(issueLocationStore);
        }
    }
}
