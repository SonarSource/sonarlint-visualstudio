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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.Infrastructure.VS;

namespace SonarLint.VisualStudio.IssueVisualization.UnitTests.Selection;

[TestClass]
public class AnalysisIssueSelectionServiceTests
{
    private IVsMonitorSelection monitorSelection;
    private IIssueSelectionService selectionService;
    private IVsUIServiceOperation uiServiceOperation;
    private AnalysisIssueSelectionService testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        monitorSelection = Substitute.For<IVsMonitorSelection>();
        selectionService = Substitute.For<IIssueSelectionService>();
        uiServiceOperation = Substitute.For<IVsUIServiceOperation>();

        uiServiceOperation.When(x => x.Execute<SVsShellMonitorSelection, IVsMonitorSelection>(Arg.Any<Action<IVsMonitorSelection>>()))
            .Do(x =>
            {
                var action = x.Arg<Action<IVsMonitorSelection>>();
                action(monitorSelection);
            });

        testSubject = new AnalysisIssueSelectionService(uiServiceOperation, selectionService);
    }

    [TestMethod]
    public void MefCtor_CheckIsExported() =>
        MefTestHelpers.CheckTypeCanBeImported<AnalysisIssueSelectionService, IAnalysisIssueSelectionService>(
            MefTestHelpers.CreateExport<IVsUIServiceOperation>(Substitute.For<IVsUIServiceOperation>()),
            MefTestHelpers.CreateExport<IIssueSelectionService>());

    [TestMethod]
    public void MefCtor_CheckIsSingleton() => MefTestHelpers.CheckIsSingletonMefComponent<AnalysisIssueSelectionService>();

    [TestMethod]
    public void Ctor_RegisterToIssueSelectionEvent()
    {
        selectionService.Received().SelectedIssueChanged += Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void Dispose_UnregisterFromIssueSelectionEvent()
    {
        testSubject.Dispose();
        testSubject.Dispose();
        testSubject.Dispose();

        selectionService.Received(1).SelectedIssueChanged -= Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void SetSelectedIssue_IssueIsNull_UiContextIsHidden()
    {
        var cookie = SetupContextMock();

        testSubject.SelectedIssue = null;

        monitorSelection.Received(1).SetCmdUIContext(cookie, 0);
    }

    [TestMethod]
    public void SetSelectedIssue_IssueIsNotNull_UiContextIsShown()
    {
        var cookie = SetupContextMock();

        testSubject.SelectedIssue = Substitute.For<IAnalysisIssueVisualization>();

        monitorSelection.Received(1).SetCmdUIContext(cookie, 1);
    }

    [TestMethod]
    public void SetSelectedIssue_FailsToGetContext_NoException()
    {
        SetupContextMock(VSConstants.E_FAIL);

        Action act = () => testSubject.SelectedIssue = Substitute.For<IAnalysisIssueVisualization>();

        act.Should().NotThrow();
        monitorSelection.DidNotReceive().SetCmdUIContext(Arg.Any<uint>(), Arg.Any<int>());
    }

    [TestMethod]
    public void SetSelectedIssue_NoSubscribers_NoException()
    {
        Action act = () => testSubject.SelectedIssue = Substitute.For<IAnalysisIssueVisualization>();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void SetSelectedFlow_NoSubscribers_NoException()
    {
        Action act = () => testSubject.SelectedFlow = Substitute.For<IAnalysisIssueFlowVisualization>();

        act.Should().NotThrow();
    }

    [TestMethod]
    public void SetSelectedLocation_NoSubscribers_NoException()
    {
        Action act = () => testSubject.SelectedLocation = Substitute.For<IAnalysisIssueLocationVisualization>();

        act.Should().NotThrow();
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetSelectedIssue_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelIssue(bool isNewIssueNull)
    {
        var eventHandler = Substitute.For<EventHandler<SelectionChangedEventArgs>>();
        testSubject.SelectionChanged += eventHandler;
        var expectedIssue = isNewIssueNull ? null : Substitute.For<IAnalysisIssueVisualization>();

        testSubject.SelectedIssue = expectedIssue;

        eventHandler.Received(1).Invoke(testSubject,
            Arg.Is<SelectionChangedEventArgs>(args => args.SelectedIssue == expectedIssue &&
                                                      args.SelectionChangeLevel == SelectionChangeLevel.Issue));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetSelectedFlow_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelFlow(bool isNewFlowNull)
    {
        var eventHandler = Substitute.For<EventHandler<SelectionChangedEventArgs>>();
        testSubject.SelectionChanged += eventHandler;
        var expectedFlow = isNewFlowNull ? null : Substitute.For<IAnalysisIssueFlowVisualization>();

        testSubject.SelectedFlow = expectedFlow;

        eventHandler.Received(1).Invoke(testSubject,
            Arg.Is<SelectionChangedEventArgs>(args => args.SelectedFlow == expectedFlow &&
                                                      args.SelectionChangeLevel == SelectionChangeLevel.Flow));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetSelectedLocation_HasSubscribers_RaisesSelectionChangedEventWithChangeLevelLocation(bool isNewLocationNull)
    {
        var eventHandler = Substitute.For<EventHandler<SelectionChangedEventArgs>>();
        testSubject.SelectionChanged += eventHandler;
        var expectedLocation = isNewLocationNull ? null : Substitute.For<IAnalysisIssueLocationVisualization>();

        testSubject.SelectedLocation = expectedLocation;

        eventHandler.Received(1).Invoke(testSubject,
            Arg.Is<SelectionChangedEventArgs>(args => args.SelectedLocation == expectedLocation &&
                                                      args.SelectionChangeLevel == SelectionChangeLevel.Location));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetSelectedIssue_ReturnsValue(bool isNewIssueNull)
    {
        testSubject.SelectedIssue.Should().BeNull();
        var expectedIssue = isNewIssueNull ? null : Substitute.For<IAnalysisIssueVisualization>();

        testSubject.SelectedIssue = expectedIssue;

        testSubject.SelectedIssue.Should().Be(expectedIssue);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetSelectedFlow_ReturnsValue(bool isNewFlowNull)
    {
        testSubject.SelectedFlow.Should().BeNull();
        var expectedFlow = isNewFlowNull ? null : Substitute.For<IAnalysisIssueFlowVisualization>();

        testSubject.SelectedFlow = expectedFlow;

        testSubject.SelectedFlow.Should().Be(expectedFlow);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void GetSelectedLocation_ReturnsValue(bool isNewLocationNull)
    {
        testSubject.SelectedLocation.Should().BeNull();
        var expectedLocation = isNewLocationNull ? null : Substitute.For<IAnalysisIssueLocationVisualization>();

        testSubject.SelectedLocation = expectedLocation;

        testSubject.SelectedLocation.Should().Be(expectedLocation);
    }

    [TestMethod]
    public void Dispose_HasSubscribers_RemovesSubscribers()
    {
        var eventHandler = Substitute.For<EventHandler<SelectionChangedEventArgs>>();
        testSubject.SelectionChanged += eventHandler;

        testSubject.Dispose();
        testSubject.SelectedIssue = Substitute.For<IAnalysisIssueVisualization>();

        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    [TestMethod]
    public void SelectedFlowChanged_ChangesSelectedLocation()
    {
        var eventHandler = Substitute.For<EventHandler<SelectionChangedEventArgs>>();
        testSubject.SelectionChanged += eventHandler;

        // Set flow to value
        var firstFlowFirstLocation = Substitute.For<IAnalysisIssueLocationVisualization>();
        var firstFlow = CreateFlow(firstFlowFirstLocation);
        testSubject.SelectedFlow = firstFlow;
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SelectionChangedEventArgs>(args => args.SelectedLocation == firstFlowFirstLocation));
        testSubject.SelectedLocation.Should().Be(firstFlowFirstLocation);

        // Set flow to null
        testSubject.SelectedFlow = null;
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SelectionChangedEventArgs>(args => args.SelectedLocation == null));
        testSubject.SelectedLocation.Should().BeNull();

        // Set flow to a different value
        var secondFlowFirstLocation = Substitute.For<IAnalysisIssueLocationVisualization>();
        var secondFlow = CreateFlow(secondFlowFirstLocation);
        testSubject.SelectedFlow = secondFlow;
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SelectionChangedEventArgs>(args => args.SelectedLocation == secondFlowFirstLocation));
        testSubject.SelectedLocation.Should().Be(secondFlowFirstLocation);
    }

    [TestMethod]
    public void SelectedIssueChanged_ChangesSelectedFlow()
    {
        var eventHandler = Substitute.For<EventHandler<SelectionChangedEventArgs>>();
        testSubject.SelectionChanged += eventHandler;

        // Set issue to value
        var firstIssueFirstFlow = CreateFlow();
        var firstIssue = CreateIssue(firstIssueFirstFlow);
        testSubject.SelectedIssue = firstIssue;
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SelectionChangedEventArgs>(args => args.SelectedFlow == firstIssueFirstFlow));
        testSubject.SelectedFlow.Should().Be(firstIssueFirstFlow);

        // Set issue to null
        testSubject.SelectedIssue = null;
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SelectionChangedEventArgs>(args => args.SelectedFlow == null));
        testSubject.SelectedFlow.Should().BeNull();

        // Set issue to different value
        var secondIssueFirstFlow = CreateFlow();
        var secondIssue = CreateIssue(secondIssueFirstFlow);
        testSubject.SelectedIssue = secondIssue;
        eventHandler.Received(1).Invoke(testSubject, Arg.Is<SelectionChangedEventArgs>(args => args.SelectedFlow == secondIssueFirstFlow));
        testSubject.SelectedFlow.Should().Be(secondIssueFirstFlow);
    }

    [TestMethod]
    public void IssueSelectionServiceEvent_IssueHasSecondaryLocations_SelectedIssueIsSet()
    {
        var location = Substitute.For<IAnalysisIssueLocationVisualization>();
        var flow = CreateFlow(location);
        var issue = CreateIssue(flow);

        RaiseSelectedIssueChangedEvent(issue);

        testSubject.SelectedIssue.Should().Be(issue);
        testSubject.SelectedFlow.Should().Be(flow);
        testSubject.SelectedLocation.Should().Be(location);
    }

    [TestMethod]
    public void IssueSelectionServiceEvent_IssueHasNoSecondaryLocations_SelectedIssueIsCleared()
    {
        var issue = CreateIssue();
        var oldSelection = Substitute.For<IAnalysisIssueVisualization>();
        testSubject.SelectedIssue = oldSelection;
        testSubject.SelectedIssue.Should().Be(oldSelection);

        RaiseSelectedIssueChangedEvent(issue);

        testSubject.SelectedIssue.Should().BeNull();
        testSubject.SelectedFlow.Should().BeNull();
        testSubject.SelectedLocation.Should().BeNull();
    }

    [TestMethod]
    public void IssueSelectionServiceEvent_IssueIsNull_SelectedIssueIsCleared()
    {
        var oldSelection = Substitute.For<IAnalysisIssueVisualization>();
        testSubject.SelectedIssue = oldSelection;
        testSubject.SelectedIssue.Should().Be(oldSelection);

        RaiseSelectedIssueChangedEvent(null);

        testSubject.SelectedIssue.Should().BeNull();
        testSubject.SelectedFlow.Should().BeNull();
        testSubject.SelectedLocation.Should().BeNull();
    }

    private void RaiseSelectedIssueChangedEvent(IAnalysisIssueVisualization issue)
    {
        selectionService.SelectedIssue.Returns(issue);
        selectionService.SelectedIssueChanged += Raise.Event<EventHandler>(null, EventArgs.Empty);
    }

    private IAnalysisIssueVisualization CreateIssue(params IAnalysisIssueFlowVisualization[] flows)
    {
        var issue = Substitute.For<IAnalysisIssueVisualization>();
        issue.Flows.Returns(flows);
        return issue;
    }

    private IAnalysisIssueFlowVisualization CreateFlow(params IAnalysisIssueLocationVisualization[] locations)
    {
        var flow = Substitute.For<IAnalysisIssueFlowVisualization>();
        flow.Locations.Returns(locations);
        return flow;
    }

    private uint SetupContextMock(int result = VSConstants.S_OK)
    {
        uint cookie = 0;
        monitorSelection
            .GetCmdUIContextCookie(ref Arg.Any<Guid>(), out cookie)
            .Returns(x =>
            {
                x[1] = cookie;
                return result;
            });
        return cookie;
    }
}
