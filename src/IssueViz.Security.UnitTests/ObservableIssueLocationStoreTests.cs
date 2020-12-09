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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Editor.LocationTagging;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests
{
    [TestClass]
    public class ObservableIssueLocationStoreTests
    {
        [TestMethod]
        public void Ctor_NullCollection_ArgumentNullException()
        {
            Action act = () => new ObservableIssueLocationStore(null);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void UnderlyingCollectionChanged_IssueVizHasNoFilePath_SubscribersNotNotified(string filePath)
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(originalCollection);

            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { eventCount++; };

            var issueViz = CreateIssueViz(filePath: filePath);
            originalCollection.Add(issueViz);

            eventCount.Should().Be(0);
        }

        [TestMethod]
        public void UnderlyingCollectionChanged_NoSubscribersToIssuesChangedEvent_NoException()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            CreateTestSubject(originalCollection);

            var act = new Action(() => originalCollection.Add(CreateIssueViz()));
            act.Should().NotThrow();
        }

        [TestMethod]
        public void UnderlyingCollectionChanged_HasSubscribersToIssuesChangedEvent_SubscribersNotified()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(originalCollection);

            IssuesChangedEventArgs suppliedArgs = null;
            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { suppliedArgs = args; eventCount++; };

            var location1 = new Mock<IAnalysisIssueLocationVisualization>();
            location1.SetupGet(x => x.CurrentFilePath).Returns("b.cpp");
            var location2 = new Mock<IAnalysisIssueLocationVisualization>();
            location2.SetupGet(x => x.CurrentFilePath).Returns("B.cpp");
            var issueViz = CreateIssueViz(filePath: "a.cpp", locations: new[] { location1.Object, location2.Object });

            originalCollection.Add(issueViz);

            eventCount.Should().Be(1);
            suppliedArgs.Should().NotBeNull();
            suppliedArgs.AnalyzedFiles.Should().BeEquivalentTo("a.cpp", "b.cpp");
        }

        [TestMethod]
        public void GetLocations_NoIssueVizs_EmptyList()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(originalCollection);

            var locations = testSubject.GetLocations("test.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_NoIssueVizsForGivenFilePath_EmptyList()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(originalCollection);
            
            originalCollection.Add(CreateIssueViz("file1.cpp"));

            var locations = testSubject.GetLocations("file2.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [Description("Regression test for #1958")]
        public void GetLocations_IssueVizHasNoFilePath_IssueVizIgnored(string filePath)
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(originalCollection);

            var issueViz = CreateIssueViz(filePath: filePath);
            originalCollection.Add(issueViz);

            var locations = testSubject.GetLocations("somefile.cpp");
            locations.Should().BeEmpty();
        }

        [TestMethod]
        public void GetLocations_HasIssueVizsForGivenFilePath_ReturnsMatchingLocations()
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.CurrentFilePath).Returns("SomeFile.cpp");

            var issueViz1 = CreateIssueViz(filePath: "somefile.cpp");
            var issueViz2 = CreateIssueViz(filePath: "someotherfile.cpp", locations: locationViz.Object);
            var issueViz3 = CreateIssueViz(filePath: "SOMEFILE.cpp");

            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(originalCollection);
            originalCollection.Add(issueViz1);
            originalCollection.Add(issueViz2);
            originalCollection.Add(issueViz3);

            var locations = testSubject.GetLocations("somefile.cpp");
            locations.Should().BeEquivalentTo(issueViz1, issueViz3, locationViz.Object);
        }

        [TestMethod]
        public void Dispose_UnsubscribeFromUnderlyingCollectionEvents_SubscribersNoLongerNotified()
        {
            var originalCollection = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(originalCollection);

            var eventCount = 0;
            testSubject.IssuesChanged += (sender, args) => { eventCount++; };

            (testSubject as IDisposable).Dispose();
            originalCollection.Add(CreateIssueViz());

            eventCount.Should().Be(0);
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

        private IIssueLocationStore CreateTestSubject(ObservableCollection<IAnalysisIssueVisualization> issueVizs) => 
            new ObservableIssueLocationStore(issueVizs);
    }
}
