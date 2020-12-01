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
using SonarLint.VisualStudio.IssueVisualization.Security.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Store;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Store
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

            var issueViz1 = CreateIssueViz();
            var issueViz2 = CreateIssueViz();

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
        public void GetOrGetOrAdd_NoExistingHotspots_HotspotAdded()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            var hotspotToAdd = CreateIssueViz(hotspotKey:"some hotspot");
            var addedHotspot = testSubject.GetOrAdd(hotspotToAdd);

            addedHotspot.Should().BeSameAs(hotspotToAdd);
            testSubject.GetAll().Count.Should().Be(1);
            testSubject.GetAll()[0].Should().BeSameAs(hotspotToAdd);
        }

        [TestMethod]
        public void GetOrGetOrAdd_NoMatchingHotspot_HotspotAdded()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

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
        public void GetOrGetOrAdd_HasMatchingHotspot_HotspotNotAdded()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            var firstHotspot = CreateIssueViz(hotspotKey: "some hotspot");
            var secondHotspot = CreateIssueViz(hotspotKey: "some hotspot");
            
            testSubject.GetOrAdd(firstHotspot);
            var secondAddedHotspot = testSubject.GetOrAdd(secondHotspot);

            secondAddedHotspot.Should().BeSameAs(firstHotspot);
            testSubject.GetAll().Count.Should().Be(1);
            testSubject.GetAll()[0].Should().BeSameAs(firstHotspot);
        }

        [TestMethod]
        public void GetOrGetOrAdd_HasMatchingHotspot_SubscribersNotNotified()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            ((IIssueLocationStore)testSubject).IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            var hotspotToAdd = CreateIssueViz(hotspotKey: "some hotspot");
            testSubject.GetOrAdd(hotspotToAdd);

            eventCount.Should().Be(1);

            testSubject.GetOrAdd(hotspotToAdd);

            eventCount.Should().Be(1);
        }

        [TestMethod]
        public void GetOrAdd_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            var act = new Action(() => testSubject.GetOrAdd(CreateIssueViz()));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void GetOrAdd_HasSubscribersToIssuesChangedEvent_SubscribersNotified()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            ((IIssueLocationStore)testSubject).IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            var location1 = new Mock<IAnalysisIssueLocationVisualization>();
            location1.SetupGet(x => x.CurrentFilePath).Returns("b.cpp");
            var location2 = new Mock<IAnalysisIssueLocationVisualization>();
            location2.SetupGet(x => x.CurrentFilePath).Returns("B.cpp");
            var issueViz = CreateIssueViz(filePath: "a.cpp", locations: new [] {location1.Object, location2.Object});

            testSubject.GetOrAdd(issueViz);

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("a.cpp", "b.cpp");
        }

        [TestMethod]
        public void Remove_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            var act = new Action(() => testSubject.Remove(CreateIssueViz()));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Remove_HasSubscribersToIssuesChangedEvent_SubscribersNotified()
        {
            var location1 = new Mock<IAnalysisIssueLocationVisualization>();
            location1.SetupGet(x => x.CurrentFilePath).Returns("b.cpp");
            var location2 = new Mock<IAnalysisIssueLocationVisualization>();
            location2.SetupGet(x => x.CurrentFilePath).Returns("B.cpp");
            var issueViz = CreateIssueViz(filePath: "a.cpp", locations: new [] {location1.Object, location2.Object});

            var testSubject = new HotspotsStore() as IHotspotsStore;
            testSubject.GetOrAdd(issueViz);

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            ((IIssueLocationStore)testSubject).IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            testSubject.Remove(issueViz);

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("a.cpp", "b.cpp");
        }

        [TestMethod]
        public void GetLocations_NoHotspots_EmptyList()
        {
            var testSubject = new HotspotsStore() as IIssueLocationStore;

            var locations = testSubject.GetLocations("test.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_NoHotspotsForGivenFilePath_EmptyList()
        {
            var testSubject = new HotspotsStore() as IIssueLocationStore;
            ((IHotspotsStore)testSubject).GetOrAdd(CreateIssueViz("file1.cpp"));

            var locations = testSubject.GetLocations("file2.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_HasHotspotsForGivenFilePath_ReturnsMatchingLocations()
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.CurrentFilePath).Returns("SomeFile.cpp");

            var issueViz1 = CreateIssueViz(filePath:"somefile.cpp");
            var issueViz2 = CreateIssueViz(filePath: "someotherfile.cpp", locations: locationViz.Object);
            var issueViz3 = CreateIssueViz(filePath: "SOMEFILE.cpp");

            var testSubject = new HotspotsStore() as IIssueLocationStore;
            ((IHotspotsStore)testSubject).GetOrAdd(issueViz1);
            ((IHotspotsStore)testSubject).GetOrAdd(issueViz2);
            ((IHotspotsStore)testSubject).GetOrAdd(issueViz3);

            var locations = testSubject.GetLocations("somefile.cpp");
            locations.Should().BeEquivalentTo(issueViz1, issueViz3, locationViz.Object);
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string filePath = "test.cpp", string hotspotKey = null, params IAnalysisIssueLocationVisualization[] locations)
        {
            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.Setup(x => x.Locations).Returns(locations);

            hotspotKey ??= Guid.NewGuid().ToString();
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.HotspotKey).Returns(hotspotKey);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);
            issueViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            issueViz.Setup(x => x.Flows).Returns(new[] { flowViz.Object });

            return issueViz.Object;
        }
    }
}
