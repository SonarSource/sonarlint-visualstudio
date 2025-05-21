/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots;

[TestClass]
public class LocalHotspotStoreTests
{
    private IThreadHandling threadHandling;
    private LocalHotspotsStore testSubject;
    private TestEventListener eventListener;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = Substitute.For<IThreadHandling>();
        testSubject = new LocalHotspotsStore(threadHandling);
        eventListener = new TestEventListener(testSubject);
    }

    [TestMethod]
    public void MefCtor_CheckExports_ILocalHotspotsStore() =>
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, ILocalHotspotsStore>(
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckExports_ILocalHotspotsStoreUpdater() =>
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, ILocalHotspotsStoreUpdater>(
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void MefCtor_CheckExports_IIssuesStore() =>
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, IIssuesStore>(
            MefTestHelpers.CreateExport<IThreadHandling>());

    [TestMethod]
    public void UpdateForFile_NoHotspots_NothingHappens()
    {
        testSubject.UpdateForFile("file1", []);
        testSubject.UpdateForFile("file1", []);

        VerifyContent(testSubject);
        eventListener.Events.Should().BeEmpty();
    }

    [TestMethod]
    public void UpdateForFile_AddsHotspots()
    {
        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var hotspots = new[] { issueVis1, issueVis2 };

        testSubject.UpdateForFile("file1", hotspots);

        threadHandling.Received(1).ThrowIfOnUIThread();
        VerifyContent(testSubject, hotspots.Select(x => new LocalHotspot(x, default, default)).ToArray());
        eventListener.Events.Should()
            .BeEquivalentTo(new IssuesChangedEventArgs([], hotspots));
    }

    [TestMethod]
    public void UpdateForFile_UpdatesForSameFile()
    {
        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        var issueVis3 = CreateUniqueIssueViz();
        var oldHotspots = new[] { issueVis1 };
        testSubject.UpdateForFile("file1", oldHotspots);
        var newHotspots = new[] { issueVis2, issueVis3 };

        testSubject.UpdateForFile("file1", newHotspots);

        VerifyContent(testSubject, newHotspots.Select(x => new LocalHotspot(x, default, default)).ToArray());
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs(oldHotspots, newHotspots));
    }

    [TestMethod]
    public void UpdateForFile_AddsForDifferentFile()
    {
        var issueVis1 = CreateUniqueIssueViz();
        var issueVis2 = CreateUniqueIssueViz();
        testSubject.UpdateForFile("file1", [issueVis1]);
        var newHotspots = new[] { issueVis2 };

        testSubject.UpdateForFile("file2", newHotspots);

        VerifyContent(testSubject,
            new LocalHotspot(issueVis1, default, default),
            new LocalHotspot(issueVis2, default, default));
        eventListener.Events.Should().HaveCount(2).And.Subject.Last().Should()
            .BeEquivalentTo(new IssuesChangedEventArgs([], newHotspots));
    }

    [TestMethod]
    public void UpdateForFile_UsesDefaultReviewPriorityWhenUnmapped()
    {
        var issueVis1 = CreateUniqueIssueViz();
        const string rule1 = "rule1";
        issueVis1.RuleId.Returns(rule1);

        testSubject.UpdateForFile("file1", [issueVis1]);

        VerifyContent(testSubject, new LocalHotspot(issueVis1, HotspotPriority.High, default));
    }

    [TestMethod]
    [DataRow(HotspotPriority.High)]
    [DataRow(HotspotPriority.Medium)]
    [DataRow(HotspotPriority.Low)]
    public void UpdateForFile_ShouldAssignHotspotPriority(HotspotPriority priority)
    {
        const string rule1 = "rule:s1";
        var issueVis1 = CreateIssueVisualizationWithHotspot(rule1, priority);

        testSubject.UpdateForFile("file1", [issueVis1]);

        VerifyContent(testSubject, new LocalHotspot(issueVis1, priority, default));
    }

    [TestMethod]
    [DataRow(HotspotStatus.ToReview)]
    [DataRow(HotspotStatus.Acknowledged)]
    [DataRow(HotspotStatus.Fixed)]
    [DataRow(HotspotStatus.Safe)]
    public void UpdateForFile_ShouldAssignHotspotStatus(HotspotStatus status)
    {
        const string rule1 = "rule:s1";
        var issueVis1 = CreateIssueVisualizationWithHotspot(rule1, default, status);

        testSubject.UpdateForFile("file1", [issueVis1]);

        VerifyContent(testSubject, new LocalHotspot(issueVis1, default, status));
    }

    [TestMethod]
    public void GetAll_ReturnsOnlyOpenHotspots()
    {
        var issueVis1 = CreateIssueVisualizationWithHotspot("rule:s1", HotspotPriority.Low, isResolved: true);
        var issueVis2 = CreateIssueVisualizationWithHotspot("rule:s2", HotspotPriority.Medium, isResolved: false);
        var issueVis3 = CreateIssueVisualizationWithHotspot("rule:s3", HotspotPriority.High, isResolved: true);

        testSubject.UpdateForFile("file1", [issueVis1, issueVis2, issueVis3]);

        VerifyContent(testSubject, new LocalHotspot(issueVis2, HotspotPriority.Medium, default));
    }

    [TestMethod]
    public void RemoveForFile_NoHotspots_NothingHappens()
    {
        testSubject.RemoveForFile("file1");

        eventListener.Events.Should().BeEmpty();
    }

    [TestMethod]
    public void RemoveForFile_RemovesForCorrectFile()
    {
        var visToKeep = CreateUniqueIssueViz();
        var visToRemove = CreateUniqueIssueViz();
        testSubject.UpdateForFile("file1", [visToKeep]);
        testSubject.UpdateForFile("file2", [visToRemove]);
        eventListener.Events.Clear();

        testSubject.RemoveForFile("file2");

        VerifyContent(testSubject, new LocalHotspot(visToKeep, default, default));
        eventListener.Events.Single()
            .Should()
            .BeEquivalentTo(new IssuesChangedEventArgs([visToRemove],
                []));
    }

    [TestMethod]
    public void Clear_ClearsMapping()
    {
        testSubject.UpdateForFile("fileA", [CreateUniqueIssueViz(), CreateUniqueIssueViz()]);
        testSubject.UpdateForFile("fileB", [CreateUniqueIssueViz()]);
        threadHandling.ClearReceivedCalls();
        eventListener.Events.Clear();

        testSubject.GetAll().Count.Should().Be(3);

        testSubject.Clear();

        testSubject.GetAll().Count.Should().Be(0);
        eventListener.Events.Single().RemovedIssues.Should().HaveCount(3);
        threadHandling.Received(1).ThrowIfOnUIThread();
    }

    private static void VerifyContent(ILocalHotspotsStore store, params LocalHotspot[] expected)
    {
        store.GetAllLocalHotspots().Should().BeEquivalentTo(expected);
        store.GetAll().Should().BeEquivalentTo(expected.Select(x => x.Visualization));
    }

    private static IAnalysisIssueVisualization CreateUniqueIssueViz()
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.LineHash.Returns(Guid.NewGuid().ToString());
        issueViz.Issue.Returns(Substitute.For<IAnalysisHotspotIssue>());
        return issueViz;
    }

    private class TestEventListener
    {
        public TestEventListener(ILocalHotspotsStore localHotspotsStore)
        {
            localHotspotsStore.IssuesChanged += EventHandler;
        }

        public List<IssuesChangedEventArgs> Events { get; } =
        [
        ];

        private void EventHandler(object sender, IssuesChangedEventArgs eventArgs) => Events.Add(eventArgs);
    }

    private static IAnalysisIssueVisualization CreateIssueVisualizationWithHotspot(
        string rule,
        HotspotPriority priority,
        HotspotStatus status = default,
        bool isResolved = false)
    {
        var issueVis = Substitute.For<IAnalysisIssueVisualization>();
        var hotspotIssue = Substitute.For<IAnalysisHotspotIssue>();
        hotspotIssue.HotspotPriority.Returns(priority);
        hotspotIssue.HotspotStatus.Returns(status);
        issueVis.Issue.Returns(hotspotIssue);
        issueVis.RuleId.Returns(rule);
        issueVis.IsResolved.Returns(isResolved);

        return issueVis;
    }
}
