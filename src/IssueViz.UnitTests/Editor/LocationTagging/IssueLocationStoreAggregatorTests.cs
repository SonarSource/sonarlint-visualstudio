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

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Editor.LocationTagging
{
    [TestClass]
    public class IssueLocationStoreAggregatorTests
    {
        [TestMethod]
        public void MefCtor_CheckExports()
        {
            var batch = new CompositionBatch();

            batch.AddExport(MefTestHelpers.CreateExport<IIssueLocationStore>(Mock.Of<IIssueLocationStore>()));
            batch.AddExport(MefTestHelpers.CreateExport<IIssueLocationStore>(Mock.Of<IIssueLocationStore>()));
            batch.AddExport(MefTestHelpers.CreateExport<IIssueLocationStore>(Mock.Of<IIssueLocationStore>()));

            var issueLocationStoreAggregatorImporter = new SingleObjectImporter<IIssueLocationStoreAggregator>();
            batch.AddPart(issueLocationStoreAggregatorImporter);

            using var catalog = new TypeCatalog(typeof(IssueLocationStoreAggregator));
            using var container = new CompositionContainer(catalog);
            container.Compose(batch);
                
            issueLocationStoreAggregatorImporter.Import.Should().NotBeNull();
        }

        [TestMethod]
        public void IssuesChanged_Subscribe_NoStores_NoException()
        {
            var eventHandler = new Mock<EventHandler<IssuesChangedEventArgs>>();
            var testSubject = new IssueLocationStoreAggregator(Enumerable.Empty<IIssueLocationStore>());

            Action act = () => testSubject.IssuesChanged += eventHandler.Object;
            act.Should().NotThrow();
        }

        [TestMethod]
        public void IssuesChanged_Unsubscribe_NoStores_NoException()
        {
            var eventHandler = new Mock<EventHandler<IssuesChangedEventArgs>>();
            var testSubject = new IssueLocationStoreAggregator(Enumerable.Empty<IIssueLocationStore>());

            Action act = () => testSubject.IssuesChanged -= eventHandler.Object;
            act.Should().NotThrow();
        }

        [TestMethod]
        public void IssuesChanged_Subscribe_SubscribesToAllStores()
        {
            var stores = new List<Mock<IIssueLocationStore>>
            {
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>()
            };

            foreach (var store in stores)
            {
                store.SetupAdd(x => x.IssuesChanged += (sender, args) => { });
            }

            var eventHandler = new Mock<EventHandler<IssuesChangedEventArgs>>();
            var testSubject = new IssueLocationStoreAggregator(stores.Select(x=> x.Object));

            testSubject.IssuesChanged += eventHandler.Object;

            foreach (var store in stores)
            {
                store.VerifyAdd(x => x.IssuesChanged += eventHandler.Object, Times.Once);
            }
        }

        [TestMethod]
        public void IssuesChanged_Unsubscribe_UnsubscribesFromAllStores()
        {
            var stores = new List<Mock<IIssueLocationStore>>
            {
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>()
            };

            foreach (var store in stores)
            {
                store.SetupRemove(x => x.IssuesChanged -= (sender, args) => { });
            }

            var eventHandler = new Mock<EventHandler<IssuesChangedEventArgs>>();
            var testSubject = new IssueLocationStoreAggregator(stores.Select(x => x.Object));

            testSubject.IssuesChanged -= eventHandler.Object;

            foreach (var store in stores)
            {
                store.VerifyRemove(x => x.IssuesChanged -= eventHandler.Object, Times.Once);
            }
        }

        [TestMethod]
        public void GetLocations_NullPath_ArgumentNullException()
        {
            var testSubject = new IssueLocationStoreAggregator(Enumerable.Empty<IIssueLocationStore>());

            Action act = () => testSubject.GetLocations(null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("filePath");
        }

        [TestMethod]
        public void GetLocations_NoStores_EmptyList()
        {
            var testSubject = new IssueLocationStoreAggregator(Enumerable.Empty<IIssueLocationStore>());
            var result = testSubject.GetLocations("test.cpp");

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_NoMatchingLocations_EmptyList()
        {
            const string filePath = "test.cpp";

            var stores = new List<Mock<IIssueLocationStore>>
            {
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>()
            };

            foreach (var store in stores)
            {
                store
                    .Setup(x => x.GetLocations(filePath))
                    .Returns(Enumerable.Empty<IAnalysisIssueLocationVisualization>());
            }

            var testSubject = new IssueLocationStoreAggregator(Enumerable.Empty<IIssueLocationStore>());
            var result = testSubject.GetLocations(filePath);

            result.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_HasMatchingLocations_LocationsReturned()
        {
            const string filePath = "test.cpp";

            var store1 = new Mock<IIssueLocationStore>();
            store1.Setup(x => x.GetLocations(filePath)).Returns(Enumerable.Empty<IAnalysisIssueLocationVisualization>());

            var location1 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var store2 = new Mock<IIssueLocationStore>();
            store2.Setup(x => x.GetLocations(filePath)).Returns(new []{ location1 });

            var location2 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var location3 = Mock.Of<IAnalysisIssueLocationVisualization>();
            var store3 = new Mock<IIssueLocationStore>();
            store3.Setup(x => x.GetLocations(filePath)).Returns(new[] { location2, location3 });

            var testSubject = new IssueLocationStoreAggregator(new[] {store1.Object, store2.Object, store3.Object});
            var result = testSubject.GetLocations(filePath);

            result.Should().BeEquivalentTo(location1, location2, location3);
        }

        [TestMethod]
        public void Refresh_NullFilePaths_ArgumentNullException()
        {
            var testSubject = new IssueLocationStoreAggregator(Enumerable.Empty<IIssueLocationStore>());

            Action act = () => testSubject.Refresh(null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("affectedFilePaths");
        }

        [TestMethod]
        public void Refresh_NoStores_NoException()
        {
            var testSubject = new IssueLocationStoreAggregator(Enumerable.Empty<IIssueLocationStore>());

            Action act = () => testSubject.Refresh(new []{"test.cpp"});
            act.Should().NotThrow();
        }

        [TestMethod]
        public void Refresh_HasStores_AllStoresRefreshed()
        {
            var stores = new List<Mock<IIssueLocationStore>>
            {
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>(),
                new Mock<IIssueLocationStore>()
            };

            var affectedFilePaths = new[] {"a.cpp", "b.cpp"};

            var testSubject = new IssueLocationStoreAggregator(stores.Select(x=> x.Object));
            testSubject.Refresh(affectedFilePaths);

            foreach (var store in stores)
            {
                store.Verify(x=> x.Refresh(affectedFilePaths), Times.Once);
            }
        }
    }
}
