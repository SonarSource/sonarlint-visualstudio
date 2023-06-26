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
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots;

[TestClass]
public class LocalHotspotStoreTests
{
    [TestMethod]
    public void MefCtor_CheckExports_ILocalHotspotsStore()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, ILocalHotspotsStore>(
            MefTestHelpers.CreateExport<IServerHotspotStore>(),
            MefTestHelpers.CreateExport<IHotspotMatcher>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckExports_ILocalHotspotsStoreUpdater()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, ILocalHotspotsStoreUpdater>(
            MefTestHelpers.CreateExport<IServerHotspotStore>(),
            MefTestHelpers.CreateExport<IHotspotMatcher>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckExports_IIssuesStore()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, IIssuesStore>(
            MefTestHelpers.CreateExport<IServerHotspotStore>(),
            MefTestHelpers.CreateExport<IHotspotMatcher>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void UpdateForFile_NoHotspots_NothingHappens()
    {
        var testSubject = CreateTestSubject(out var eventListener);

        testSubject.UpdateForFile("file1", Array.Empty<IAnalysisIssueVisualization>());
        testSubject.UpdateForFile("file1", Array.Empty<IAnalysisIssueVisualization>());

        VerifyContent(testSubject, Array.Empty<LocalHotspot>());
        eventListener.Events.Should().BeEmpty();
    }

    [TestMethod]
    public void UpdateForFile_NoServerHotspots_AddsLocalHotspots()
    {
        var threadHandlingMock = new Mock<IThreadHandling>();
        var issueVis1 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis2 = Mock.Of<IAnalysisIssueVisualization>();
        var testSubject = CreateTestSubject(out var eventListener, threadHandling: threadHandlingMock.Object);
        var hotspots = new[] { issueVis1, issueVis2 };

        testSubject.UpdateForFile("file1", hotspots);

        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread(), Times.Once);
        VerifyContent(testSubject, hotspots.Select(x => new LocalHotspot(x)).ToArray());
        eventListener.Events.Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(Array.Empty<IAnalysisIssueVisualization>(), hotspots));
    }

    [TestMethod]
    public void UpdateForFile_NoServerHotspots_UpdatesForSameFile()
    {
        var issueVis1 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis2 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis3 = Mock.Of<IAnalysisIssueVisualization>();
        var testSubject = CreateTestSubject(out var eventListener);
        var oldHotspots = new[] { issueVis1 };
        testSubject.UpdateForFile("file1", oldHotspots);
        var newHotspots = new[] { issueVis2, issueVis3 };

        testSubject.UpdateForFile("file1", newHotspots);

        VerifyContent(testSubject, newHotspots.Select(x => new LocalHotspot(x)).ToArray());
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(oldHotspots, newHotspots));
    }

