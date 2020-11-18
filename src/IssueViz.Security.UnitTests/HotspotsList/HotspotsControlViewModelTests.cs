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

using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList2.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Store;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsControlViewModelTests
    {
        [TestMethod]
        public void Ctor_RegisterToHotspotsCollectionChanges()
        {
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(storeHotspots);

            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            storeHotspots.Add(issueViz1);

            testSubject.Hotspots.Count.Should().Be(1);
            testSubject.Hotspots.First().Hotspot.Should().Be(issueViz1);

            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            storeHotspots.Add(issueViz2);

            // todo: deletion check
            testSubject.Hotspots.Count.Should().Be(2);
            testSubject.Hotspots.First().Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots.Last().Hotspot.Should().Be(issueViz2);
        }

        [TestMethod]
        public void Ctor_InitializeListWithHotspotsStoreCollection()
        {
            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> {issueViz1, issueViz2};

            var testSubject = CreateTestSubject(storeHotspots);

            testSubject.Hotspots.Count.Should().Be(2);
            testSubject.Hotspots.First().Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots.Last().Hotspot.Should().Be(issueViz2);
        }

        [TestMethod]
        public void Dispose_UnregisterFromHotspotsCollectionChanges()
        {
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(storeHotspots);

            testSubject.Dispose();

            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            storeHotspots.Add(issueViz);

            testSubject.Hotspots.Count.Should().Be(0);
        }

        private static HotspotsControlViewModel CreateTestSubject(ObservableCollection<IAnalysisIssueVisualization> originalCollection)
        {
            var readOnlyWrapper = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection);

            var store = new Mock<IHotspotsStore>();
            store.Setup(x => x.GetAll()).Returns(readOnlyWrapper);

            return new HotspotsControlViewModel(store.Object);
        }
    }
}
