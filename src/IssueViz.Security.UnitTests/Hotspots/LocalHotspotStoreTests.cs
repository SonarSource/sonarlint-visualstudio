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
using SonarLint.VisualStudio.ConnectedMode.Hotspots;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
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
            MefTestHelpers.CreateExport<IHotspotReviewPriorityProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckExports_ILocalHotspotsStoreUpdater()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, ILocalHotspotsStoreUpdater>(
            MefTestHelpers.CreateExport<IServerHotspotStore>(),
            MefTestHelpers.CreateExport<IHotspotMatcher>(),
            MefTestHelpers.CreateExport<IHotspotReviewPriorityProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckExports_IIssuesStore()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, IIssuesStore>(
            MefTestHelpers.CreateExport<IServerHotspotStore>(),
            MefTestHelpers.CreateExport<IHotspotMatcher>(),
            MefTestHelpers.CreateExport<IHotspotReviewPriorityProvider>(),
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
        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var testSubject = CreateTestSubject(out var eventListener, threadHandling: threadHandlingMock.Object);
        var hotspots = new[] { issueVis1, issueVis2 };

        testSubject.UpdateForFile("file1", hotspots);

        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread(), Times.Once);
        VerifyContent(testSubject, hotspots.Select(x => new LocalHotspot(x, default)).ToArray());
        eventListener.Events.Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(Array.Empty<IAnalysisIssueVisualization>(), hotspots));
    }

    [TestMethod]
    public void UpdateForFile_NoServerHotspots_UpdatesForSameFile()
    {
        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var issueVis3 = CreateUniqueIssueViz();
        var testSubject = CreateTestSubject(out var eventListener);
        var oldHotspots = new[] { issueVis1 };
        testSubject.UpdateForFile("file1", oldHotspots);
        var newHotspots = new[] { issueVis2, issueVis3 };

        testSubject.UpdateForFile("file1", newHotspots);

        VerifyContent(testSubject, newHotspots.Select(x => new LocalHotspot(x, default)).ToArray());
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(oldHotspots, newHotspots));
    }

    [TestMethod]
    public void UpdateForFile_NoServerHotspots_AddsForDifferentFile()
    {
        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var testSubject = CreateTestSubject(out var eventListener);
        testSubject.UpdateForFile("file1", new[] { issueVis1 });
        var newHotspots = new[] { issueVis2 };

        testSubject.UpdateForFile("file2", newHotspots);

        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default),
            new LocalHotspot(issueVis2, default));
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(Array.Empty<IAnalysisIssueVisualization>(), newHotspots));
    }

    [TestMethod]
    public void UpdateForFile_NoServerHotspots_UsesDefaultReviewPriorityWhenUnmapped()
    {
        var issueVis1 = new Mock<IAnalysisIssueVisualization>();
        const string rule1 = "rule1";
        issueVis1.SetupGet(x => x.RuleId).Returns(rule1);
        var testSubject = CreateTestSubject(out _);

        var hotspotPriorityProviderMock = new Mock<IHotspotReviewPriorityProvider>();
        hotspotPriorityProviderMock.Setup(x => x.GetPriority(rule1)).Returns((HotspotPriority?)null);

        testSubject.UpdateForFile("file1", new[] { issueVis1.Object });

        VerifyContent(testSubject, new LocalHotspot(issueVis1.Object, HotspotPriority.High));
    }

    [TestMethod]
    public void UpdateForFile_NoServerHotspots_UsesReviewPriority()
    {
        /*
         * issue1 -> rule1 -> Low
         * issue2 -> rule2 -> Medium
         * issue3 -> rule1 -> Low
         */

        const string rule1 = "rule:s1";
        const string rule2 = "rule:s2";
        var issueVis1 = new Mock<IAnalysisIssueVisualization>();
        issueVis1.SetupGet(x => x.RuleId).Returns(rule1);
        var issueVis2 = new Mock<IAnalysisIssueVisualization>();
        issueVis2.SetupGet(x => x.RuleId).Returns(rule2);
        var issueVis3 = new Mock<IAnalysisIssueVisualization>();
        issueVis3.SetupGet(x => x.RuleId).Returns(rule1);

        var reviewPriorityProviderMock = new Mock<IHotspotReviewPriorityProvider>();
        reviewPriorityProviderMock.Setup(x => x.GetPriority(rule1)).Returns(HotspotPriority.Low);
        reviewPriorityProviderMock.Setup(x => x.GetPriority(rule2)).Returns(HotspotPriority.Medium);

        var testSubject = CreateTestSubject(out _, reviewPriorityProvider: reviewPriorityProviderMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1.Object, issueVis2.Object, issueVis3.Object });

        VerifyContent(testSubject,
            new LocalHotspot(issueVis1.Object, HotspotPriority.Low),
            new LocalHotspot(issueVis2.Object, HotspotPriority.Medium),
            new LocalHotspot(issueVis3.Object, HotspotPriority.Low));
    }

    [TestMethod]
    public void UpdateForFile_ServerHotspots_MatchesCorrectly()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot();
        var serverHotspot2 = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2 });

        var issueVis3 = CreateUniqueIssueViz();
        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(issueVis1, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis2, serverHotspot2)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis3, serverHotspot1))
            .Returns(true); // 2 local hotspots match 1 server but only one pair should be created

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1, issueVis2, issueVis3 });

        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, serverHotspot1),
            new LocalHotspot(issueVis2, default, serverHotspot2),
            new LocalHotspot(issueVis3, default));
    }

    [TestMethod]
    public void UpdateForFile_ServerHotspots_UsesReviewPriority()
    {
        /*
         * issue1 + server1 -> rule1 -> Low - could be changed to test server override once implemented
         * issue2 + server2 -> rule2 -> Medium
         * issue3 -> rule1 -> Low
         */
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot();
        var serverHotspot2 = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2 });

        const string rule1 = "rule:s1";
        const string rule2 = "rule:s2";
        var issueVis1 = new Mock<IAnalysisIssueVisualization>();
        issueVis1.SetupGet(x => x.RuleId).Returns(rule1);
        var issueVis2 = new Mock<IAnalysisIssueVisualization>();
        issueVis2.SetupGet(x => x.RuleId).Returns(rule2);
        var issueVis3 = new Mock<IAnalysisIssueVisualization>();
        issueVis3.SetupGet(x => x.RuleId).Returns(rule1);

        var reviewPriorityProviderMock = new Mock<IHotspotReviewPriorityProvider>();
        reviewPriorityProviderMock.Setup(x => x.GetPriority(rule1)).Returns(HotspotPriority.Low);
        reviewPriorityProviderMock.Setup(x => x.GetPriority(rule2)).Returns(HotspotPriority.Medium);

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(issueVis1.Object, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis2.Object, serverHotspot2)).Returns(true);

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, reviewPriorityProviderMock.Object, matcherMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1.Object, issueVis2.Object, issueVis3.Object });

        VerifyContent(testSubject,
            new LocalHotspot(issueVis1.Object, HotspotPriority.Low, serverHotspot1),
            new LocalHotspot(issueVis2.Object, HotspotPriority.Medium, serverHotspot2),
            new LocalHotspot(issueVis3.Object, HotspotPriority.Low));
    }

    [TestMethod]
    public void UpdateForFile_ServerHotspots_SameFileUpdate_MakesUnmatchedServerHotspotsAvailable()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot();
        var serverHotspot2 = CreateEmptyServerHotspot();
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2 });

        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var issueVis3 = CreateUniqueIssueViz();
        var issueVis4 = CreateUniqueIssueViz();

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(issueVis1, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis2, serverHotspot2)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis3, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis4, serverHotspot2)).Returns(true);

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1, issueVis2 });
        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, serverHotspot1),
            new LocalHotspot(issueVis2, default, serverHotspot2));

        testSubject.UpdateForFile("file1", new[] { issueVis3 });
        VerifyContent(testSubject,
            new LocalHotspot(issueVis3, default, serverHotspot1));

        testSubject.UpdateForFile("file1", new[] { issueVis3, issueVis4 });
        VerifyContent(testSubject,
            new LocalHotspot(issueVis3, default, serverHotspot1),
            new LocalHotspot(issueVis4, default, serverHotspot2));

        serverStoreMock.Verify(store => store.GetAll(), Times.Once);
    }

    [TestMethod]
    // Regression test for https://github.com/SonarSource/sonarlint-visualstudio/issues/4602
    public void UpdateForFile_ServerHotspots_SameFileUpdate_ServerHotspotMatchingIsDeterministic()
    {
        // Note: the original bug was caused because *in some cases* removing items from a HashSet<>
        // then adding them back in again in a different order changes the order in which the
        // items in the HashSet<> are enumerated -> we get different matches next time.

        var serverStoreMock = new Mock<IServerHotspotStore>();
        var unusedButEssential = CreateEmptyServerHotspot(hotspotKey: "unused1");
        var matchingServerHotspot1 = CreateEmptyServerHotspot(status: "REVIEWED", resolution: "SAFE", hotspotKey: "ZZZ");
        var matchingServerHotspot2 = CreateEmptyServerHotspot(status: "TO_REVIEW", hotspotKey: "AAA");

        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] {
            unusedButEssential, // if this element is omitted then the bug doesn't repro
            matchingServerHotspot2,
            matchingServerHotspot1 });

        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();

        // The server hotspots can match both local hotspots
        var matcherMock = new Mock<IHotspotMatcher>();

        matcherMock.Setup(x => x.IsMatch(It.IsAny<IAnalysisIssueVisualization>(), matchingServerHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(It.IsAny<IAnalysisIssueVisualization>(), matchingServerHotspot2)).Returns(true);

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);

        // 1. Initial analysis
        testSubject.UpdateForFile("file1", Enumerable.Empty<IAnalysisIssueVisualization>());
        VerifyContent(testSubject /* empty */ );

        testSubject.UpdateForFile("file1", new[] { issueVis1, issueVis2 });
        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, matchingServerHotspot2));

        // Simulate what happens when an analysis is run 
        // 2a. Update with no issues
        testSubject.UpdateForFile("file1", Enumerable.Empty<IAnalysisIssueVisualization>());
        VerifyContent(testSubject /* empty */ );

        // 2b. Update with issues again
        // Expecting the issues to be matched deterministically i.e. same active issue returned
        testSubject.UpdateForFile("file1", new[] { issueVis1, issueVis2 });
        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, matchingServerHotspot2));

        serverStoreMock.Verify(store => store.GetAll(), Times.Once);
    }

    [TestMethod]
    public void UpdateForFile_MatchingDoesNotDependOnOrderServerHotspotsAreReturnedByTheServer()
    {
        var issueVis1 = CreateUniqueIssueViz();

        var serverHotspot1 = CreateEmptyServerHotspot(hotspotKey: "111", startLine: 10, startLineOffset: 11);
        var serverHotspot2 = CreateEmptyServerHotspot(hotspotKey: "222", startLine: 20, startLineOffset: 22);

        var testSubject = CreateAndUpdateForFile(serverHotspot1, serverHotspot2);
        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, serverHotspot1));

        // Should match the issue to the same server hotspot, even if the server hotspots
        // are returned in the reverse order
        testSubject = CreateAndUpdateForFile(serverHotspot2, serverHotspot1);
        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, serverHotspot1));

        ILocalHotspotsStore CreateAndUpdateForFile(params SonarQubeHotspot[] serverHotpotsInOrder)
        {
            var serverStoreMock = new Mock<IServerHotspotStore>();

            serverStoreMock.Setup(x => x.GetAll()).Returns(serverHotpotsInOrder);

            var matcherMock = new Mock<IHotspotMatcher>();
            matcherMock.Setup(x => x.IsMatch(It.IsAny<IAnalysisIssueVisualization>(), It.IsAny<SonarQubeHotspot>())).Returns(true);

            var testSubject = CreateTestSubject(out _, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);

            testSubject.UpdateForFile("file1", new[] { issueVis1 });
            return testSubject;
        }
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
        var testSubject = CreateTestSubject(out var eventListener, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);

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

        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var issueVisualizations = new[] { issueVis1, issueVis2 };

        var testSubject = CreateTestSubject(out var eventListener, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);

        testSubject.UpdateForFile("file1", issueVisualizations);
        VerifyContent(testSubject, new LocalHotspot(issueVis1, default, serverHotspot1),
            new LocalHotspot(issueVis2, default, serverHotspot2));

        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot3 });
        RaiseRefreshedEvent(serverStoreMock);

        VerifyContent(testSubject, new LocalHotspot(issueVis1, default, serverHotspot3), new LocalHotspot(issueVis2, default));
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(issueVisualizations, issueVisualizations));
    }

    [TestMethod]
    public void GetAll_ReviewedServerHotspots_Filters()
    {
        var serverStoreMock = new Mock<IServerHotspotStore>();
        var serverHotspot1 = CreateEmptyServerHotspot(status: "TO_REVIEW");
        var serverHotspot2 = CreateEmptyServerHotspot(status: "REVIEWED", resolution: "ACKNOWLEDGED");
        var serverHotspot3 = CreateEmptyServerHotspot(status: "REVIEWED", resolution: "FIXED"); //Expected to be filtered out
        var serverHotspot4 = CreateEmptyServerHotspot(status: "REVIEWED", resolution: "SAFE"); //Expected to be filtered out
        serverStoreMock.Setup(x => x.GetAll()).Returns(new[] { serverHotspot1, serverHotspot2, serverHotspot3, serverHotspot4 });

        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var issueVis3 = CreateUniqueIssueViz();
        var issueVis4 = CreateUniqueIssueViz();
        var issueVis5 = CreateUniqueIssueViz();
        var issueVisualizations = new[] { issueVis1, issueVis2, issueVis3, issueVis4, issueVis5 };

        var matcherMock = new Mock<IHotspotMatcher>();
        matcherMock.Setup(x => x.IsMatch(It.IsAny<IAnalysisIssueVisualization>(), It.IsAny<SonarQubeHotspot>())).Returns(false);
        matcherMock.Setup(x => x.IsMatch(issueVis1, serverHotspot1)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis2, serverHotspot2)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis3, serverHotspot3)).Returns(true);
        matcherMock.Setup(x => x.IsMatch(issueVis4, serverHotspot4)).Returns(true);

        var testSubject = CreateTestSubject(out var eventListener, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);
        testSubject.UpdateForFile("file1", issueVisualizations);
        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, serverHotspot1),
            new LocalHotspot(issueVis2, default, serverHotspot2),
            new LocalHotspot(issueVis5, default));
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
        var visToKeep = CreateUniqueIssueViz();
        var visToRemove = CreateUniqueIssueViz();
        testSubject.UpdateForFile("file1", new[] { visToKeep });
        testSubject.UpdateForFile("file2", new[] { visToRemove });
        eventListener.Events.Clear();

        testSubject.RemoveForFile("file2");

        VerifyContent(testSubject, new LocalHotspot(visToKeep, default));
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

        var oldHotspot = CreateUniqueIssueViz();
        var newHotspot = CreateUniqueIssueViz();

        var testSubject = CreateTestSubject(out _, serverStoreMock.Object, hotspotMatcher: matcherMock.Object);
        testSubject.UpdateForFile("file1", new[] { oldHotspot });
        VerifyContent(testSubject, new LocalHotspot(oldHotspot, default, serverHotspot));

        testSubject.RemoveForFile("file1");
        testSubject.UpdateForFile("file2", new[] { newHotspot });

        VerifyContent(testSubject, new LocalHotspot(newHotspot, default, serverHotspot));
    }

    [TestMethod]
    public void Clear_ClearsMapping()
    {
        var threadHandlingMock = new Mock<IThreadHandling>();

        var testSubject = CreateTestSubject(out var eventListener, threadHandling:threadHandlingMock.Object);
        
        testSubject.UpdateForFile("fileA", new []{CreateUniqueIssueViz(), CreateUniqueIssueViz()});
        testSubject.UpdateForFile("fileB", new []{CreateUniqueIssueViz()});
        threadHandlingMock.Invocations.Clear();
        eventListener.Events.Clear();

        testSubject.GetAll().Count.Should().Be(3);
        
        testSubject.Clear();

        testSubject.GetAll().Count.Should().Be(0);
        eventListener.Events.Single().RemovedIssues.Should().HaveCount(3);
        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread(), Times.Once);
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

    private static IAnalysisIssueVisualization CreateUniqueIssueViz()
    {
        var issueViz = new Mock<IAnalysisIssueVisualization>();
        issueViz.Setup(x => x.LineHash).Returns(Guid.NewGuid().ToString());
        return issueViz.Object;
    }

   private static SonarQubeHotspot CreateEmptyServerHotspot(string status = "TO_REVIEW",
        string resolution = null,
        string hotspotKey = null,
        int startLine = 0,
        int startLineOffset = 0)
    {
        // Need a unique value for the hotspot key otherwise the comparer will treat
        // different hotspots as being equal.
        hotspotKey ??= Guid.NewGuid().ToString();
        var textRange = new IssueTextRange(startLine, startLine + 1, startLineOffset, startLineOffset + 1);

        return new SonarQubeHotspot(hotspotKey,
             null,
            null,
            null,
            status,
            null,
            null,
            null,
            null,
            null,
            DateTimeOffset.Now,
            DateTimeOffset.Now,
            null,
            textRange,
            resolution);
    }

    private ILocalHotspotsStore CreateTestSubject(out TestEventListener eventListener,
        IServerHotspotStore serverHotspotStore = null,
        IHotspotReviewPriorityProvider reviewPriorityProvider = null,
        IHotspotMatcher hotspotMatcher = null,
        IThreadHandling threadHandling = null)
    {
        var localHotspotsStore = new LocalHotspotsStore(serverHotspotStore ?? Mock.Of<IServerHotspotStore>(),
            reviewPriorityProvider ?? Mock.Of<IHotspotReviewPriorityProvider>(),
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
