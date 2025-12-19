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

using System.Collections.ObjectModel;
using System.ComponentModel;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Issues;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class ReportViewModelTest
{
    private ReportViewModel testSubject;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IDependencyRisksReportViewModel dependencyRisksReportViewModel;
    private IHotspotsReportViewModel hotspotsReportViewModel;
    private ITaintsReportViewModel taintsReportViewModel;
    private IFileAwareTaintsReportViewModel fileAwareTaintsReportViewModel;
    private IIssuesReportViewModel issuesReportViewModel;
    private INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
    private ILocationNavigator locationNavigator;
    private ITelemetryManager telemetryManager;
    private IThreadHandling threadHandling;
    private PropertyChangedEventHandler eventHandler;
    private IIssueSelectionService selectionService;
    private IActiveDocumentLocator activeDocumentLocator;
    private IActiveDocumentTracker activeDocumentTracker;
    private IDocumentTracker documentTracker;
    private IFocusOnNewCodeServiceUpdater focusOnNewCodeService;

    [TestInitialize]
    public void Initialize()
    {
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        activeSolutionBoundTracker.CurrentConfiguration.Returns(BindingConfiguration.Standalone);
        dependencyRisksReportViewModel = Substitute.For<IDependencyRisksReportViewModel>();
        hotspotsReportViewModel = Substitute.For<IHotspotsReportViewModel>();
        taintsReportViewModel = Substitute.For<ITaintsReportViewModel>();
        fileAwareTaintsReportViewModel = Substitute.For<IFileAwareTaintsReportViewModel>();
        issuesReportViewModel = Substitute.For<IIssuesReportViewModel>();
        navigateToRuleDescriptionCommand = Substitute.For<INavigateToRuleDescriptionCommand>();
        locationNavigator = Substitute.For<ILocationNavigator>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        selectionService = Substitute.For<IIssueSelectionService>();
        activeDocumentLocator = Substitute.For<IActiveDocumentLocator>();
        activeDocumentTracker = Substitute.For<IActiveDocumentTracker>();
        eventHandler = Substitute.For<PropertyChangedEventHandler>();
        documentTracker = Substitute.For<IDocumentTracker>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([]);
        hotspotsReportViewModel.GetIssueViewModels().Returns([]);
        taintsReportViewModel.GetIssueViewModels().Returns([]);
        fileAwareTaintsReportViewModel.GetIssueViewModels().Returns([]);
        issuesReportViewModel.GetIssueViewModels().Returns([]);
        focusOnNewCodeService = Substitute.For<IFocusOnNewCodeServiceUpdater>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));

        CreateTestSubject();
    }

    [TestMethod]
    public void Class_InheritsFromServerViewModel() => testSubject.Should().BeAssignableTo<ServerViewModel>();

    [TestMethod]
    public void Class_SubscribesToEvents()
    {
        hotspotsReportViewModel.Received(1).IssuesChanged += Arg.Any<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
        dependencyRisksReportViewModel.Received(1).DependencyRisksChanged += Arg.Any<EventHandler>();
        taintsReportViewModel.Received(1).IssuesChanged += Arg.Any<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
        issuesReportViewModel.Received(1).IssuesChanged += Arg.Any<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
    }

    [TestMethod]
    public void Class_InitializesProperties()
    {
        testSubject.NavigateToRuleDescriptionCommand.Should().BeSameAs(navigateToRuleDescriptionCommand);
    }

    [TestMethod]
    public void Ctor_InitializesDependencyRisks()
    {
        var dependencyRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([dependencyRisk]);

        CreateTestSubject();

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as SolutionFindingsGroupViewModel, dependencyRisk); // todo taints
    }

    [TestMethod]
    public void Ctor_MixedIssuesTypes_CreatesGroupViewModelsCorrectly()
    {
        var dependencyRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([dependencyRisk]);

        var filePath = "myFile.cs";
        var filePath2 = "myFile2.cs";

        var file1Hotspot = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "any", filePath));
        var file2Hotspot = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "any", filePath2));
        hotspotsReportViewModel.GetIssueViewModels().Returns([file1Hotspot, file2Hotspot]);

        var solutionLevelTaint = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "any", filePath));
        taintsReportViewModel.GetIssueViewModels().Returns([solutionLevelTaint]);

        var file1Taint = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "any", filePath));
        fileAwareTaintsReportViewModel.GetIssueViewModels().Returns([file1Taint]);

        var file1Issue = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), filePath));
        var file2Issue = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(),  filePath2));
        issuesReportViewModel.GetIssueViewModels().Returns([file1Issue, file2Issue]);

        InitializeTestSubjectWithInitialGroup(filePath, filePath2, "myFile3.cs");

        CreateTestSubject();

        testSubject.FilteredGroupViewModels.Should().HaveCount(4);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as SolutionFindingsGroupViewModel, dependencyRisk, solutionLevelTaint);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[1], file1Hotspot, file1Taint, file1Issue);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[2], file2Hotspot, file2Issue);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[3]);
    }

    [TestMethod]
    public void Ctor_NoIssues_CreatesNoGroupViewModel()
    {
        CreateTestSubject();

        testSubject.FilteredGroupViewModels.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_InitializesActiveDocument()
    {
        activeDocumentLocator.Received(1).FindActiveDocument();
        activeDocumentTracker.Received(1).ActiveDocumentChanged += Arg.Any<EventHandler<ActiveDocumentChangedEventArgs>>();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        CreateTestSubject();

        testSubject.Dispose();

        dependencyRisksReportViewModel.Received(1).DependencyRisksChanged -= Arg.Any<EventHandler>();
        dependencyRisksReportViewModel.Received(1).Dispose();
        hotspotsReportViewModel.Received(1).IssuesChanged -= Arg.Any<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
        hotspotsReportViewModel.Received(1).Dispose();
        taintsReportViewModel.Received(1).IssuesChanged -= Arg.Any<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
        taintsReportViewModel.Received(1).Dispose();
        issuesReportViewModel.Received(1).IssuesChanged -= Arg.Any<EventHandler<ViewModelAnalysisIssuesChangedEventArgs>>();
        issuesReportViewModel.Received(1).Dispose();
        activeDocumentTracker.Received(1).ActiveDocumentChanged -= Arg.Any<EventHandler<ActiveDocumentChangedEventArgs>>();
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
    public void SelectedItem_SetToHotspotViewModel_CallsTelemetryForHotspot()
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
    public void SelectedItem_SetToTaintRiskViewModel_CallsTelemetryForTaint()
    {
        var taint = CreateTaintVisualization(default, default, default);
        var riskViewModel = new TaintViewModel(taint);

        testSubject.SelectedItem = riskViewModel;

        testSubject.SelectedItem.Should().BeSameAs(riskViewModel);
        telemetryManager.Received(1).TaintIssueInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToSameTaintRiskViewModel_DoesNotCallTelemetry()
    {
        var taintViewModel = new TaintViewModel(CreateTaintVisualization(default, default, default));
        testSubject.SelectedItem = taintViewModel;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = taintViewModel;

        telemetryManager.DidNotReceive().TaintIssueInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToDifferentTaintRiskViewModel_CallsTelemetry()
    {
        var taintViewModel1 = new TaintViewModel(CreateTaintVisualization(default, default, default));
        var taintViewModel2 = new TaintViewModel(CreateTaintVisualization(default, default, default));
        testSubject.SelectedItem = taintViewModel1;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = taintViewModel2;

        testSubject.SelectedItem.Should().BeSameAs(taintViewModel2);
        telemetryManager.Received(1).TaintIssueInvestigatedLocally();
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
        telemetryManager.DidNotReceive().TaintIssueInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_CallsSelectionService()
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var viewModel = Substitute.For<IAnalysisIssueViewModel>();
        viewModel.Issue.Returns(analysisIssueVisualization);

        testSubject.SelectedItem = viewModel;

        selectionService.Received(1).SelectedIssue = analysisIssueVisualization;
    }

    [TestMethod]
    public void SelectedItem_SetToSameViewModel_CallsSelectionService()
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var viewModel = Substitute.For<IAnalysisIssueViewModel>();
        viewModel.Issue.Returns(analysisIssueVisualization);

        testSubject.SelectedItem = viewModel;
        testSubject.SelectedItem = viewModel;

        selectionService.Received(1).SelectedIssue = analysisIssueVisualization;
    }

    [TestMethod]
    public void HotspotsChanged_HotspotsAdded_NoGroupExists_CreatesGroup()
    {
        ClearCallsForReportsViewModels();
        var hotspot = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath: "myFile.cs"));
        var hotspot2 = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", filePath: "myFile.cs"));

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([hotspot, hotspot2], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel, hotspot, hotspot2);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotAdded_GroupAlreadyExists_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var hotspotViewModel = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", filePath));
        hotspotsReportViewModel.GetIssueViewModels().Returns([hotspotViewModel]);
        InitializeTestSubjectWithInitialGroup(filePath);
        var newHotspotSameFile = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath));

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([newHotspotSameFile], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], hotspotViewModel, newHotspotSameFile);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotAddedToDifferentFile_CreatesNewGroup()
    {
        var filePath = "myFile.cs";
        var existingHotspot = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", filePath));
        hotspotsReportViewModel.GetIssueViewModels().Returns([existingHotspot]);
        InitializeTestSubjectWithInitialGroup(filePath);
        var newHotspotDifferentFile = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "serverKey", "myFile2.cs"));

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(newHotspotDifferentFile)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel, existingHotspot);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[1] as GroupFileViewModel, newHotspotDifferentFile);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotRemoved_GroupHasJustOneIssue_KeepsGroup()
    {
        var filePath = "myFile.cs";
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath);
        var hotspotViewModel = CreateHotspotViewModel(existingHotspot);
        hotspotsReportViewModel.GetIssueViewModels().Returns([hotspotViewModel]);
        InitializeTestSubjectWithInitialGroup(filePath);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [existingHotspot.Issue.Id]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotRemoved_GroupHasMultipleIssue_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath);
        var existingHotspot2 = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "serverKey2", filePath));
        IEnumerable<IIssueViewModel> issueViewModels = [CreateHotspotViewModel(existingHotspot), existingHotspot2];
        hotspotsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [existingHotspot.Issue.Id]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel, existingHotspot2);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void HotspotsChanged_HotspotRemoved_NoGroupContainsIssue_DoesNothing()
    {
        var filePath = "myFile.cs";
        var existingHotspot = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath);
        InitializeTestSubjectWithInitialGroup(filePath);

        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [Guid.NewGuid()]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0]);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintAdded_NoGroupExists_CreatesGroup()
    {
        ClearCallsForReportsViewModels();
        var taint = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath: "myFile.cs"));
        var taint2 = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath: "myFile.cs"));

        fileAwareTaintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([taint, (taint2)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel, taint, taint2);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintAdded_GroupAlreadyExists_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingTaint = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey2", filePath));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingTaint)];
        fileAwareTaintsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);
        var newTaintSameFile = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath));

        fileAwareTaintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(newTaintSameFile)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], existingTaint, newTaintSameFile);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintAddedToDifferentFile_CreatesNewGroup()
    {
        var filePath = "myFile.cs";
        var existingTaint = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey2", filePath));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingTaint)];
        fileAwareTaintsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);
        var newTaintDifferentFile = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey", "myFile2.cs"));

        fileAwareTaintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(newTaintDifferentFile)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel, existingTaint);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[1] as GroupFileViewModel, newTaintDifferentFile);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintRemoved_GroupHasJustOneIssue_KeepsGroup()
    {
        var filePath = "myFile.cs";
        var existingTaint = CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath);
        IEnumerable<IIssueViewModel> issueViewModels = [CreateTaintViewModel(existingTaint)];
        taintsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);

        taintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [existingTaint.Issue.Id]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0]);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintRemoved_GroupHasMultipleIssue_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingTaintIssueId = Guid.NewGuid();
        var existingTaint = CreateTaintViewModel(CreateTaintVisualization(existingTaintIssueId, "serverKey", filePath));
        var existingTaint2 = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey2", filePath));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingTaint), (existingTaint2)];
        fileAwareTaintsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);

        fileAwareTaintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [existingTaintIssueId]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], existingTaint2);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void TaintsChanged_TaintRemoved_NoGroupContainsIssue_DoesNothing()
    {
        var myfileCs = "myFile.cs";
        var existingTaint = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "serverKey", myfileCs));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingTaint)];
        fileAwareTaintsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(myfileCs);

        fileAwareTaintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [Guid.NewGuid()]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], existingTaint);
        VerifyHasGroupsUpdated();
        dependencyRisksReportViewModel.DidNotReceive().GetDependencyRiskViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void IssuesChanged_IssuesAdded_NoGroupExists_CreatesGroup()
    {
        ClearCallsForReportsViewModels();
        var issue = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), filePath: "myFile.cs"));
        var issue2 = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), filePath: "myFile.cs"));

        issuesReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(issue), (issue2)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel, issue, issue2);
        VerifyHasGroupsUpdated();
        issuesReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void IssuesChanged_TaintAdded_GroupAlreadyExists_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingIssue = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), filePath));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingIssue)];
        issuesReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);
        var newIssueSameFile = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), filePath));

        issuesReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(newIssueSameFile)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], existingIssue, newIssueSameFile);
        VerifyHasGroupsUpdated();
        issuesReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void IssuesChanged_IssueAddedToDifferentFile_CreatesNewGroup()
    {
        var filePath = "myFile.cs";
        var existingIssue = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), filePath));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingIssue)];
        issuesReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);
        var newIssueDifferentFile = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), "myFile2.cs"));

        issuesReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(newIssueDifferentFile)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as GroupFileViewModel, existingIssue);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[1] as GroupFileViewModel, newIssueDifferentFile);
        VerifyHasGroupsUpdated();
        issuesReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void IssuesChanged_IssueRemoved_GroupHasJustOneIssue_KeepsGroup()
    {
        var filePath = "myFile.cs";
        var existingIssue = CreateIssueVisualization(Guid.NewGuid(), filePath);
        IEnumerable<IIssueViewModel> issueViewModels = [CreateIssueViewModel(existingIssue)];
        issuesReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);

        issuesReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [existingIssue.Issue.Id]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0]);
        VerifyHasGroupsUpdated();
        issuesReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void IssuesChanged_IssueRemoved_GroupHasMultipleIssue_UpdatesGroup()
    {
        var filePath = "myFile.cs";
        var existingIssueId = Guid.NewGuid();
        var existingIssue = CreateIssueViewModel(CreateIssueVisualization(existingIssueId, filePath));
        var existingIssue2 = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), filePath));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingIssue), (existingIssue2)];
        issuesReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);

        issuesReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [existingIssueId]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], existingIssue2);
        VerifyHasGroupsUpdated();
        issuesReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void IssuesChanged_IssueRemoved_NoGroupContainsIssue_DoesNothing()
    {
        var myfileCs = "myFile.cs";
        var existingIssue = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), myfileCs));
        IEnumerable<IIssueViewModel> issueViewModels = [(existingIssue)];
        issuesReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(myfileCs);

        issuesReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([], [Guid.NewGuid()]));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], existingIssue);
        VerifyHasGroupsUpdated();
        issuesReportViewModel.DidNotReceive().GetIssueViewModels();
    }

    [TestMethod]
    public void IssuesChanged_MultipleFindingTypes_HasAllInOneGroup()
    {
        var myfileCs = "myFile.cs";
        var hotspot = CreateHotspotViewModel(CreateHotspotVisualization(Guid.NewGuid(), "any", myfileCs));
        var issue = CreateIssueViewModel(CreateIssueVisualization(Guid.NewGuid(), myfileCs));
        var taint = CreateTaintViewModel(CreateTaintVisualization(Guid.NewGuid(), "any", myfileCs));
        InitializeTestSubjectWithInitialGroup(myfileCs);

        issuesReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(issue)], []));
        hotspotsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(hotspot)], []));
        fileAwareTaintsReportViewModel.IssuesChanged += Raise.EventWith(testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([(taint)], []));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel((GroupFileViewModel)testSubject.FilteredGroupViewModels[0], issue, hotspot, taint);
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void DependencyRisksAddedInStore_NoGroupDependencyRisk_CreatesGroup()
    {
        var addedRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([addedRisk]);
        CreateTestSubject();

        dependencyRisksReportViewModel.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as SolutionFindingsGroupViewModel, addedRisk);
    }

    [TestMethod]
    public void DependencyRisksAddedInStore_GroupDependencyRiskExists_RefreshesDependencyRisk()
    {
        var initialRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        var addedRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([initialRisk], [initialRisk, addedRisk]);
        CreateTestSubject();

        dependencyRisksReportViewModel.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as SolutionFindingsGroupViewModel, initialRisk, addedRisk);
    }

    [TestMethod]
    public void DependencyRisksAddedInStore_DoesNotUpdateHotspots()
    {
        var addedRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([addedRisk]);
        CreateTestSubject();
        ClearCallsForReportsViewModels();

        dependencyRisksReportViewModel.DependencyRisksChanged += Raise.Event<EventHandler>();

        hotspotsReportViewModel.DidNotReceive().GetIssueViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void DependencyRisksRemovedFromStore_NoRisksAnymore_RemovesGroupDependencyRisk()
    {
        var initialRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([initialRisk], [[]]);
        CreateTestSubject();

        dependencyRisksReportViewModel.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.FilteredGroupViewModels.Should().BeEmpty();
    }

    [TestMethod]
    public void DependencyRisksRemovedFromStore_RefreshesDependencyRisk()
    {
        var initialRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        var initialRisk2 = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([initialRisk, initialRisk2], [initialRisk]);
        CreateTestSubject();

        dependencyRisksReportViewModel.DependencyRisksChanged += Raise.Event<EventHandler>();

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as SolutionFindingsGroupViewModel, initialRisk);
    }

    [TestMethod]
    public void DependencyRisksRemovedFromStore_DoesNotUpdateHotspots()
    {
        var initialRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([initialRisk], [[]]);
        CreateTestSubject();
        ClearCallsForReportsViewModels();

        dependencyRisksReportViewModel.DependencyRisksChanged += Raise.Event<EventHandler>();

        hotspotsReportViewModel.DidNotReceive().GetIssueViewModels();
        taintsReportViewModel.DidNotReceive().GetIssueViewModels();
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void TaintsChanged_SolutionLevelTaints_HandlesAddedAndRemovedInSingleEvent()
    {
        var taint1Visualization = CreateTaintVisualization(Guid.NewGuid(), "taintKey1", filePath: null);
        var taint2Visualization = CreateTaintVisualization(Guid.NewGuid(), "taintKey2", filePath: null);
        var taint1 = CreateTaintViewModel(taint1Visualization);
        var taint2 = CreateTaintViewModel(taint2Visualization);
        taintsReportViewModel.GetIssueViewModels().Returns([taint1]);
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([]);
        CreateTestSubject();
        ClearCallsForReportsViewModels();

        taintsReportViewModel.IssuesChanged += Raise.EventWith(
            testSubject,
            new ViewModelAnalysisIssuesChangedEventArgs([taint2], [taint1.Id])
        );

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyExpectedGroupFileViewModel(testSubject.FilteredGroupViewModels[0] as SolutionFindingsGroupViewModel, taint2);
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void HasGroups_ReturnsTrue_WhenThereAreRisks()
    {
        var initialRisk = CreateDependencyRiskViewModel(CreateDependencyRisk());
        dependencyRisksReportViewModel.GetDependencyRiskViewModels().Returns([initialRisk]);
        CreateTestSubject();

        testSubject.HasAnyGroups.Should().BeTrue();
        testSubject.HasFilteredGroups.Should().BeTrue();
        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeFalse();
    }

    [TestMethod]
    public void HasGroups_ReturnsFalse_WhenThereAreNoRisks()
    {
        testSubject.HasAnyGroups.Should().BeFalse();
        testSubject.HasFilteredGroups.Should().BeFalse();
        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeFalse();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_HasNoFilteredGroups_True()
    {
        testSubject.AllGroupViewModels.Add(CreateFakeGroup(true, true, true));

        testSubject.HasAnyGroups.Should().BeTrue();
        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeTrue();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_GroupHasNoIssues_False()
    {
        var groupViewModel = CreateFakeGroup(false, false, false);
        testSubject.AllGroupViewModels.Add(groupViewModel);
        testSubject.FilteredGroupViewModels.Add(groupViewModel);

        testSubject.HasAnyGroups.Should().BeTrue();
        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeFalse();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_GroupsHasIssuesButNoPreFilteredOrFilteredIssues_False()
    {
        var groupViewModel = CreateFakeGroup(true, false, false);
        testSubject.AllGroupViewModels.Add(groupViewModel);
        testSubject.FilteredGroupViewModels.Add(groupViewModel);

        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeFalse();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_GroupHasIssuesAndPreFilteredButNoFilteredIssues_True()
    {
        var groupViewModel = CreateFakeGroup(true, true, false);
        testSubject.AllGroupViewModels.Add(groupViewModel);
        testSubject.FilteredGroupViewModels.Add(groupViewModel);

        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeTrue();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_GroupHasIssuesAndPreFilteredAndFilteredIssues_False()
    {
        var groupViewModel = CreateFakeGroup(true, true, true);
        testSubject.AllGroupViewModels.Add(groupViewModel);
        testSubject.FilteredGroupViewModels.Add(groupViewModel);

        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeFalse();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_AtLeastOneGroupHasFilteredIssues_False()
    {
        var groupViewModel = CreateFakeGroup(true, true, true);
        var groupViewModel2 = CreateFakeGroup(true, true, false);
        testSubject.AllGroupViewModels.Add(groupViewModel);
        testSubject.AllGroupViewModels.Add(groupViewModel2);
        testSubject.FilteredGroupViewModels.Add(groupViewModel);
        testSubject.FilteredGroupViewModels.Add(groupViewModel2);

        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeFalse();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_OnlyRestrictedGroupIsFiltered_False()
    {
        var groupViewModel = CreateFakeGroup(true, true, true);
        var groupViewModel2 = CreateFakeGroup(true, true, false);
        testSubject.AllGroupViewModels.Add(groupViewModel);
        testSubject.AllGroupViewModels.Add(groupViewModel2);
        testSubject.FilteredGroupViewModels.Add(groupViewModel2);

        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeTrue();
    }

    [TestMethod]
    public void HasNoFilteredIssuesForGroupsWithIssues_AllGroupsRestrictedByFilters_True()
    {
        var groupViewModel = CreateFakeGroup(true, true, false);
        var groupViewModel2 = CreateFakeGroup(true, true, false);
        testSubject.AllGroupViewModels.Add(groupViewModel);
        testSubject.AllGroupViewModels.Add(groupViewModel2);
        testSubject.FilteredGroupViewModels.Add(groupViewModel);
        testSubject.FilteredGroupViewModels.Add(groupViewModel2);

        testSubject.HasNoFilteredIssuesForGroupsWithIssues.Should().BeTrue();
    }

    [TestMethod]
    public void NavigateToIssueLocation_NavigatesToLocation()
    {
        var analysisIssueViewModel = Substitute.For<IAnalysisIssueViewModel>();

        testSubject.Navigate(analysisIssueViewModel);

        locationNavigator.Received(1).TryNavigatePartial(analysisIssueViewModel.Issue);
    }

    [TestMethod]
    public void NavigateToFile_NavigatesToLocation()
    {
        const string filePath = "file path";
        var groupFileViewModel = new GroupFileViewModel(filePath, []);

        testSubject.Navigate(groupFileViewModel);

        locationNavigator.Received(1).TryNavigateFile(filePath);
    }

    [TestMethod]
    public void DocumentOpened_FileWithNoGroup_AddsGroup()
    {
        var filePath = "file";

        documentTracker.DocumentOpened += Raise.EventWith(testSubject, new DocumentEventArgs(new Document(filePath, [])));

        VerifyExpectedGroups(true, filePath);
    }

    [TestMethod]
    public void DocumentOpened_FileWithExistingGroup_DoesNotAddDuplicateGroup()
    {
        var filePath = "existingFile.cs";
        InitializeTestSubjectWithInitialGroup(filePath);

        documentTracker.DocumentOpened += Raise.EventWith(testSubject, new DocumentEventArgs(new Document(filePath, [])));

        VerifyExpectedGroups(false, filePath);
    }

    [TestMethod]
    public void DocumentClosed_FileWithNonTaints_RemovesGroup()
    {
        var filePath = "closedFile.cs";
        var existingTaint = CreateHotspotVisualization(Guid.NewGuid(), "serverKey", filePath);
        IEnumerable<IIssueViewModel> issueViewModels = [CreateHotspotViewModel(existingTaint)];
        hotspotsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);

        documentTracker.DocumentClosed += Raise.EventWith(testSubject, new DocumentEventArgs(new Document(filePath, [])));

        VerifyExpectedGroups(true, []);
    }

    [TestMethod]
    public void DocumentClosed_FileWithGroup_RemovesGroup()
    {
        var filePath = "closedFile.cs";
        var existingTaint = CreateTaintVisualization(Guid.NewGuid(), "serverKey", filePath);
        IEnumerable<IIssueViewModel> issueViewModels = [CreateTaintViewModel(existingTaint)];
        fileAwareTaintsReportViewModel.GetIssueViewModels().Returns(issueViewModels);
        InitializeTestSubjectWithInitialGroup(filePath);

        documentTracker.DocumentClosed += Raise.EventWith(testSubject, new DocumentEventArgs(new Document(filePath, [])));

        VerifyExpectedGroups(true, []);
    }

    [TestMethod]
    public void DocumentClosed_FileWithNoGroup_DoesNothing()
    {
        var filePath = "neverOpened.cs";

        documentTracker.DocumentClosed += Raise.EventWith(testSubject, new DocumentEventArgs(new Document(filePath, [])));

        VerifyExpectedGroups(false);
    }

    [TestMethod]
    public void BindingChange_ClearsGroups()
    {
        // todo https://sonarsource.atlassian.net/browse/SLVS-2620 binding change force clears issue list
        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(BindingConfiguration.Standalone));

        testSubject.AllGroupViewModels.Should().HaveCount(0);
        testSubject.FilteredGroupViewModels.Should().HaveCount(0);
    }

    private void CreateTestSubject()
    {
        var reportViewModel = new ReportViewModel(activeSolutionBoundTracker,
            navigateToRuleDescriptionCommand,
            locationNavigator,
            hotspotsReportViewModel,
            dependencyRisksReportViewModel,
            taintsReportViewModel,
            fileAwareTaintsReportViewModel,
            issuesReportViewModel,
            telemetryManager,
            selectionService,
            activeDocumentLocator,
            activeDocumentTracker,
            documentTracker,
            focusOnNewCodeService,
            threadHandling,
            MockableInitializationProcessor.CreateFactory<ServerViewModel>(Substitute.ForPartsOf<NoOpThreadHandler>(), Substitute.For<ILogger>()));
        reportViewModel.PropertyChanged += eventHandler;
        testSubject = reportViewModel;
    }

    private static IDependencyRisk CreateDependencyRisk(Guid? id = null, bool isResolved = false)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Transitions.Returns([]);
        risk.Status.Returns(isResolved ? DependencyRiskStatus.Accepted : DependencyRiskStatus.Open);
        return risk;
    }

    private void VerifyHasGroupsUpdated()
    {
        eventHandler.Received().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasAnyGroups)));
        eventHandler.Received().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasFilteredGroups)));
    }

    private void VerifyHasNotGroupsUpdated()
    {
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasAnyGroups)));
        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasFilteredGroups)));
    }

    private void ClearCallsForReportsViewModels()
    {
        dependencyRisksReportViewModel.ClearReceivedCalls();
        taintsReportViewModel.ClearReceivedCalls();
        hotspotsReportViewModel.ClearReceivedCalls();
        issuesReportViewModel.ClearReceivedCalls();
    }

    private static LocalHotspot CreateMockedHotspot() => new(Substitute.For<IAnalysisIssueVisualization>(), default, default);

    private static IAnalysisIssueVisualization CreateHotspotVisualization(Guid id, string serverKey, string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.Issue.Returns(Substitute.For<IAnalysisHotspotIssue>());
        analysisIssueVisualization.IssueId.Returns(id);
        analysisIssueVisualization.Issue.Id.Returns(id);
        analysisIssueVisualization.Issue.IssueServerKey.Returns(serverKey);
        analysisIssueVisualization.Issue.PrimaryLocation.FilePath.Returns(filePath);
        return analysisIssueVisualization;
    }

    private static IIssueViewModel CreateDependencyRiskViewModel(IDependencyRisk dependencyRisk) => new DependencyRiskViewModel(dependencyRisk);

    private static IIssueViewModel CreateHotspotViewModel(IAnalysisIssueVisualization issue) => new HotspotViewModel(new LocalHotspot(issue, HotspotPriority.Medium, HotspotStatus.ToReview));

    private static IIssueViewModel CreateTaintViewModel(IAnalysisIssueVisualization issue) => new TaintViewModel(issue);

    private static IIssueViewModel CreateIssueViewModel(IAnalysisIssueVisualization issue) => new IssueViewModel(issue);

    private static IAnalysisIssueVisualization CreateTaintVisualization(Guid id, string serverKey, string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.Issue.Returns(Substitute.For<ITaintIssue>());
        analysisIssueVisualization.IssueId.Returns(id);
        analysisIssueVisualization.Issue.Id.Returns(id);
        analysisIssueVisualization.Issue.IssueServerKey.Returns(serverKey);
        analysisIssueVisualization.Issue.PrimaryLocation.FilePath.Returns(filePath);
        return analysisIssueVisualization;
    }

    private static IAnalysisIssueVisualization CreateIssueVisualization(Guid id, string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.Issue.Returns(Substitute.For<IAnalysisIssue>());
        analysisIssueVisualization.Issue.Id.Returns(id);
        analysisIssueVisualization.IssueId.Returns(id);
        analysisIssueVisualization.Issue.PrimaryLocation.FilePath.Returns(filePath);
        return analysisIssueVisualization;
    }

    private static void VerifyExpectedGroupFileViewModel(GroupViewModelBase groupFileViewModel, params IIssueViewModel[] expectedViewModels)
    {
        groupFileViewModel.Should().NotBeNull();
        groupFileViewModel.AllIssues.Should().BeEquivalentTo(expectedViewModels);
    }

    private void InitializeTestSubjectWithInitialGroup(params string[] files)
    {
        testSubject?.Dispose();
        documentTracker.GetOpenDocuments().Returns(files.Select(x => new Document(x, [])).ToArray());
        CreateTestSubject();
        ClearCallsForReportsViewModels();
        eventHandler.ClearReceivedCalls();
    }

    private void VerifyExpectedGroups(bool updated, params string[] filePaths)
    {
        testSubject.AllGroupViewModels.Select(x => x.FilePath).Should().BeEquivalentTo(filePaths);
        if (updated)
        {
            VerifyHasGroupsUpdated();
        }
        else
        {
            VerifyHasNotGroupsUpdated();
        }
    }

    private IGroupViewModel CreateFakeGroup(bool total, bool prefiltered, bool filtered)
    {
        var vm = Substitute.For<IIssueViewModel>();
        var groupViewModel = Substitute.For<IGroupViewModel>();
        groupViewModel.AllIssues.Returns(total ? [vm] : []);
        groupViewModel.PreFilteredIssues.Returns(prefiltered ? [vm] : []);
        groupViewModel.FilteredIssues.Returns(new ObservableCollection<IIssueViewModel>(filtered ? [vm] : []));
        return groupViewModel;
    }
}
