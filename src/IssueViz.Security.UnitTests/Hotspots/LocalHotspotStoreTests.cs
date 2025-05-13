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

using Moq;
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
    [TestMethod]
    public void MefCtor_CheckExports_ILocalHotspotsStore()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, ILocalHotspotsStore>(
            MefTestHelpers.CreateExport<IHotspotReviewPriorityProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckExports_ILocalHotspotsStoreUpdater()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, ILocalHotspotsStoreUpdater>(
            MefTestHelpers.CreateExport<IHotspotReviewPriorityProvider>(),
            MefTestHelpers.CreateExport<IThreadHandling>());
    }

    [TestMethod]
    public void MefCtor_CheckExports_IIssuesStore()
    {
        MefTestHelpers.CheckTypeCanBeImported<LocalHotspotsStore, IIssuesStore>(
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
    public void UpdateForFile_AddsLocalHotspots()
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
    public void UpdateForFile_UpdatesForSameFile()
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
    public void UpdateForFile_AddsForDifferentFile()
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
    public void UpdateForFile_UsesDefaultReviewPriorityWhenUnmapped()
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
    public void UpdateForFile_UsesReviewPriority()
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
    [DataRow(HotspotPriority.High)]
    [DataRow(HotspotPriority.Medium)]
    [DataRow(HotspotPriority.Low)]
    public void UpdateForFile_ShouldAssignHotspotPriority(HotspotPriority priority)
    {
        const string rule1 = "rule:s1";
        var issueVis1 = CreateIssueVisualizationWithHotspot(rule1, priority);
        var reviewPriorityProviderMock = new Mock<IHotspotReviewPriorityProvider>();
        var testSubject = CreateTestSubject(out _, reviewPriorityProvider: reviewPriorityProviderMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1 });

        VerifyContent(testSubject, new LocalHotspot(issueVis1, priority));
        reviewPriorityProviderMock.Verify(mock => mock.GetPriority(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public void UpdateForFile_ForCFamily_ShouldAssignHotspotPriority()
    {
        /*
         * issue1 + server1 -> rule1 -> Low - could be changed to test server override once implemented
         * issue2 + server2 -> rule2 -> Medium
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

        var testSubject = CreateTestSubject(out _, reviewPriorityProviderMock.Object);

        testSubject.UpdateForFile("file1", new[] { issueVis1.Object, issueVis2.Object, issueVis3.Object });

        VerifyContent(testSubject,
            new LocalHotspot(issueVis1.Object, HotspotPriority.Low),
            new LocalHotspot(issueVis2.Object, HotspotPriority.Medium),
            new LocalHotspot(issueVis3.Object, HotspotPriority.Low));
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
    public void Clear_ClearsMapping()
    {
        var threadHandlingMock = new Mock<IThreadHandling>();

        var testSubject = CreateTestSubject(out var eventListener, threadHandling: threadHandlingMock.Object);

        testSubject.UpdateForFile("fileA", new[] { CreateUniqueIssueViz(), CreateUniqueIssueViz() });
        testSubject.UpdateForFile("fileB", new[] { CreateUniqueIssueViz() });
        threadHandlingMock.Invocations.Clear();
        eventListener.Events.Clear();

        testSubject.GetAll().Count.Should().Be(3);

        testSubject.Clear();

        testSubject.GetAll().Count.Should().Be(0);
        eventListener.Events.Single().RemovedIssues.Should().HaveCount(3);
        threadHandlingMock.Verify(x => x.ThrowIfOnUIThread(), Times.Once);
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

    private ILocalHotspotsStore CreateTestSubject(
        out TestEventListener eventListener,
        IHotspotReviewPriorityProvider reviewPriorityProvider = null,
        IThreadHandling threadHandling = null)
    {
        var localHotspotsStore = new LocalHotspotsStore(
            reviewPriorityProvider ?? Mock.Of<IHotspotReviewPriorityProvider>(),
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

    private static IAnalysisIssueVisualization CreateIssueVisualizationWithHotspot(string rule, HotspotPriority priority)
    {
        var issueVis = new Mock<IAnalysisIssueVisualization>();
        var hotspotIssue = new Mock<IAnalysisHotspotIssue>();
        hotspotIssue.SetupGet(x => x.HotspotPriority).Returns(priority);
        issueVis.Setup(x => x.Issue).Returns(hotspotIssue.Object);
        issueVis.Setup(x => x.RuleId).Returns(rule);

        return issueVis.Object;
    }
}
