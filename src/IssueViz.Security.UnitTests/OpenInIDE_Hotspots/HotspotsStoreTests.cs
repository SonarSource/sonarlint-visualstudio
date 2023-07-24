/*
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

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE_Hotspots;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE_Hotspots
{
    [TestClass]
    public class OpenInIDEHotspotsStoreTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            var storeImport = new SingleObjectImporter<IOpenInIDEHotspotsStore>();
            var issuesStoreImport = new SingleObjectImporter<IIssuesStore>();
            batch.AddPart(storeImport);
            batch.AddPart(issuesStoreImport);

            var catalog = new TypeCatalog(typeof(OpenInIDEHotspotsStore));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);

            storeImport.Import.Should().NotBeNull();
            issuesStoreImport.Import.Should().NotBeNull();

            storeImport.Import.Should().BeSameAs(issuesStoreImport.Import);
        }

        [TestMethod]
        public void GetOrAdd_NoExistingHotspots_HotspotAdded()
        {
            var testSubject = CreateTestSubject();

            var hotspotToAdd = CreateIssueViz(hotspotKey: "some hotspot");
            var addedHotspot = testSubject.GetOrAdd(hotspotToAdd);

            addedHotspot.Should().BeSameAs(hotspotToAdd);
            testSubject.GetAll().Count().Should().Be(1);
            testSubject.GetAll().First().Should().BeSameAs(hotspotToAdd);
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

            var hotspots = testSubject.GetAll().ToList();
            hotspots.Should().HaveCount(2);
            hotspots[0].Should().BeSameAs(someOtherHotspot);
            hotspots[1].Should().BeSameAs(hotspotToAdd);
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
            var hotspots = testSubject.GetAll().ToList();
            hotspots.Should().HaveCount(1);
            hotspots[0].Should().BeSameAs(firstHotspot);
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

            var hotspots = testSubject.GetAll().ToList();
            hotspots.Should().HaveCount(1);
            hotspots[0].Should().BeSameAs(secondHotspot);
        }

        [TestMethod]
        public void GetOrAdd_HasSubscribersToIssuesChanged_SubscribersNotified()
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var addedIssueViz = testSubject.GetOrAdd(CreateIssueViz(hotspotKey: "some hotspot"));

            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEmpty();
            suppliedArgs.AddedIssues.Should().BeEquivalentTo(addedIssueViz);
        }

        [TestMethod]
        public void Remove_HasSubscribersToIssuesChanged_SubscribersNotified()
        {
            var testSubject = CreateTestSubject();

            var callCount = 0;
            IssuesChangedEventArgs suppliedArgs = null;
            testSubject.IssuesChanged += (sender, args) => { callCount++; suppliedArgs = args; };

            var addedIssueViz1 = testSubject.GetOrAdd(CreateIssueViz(hotspotKey: "some hotspot1"));
            testSubject.GetOrAdd(CreateIssueViz(hotspotKey: "some hotspot2"));

            callCount = 0;
            testSubject.Remove(addedIssueViz1);

            callCount.Should().Be(1);
            suppliedArgs.RemovedIssues.Should().BeEquivalentTo(addedIssueViz1);
            suppliedArgs.AddedIssues.Should().BeEmpty();
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string hotspotKey)
        {
            var hotspot = new Mock<IHotspot>();
            hotspot.Setup(x => x.HotspotKey).Returns(hotspotKey);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(hotspot.Object);

            return issueViz.Object;
        }

        private IOpenInIDEHotspotsStore CreateTestSubject()
        {
            return new OpenInIDEHotspotsStore();
        }
    }
}
