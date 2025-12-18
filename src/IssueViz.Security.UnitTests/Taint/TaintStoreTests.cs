/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint;

[TestClass]
public class TaintStoreTests
{
    private ITaintStore testSubject;

    [TestMethod]
    public void MefCtor_CheckExports()
    {
        var batch = new CompositionBatch();

        var storeImport = new SingleObjectImporter<ITaintStore>();
        var issuesStoreImport = new SingleObjectImporter<IIssuesStore>();
        batch.AddPart(storeImport);
        batch.AddPart(issuesStoreImport);

        var catalog = new TypeCatalog(typeof(TaintStore));
        using var container = new CompositionContainer(catalog);
        container.Compose(batch);

        storeImport.Import.Should().NotBeNull();
        issuesStoreImport.Import.Should().NotBeNull();

        storeImport.Import.Should().BeSameAs(issuesStoreImport.Import);
    }

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new TaintStore();
    }

    [TestMethod]
    public void GetAll_ReturnsImmutableInstance()
    {
        var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(oldItems, "config scope");

        var issuesList1 = testSubject.GetAll();
        testSubject.Update(new TaintVulnerabilitiesUpdate("config scope", [SetupIssueViz()], [], []));
        var issuesList2 = testSubject.GetAll();

        issuesList1.Count.Should().Be(2);
        issuesList2.Count.Should().Be(3);
    }

    [TestMethod]
    public void Set_NullCollection_ArgumentNullException()
    {
        Action act = () => testSubject.Set(null, "any");

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("issueVisualizations");
    }

    [TestMethod]
    public void Set_NullConfigScope_ArgumentNullException()
    {
        Action act = () => testSubject.Set([], null);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("newConfigurationScope");
    }

    [TestMethod]
    public void Set_NoSubscribersToIssuesChangedEvent_NoException()
    {
        Action act = () => testSubject.Set(new[] { SetupIssueViz() }, "some config scope");

        act.Should().NotThrow();
    }

    [TestMethod]
    public void Set_NoPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        testSubject.Set([], "some config scope");

        testSubject.GetAll().Should().BeEmpty();
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
    }

    [TestMethod]
    public void Reset_NoPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        testSubject.Reset();

        testSubject.GetAll().Should().BeEmpty();
        eventHandlerMock.ReceivedWithAnyArgs(1).Invoke(default, default);
    }

    [TestMethod]
    public void Set_NoPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
    {
        var receivedEventGetter = CaptureIssuesChangedEventArgs();
        var newItems = new[] { SetupIssueViz(), SetupIssueViz() };

        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], newItems));
    }

    [TestMethod]
    public void Set_HasPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
    {
        var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(oldItems, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Set([], "some config scope");

        testSubject.GetAll().Should().BeEmpty();
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs(oldItems, []));
    }

    [TestMethod]
    public void Set_HasPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
    {
        var oldItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(oldItems, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        var newItems = new[] { SetupIssueViz(), SetupIssueViz() };
        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs(oldItems, newItems));
    }

    [TestMethod]
    public void Set_HasPreviousItems_HasSomeNewItems_CollectionChangedAndEventRaised()
    {
        var issueViz1 = SetupIssueViz();
        var issueViz2Id = Guid.NewGuid();
        var issueViz2 = SetupIssueViz(issueViz2Id);
        var issueViz2NewObject = SetupIssueViz(issueViz2Id);
        var issueViz3 = SetupIssueViz();

        var oldItems = new[] { issueViz1, issueViz2 };
        testSubject.Set(oldItems, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        var newItems = new[] { issueViz2NewObject, issueViz3 };
        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([issueViz1, issueViz2], [issueViz2NewObject, issueViz3]));
    }

    [TestMethod]
    public void Set_HasItems_NoConfigScope_Throws()
    {
        var issueViz1 = SetupIssueViz();

        var act = () => testSubject.Set([issueViz1], null);

        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("newConfigurationScope");
    }

    [TestMethod]
    public void ConfigScope_NoInformation_ReturnsNull()
    {
        testSubject.Reset();

        var result = testSubject.ConfigurationScope;
        result.Should().BeNull();
    }

    [TestMethod]
    public void ConfigScope_HasInformation_ReturnsInformation()
    {
        const string newConfigurationScope = "some config scope";

        testSubject.Set([], newConfigurationScope);

        var result = testSubject.ConfigurationScope;
        result.Should().BeSameAs(newConfigurationScope);
    }

    [TestMethod]
    public void Update_NullParameter_Throws()
    {
        var act = () => testSubject.Update(null);

        act.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("taintVulnerabilitiesUpdate");
    }

    [TestMethod]
    public void Update_NoConfigScope_Ignored()
    {
        testSubject.Reset();
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [SetupIssueViz()], [], []));

        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void Update_ClosedIssues_Removed()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [], [analysisIssueVisualizations[0].IssueId, analysisIssueVisualizations[2].IssueId]));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations[1]);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0], analysisIssueVisualizations[2]], []));
    }

    [TestMethod]
    public void Update_ClosedIssues_PartiallyPresent_Removed()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [], [analysisIssueVisualizations[0].IssueId, Guid.NewGuid()]));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations.Skip(1));
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0]], []));
    }

    [TestMethod]
    public void Update_ClosedIssues_NotPresent_Ignored()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var eventHandlerMock = CreateEventHandlerMock();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [], [Guid.NewGuid()]));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations);
        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void Update_UpdatedIssues_Replaced()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        var updated1 = SetupIssueViz(analysisIssueVisualizations[0].IssueId);
        var updated2 = SetupIssueViz(analysisIssueVisualizations[2].IssueId);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated1, updated2], []));

        testSubject.GetAll().Should().BeEquivalentTo(updated1, updated2, analysisIssueVisualizations[1]);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0], analysisIssueVisualizations[2]], [updated1, updated2]));
    }

    [TestMethod]
    public void Update_UpdatedIssues_PartiallyPresent_Replaced()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        var updated1 = SetupIssueViz(analysisIssueVisualizations[0].IssueId);
        var updated2 = SetupIssueViz();
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated1, updated2], []));

        testSubject.GetAll().Should().BeEquivalentTo(updated1, analysisIssueVisualizations[1], analysisIssueVisualizations[2]);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0]], [updated1]));
    }

    [TestMethod]
    public void Update_UpdatedIssues_NotPresent_Ignored()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        var updated1 = SetupIssueViz();
        var updated2 = SetupIssueViz();
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var eventHandlerMock = CreateEventHandlerMock();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated1, updated2], []));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations);
        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void Update_UpdatedIssues_ChangedId_MatchedByIssueKeyAndReplaced()
    {
        const string serverKey = "taint-1";
        var taintWithChangedId = SetupIssueViz(issueKey: serverKey);
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [taintWithChangedId, SetupIssueViz(), SetupIssueViz()];
        var updated = SetupIssueViz(issueKey: serverKey);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated], []));

        testSubject.GetAll().Should().NotContain(taintWithChangedId);
        testSubject.GetAll().Should().Contain(updated);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([taintWithChangedId], [updated]));
    }

    [TestMethod]
    public void Update_AddedIssues_Adds()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        var added1 = SetupIssueViz();
        var added2 = SetupIssueViz();
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added1, added2], [], []));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations.Concat([added1, added2]));
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], [added1, added2]));
    }

    [TestMethod]
    public void Update_AddedIssues_PartiallyPresent_AddsMissing()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        var added1 = SetupIssueViz(analysisIssueVisualizations[0].IssueId);
        var added2 = SetupIssueViz();
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added1, added2], [], []));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations.Concat([added2]));
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], [added2]));
    }

    [TestMethod]
    public void Update_AddedIssues_AllPresent_Ignored()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(), SetupIssueViz(), SetupIssueViz()];
        var added1 = SetupIssueViz(analysisIssueVisualizations[0].IssueId);
        var added2 = SetupIssueViz(analysisIssueVisualizations[2].IssueId);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var eventHandlerMock = CreateEventHandlerMock();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added1, added2], [], []));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations);
        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void Update_Complex_RemovesUpdatesAndAdds()
    {
        var added = SetupIssueViz();
        var toUpdate = SetupIssueViz();
        var updated = SetupIssueViz(toUpdate.IssueId);
        var toRemove = SetupIssueViz();
        var notTouched = SetupIssueViz();
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [toUpdate, toRemove, notTouched];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added], [updated], [toRemove.IssueId]));

        testSubject.GetAll().Should().BeEquivalentTo(added, updated, notTouched);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([toUpdate, toRemove], [added, updated]));
    }

    [TestMethod]
    public void Update_CloseAndUpdateSameIssue_RemovesAndIgnoresUpdate()
    {
        var original = SetupIssueViz();
        var updated = SetupIssueViz(original.IssueId);
        var remove = original.IssueId;
        testSubject.Set([original], "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated], [remove]));

        testSubject.GetAll().Should().BeEmpty();
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([original], []));
    }

    [TestMethod]
    public void Update_UpdateAndAddSameIssue_UpdatesAndIgnoresAdd()
    {
        var original = SetupIssueViz();
        var updated = SetupIssueViz(original.IssueId);
        var add = SetupIssueViz(original.IssueId);
        testSubject.Set([original], "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [add], [updated], []));

        testSubject.GetAll().Should().BeEquivalentTo(updated);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([original], [updated]));
    }

    [TestMethod]
    public void Update_RemoveAndAddSameIssue_Updates()
    {
        var original = SetupIssueViz();
        var remove = original.IssueId;
        var add = SetupIssueViz(original.IssueId);
        testSubject.Set([original], "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [add], [], [remove]));

        testSubject.GetAll().Should().BeEquivalentTo(add);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([original], [add]));
    }

    private Func<IssuesChangedEventArgs> CaptureIssuesChangedEventArgs()
    {
        IssuesChangedEventArgs receivedEvent = null;
        var eventHandlerMock = CreateEventHandlerMock();
        eventHandlerMock.Invoke(Arg.Any<object>(), Arg.Do<IssuesChangedEventArgs>(x => receivedEvent = x));
        return () => receivedEvent;
    }

    private EventHandler<IssuesChangedEventArgs> CreateEventHandlerMock()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;
        return eventHandlerMock;
    }

    private static IAnalysisIssueVisualization SetupIssueViz(Guid? id = null, string issueKey = null)
    {
        id ??= Guid.NewGuid();
        issueKey ??= Guid.NewGuid().ToString();

        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.IssueId.Returns(id.Value);
        var taintIssue = Substitute.For<ITaintIssue>();
        taintIssue.IssueServerKey.Returns(issueKey);
        issueViz.Issue.Returns(taintIssue);

        return issueViz;
    }
}
