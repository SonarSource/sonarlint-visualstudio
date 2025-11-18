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
    private IDocumentTracker documentTracker;

    private const string DefaultFilePath = "default path";

    [TestInitialize]
    public void TestInitialize()
    {
        documentTracker = Substitute.For<IDocumentTracker>();
        documentTracker.GetOpenDocuments().Returns([new Document(DefaultFilePath, [])]);
        testSubject = new TaintStore(documentTracker);
    }

    [TestMethod]
    public void MefCtor_CheckExports() => MefTestHelpers.CheckTypeCanBeImported<TaintStore, ITaintStore>(MefTestHelpers.CreateExport<IDocumentTracker>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<TaintStore>();

    [TestMethod]
    public void Ctor_Called_EventHandlersSubscribed()
    {
        documentTracker.Received().DocumentClosed += Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received().DocumentOpened += Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.Received().OpenDocumentRenamed += Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
    }

    [TestMethod]
    public void GetAll_ReturnsImmutableInstance()
    {
        var oldItems = new[] { SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath) };
        testSubject.Set(oldItems, "config scope");

        var issuesList1 = testSubject.GetAll();
        testSubject.Update(new TaintVulnerabilitiesUpdate("config scope", [SetupIssueViz(DefaultFilePath)], [], []));
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
        Action act = () => testSubject.Set(new[] { SetupIssueViz(DefaultFilePath) }, "some config scope");

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
        var newItems = new[] { SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath) };

        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], newItems));
    }

    [TestMethod]
    public void Set_HasPreviousItems_NoNewItems_CollectionChangedAndEventRaised()
    {
        var oldItems = new[] { SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath) };
        testSubject.Set(oldItems, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Set([], "some config scope");

        testSubject.GetAll().Should().BeEmpty();
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs(oldItems, []));
    }

    [TestMethod]
    public void Set_HasPreviousItems_HasNewItems_CollectionChangedAndEventRaised()
    {
        var oldItems = new[] { SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath) };
        testSubject.Set(oldItems, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        var newItems = new[] { SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath) };
        testSubject.Set(newItems, "some config scope");

        testSubject.GetAll().Should().BeEquivalentTo(newItems);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs(oldItems, newItems));
    }

    [TestMethod]
    public void Set_HasPreviousItems_HasSomeNewItems_CollectionChangedAndEventRaised()
    {
        var issueViz1 = SetupIssueViz(DefaultFilePath);
        var issueViz2Id = Guid.NewGuid();
        var issueViz2 = SetupIssueViz(DefaultFilePath, issueViz2Id);
        var issueViz2NewObject = SetupIssueViz(DefaultFilePath, issueViz2Id);
        var issueViz3 = SetupIssueViz(DefaultFilePath);

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
        var issueViz1 = SetupIssueViz(DefaultFilePath);

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

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [SetupIssueViz(DefaultFilePath)], [], []));

        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void Update_ClosedIssues_Removed()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [], [analysisIssueVisualizations[0].IssueId!.Value, analysisIssueVisualizations[2].IssueId!.Value]));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations[1]);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0], analysisIssueVisualizations[2]], []));
    }

    [TestMethod]
    public void Update_ClosedIssues_PartiallyPresent_Removed()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [], [analysisIssueVisualizations[0].IssueId!.Value, Guid.NewGuid()]));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations.Skip(1));
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0]], []));
    }

    [TestMethod]
    public void Update_ClosedIssues_NotPresent_Ignored()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var eventHandlerMock = CreateEventHandlerMock();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [], [Guid.NewGuid()]));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations);
        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void Update_UpdatedIssues_Replaced()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        var updated1 = SetupIssueViz(DefaultFilePath, analysisIssueVisualizations[0].IssueId);
        var updated2 = SetupIssueViz(DefaultFilePath, analysisIssueVisualizations[2].IssueId);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated1, updated2], []));

        testSubject.GetAll().Should().BeEquivalentTo(updated1, updated2, analysisIssueVisualizations[1]);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0], analysisIssueVisualizations[2]], [updated1, updated2]));
    }

    [TestMethod]
    public void Update_UpdatedIssues_PartiallyPresent_Replaced()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        var updated1 = SetupIssueViz(DefaultFilePath, analysisIssueVisualizations[0].IssueId);
        var updated2 = SetupIssueViz(DefaultFilePath);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated1, updated2], []));

        testSubject.GetAll().Should().BeEquivalentTo(updated1, analysisIssueVisualizations[1], analysisIssueVisualizations[2]);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([analysisIssueVisualizations[0]], [updated1]));
    }

    [TestMethod]
    public void Update_UpdatedIssues_NotPresent_Ignored()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        var updated1 = SetupIssueViz(DefaultFilePath);
        var updated2 = SetupIssueViz(DefaultFilePath);
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
        var taintWithChangedId = SetupIssueViz(DefaultFilePath, issueKey: serverKey);
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [taintWithChangedId, SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        var updated = SetupIssueViz(DefaultFilePath, issueKey: serverKey);
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
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        var added1 = SetupIssueViz(DefaultFilePath);
        var added2 = SetupIssueViz(DefaultFilePath);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added1, added2], [], []));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations.Concat([added1, added2]));
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], [added1, added2]));
    }

    [TestMethod]
    public void Update_AddedIssues_PartiallyPresent_AddsMissing()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        var added1 = SetupIssueViz(DefaultFilePath, analysisIssueVisualizations[0].IssueId);
        var added2 = SetupIssueViz(DefaultFilePath);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added1, added2], [], []));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations.Concat([added2]));
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], [added2]));
    }

    [TestMethod]
    public void Update_AddedIssues_AllPresent_Ignored()
    {
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath), SetupIssueViz(DefaultFilePath)];
        var added1 = SetupIssueViz(DefaultFilePath, analysisIssueVisualizations[0].IssueId);
        var added2 = SetupIssueViz(DefaultFilePath, analysisIssueVisualizations[2].IssueId);
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var eventHandlerMock = CreateEventHandlerMock();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added1, added2], [], []));

        testSubject.GetAll().Should().BeEquivalentTo(analysisIssueVisualizations);
        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void Update_Complex_RemovesUpdatesAndAdds()
    {
        var added = SetupIssueViz(DefaultFilePath);
        var toUpdate = SetupIssueViz(DefaultFilePath);
        var updated = SetupIssueViz(DefaultFilePath, toUpdate.IssueId);
        var toRemove = SetupIssueViz(DefaultFilePath);
        var notTouched = SetupIssueViz(DefaultFilePath);
        List<IAnalysisIssueVisualization> analysisIssueVisualizations = [toUpdate, toRemove, notTouched];
        testSubject.Set(analysisIssueVisualizations, "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [added], [updated], [toRemove.IssueId!.Value]));

        testSubject.GetAll().Should().BeEquivalentTo(added, updated, notTouched);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([toUpdate, toRemove], [added, updated]));
    }

    [TestMethod]
    public void Update_CloseAndUpdateSameIssue_RemovesAndIgnoresUpdate()
    {
        var original = SetupIssueViz(DefaultFilePath);
        var updated = SetupIssueViz(DefaultFilePath, original.IssueId);
        var remove = original.IssueId!.Value;
        testSubject.Set([original], "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [], [updated], [remove]));

        testSubject.GetAll().Should().BeEmpty();
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([original], []));
    }

    [TestMethod]
    public void Update_UpdateAndAddSameIssue_UpdatesAndIgnoresAdd()
    {
        var original = SetupIssueViz(DefaultFilePath);
        var updated = SetupIssueViz(DefaultFilePath, original.IssueId);
        var add = SetupIssueViz(DefaultFilePath, original.IssueId);
        testSubject.Set([original], "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [add], [updated], []));

        testSubject.GetAll().Should().BeEquivalentTo(updated);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([original], [updated]));
    }

    [TestMethod]
    public void Update_RemoveAndAddSameIssue_Updates()
    {
        var original = SetupIssueViz(DefaultFilePath);
        var remove = original.IssueId!.Value;
        var add = SetupIssueViz(DefaultFilePath, original.IssueId);
        testSubject.Set([original], "some config scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("some config scope", [add], [], [remove]));

        testSubject.GetAll().Should().BeEquivalentTo(add);
        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([original], [add]));
    }

    [TestMethod]
    public void HandleTaintFileOpened_FileWithNoTaints_NoEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        documentTracker.DocumentOpened += Raise.EventWith(new DocumentEventArgs(new Document(DefaultFilePath, [])));

        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void HandleTaintFileOpened_FileWithTaints_EventRaisedWithAddedIssues()
    {
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        documentTracker.DocumentOpened += Raise.EventWith(new DocumentEventArgs(new Document(DefaultFilePath, [])));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], [taint]));
    }

    [TestMethod]
    public void HandleTaintFileClosed_FileWithNoTaints_NoEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;

        documentTracker.DocumentClosed += Raise.EventWith(new DocumentEventArgs(new Document(DefaultFilePath, [])));

        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void HandleTaintFileClosed_FileWithTaints_EventRaisedWithRemovedIssues()
    {
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        documentTracker.DocumentClosed += Raise.EventWith(new DocumentEventArgs(new Document(DefaultFilePath, [])));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([taint], []));
    }

    [TestMethod]
    public void HandleTaintFileRenamed_FileWithNoTaints_NoEventRaised()
    {
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.IssuesChanged += eventHandlerMock;
        var doc = new Document(DefaultFilePath, []);
        var args = new DocumentRenamedEventArgs(doc, DefaultFilePath);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(args);

        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void HandleTaintFileRenamed_FileWithTaints_EventRaisedWithRemovedIssues()
    {
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();
        var doc = new Document("new file path", []);

        documentTracker.OpenDocumentRenamed += Raise.EventWith(new DocumentRenamedEventArgs(doc, DefaultFilePath));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([taint], []));
    }

    [TestMethod]
    public void GetAll_NoOpenFiles_ReturnsEmpty()
    {
        documentTracker.GetOpenDocuments().Returns([]);
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");

        var result = testSubject.GetAll();

        result.Should().BeEmpty();
    }

    [TestMethod]
    public void GetAll_SomeTaintsNotInOpenFiles_ReturnsOnlyMatchingTaints()
    {
        var taint1 = SetupIssueViz(DefaultFilePath);
        const string otherfileCs = "otherfile.cs";
        var taint2 = SetupIssueViz(otherfileCs);
        testSubject.Set([taint1, taint2], "scope");

        var result = testSubject.GetAll();

        result.Should().BeEquivalentTo(taint1);

        documentTracker.GetOpenDocuments().Returns([new Document(DefaultFilePath, []), new Document(otherfileCs, [])]);

        result = testSubject.GetAll();

        result.Should().BeEquivalentTo(taint1, taint2);
    }

    [TestMethod]
    public void Update_AddTaintForClosedFile_NoEventRaisedAndNotInGetAll()
    {
        documentTracker.GetOpenDocuments().Returns([]);
        var taint = SetupIssueViz(DefaultFilePath);
        var eventHandlerMock = Substitute.For<EventHandler<IssuesChangedEventArgs>>();
        testSubject.Set([], "scope");
        testSubject.IssuesChanged += eventHandlerMock;

        testSubject.Update(new TaintVulnerabilitiesUpdate("scope", [taint], [], []));

        eventHandlerMock.DidNotReceiveWithAnyArgs().Invoke(default, default);
        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void Update_AddTaintsForMixedOpenAndClosedFiles_EventRaisedWithOnlyOpenFileTaints()
    {
        var closedFile = "closed.cs";
        var openTaint = SetupIssueViz(DefaultFilePath);
        var closedTaint = SetupIssueViz(closedFile);
        testSubject.Set([], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("scope", [openTaint, closedTaint], [], []));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([], [openTaint]));
        testSubject.GetAll().Should().BeEquivalentTo(openTaint);
    }

    [TestMethod]
    public void Update_UpdateTaintForClosedFile_NoEventRaisedAndNoChangeInGetAll()
    {
        documentTracker.GetOpenDocuments().Returns([]);
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();
        var updatedTaint = SetupIssueViz(DefaultFilePath, taint.IssueId);

        testSubject.Update(new TaintVulnerabilitiesUpdate("scope", [], [updatedTaint], []));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([taint], []));
        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void Update_UpdateTaintForOpenFile_EventRaisedAndTaintUpdatedInGetAll()
    {
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();
        var updatedTaint = SetupIssueViz(DefaultFilePath, taint.IssueId);

        testSubject.Update(new TaintVulnerabilitiesUpdate("scope", [], [updatedTaint], []));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([taint], [updatedTaint]));
        testSubject.GetAll().Should().BeEquivalentTo(updatedTaint);
    }

    [TestMethod]
    public void Update_RemoveTaintForClosedFile_EventRaisedAndTaintRemovedFromGetAll()
    {
        documentTracker.GetOpenDocuments().Returns([]);
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("scope", [], [], [taint.IssueId!.Value]));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([taint], []));
        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void Update_RemoveTaintForOpenFile_EventRaisedAndTaintRemovedFromGetAll()
    {
        var taint = SetupIssueViz(DefaultFilePath);
        testSubject.Set([taint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();

        testSubject.Update(new TaintVulnerabilitiesUpdate("scope", [], [], [taint.IssueId!.Value]));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([taint], []));
        testSubject.GetAll().Should().BeEmpty();
    }

    [TestMethod]
    public void Update_AddUpdateRemoveTaints_MixedOpenAndClosedFiles_EventReflectsOnlyOpenFileChanges()
    {
        var closedFile = "closed.cs";
        var openTaint = SetupIssueViz(DefaultFilePath);
        var closedTaint = SetupIssueViz(closedFile);
        testSubject.Set([openTaint, closedTaint], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();
        var newOpenTaint = SetupIssueViz(DefaultFilePath);
        var updatedOpenTaint = SetupIssueViz(DefaultFilePath, openTaint.IssueId);
        var updatedClosedTaint = SetupIssueViz(closedFile, closedTaint.IssueId);

        testSubject.Update(new TaintVulnerabilitiesUpdate("scope", [newOpenTaint], [updatedOpenTaint, updatedClosedTaint], [closedTaint.IssueId!.Value]));

        receivedEventGetter().Should().BeEquivalentTo(new IssuesChangedEventArgs([openTaint, closedTaint], [newOpenTaint, updatedOpenTaint]));
        testSubject.GetAll().Should().BeEquivalentTo(newOpenTaint, updatedOpenTaint);
    }

    [TestMethod]
    public void Set_RespectsOpenFilesForAddedIssues_ButAllRemovedIssuesReported()
    {
        var closedFile = "closed.cs";
        var openFileIssue = SetupIssueViz(DefaultFilePath);
        var closedFileIssue = SetupIssueViz(closedFile);
        testSubject.Set([openFileIssue, closedFileIssue], "scope");
        var receivedEventGetter = CaptureIssuesChangedEventArgs();
        var newOpenFileIssue = SetupIssueViz(DefaultFilePath);
        var newClosedFileIssue = SetupIssueViz(closedFile);

        testSubject.Set([newOpenFileIssue, newClosedFileIssue], "scope");

        var eventArgs = receivedEventGetter();
        eventArgs.RemovedIssues.Should().BeEquivalentTo(openFileIssue, closedFileIssue);
        eventArgs.AddedIssues.Should().BeEquivalentTo(newOpenFileIssue);
    }

    [TestMethod]
    public void Dispose_Called_EventHandlersUnsubscribed()
    {
        testSubject.Dispose();
        documentTracker.DocumentOpened -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.DocumentClosed -= Arg.Any<EventHandler<DocumentEventArgs>>();
        documentTracker.OpenDocumentRenamed -= Arg.Any<EventHandler<DocumentRenamedEventArgs>>();
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

    private static IAnalysisIssueVisualization SetupIssueViz(string filePath, Guid? id = null, string issueKey = null)
    {
        id ??= Guid.NewGuid();
        issueKey ??= Guid.NewGuid().ToString();

        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.IssueId.Returns(id.Value);
        var taintIssue = Substitute.For<ITaintIssue>();
        taintIssue.IssueServerKey.Returns(issueKey);
        issueViz.Issue.Returns(taintIssue);
        issueViz.CurrentFilePath.Returns(filePath);

        return issueViz;
    }
}
