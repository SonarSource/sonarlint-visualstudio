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

using System;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using IssuesChangedEventArgs = SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging.IssuesChangedEventArgs;
using System.Collections.Generic;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests
{
    [TestClass]
    public class AggregatingIssueLocationStoreAdapterTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            var stores = new[]
            {
                new Mock<IIssuesStore>(), 
                new Mock<IIssuesStore>(), 
                new Mock<IIssuesStore>()
            };

            foreach (var issuesStore in stores)
            {
                issuesStore.SetupAdd(x => x.IssuesChanged += null);
                batch.AddExport(MefTestHelpers.CreateExport<IIssuesStore>(issuesStore.Object));
            }

            var locationStoreImport = new SingleObjectImporter<IIssueLocationStore>();
            batch.AddPart(locationStoreImport);

            using var catalog = new TypeCatalog(typeof(AggregatingIssueLocationStoreAdapter));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);

            locationStoreImport.Import.Should().NotBeNull();

            // Verify that the stores are used
            foreach (var issuesStore in stores)
            {
                issuesStore.VerifyAdd(x=> x.IssuesChanged += It.IsAny<EventHandler<IssuesStore.IssuesChangedEventArgs>>(), Times.Once);
            }
        }

        [TestMethod]
        public void IssueStoresChanged_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var issueStore = new Mock<IIssuesStore>();
            CreateTestSubject(issueStore.Object);

            var act = new Action(() => RaiseIssuesChangedEvent(issueStore, newIssues: new[] {CreateIssueViz()}));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void IssueStoresChanged_HasSubscribersToIssuesChangedEvent_SubscribersNotified()
        {
            var issueStore1 = new Mock<IIssuesStore>();
            var issueStore2 = new Mock<IIssuesStore>();

            var testSubject = CreateTestSubject(issueStore1.Object, issueStore2.Object);

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            var location1 = CreateLocation("b.cpp");
            var location2 = CreateLocation("B.cpp");
            var issueViz1 = CreateIssueViz("a.cpp", location1, location2);

            RaiseIssuesChangedEvent(issueStore1, newIssues: new[] {issueViz1});

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("a.cpp", "b.cpp");

            var issueViz2 = CreateIssueViz("c.cpp");

            eventCount = 0;
            RaiseIssuesChangedEvent(issueStore2, newIssues: new[] { issueViz2 });

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("c.cpp");

            eventCount = 0;
            RaiseIssuesChangedEvent(issueStore2, oldIssues: new[] {issueViz1}, newIssues: new[] {issueViz2});

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("a.cpp", "b.cpp", "c.cpp");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void IssueStoresChanged_AddedIssueVizWithoutFilePath_SubscribersNotNotified(string filePath)
        {
            var issueStore = new Mock<IIssuesStore>();
            var testSubject = CreateTestSubject(issueStore.Object);

            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { eventCount++; };

            var issueViz = CreateIssueViz(filePath);
            RaiseIssuesChangedEvent(issueStore, newIssues: new[] {issueViz});

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void GetLocations_NoIssueStores_EmptyList()
        {
            var testSubject = CreateTestSubject();

            var locations = testSubject.GetLocations("test.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_NoIssueVizsForGivenFilePath_EmptyList()
        {
            var issueStore = CreateIssuesStore(CreateIssueViz("file1.cpp"));
            var testSubject = CreateTestSubject(issueStore);

            var locations = testSubject.GetLocations("file2.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [Description("Regression test for #1958")]
        public void GetLocations_HasIssueVizWithoutFilePath_IssueVizIgnored(string filePath)
        {
            var issueVizWithoutFilePath = CreateIssueViz(filePath);
            var issueVizWithFilePath = CreateIssueViz("somefile.cpp");
            var issueStore = CreateIssuesStore(issueVizWithoutFilePath, issueVizWithFilePath);

            var testSubject = CreateTestSubject(issueStore);

            var locations = testSubject.GetLocations("somefile.cpp");
            locations.Should().NotBeEmpty();
            locations.Count().Should().Be(1);
            locations.First().Should().Be(issueVizWithFilePath);
        }

        [TestMethod]
        public void GetLocations_HasIssueVizsForGivenFilePath_ReturnsMatchingLocations()
        {
            var locationViz = CreateLocation("SomeFile.cpp");
            var issueViz1 = CreateIssueViz(filePath: "somefile.cpp");
            var issueViz2 = CreateIssueViz(filePath: "someotherfile.cpp", locations: locationViz);
            var issueViz3 = CreateIssueViz(filePath: "SOMEFILE.cpp");

            var issueStore1 = CreateIssuesStore(issueViz1);
            var issueStore2 = CreateIssuesStore(issueViz2, issueViz3);

            var testSubject = CreateTestSubject(issueStore1, issueStore2);

            var locations = testSubject.GetLocations("somefile.cpp");
            locations.Should().BeEquivalentTo(issueViz1, issueViz3, locationViz);
        }

        [TestMethod]
        public void Contains_IssueVizIsNull_ArgumentNullException()
        {
            var testSubject = CreateTestSubject();

            Action act = () => testSubject.Contains(null);

            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualization");
        }

        [TestMethod]
        public void Contains_NoIssueStores_False()
        {
            var testSubject = CreateTestSubject();

            var issueViz = Mock.Of<IAnalysisIssueVisualization>();

            var result = testSubject.Contains(issueViz);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void Contains_GivenIssueVizDoesNotExist_False()
        {
            var issueViz = CreateIssueViz(filePath: "someFile.cpp");

            var issueStore1 = CreateIssuesStore();
            var issueStore2 = CreateIssuesStore(issueViz);

            var testSubject = CreateTestSubject(issueStore1, issueStore2);
            var otherIssueViz = Mock.Of<IAnalysisIssueVisualization>();

            var result = testSubject.Contains(otherIssueViz);
            result.Should().BeFalse();
        }

        [TestMethod]
        public void Contains_GivenIssueVizExists_True()
        {
            var issueViz = CreateIssueViz(filePath: "someFile.cpp");

            var issueStore1 = CreateIssuesStore();
            var issueStore2 = CreateIssuesStore(issueViz);

            var testSubject = CreateTestSubject(issueStore1, issueStore2);

            var result = testSubject.Contains(issueViz);
            result.Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_UnsubscribeFromIssueStores_SubscribersNoLongerNotified()
        {
            var issueStore = new Mock<IIssuesStore>();

            var testSubject = CreateTestSubject(issueStore.Object);

            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { eventCount++; };

            testSubject.Dispose();

            RaiseIssuesChangedEvent(issueStore, newIssues: new[] {CreateIssueViz()});

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void Get_NoMatchingLocations_EmptyList()
        {
            var stores = new List<Mock<IIssuesStore>>
            {
                new(),
                new(),
                new()
            };

            foreach (var store in stores)
            {
                store
                    .Setup(x => x.GetAll())
                    .Returns(Array.Empty<IAnalysisIssueVisualization>());
            }

            var testSubject = CreateTestSubject(stores.Select(x=> x.Object).ToArray());
            var result = testSubject.Get();

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_HasMatchingLocations_AggregatedLocationsList()
        {
            var store1 = new Mock<IIssuesStore>();
            store1.Setup(x => x.GetAll()).Returns(Array.Empty<IAnalysisIssueVisualization>());

            var location1 = Mock.Of<IAnalysisIssueVisualization>();
            var store2 = new Mock<IIssuesStore>();
            store2.Setup(x => x.GetAll()).Returns(new[] { location1 });

            var location2 = Mock.Of<IAnalysisIssueVisualization>();
            var location3 = Mock.Of<IAnalysisIssueVisualization>();
            var store3 = new Mock<IIssuesStore>();
            store3.Setup(x => x.GetAll()).Returns(new[] { location2, location3 });

            var testSubject = CreateTestSubject(store1.Object, store2.Object, store3.Object);
            var result = testSubject.Get();

            result.Should().BeEquivalentTo(location1, location2, location3);
        }

        private static IAnalysisIssueLocationVisualization CreateLocation(string filePath)
        {
            var location = new Mock<IAnalysisIssueLocationVisualization>();
            location.SetupGet(x => x.CurrentFilePath).Returns(filePath);

            return location.Object;
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string filePath = "test.cpp", params IAnalysisIssueLocationVisualization[] locations)
        {
            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.Setup(x => x.Locations).Returns(locations);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.Issue).Returns(Mock.Of<IAnalysisIssueBase>());
            issueViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            issueViz.Setup(x => x.Flows).Returns(new[] { flowViz.Object });

            return issueViz.Object;
        }

        private IIssuesStore CreateIssuesStore(params IAnalysisIssueVisualization[] issueVizs)
        {
            var issueStore = new Mock<IIssuesStore>();
            issueStore.Setup(x => x.GetAll()).Returns(issueVizs);

            return issueStore.Object;
        }

        private static AggregatingIssueLocationStoreAdapter CreateTestSubject(params IIssuesStore[] issueStores)
        {
            issueStores ??= Array.Empty<IIssuesStore>();

            return new AggregatingIssueLocationStoreAdapter(issueStores);
        }

        private static void RaiseIssuesChangedEvent(Mock<IIssuesStore> issuesStore, 
            IAnalysisIssueVisualization[] oldIssues = null, 
            IAnalysisIssueVisualization[] newIssues = null)
        {
            oldIssues ??= Array.Empty<IAnalysisIssueVisualization>();
            newIssues ??= Array.Empty<IAnalysisIssueVisualization>();

            issuesStore.Raise(x => x.IssuesChanged += null, null, new IssuesStore.IssuesChangedEventArgs(oldIssues, newIssues));
        }
    }
}
