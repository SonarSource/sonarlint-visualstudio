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

using System.ComponentModel;
using System.Windows;
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
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class ReportViewModelTest
{
    private ReportViewModel testSubject;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IDependencyRisksStore dependencyRisksStore;
    private ILocalHotspotsStore localHotspotsStore;
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
        localHotspotsStore = Substitute.For<ILocalHotspotsStore>();
        showDependencyRiskInBrowserHandler = Substitute.For<IShowDependencyRiskInBrowserHandler>();
        changeDependencyRiskStatusHandler = Substitute.For<IChangeDependencyRiskStatusHandler>();
        navigateToRuleDescriptionCommand = Substitute.For<INavigateToRuleDescriptionCommand>();
        locationNavigator = Substitute.For<ILocationNavigator>();
        messageBox = Substitute.For<IMessageBox>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        eventHandler = Substitute.For<PropertyChangedEventHandler>();

        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void Class_InheritsFromServerViewModel() => testSubject.Should().BeAssignableTo<ServerViewModel>();

    [TestMethod]
    public void Class_SubscribesToEvents()
    {
        localHotspotsStore.Received(1).IssuesChanged += Arg.Any<EventHandler<IssuesChangedEventArgs>>();
        dependencyRisksStore.Received(1).DependencyRisksChanged += Arg.Any<EventHandler>();
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
    public void Ctor_TwoHotspotsInSameFile_CreatesOneGroupVmWithTwoIssues()
    {
        var path = "myFile.cs";
        var hotspot1 = CreateMockedHotspot(filePath: path);
        var hotspot2 = CreateMockedHotspot(filePath: path);
        MockHotspotsInStore(hotspot1, hotspot2);

        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, hotspot1, hotspot2);
    }

    [TestMethod]
    public void Ctor_TwoHotspotsInDifferentFiles_CreatesTwoGroupsWithOneIssueEach()
    {
        var hotspot1 = CreateMockedHotspot(filePath: "myFile.cs");
        var hotspot2 = CreateMockedHotspot(filePath: "myFile.js");
        MockHotspotsInStore(hotspot1, hotspot2);

        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().HaveCount(2);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, hotspot1);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[1] as GroupFileViewModel, hotspot2);
    }

    [TestMethod]
    public void Ctor_MixedIssuesTypes_CreatesGroupViewModelsCorrectly()
    {
        var dependencyRisk = CreateDependencyRisk();
        MockRisksInStore(dependencyRisk);
        var hotspot = CreateMockedHotspot(filePath: "myFile.cs");
        MockHotspotsInStore(hotspot);

        testSubject = CreateTestSubject();

        testSubject.GroupViewModels.Should().HaveCount(2);
        VerifyExpectedDependencyRiskGroupViewModel(testSubject.GroupViewModels[0] as GroupDependencyRiskViewModel, dependencyRisk);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[1] as GroupFileViewModel, hotspot);
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
        localHotspotsStore.Received(1).IssuesChanged -= Arg.Any<EventHandler<IssuesChangedEventArgs>>();
    }

    [TestMethod]
    public void ShowInBrowser_CallsHandler()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);

        testSubject.ShowInBrowser(dependencyRisk);

        showDependencyRiskInBrowserHandler.Received(1).ShowInBrowser(riskId);
    }

    [TestMethod]
    public async Task ChangeStatusAsync_CallsHandler_Success()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);
        var transition = DependencyRiskTransition.Accept;
        var comment = "test comment";
        changeDependencyRiskStatusHandler.ChangeStatusAsync(riskId, transition, comment).Returns(true);

        await testSubject.ChangeStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.Received(1).ChangeStatusAsync(riskId, transition, comment);
        messageBox.DidNotReceiveWithAnyArgs().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task ChangeStatusAsync_CallsHandler_Failure_ShowsMessageBox()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);
        const DependencyRiskTransition transition = DependencyRiskTransition.Accept;
        const string comment = "test comment";
        changeDependencyRiskStatusHandler.ChangeStatusAsync(riskId, transition, comment).Returns(false);

        await testSubject.ChangeStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.Received(1).ChangeStatusAsync(riskId, transition, comment);
        messageBox.Received(1).Show(Resources.DependencyRiskStatusChangeFailedTitle, Resources.DependencyRiskStatusChangeError, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    public async Task ChangeStatusAsync_NullTransition_DoesNotCallHandler_ShowsMessageBox()
    {
        var dependencyRisk = CreateDependencyRisk();
        DependencyRiskTransition? transition = null;
        const string comment = "test comment";

        await testSubject.ChangeStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.DidNotReceiveWithAnyArgs().ChangeStatusAsync(Arg.Any<Guid>(), Arg.Any<DependencyRiskTransition>(), Arg.Any<string>());
        messageBox.Received(1).Show(Resources.DependencyRiskStatusChangeFailedTitle, Resources.DependencyRiskNullTransitionError, MessageBoxButton.OK, MessageBoxImage.Error);
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
    }

    [TestMethod]
    public void HotspotsAddedInStore_ExistingFile_UpdatesExistingGroup()
    {
        var initialHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        var addedHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        localHotspotsStore.GetAllLocalHotspots().Returns([initialHotspot], [initialHotspot, addedHotspot]);
        testSubject = CreateTestSubject();

        localHotspotsStore.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], []));

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, initialHotspot, addedHotspot);
    }

    [TestMethod]
    public void HotspotsAddedInStore_NewFile_CreatesNewGroup()
    {
        var initialHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        var addedHotspot = CreateMockedHotspot(filePath: "otherFile.cs");
        localHotspotsStore.GetAllLocalHotspots().Returns([initialHotspot], [initialHotspot, addedHotspot]);
        testSubject = CreateTestSubject();

        localHotspotsStore.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], []));

        testSubject.GroupViewModels.Should().HaveCount(2);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, initialHotspot);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[1] as GroupFileViewModel, addedHotspot);
    }

    [TestMethod]
    public void HotspotsAddedInStore_DoesNotUpdateDependencyRisks()
    {
        var initialHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        var addedHotspot = CreateMockedHotspot(filePath: "otherFile.cs");
        localHotspotsStore.GetAllLocalHotspots().Returns([initialHotspot], [initialHotspot, addedHotspot]);
        testSubject = CreateTestSubject();
        dependencyRisksStore.ClearReceivedCalls();

        localHotspotsStore.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], []));

        dependencyRisksStore.DidNotReceive().GetAll();
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void HotspotsRemovedFromStore_DeletedHotspotFromExistingFile_UpdatesExistingGroup()
    {
        var initialHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        var initialHotspot2 = CreateMockedHotspot(filePath: "myFile.cs");
        localHotspotsStore.GetAllLocalHotspots().Returns([initialHotspot, initialHotspot2], [initialHotspot]);
        testSubject = CreateTestSubject();

        localHotspotsStore.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], []));

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, initialHotspot);
    }

    [TestMethod]
    public void HotspotsRemovedFromStore_DeletedSingleHotspotFromExistingFile_RemovesGroup()
    {
        var initialHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        localHotspotsStore.GetAllLocalHotspots().Returns([initialHotspot], new LocalHotspot[] { });
        testSubject = CreateTestSubject();

        localHotspotsStore.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], []));

        testSubject.GroupViewModels.Should().BeEmpty();
    }

    [TestMethod]
    public void HotspotsRemovedFromStore_DeletedHotspotFromDifferentFile_UpdatesGroupCorrectly()
    {
        var initialHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        var initialHotspot2 = CreateMockedHotspot(filePath: "otherFile.cs");
        localHotspotsStore.GetAllLocalHotspots().Returns([initialHotspot, initialHotspot2], [initialHotspot]);
        testSubject = CreateTestSubject();

        localHotspotsStore.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], []));

        testSubject.GroupViewModels.Should().HaveCount(1);
        VerifyExpectedHotspotGroupViewModel(testSubject.GroupViewModels[0] as GroupFileViewModel, initialHotspot);
    }

    [TestMethod]
    public void HotspotsRemovedFromStore_DoesNotUpdateDependencyRisks()
    {
        var initialHotspot = CreateMockedHotspot(filePath: "myFile.cs");
        localHotspotsStore.GetAllLocalHotspots().Returns([initialHotspot], []);
        testSubject = CreateTestSubject();
        dependencyRisksStore.ClearReceivedCalls();

        localHotspotsStore.IssuesChanged += Raise.EventWith(testSubject, new IssuesChangedEventArgs([], []));

        dependencyRisksStore.DidNotReceive().GetAll();
        VerifyHasGroupsUpdated();
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
        dependencyRisksStore.ClearReceivedCalls();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        localHotspotsStore.DidNotReceive().GetAll();
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
        dependencyRisksStore.ClearReceivedCalls();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        localHotspotsStore.DidNotReceive().GetAll();
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void HasRisks_ReturnsTrue_WhenThereAreRisks()
    {
        MockRisksInStore(CreateDependencyRisk());
        testSubject = CreateTestSubject();

        testSubject.HasGroups.Should().BeTrue();
    }

    [TestMethod]
    public void HasRisks_ReturnsFalse_WhenThereAreNoRisks() => testSubject.HasGroups.Should().BeFalse();

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
            dependencyRisksStore,
            localHotspotsStore,
            showDependencyRiskInBrowserHandler,
            changeDependencyRiskStatusHandler,
            navigateToRuleDescriptionCommand,
            locationNavigator,
            messageBox,
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

    private static LocalHotspot CreateMockedHotspot(string filePath)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        var analysisIssueBase = Substitute.For<IAnalysisIssueBase>();
        analysisIssueBase.PrimaryLocation.FilePath.Returns(filePath);
        analysisIssueVisualization.Issue.Returns(analysisIssueBase);

        return new LocalHotspot(analysisIssueVisualization, default, default);
    }

    private void MockRisksInStore(params IDependencyRisk[] dependencyRisks) => dependencyRisksStore.GetAll().Returns(dependencyRisks);

    private void MockHotspotsInStore(params LocalHotspot[] hotspots) => localHotspotsStore.GetAllLocalHotspots().Returns(hotspots);

    private static void VerifyExpectedHotspotGroupViewModel(GroupFileViewModel groupFileVm, params LocalHotspot[] expectedHotspots)
    {
        groupFileVm.Should().NotBeNull();
        groupFileVm.FilePath.Should().Be(expectedHotspots[0].Visualization.Issue.PrimaryLocation.FilePath);
        groupFileVm.FilteredIssues.Should().HaveCount(expectedHotspots.Length);
        foreach (var expectedHotspot in expectedHotspots)
        {
            groupFileVm.FilteredIssues.Should().ContainSingle(vm => ((HotspotViewModel)vm).LocalHotspot == expectedHotspot);
        }
    }

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
}
