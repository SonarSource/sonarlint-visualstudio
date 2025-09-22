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

using System.Collections.ObjectModel;
using System.ComponentModel;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class ReportViewModelTest
{
    private ReportViewModel testSubject;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IDependencyRisksStore dependencyRisksStore;
    private IHotspotsReportViewModel hotspotsReportViewModel;
    private ITaintsReportViewModel taintsReportViewModel;
    private IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler;
    private IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler;
    private INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
    private ILocationNavigator locationNavigator;
    private IMessageBox messageBox;
    private ITelemetryManager telemetryManager;
    private IThreadHandling threadHandling;
    private PropertyChangedEventHandler eventHandler;

    [TestInitialize]
    public void Initialize()
    {
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        hotspotsReportViewModel = Substitute.For<IHotspotsReportViewModel>();
        taintsReportViewModel = Substitute.For<ITaintsReportViewModel>();
        showDependencyRiskInBrowserHandler = Substitute.For<IShowDependencyRiskInBrowserHandler>();
        changeDependencyRiskStatusHandler = Substitute.For<IChangeDependencyRiskStatusHandler>();
        navigateToRuleDescriptionCommand = Substitute.For<INavigateToRuleDescriptionCommand>();
        locationNavigator = Substitute.For<ILocationNavigator>();
        messageBox = Substitute.For<IMessageBox>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        eventHandler = Substitute.For<PropertyChangedEventHandler>();
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns([]);
        taintsReportViewModel.GetTaintsGroupViewModels().Returns([]);

        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void Class_InheritsFromServerViewModel() => testSubject.Should().BeAssignableTo<ServerViewModel>();

    [TestMethod]
    public void Class_SubscribesToEvents()
    {
        hotspotsReportViewModel.Received(1).IssuesChanged += Arg.Any<EventHandler<IssuesChangedEventArgs>>();
        dependencyRisksStore.Received(1).DependencyRisksChanged += Arg.Any<EventHandler>();
        taintsReportViewModel.Received(1).IssuesChanged += Arg.Any<EventHandler<IssuesChangedEventArgs>>();
    }

    [TestMethod]
    public void Class_InitializesProperties()
    {
        testSubject.NavigateToRuleDescriptionCommand.Should().BeSameAs(navigateToRuleDescriptionCommand);
        testSubject.NavigateToLocationCommand.Should().NotBeNull();
    }

    [TestMethod]
    public void Ctor_InitializesDependencyRisks()
    {
        var dependencyRisk = CreateDependencyRisk();
        MockRisksInStore(dependencyRisk);

        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedDependencyRiskGroupViewModel(testSubject.GroupViewModels[0] as GroupDependencyRiskViewModel, dependencyRisk);
    }

    [TestMethod]
    public void Ctor_InitializesHotspots()
    {
        var hotspotGroupViewModel = CreateMockedGroupViewModel(filePath: "myFile.cs");
        var hotspotGroupViewModel2 = CreateMockedGroupViewModel(filePath: "myFile2.cs");
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns([hotspotGroupViewModel, hotspotGroupViewModel2]);

        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().HaveCount(2);
        testSubject.GroupViewModels.Should().Contain(hotspotGroupViewModel);
        testSubject.GroupViewModels.Should().Contain(hotspotGroupViewModel2);
    }

    [TestMethod]
    public void Ctor_InitializesTaints()
    {
        var taintGroupViewModel = CreateMockedGroupViewModel(filePath: "myFile.cs");
        var taintGroupViewModel2 = CreateMockedGroupViewModel(filePath: "myFile2.cs");
        taintsReportViewModel.GetTaintsGroupViewModels().Returns([taintGroupViewModel, taintGroupViewModel2]);

        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().HaveCount(2);
        testSubject.GroupViewModels.Should().Contain(taintGroupViewModel);
        testSubject.GroupViewModels.Should().Contain(taintGroupViewModel2);
    }

    [TestMethod]
    public void Ctor_MixedIssuesTypes_CreatesGroupViewModelsCorrectly()
    {
        var dependencyRisk = CreateDependencyRisk();
        MockRisksInStore(dependencyRisk);
        var hotspotGroupViewModel = CreateMockedGroupViewModel(filePath: "myFile.cs");
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns([hotspotGroupViewModel]);
        var taintGroupViewModel = CreateMockedGroupViewModel(filePath: "myFile2.cs");
        taintsReportViewModel.GetTaintsGroupViewModels().Returns([taintGroupViewModel]);

        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().HaveCount(3);
        VerifyExpectedDependencyRiskGroupViewModel(testSubject.GroupViewModels[0] as GroupDependencyRiskViewModel, dependencyRisk);
        testSubject.GroupViewModels.Should().Contain(hotspotGroupViewModel);
        testSubject.GroupViewModels.Should().Contain(taintGroupViewModel);
    }

    [TestMethod]
    public void Ctor_NoIssues_CreatesNoGroupViewModel()
    {
        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().BeEmpty();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        MockRisksInStore(CreateDependencyRisk(), CreateDependencyRisk());
        testSubject = CreateTestSubject();

        testSubject.Dispose();

        dependencyRisksStore.Received(1).DependencyRisksChanged -= Arg.Any<EventHandler>();
        hotspotsReportViewModel.Received(1).IssuesChanged -= Arg.Any<EventHandler<IssuesChangedEventArgs>>();
        hotspotsReportViewModel.Received(1).Dispose();
        taintsReportViewModel.Received(1).IssuesChanged -= Arg.Any<EventHandler<IssuesChangedEventArgs>>();
        taintsReportViewModel.Received(1).Dispose();
    }

    [TestMethod]
    public void SelectedItem_Initially_IsNull() => testSubject.SelectedItem.Should().BeNull();

    [TestMethod]
    public void SelectedItem_SetToDependencyRiskViewModel_CallsTelemetryForDependencyRisk()
    {
        var risk = CreateDependencyRisk();
        var riskViewModel = new DependencyRiskViewModel(risk);

        testSubject.SelectedItem = riskViewModel;

        testSubject.SelectedItem.Should().BeSameAs(riskViewModel);
        telemetryManager.Received(1).DependencyRiskInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToSameDependencyRiskViewModel_DoesNotCallTelemetry()
    {
        var riskViewModel = new DependencyRiskViewModel(CreateDependencyRisk());
        testSubject.SelectedItem = riskViewModel;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = riskViewModel;

        telemetryManager.DidNotReceive().DependencyRiskInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToDifferentDependencyRiskViewModel_CallsTelemetry()
    {
        var riskViewModel1 = new DependencyRiskViewModel(CreateDependencyRisk());
        var riskViewModel2 = new DependencyRiskViewModel(CreateDependencyRisk());
        testSubject.SelectedItem = riskViewModel1;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = riskViewModel2;

        testSubject.SelectedItem.Should().BeSameAs(riskViewModel2);
        telemetryManager.Received(1).DependencyRiskInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToIssueViewModel_DoesNotCallTelemetryForDependencyRisk()
    {
        var issueViewModel = Substitute.For<IIssueViewModel>();

        testSubject.SelectedItem = issueViewModel;

        testSubject.SelectedItem.Should().BeSameAs(issueViewModel);
        telemetryManager.DidNotReceive().DependencyRiskInvestigatedLocally();
        telemetryManager.DidNotReceive().HotspotInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToHotspotViewModel_CallsTelemetryForDependencyRisk()
    {
        var hotspotViewModel = new HotspotViewModel(CreateMockedHotspot());

        testSubject.SelectedItem = hotspotViewModel;

        testSubject.SelectedItem.Should().BeSameAs(hotspotViewModel);
        telemetryManager.Received(1).HotspotInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToSameHotspotViewModel_DoesNotCallTelemetry()
    {
        var hotspotViewModel = new HotspotViewModel(CreateMockedHotspot());
        testSubject.SelectedItem = hotspotViewModel;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = hotspotViewModel;

        telemetryManager.DidNotReceive().HotspotInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToDifferentHotspotViewModel_CallsTelemetry()
    {
        var hotspotViewModel1 = new HotspotViewModel(CreateMockedHotspot());
        var hotspotViewModel2 = new HotspotViewModel(CreateMockedHotspot());
        testSubject.SelectedItem = hotspotViewModel1;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = hotspotViewModel2;

        testSubject.SelectedItem.Should().BeSameAs(hotspotViewModel2);
        telemetryManager.Received(1).HotspotInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToNull_DoesNotCallTelemetry()
    {
        var riskViewModel1 = new DependencyRiskViewModel(CreateDependencyRisk());
        testSubject.SelectedItem = riskViewModel1;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = null;

        testSubject.SelectedItem.Should().BeNull();
        telemetryManager.DidNotReceive().DependencyRiskInvestigatedLocally();
        telemetryManager.DidNotReceive().HotspotInvestigatedLocally();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotsAdded_NoGroupExists_CreatesGroup()
    {
        ClearCallsForReportsViewModels();
        var hotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath: "myFile.cs");
        var hotspot2 = CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", filePath: "myFile.cs");

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], [hotspot, hotspot2]));

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, hotspot, hotspot2);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotAdded_GroupAlreadyExists_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", filePath);
        var newHotspotSameFile = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath);
        var existingGroup = CreateGroupFileViewModelWithHotspots(existingHotspot);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], [newHotspotSameFile]));

        testSubject.GroupViewModels.Should().HaveCount(1);
        testSubject.GroupViewModels.Single().Should().BeSameAs(existingGroup);
        VerifyExpectedGroupFileViewModel(existingGroup, existingHotspot, newHotspotSameFile);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotAddedToDifferentFile_CreatesNewGroup()
    {
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", "myFile.cs");
        var newHotspotDifferentFile = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", "myFile2.cs");
        var existingGroup = CreateGroupFileViewModelWithHotspots(existingHotspot);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], [newHotspotDifferentFile]));

        testSubject.GroupViewModels.Should().HaveCount(2);
        testSubject.GroupViewModels[0].Should().BeSameAs(existingGroup);
        VerifyExpectedGroupFileViewModel(testSubject.GroupViewModels[1] as GroupFileViewModel, newHotspotDifferentFile);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotRemoved_GroupHasJustOneIssue_RemovesGroup()
    {
        var filePath = "myFile.cs";
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath);
        var existingGroup = CreateGroupFileViewModelWithHotspots(existingHotspot);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new IssuesChangedEventArgs([CreateHotspotVisualization(existingHotspot.Issue.Id, existingHotspot.Issue.IssueServerKey, filePath)], []));

        testSubject.GroupViewModels.Should().BeEmpty();
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotRemoved_GroupHasMultipleIssue_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath);
        var existingHotspot2 = CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", filePath);
        var existingGroup = CreateGroupFileViewModelWithHotspots(existingHotspot, existingHotspot2);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new IssuesChangedEventArgs([CreateHotspotVisualization(existingHotspot.Issue.Id, existingHotspot.Issue.IssueServerKey, filePath)], []));

        testSubject.GroupViewModels.Should().HaveCount(1);
        testSubject.GroupViewModels[0].Should().BeSameAs(existingGroup);
        VerifyExpectedGroupFileViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, existingHotspot2);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotRemoved_NoGroupContainsIssue_DoesNothing()
    {
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", "myFile.cs");
        var existingGroup = CreateGroupFileViewModelWithHotspots(existingHotspot);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new IssuesChangedEventArgs([CreateHotspotVisualization(existingHotspot.Issue.Id, "notExistingKey", "myFile.cs")], []));

        testSubject.GroupViewModels.Should().HaveCount(1);
        testSubject.GroupViewModels[0].Should().BeSameAs(existingGroup);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintAdded_NoGroupExists_CreatesGroup()
    {
        ClearCallsForReportsViewModels();
        var taint = CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath: "myFile.cs");
        var taint2 = CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath: "myFile.cs");

        taintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], [taint, taint2]));

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, taint, taint2);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintAdded_GroupAlreadyExists_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingTaint = CreateTaintVisualization(Guid.NewGuid(), "serverKey2", filePath);
        var newTaintSameFile = CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath);
        var existingGroup = CreateGroupFileViewModelWithTaints(existingTaint);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        taintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], [newTaintSameFile]));

        testSubject.GroupViewModels.Should().HaveCount(1);
        testSubject.GroupViewModels.Single().Should().BeSameAs(existingGroup);
        VerifyExpectedGroupFileViewModel(existingGroup, existingTaint, newTaintSameFile);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintAddedToDifferentFile_CreatesNewGroup()
    {
        var existingTaint = CreateTaintVisualization(Guid.NewGuid(), "serverKey2", "myFile.cs");
        var newTaintDifferentFile = CreateTaintVisualization(Guid.NewGuid(), "serverKey", "myFile2.cs");
        var existingGroup = CreateGroupFileViewModelWithTaints(existingTaint);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        taintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], [newTaintDifferentFile]));

        testSubject.GroupViewModels.Should().HaveCount(2);
        testSubject.GroupViewModels[0].Should().BeSameAs(existingGroup);
        VerifyExpectedGroupFileViewModel(testSubject.GroupViewModels[1] as GroupFileViewModel, newTaintDifferentFile);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintRemoved_GroupHasJustOneIssue_RemovesGroup()
    {
        var filePath = "myFile.cs";
        var existingTaint = CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath);
        var existingGroup = CreateGroupFileViewModelWithTaints(existingTaint);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        taintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new IssuesChangedEventArgs([CreateTaintVisualization(existingTaint.Issue.Id, existingTaint.Issue.IssueServerKey, filePath)], []));

        testSubject.GroupViewModels.Should().BeEmpty();
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintRemoved_GroupHasMultipleIssue_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingTaint = CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath);
        var existingTaint2 = CreateTaintVisualization(Guid.NewGuid(), "serverKey2", filePath);
        var existingGroup = CreateGroupFileViewModelWithTaints(existingTaint, existingTaint2);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        taintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new IssuesChangedEventArgs([CreateTaintVisualization(existingTaint.Issue.Id, existingTaint.Issue.IssueServerKey, filePath)], []));

        testSubject.GroupViewModels.Should().HaveCount(1);
        testSubject.GroupViewModels[0].Should().BeSameAs(existingGroup);
        VerifyExpectedGroupFileViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, existingTaint2);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintRemoved_NoGroupContainsIssue_DoesNothing()
    {
        var existingTaint = CreateTaintVisualization(Guid.NewGuid(), "serverKey", "myFile.cs");
        var existingGroup = CreateGroupFileViewModelWithTaints(existingTaint);
        InitializeTestSubjectWithInitialGroup(existingGroup);

        taintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new IssuesChangedEventArgs([CreateTaintVisualization(existingTaint.Issue.Id, "notExistingKey", "myFile.cs")], []));

        testSubject.GroupViewModels.Should().HaveCount(1);
        testSubject.GroupViewModels[0].Should().BeSameAs(existingGroup);
        VerifyHasGroupsUpdated();
        dependencyRisksStore.DidNotReceive().GetAll();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
    }

    [TestMethod]
    public void DependencyRisksAddedInStore_NoGroupDependencyRisk_CreatesGroup()
    {
        var addedRisk = CreateDependencyRisk();
        dependencyRisksStore.GetAll().Returns([], [addedRisk]);
        testSubject = CreateTestSubject();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedDependencyRiskGroupViewModel(testSubject.GroupViewModels[0] as GroupDependencyRiskViewModel, addedRisk);
    }

    [TestMethod]
    public void DependencyRisksAddedInStore_GroupDependencyRiskExists_RefreshesDependencyRisk()
    {
        var initialRisk = CreateDependencyRisk();
        var addedRisk = CreateDependencyRisk();
        dependencyRisksStore.GetAll().Returns([initialRisk], [initialRisk, addedRisk]);
        testSubject = CreateTestSubject();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedDependencyRiskGroupViewModel(testSubject.GroupViewModels[0] as GroupDependencyRiskViewModel, initialRisk, addedRisk);
    }

    [TestMethod]
    public void DependencyRisksAddedInStore_DoesNotUpdateHotspots()
    {
        var addedRisk = CreateDependencyRisk();
        dependencyRisksStore.GetAll().Returns([], [addedRisk]);
        testSubject = CreateTestSubject();
        ClearCallsForReportsViewModels();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        hotspotsReportViewModel.DidNotReceive().GetHotspotsGroupViewModels();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void DependencyRisksRemovedFromStore_NoRisksAnymore_RemovesGroupDependencyRisk()
    {
        var initialRisk = CreateDependencyRisk();
        dependencyRisksStore.GetAll().Returns([initialRisk], new IDependencyRisk[] { });
        testSubject = CreateTestSubject();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.GroupViewModels.Should().BeEmpty();
    }

    [TestMethod]
    public void DependencyRisksRemovedFromStore_RefreshesDependencyRisk()
    {
        var initialRisk = CreateDependencyRisk();
        var initialRisk2 = CreateDependencyRisk();
        dependencyRisksStore.GetAll().Returns([initialRisk, initialRisk2], [initialRisk]);
        testSubject = CreateTestSubject();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedDependencyRiskGroupViewModel(testSubject.GroupViewModels[0] as GroupDependencyRiskViewModel, initialRisk);
    }

    [TestMethod]
    public void DependencyRisksRemovedFromStore_DoesNotUpdateHotspots()
    {
        var initialRisk = CreateDependencyRisk();
        dependencyRisksStore.GetAll().Returns([initialRisk], new IDependencyRisk[] { });
        testSubject = CreateTestSubject();
        ClearCallsForReportsViewModels();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        hotspotsReportViewModel.DidNotReceive().GetHotspotsGroupViewModels();
        taintsReportViewModel.DidNotReceive().GetTaintsGroupViewModels();
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void HasGroups_ReturnsTrue_WhenThereAreRisks()
    {
        MockRisksInStore(CreateDependencyRisk());
        testSubject = CreateTestSubject();

        testSubject.HasGroups.Should().BeTrue();
    }

    [TestMethod]
    public void HasGroups_ReturnsFalse_WhenThereAreNoRisks() => testSubject.HasGroups.Should().BeFalse();

    [TestMethod]
    public void NavigateToLocationCommand_NullParameter_CanExecuteReturnsFalse() => testSubject.NavigateToLocationCommand.CanExecute(null).Should().BeFalse();

    [TestMethod]
    public void NavigateToLocationCommand_NotAnalysisIssueViewModelParameter_CanExecuteReturnsFalse()
    {
        var viewModel = Substitute.For<IIssueViewModel>();

        testSubject.NavigateToLocationCommand.CanExecute(viewModel).Should().BeFalse();
    }

    [TestMethod]
    public void NavigateToLocationCommand_AnalysisIssueViewModelParameter_CanExecuteReturnsTrue()
    {
        var analysisIssueViewModel = Substitute.For<IAnalysisIssueViewModel>();

        testSubject.NavigateToLocationCommand.CanExecute(analysisIssueViewModel).Should().BeTrue();
    }

    [TestMethod]
    public void NavigateToLocationCommand_NavigatesToLocation()
    {
        var analysisIssueViewModel = Substitute.For<IAnalysisIssueViewModel>();

        testSubject.NavigateToLocationCommand.Execute(analysisIssueViewModel);

        locationNavigator.Received(1).TryNavigatePartial(analysisIssueViewModel.Issue);
    }

    private ReportViewModel CreateTestSubject()
    {
        var reportViewModel = new ReportViewModel(activeSolutionBoundTracker,
            navigateToRuleDescriptionCommand,
            locationNavigator,
            hotspotsReportViewModel,
            new DependencyRisksReportViewModel(dependencyRisksStore, showDependencyRiskInBrowserHandler, changeDependencyRiskStatusHandler, messageBox),
            taintsReportViewModel,
            telemetryManager,
            threadHandling);
        reportViewModel.PropertyChanged += eventHandler;
        return reportViewModel;
    }

    private static IDependencyRisk CreateDependencyRisk(Guid? id = null, bool isResolved = false)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Transitions.Returns([]);
        risk.Status.Returns(isResolved ? DependencyRiskStatus.Accepted : DependencyRiskStatus.Open);
        return risk;
    }

    private static IGroupViewModel CreateMockedGroupViewModel(string filePath)
    {
        var group = Substitute.For<IGroupViewModel>();
        group.Title.Returns(filePath);
        return group;
    }

    private GroupFileViewModel CreateGroupFileViewModelWithHotspots(params IAnalysisIssueVisualization[] analysisIssueVisualizations)
    {
        var issueViewModels = analysisIssueVisualizations.Select(x =>
        {
            var localHotspot = LocalHotspot.ToLocalHotspot(x);
            var issueViewModel = new HotspotViewModel(localHotspot);
            return issueViewModel;
        });
        var group = new GroupFileViewModel(analysisIssueVisualizations[0].Issue.PrimaryLocation.FilePath, new ObservableCollection<IIssueViewModel>(issueViewModels), threadHandling);
        return group;
    }

    private GroupFileViewModel CreateGroupFileViewModelWithTaints(params IAnalysisIssueVisualization[] analysisIssueVisualizations)
    {
        var issueViewModels = analysisIssueVisualizations.Select(x =>
        {
            var issueViewModel = new TaintViewModel(x);
            return issueViewModel;
        });
        var group = new GroupFileViewModel(analysisIssueVisualizations[0].Issue.PrimaryLocation.FilePath, new ObservableCollection<IIssueViewModel>(issueViewModels), threadHandling);
        return group;
    }

    private void MockRisksInStore(params IDependencyRisk[] dependencyRisks) => dependencyRisksStore.GetAll().Returns(dependencyRisks);

    private static void VerifyExpectedDependencyRiskGroupViewModel(GroupDependencyRiskViewModel dependencyRiskGroupVm, params IDependencyRisk[] expectedDependencyRisks)
    {
        dependencyRiskGroupVm.Should().NotBeNull();
        dependencyRiskGroupVm.FilteredIssues.Should().HaveCount(expectedDependencyRisks.Length);
        foreach (var expectedDependencyRisk in expectedDependencyRisks)
        {
            dependencyRiskGroupVm.FilteredIssues.Should().ContainSingle(vm => ((DependencyRiskViewModel)vm).DependencyRisk == expectedDependencyRisk);
        }
    }

    private void VerifyHasGroupsUpdated() => eventHandler.Received().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasGroups)));

    private void ClearCallsForReportsViewModels()
    {
        dependencyRisksStore.ClearReceivedCalls();
        taintsReportViewModel.ClearReceivedCalls();
        hotspotsReportViewModel.ClearReceivedCalls();
    }

    private static LocalHotspot CreateMockedHotspot() => new(Substitute.For<IAnalysisIssueVisualization>(), default, default);

    private static IAnalysisIssueVisualization CreateHotspotVisualization(Guid? id, string serverKey, string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.Issue.Returns(Substitute.For<IAnalysisHotspotIssue>());
        analysisIssueVisualization.Issue.Id.Returns(id);
        analysisIssueVisualization.Issue.IssueServerKey.Returns(serverKey);
        analysisIssueVisualization.Issue.PrimaryLocation.FilePath.Returns(filePath);
        return analysisIssueVisualization;
    }

    private static IAnalysisIssueVisualization CreateTaintVisualization(Guid? id, string serverKey, string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.Issue.Returns(Substitute.For<ITaintIssue>());
        analysisIssueVisualization.Issue.Id.Returns(id);
        analysisIssueVisualization.Issue.IssueServerKey.Returns(serverKey);
        analysisIssueVisualization.Issue.PrimaryLocation.FilePath.Returns(filePath);
        return analysisIssueVisualization;
    }

    private static void VerifyExpectedGroupFileViewModel(GroupFileViewModel groupFileViewModel, params IAnalysisIssueVisualization[] expectedAnalysisIssueVisualizations)
    {
        groupFileViewModel.Should().NotBeNull();
        groupFileViewModel.FilteredIssues.Should().HaveCount(expectedAnalysisIssueVisualizations.Length);
        foreach (var analysisIssueVisualization in expectedAnalysisIssueVisualizations)
        {
            groupFileViewModel.FilePath.Should().Be(analysisIssueVisualization.Issue.PrimaryLocation.FilePath);
            groupFileViewModel.FilteredIssues.Should().ContainSingle(vm => ((IAnalysisIssueViewModel)vm).Issue == analysisIssueVisualization);
        }
    }

    private void InitializeTestSubjectWithInitialGroup(params IGroupViewModel[] groupViewModels)
    {
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns(new ObservableCollection<IGroupViewModel>(groupViewModels));
        testSubject = CreateTestSubject();
        ClearCallsForReportsViewModels();
    }
}
