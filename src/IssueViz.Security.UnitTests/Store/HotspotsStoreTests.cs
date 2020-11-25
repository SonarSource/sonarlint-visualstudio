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

            testSubject.Add(issueViz1);
            testSubject.Add(issueViz2);

            readOnlyWrapper.Count.Should().Be(2);
            readOnlyWrapper.First().Should().Be(issueViz1);
            readOnlyWrapper.Last().Should().Be(issueViz2);
        }

        [TestMethod]
        public void Add_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            var act = new Action(() => testSubject.Add(CreateIssueViz()));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Add_HasSubscribersToIssuesChangedEvent_SubscribersNotified()
        {
            var testSubject = new HotspotsStore() as IHotspotsStore;

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            ((IIssueLocationStore) testSubject).IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            var location1 = new Mock<IAnalysisIssueLocationVisualization>();
            location1.SetupGet(x => x.CurrentFilePath).Returns("b.cpp");
            var location2 = new Mock<IAnalysisIssueLocationVisualization>();
            location2.SetupGet(x => x.CurrentFilePath).Returns("B.cpp");
            var issueViz = CreateIssueViz("a.cpp", location1.Object, location2.Object);

            testSubject.Add(issueViz);

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
            ((IHotspotsStore)testSubject).Add(CreateIssueViz("file1.cpp"));

            var locations = testSubject.GetLocations("file2.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_HasHotspotsForGivenFilePath_ReturnsMatchingLocations()
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.CurrentFilePath).Returns("SomeFile.cpp");

            var issueViz1 = CreateIssueViz("somefile.cpp");
            var issueViz2 = CreateIssueViz("someotherfile.cpp", locationViz.Object);
            var issueViz3 = CreateIssueViz("SOMEFILE.cpp");

            var testSubject = new HotspotsStore() as IIssueLocationStore;
            ((IHotspotsStore)testSubject).Add(issueViz1);
            ((IHotspotsStore)testSubject).Add(issueViz2);
            ((IHotspotsStore)testSubject).Add(issueViz3);

            var locations = testSubject.GetLocations("somefile.cpp");
            locations.Should().BeEquivalentTo(issueViz1, issueViz3, locationViz.Object);
        }

        private static IAnalysisIssueVisualization CreateIssueViz(string filePath = "test.cpp", params IAnalysisIssueLocationVisualization[] locations)
        {
            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.SetupGet(x => x.Locations).Returns(locations);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            issueViz.Setup(x => x.Flows).Returns(new[] { flowViz.Object });

            return issueViz.Object;
        }
    }
}
