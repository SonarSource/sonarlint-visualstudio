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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Integration.UnitTests;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests
{
    [TestClass]
    public class IssueStoreObserverTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            var observerStoreImport = new SingleObjectImporter<IIssueStoreObserver>();
            var issueLocationStoreImporter = new SingleObjectImporter<IIssueLocationStore>();
            batch.AddPart(observerStoreImport);
            batch.AddPart(issueLocationStoreImporter);

            var catalog = new TypeCatalog(typeof(IssueStoreObserver));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);

            observerStoreImport.Import.Should().NotBeNull();
            issueLocationStoreImporter.Import.Should().NotBeNull();

            observerStoreImport.Import.Should().BeSameAs(issueLocationStoreImporter.Import);
        }

        [TestMethod]
        public void Register_NullCollection_ArgumentNullException()
        {
            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;

            Action act = () => testSubject.Register(null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
        }

        [TestMethod]
        public void Register_DisposeCallback_StopTrackingCollection()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            var unregisterCallback = testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            var eventCount = 0;
            ((IIssueLocationStore) testSubject).IssuesChanged += (sender, args) => { eventCount++; };

            unregisterCallback.Dispose();
            originalCollection.Add(CreateIssueViz());

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void Register_DisposeCallback_RemoveUnderlyingCollections()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            var unregisterCallback = testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            originalCollection.Add(CreateIssueViz("somefile.cpp"));

            unregisterCallback.Dispose();

            var locations = ((IIssueLocationStore)testSubject).GetLocations("somefile.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void Register_CollectionAlreadyObserved_CollectionNotAddedAgain()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var readonlyWrapper = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection);

            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(readonlyWrapper);

            using (new AssertIgnoreScope())
            {
                testSubject.Register(readonlyWrapper);
            }

            var eventCount = 0;
            ((IIssueLocationStore)testSubject).IssuesChanged += (sender, args) => { eventCount++; };

            originalCollection.Add(CreateIssueViz());

            eventCount.Should().Be(1);
        }

        [TestMethod]
        public void UnderlyingCollectionsChanged_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();

            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            var act = new Action(() => originalCollection.Add(CreateIssueViz()));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void UnderlyingCollectionsChanged_HasSubscribersToIssuesChangedEvent_SubscribersNotified()
        {
            var originalCollection1 = new ObservableCollection<IAnalysisIssueVisualization>();
            var originalCollection2 = new ObservableCollection<IAnalysisIssueVisualization>();

            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection1));
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection2));

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            ((IIssueLocationStore)testSubject).IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            var location1 = new Mock<IAnalysisIssueLocationVisualization>();
            location1.SetupGet(x => x.CurrentFilePath).Returns("b.cpp");
            var location2 = new Mock<IAnalysisIssueLocationVisualization>();
            location2.SetupGet(x => x.CurrentFilePath).Returns("B.cpp");
            var issueViz1 = CreateIssueViz("a.cpp", locations: new[] { location1.Object, location2.Object });

            originalCollection1.Add(issueViz1);

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("a.cpp", "b.cpp");

            var issueViz2 = CreateIssueViz("c.cpp");

            originalCollection2.Add(issueViz2);

            eventCount.Should().Be(2);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("c.cpp");

            originalCollection1.Remove(issueViz1);

            eventCount.Should().Be(3);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("a.cpp", "b.cpp");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void UnderlyingCollectionsChanged_AddedIssueVizWithoutFilePath_SubscribersNotNotified(string filePath)
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();

            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            var eventCount = 0;
            ((IIssueLocationStore)testSubject).IssuesChanged += (sender, args) => { eventCount++; };

            var issueViz = CreateIssueViz(filePath);
            originalCollection.Add(issueViz);

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void GetLocations_NoUnderlyingCollections_EmptyList()
        {
            var testSubject = new IssueStoreObserver() as IIssueLocationStore;

            var locations = testSubject.GetLocations("test.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_NoIssueVizsForGivenFilePath_EmptyList()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            originalCollection.Add(CreateIssueViz("file1.cpp"));

            var locations = ((IIssueLocationStore)testSubject).GetLocations("file2.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [Description("Regression test for #1958")]
        public void GetLocations_HasIssueVizWithoutFilePath_IssueVizIgnored(string filePath)
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            var issueVizWithoutFilePath = CreateIssueViz(filePath);
            var issueVizWithFilePath = CreateIssueViz("somefile.cpp");

            originalCollection.Add(issueVizWithoutFilePath);
            originalCollection.Add(issueVizWithFilePath);

            var locations = ((IIssueLocationStore)testSubject).GetLocations("somefile.cpp");
            locations.Should().NotBeEmpty();
            locations.Count().Should().Be(1);
            locations.First().Should().Be(issueVizWithFilePath);
        }

        [TestMethod]
        public void GetLocations_HasIssueVizsForGivenFilePath_ReturnsMatchingLocations()
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.CurrentFilePath).Returns("SomeFile.cpp");

            var issueViz1 = CreateIssueViz(filePath: "somefile.cpp");
            var issueViz2 = CreateIssueViz(filePath: "someotherfile.cpp", locations: locationViz.Object);
            var issueViz3 = CreateIssueViz(filePath: "SOMEFILE.cpp");

            var originalCollection1 = new ObservableCollection<IAnalysisIssueVisualization>();
            var originalCollection2 = new ObservableCollection<IAnalysisIssueVisualization>();

            originalCollection1.Add(issueViz1);
            originalCollection2.Add(issueViz2);
            originalCollection2.Add(issueViz3);

            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection1));
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection2));

            var locations = ((IIssueLocationStore)testSubject).GetLocations("somefile.cpp");
            locations.Should().BeEquivalentTo(issueViz1, issueViz3, locationViz.Object);
        }

        [TestMethod]
        public void Dispose_UnsubscribeFromUnderlyingCollectionEvents_SubscribersNoLongerNotified()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            var eventCount = 0;
            ((IIssueLocationStore)testSubject).IssuesChanged += (sender, args) => { eventCount++; };

            testSubject.Dispose();
            originalCollection.Add(CreateIssueViz());

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void Dispose_RemoveUnderlyingCollections()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = new IssueStoreObserver() as IIssueStoreObserver;
            testSubject.Register(new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection));

            originalCollection.Add(CreateIssueViz("somefile.cpp"));

            testSubject.Dispose();

            var locations = ((IIssueLocationStore)testSubject).GetLocations("somefile.cpp");
            locations.Should().BeEmpty();
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
    }
}