    [TestMethod]
    public void UpdateForFile_NoServerHotspots_AddsForDifferentFile()
    {
        var issueVis1 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis2 = Mock.Of<IAnalysisIssueVisualization>();
        var testSubject = CreateTestSubject(out var eventListener);
        testSubject.UpdateForFile("file1", new[] { issueVis1 });
        var newHotspots = new[] { issueVis2 };

        testSubject.UpdateForFile("file2", newHotspots);

        VerifyContent(testSubject, 
            new LocalHotspot(issueVis1),
            new LocalHotspot(issueVis2));
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(Array.Empty<IAnalysisIssueVisualization>(), newHotspots));
    }

    [TestMethod]
    public void UpdateForFile_ServerHotspots_MatchesCorrectly()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot();
        var serverHotspot2 = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2 });

        var issueVis3 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis1 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis2 = Mock.Of<IAnalysisIssueVisualization>();

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(issueVis1, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis2, serverHotspot2)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis3, serverHotspot1))
            .Returns(true); // 2 local hotspots match 1 server but only one pair should be created

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, matcherMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1, issueVis2, issueVis3 });

        VerifyContent(testSubject, 
            new LocalHotspot(issueVis1, serverHotspot1),
            new LocalHotspot(issueVis2, serverHotspot2),
            new LocalHotspot(issueVis3));
    }

    [TestMethod]
    public void UpdateForFile_ServerHotspots_SameFileUpdate_MakesUnmatchedServerHotspotsAvailable()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot();
        var serverHotspot2 = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2 });

        var issueVis1 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis2 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis3 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis4 = Mock.Of<IAnalysisIssueVisualization>();

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(issueVis1, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis2, serverHotspot2)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis3, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis4, serverHotspot2)).Returns(true);

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, matcherMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1, issueVis2 });
        VerifyContent(testSubject, 
            new LocalHotspot(issueVis1, serverHotspot1),
            new LocalHotspot(issueVis2, serverHotspot2));
        
        testSubject.UpdateForFile("file1", new[] { issueVis3 });
        VerifyContent(testSubject, 
            new LocalHotspot(issueVis3, serverHotspot1));
        
        testSubject.UpdateForFile("file1", new[] { issueVis3, issueVis4 });
        VerifyContent(testSubject, 
            new LocalHotspot(issueVis3, serverHotspot1),
            new LocalHotspot(issueVis4, serverHotspot2));
        
        serverStoreMock.Verify(store => store.GetAll(), Times.Once);
    }


    [TestMethod]
    public void Refresh_NoLocalHotspots_NothingHappens()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot();
        var serverHotspot2 = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2 });
        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(It.IsAny<IAnalysisIssueVisualization>(), It.IsAny<SonarQubeHotspot>()))
            .Returns(true);
        var testSubject = CreateTestSubject(out var eventListener, serverStoreMock.Object, matcherMock.Object);

        RaiseRefreshedEvent(serverStoreMock);

        VerifyContent(testSubject);
        eventListener.Events.Should().BeEmpty();
    }

    [TestMethod]
    public void Refresh_NewServerHotspots_ExistingHotspotsRematched()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot();
        var serverHotspot2 = CreateEmptyServerHotspot();
        var serverHotspot3 = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2 });

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(It.IsAny<IAnalysisIssueVisualization>(), It.IsAny<SonarQubeHotspot>()))
            .Returns(true);

        var issueVis1 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVis2 = Mock.Of<IAnalysisIssueVisualization>();
        var issueVisualizations = new[] { issueVis1, issueVis2 };

        var testSubject = CreateTestSubject(out var eventListener, serverStoreMock.Object, matcherMock.Object);

        testSubject.UpdateForFile("file1", issueVisualizations);
        VerifyContent(testSubject, new LocalHotspot(issueVis1, serverHotspot1),
            new LocalHotspot(issueVis2, serverHotspot2));

        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot3 });
        RaiseRefreshedEvent(serverStoreMock);

        VerifyContent(testSubject, new LocalHotspot(issueVis1, serverHotspot3), new LocalHotspot(issueVis2));
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(issueVisualizations, issueVisualizations));
    }

    [TestMethod]
    public void RemoveForFile_NoHotspots_NothingHappens()
    {
        var testSubject = CreateTestSubject(out var eventListener);
        
        testSubject.RemoveForFile("file1");
        
        eventListener.Events.Should().BeEmpty();
    }

    [TestMethod]
    public void RemoveForFile_RemovesForCorrectFile()
    {
        var testSubject = CreateTestSubject(out var eventListener);
        var visToKeep = Mock.Of<IAnalysisIssueVisualization>();
        var visToRemove = Mock.Of<IAnalysisIssueVisualization>();
        testSubject.UpdateForFile("file1", new [] { visToKeep });
        testSubject.UpdateForFile("file2", new [] { visToRemove });
        eventListener.Events.Clear();

        testSubject.RemoveForFile("file2");
        
        VerifyContent(testSubject, new LocalHotspot(visToKeep));
        eventListener.Events.Single()
            .Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(new[] { visToRemove }, 
                Array.Empty<IAnalysisIssueVisualization>()));
    }

    [TestMethod]
    public void RemoveForFile_MakesUnmatchedServerHotspotsAvailable()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot });

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(It.IsAny<IAnalysisIssueVisualization>(), It.IsAny<SonarQubeHotspot>()))
            .Returns(true);

        var oldHotspot = Mock.Of<IAnalysisIssueVisualization>();
        var newHotspot = Mock.Of<IAnalysisIssueVisualization>();

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, matcherMock.Object);
        testSubject.UpdateForFile("file1", new[] { oldHotspot });
        VerifyContent(testSubject, new LocalHotspot(oldHotspot, serverHotspot));

        testSubject.RemoveForFile("file1");
        testSubject.UpdateForFile("file2", new[] { newHotspot });

        VerifyContent(testSubject, new LocalHotspot(newHotspot, serverHotspot));
    }

    private static void RaiseRefreshedEvent(Mock<IServerHotspotStore> serverStoreMock)
    {
        serverStoreMock.Raise(x => x.Refreshed += null, EventArgs.Empty);
    }

    private void VerifyContent(ILocalHotspotsStore store, params LocalHotspot[] expected)
    {
        store.GetAllLocalHotspots().Should().BeEquivalentTo(expected);
        store.GetAll().Should().BeEquivalentTo(expected.Select(x => x.Visualization));
    }

    private static SonarQubeHotspot CreateEmptyServerHotspot()
    {
        return new SonarQubeHotspot(null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            null,
            null);
    }

    private ILocalHotspotsStore CreateTestSubject(out TestEventListener eventListener,
        IServerHotspotStore serverHotspotStore = null,
        IHotspotMatcher hotspotMatcher = null,
        IThreadHandling threadHandling = null)
    {
        var localHotspotsStore = new LocalHotspotsStore(serverHotspotStore ?? Mock.Of<IServerHotspotStore>(),
            hotspotMatcher ?? Mock.Of<IHotspotMatcher>(),
            threadHandling ?? Mock.Of<IThreadHandling>());

        eventListener = new TestEventListener(localHotspotsStore);

        return localHotspotsStore;
    }

    private class TestEventListener
    {
        public TestEventListener(ILocalHotspotsStore localHotspotsStore)
        {
            localHotspotsStore.IssuesChanged += EventHandler;
        }

        public List<IssuesChangedEventArgs> Events { get; } = new();

        private void EventHandler(object sender, IssuesChangedEventArgs eventArgs)
        {
            Events.Add(eventArgs);
        }
    }
}
