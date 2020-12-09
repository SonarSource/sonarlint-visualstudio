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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots
{
    [TestClass]
    public class HotspotsStoreTests
    {
        [TestMethod]
        public void MefCtor_CheckIsExported()
        {
            MefTestHelpers.CheckTypeCanBeImported<HotspotsStore, IHotspotsStore>(null, new[]
            {
                MefTestHelpers.CreateExport<IObservingIssueLocationStore>(Mock.Of<IObservingIssueLocationStore>())
            });
        }

        [TestMethod]
        public void Ctor_RegisterToObservingStore()
        {
            var observingStore = new Mock<IObservingIssueLocationStore>();
            CreateTestSubject(observingStore.Object);

            observingStore.Verify(x=> x.Register(It.IsAny<ObservableCollection<IAnalysisIssueVisualization>>()), Times.Once);
            observingStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_InnerIssueVizStoreIsDisposed()
        {
            var observingStore = new Mock<IObservingIssueLocationStore>();
            var testSubject = CreateTestSubject(observingStore.Object);

            observingStore.Reset();
            testSubject.Dispose();

            observingStore.Verify(x => x.Unregister(It.IsAny<ObservableCollection<IAnalysisIssueVisualization>>()), Times.Once);
            observingStore.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void GetAll_ReturnsReadOnlyObservableWrapper()
        {
            var testSubject = CreateTestSubject();
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

        private static IAnalysisIssueVisualization CreateIssueViz(string hotspotKey)
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.HotspotKey).Returns(hotspotKey);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            return issueViz.Object;
        }

        private IHotspotsStore CreateTestSubject(IObservingIssueLocationStore observingIssueLocationStore = null)
        {
            observingIssueLocationStore ??= Mock.Of<IObservingIssueLocationStore>();
            return new HotspotsStore(observingIssueLocationStore);
        }
    }
}
