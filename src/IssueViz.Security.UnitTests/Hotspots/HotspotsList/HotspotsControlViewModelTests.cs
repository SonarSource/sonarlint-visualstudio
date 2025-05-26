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
using System.Windows;
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.HotspotsList;

[TestClass]
public class HotspotsControlViewModelTests
{
    private const string CurrentDocumentPath = "C:\\source\\myProj\\myFile.cs";
    private const string OtherDocument = "C:\\myProj\\OtherFile.cs";
    private IActiveDocumentLocator activeDocumentLocator;
    private IActiveDocumentTracker activeDocumentTracker;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private ILocalHotspotsStore hotspotsStore;
    private ILocationNavigator locationNavigator;
    private IMessageBox messageBox;
    private INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
    private ObservableCollection<IAnalysisIssueVisualization> originalCollection;
    private IReviewHotspotsService reviewHotspotsService;
    private IIssueSelectionService selectionService;
    private HotspotsControlViewModel testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void TestInitialize()
    {
        hotspotsStore = Substitute.For<ILocalHotspotsStore>();
        navigateToRuleDescriptionCommand = Substitute.For<INavigateToRuleDescriptionCommand>();
        locationNavigator = Substitute.For<ILocationNavigator>();
        selectionService = Substitute.For<IIssueSelectionService>();
        threadHandling = Substitute.For<IThreadHandling>();
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        reviewHotspotsService = Substitute.For<IReviewHotspotsService>();
        messageBox = Substitute.For<IMessageBox>();
        activeDocumentLocator = Substitute.For<IActiveDocumentLocator>();
        activeDocumentTracker = Substitute.For<IActiveDocumentTracker>();

        MockTestSubject();
        testSubject = CreateTestSubject();
    }

    [TestMethod]
    public void Ctor_RegisterToStoreCollectionChanges()
    {
        var issueViz1 = Substitute.For<IAnalysisIssueVisualization>();
        var issueViz2 = Substitute.For<IAnalysisIssueVisualization>();
        var issueViz3 = Substitute.For<IAnalysisIssueVisualization>();

        RaiseStoreIssuesChangedEvent(issueViz1);

        testSubject.Hotspots.Count.Should().Be(1);
        testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
        testSubject.Hotspots[0].HotspotPriority.Should().Be(default(HotspotPriority));
        testSubject.Hotspots[0].HotspotStatus.Should().Be(default(HotspotStatus));

        hotspotsStore.GetAllLocalHotspots().Returns([
            new LocalHotspot(issueViz1, HotspotPriority.Low, HotspotStatus.Safe),
            new LocalHotspot(issueViz2, HotspotPriority.Medium, HotspotStatus.ToReview),
            new LocalHotspot(issueViz3, HotspotPriority.High, HotspotStatus.Acknowledged)
        ]);
        RaiseIssuesChangedEvent();

        testSubject.Hotspots.Count.Should().Be(3);
        testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
        testSubject.Hotspots[0].HotspotPriority.Should().Be(HotspotPriority.Low);
        testSubject.Hotspots[0].HotspotStatus.Should().Be(HotspotStatus.Safe);
        testSubject.Hotspots[1].Hotspot.Should().Be(issueViz2);
        testSubject.Hotspots[1].HotspotPriority.Should().Be(HotspotPriority.Medium);
        testSubject.Hotspots[1].HotspotStatus.Should().Be(HotspotStatus.ToReview);
        testSubject.Hotspots[2].Hotspot.Should().Be(issueViz3);
        testSubject.Hotspots[2].HotspotPriority.Should().Be(HotspotPriority.High);
        testSubject.Hotspots[2].HotspotStatus.Should().Be(HotspotStatus.Acknowledged);
    }

    [TestMethod]
    public void Ctor_InitializesLocationFilters()
    {
        HotspotsControlViewModel.LocationFilters.Should().HaveCount(2);
        HotspotsControlViewModel.LocationFilters[0].LocationFilter.Should().Be(LocationFilter.CurrentDocument);
        HotspotsControlViewModel.LocationFilters[0].DisplayName.Should().Be(Resources.HotspotsControl_CurrentDocumentFilter);
        HotspotsControlViewModel.LocationFilters[1].LocationFilter.Should().Be(LocationFilter.OpenDocuments);
        HotspotsControlViewModel.LocationFilters[1].DisplayName.Should().Be(Resources.HotspotsControl_OpenDocumentsFilter);

        testSubject.SelectedLocationFilter.Should().NotBeNull();
        testSubject.SelectedLocationFilter.LocationFilter.Should().Be(LocationFilter.CurrentDocument);
    }

    [TestMethod]
    public void Ctor_InitializesPriorityFilters()
    {
        testSubject.PriorityFilters.Should().HaveCount(3);
        testSubject.PriorityFilters[0].HotspotPriority.Should().Be(HotspotPriority.High);
        testSubject.PriorityFilters[1].HotspotPriority.Should().Be(HotspotPriority.Medium);
        testSubject.PriorityFilters[2].HotspotPriority.Should().Be(HotspotPriority.Low);

        testSubject.PriorityFilters.All(x => x.IsSelected).Should().BeTrue();
    }

    [TestMethod]
    public void Ctor_InitializesActiveDocument()
    {
        activeDocumentLocator.Received(1).FindActiveDocument();
        activeDocumentTracker.Received(1).ActiveDocumentChanged += Arg.Any<EventHandler<ActiveDocumentChangedEventArgs>>();
    }

    [TestMethod]
    public void Ctor_RegisterToSelectionChangedEvent()
    {
        selectionService.Received(1).SelectedIssueChanged += Arg.Any<EventHandler>();
        selectionService.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void Ctor_InitializesIsCloud()
    {
        activeSolutionBoundTracker.CurrentConfiguration.Returns(CreateBindingConfiguration(new ServerConnection.SonarCloud("myOrg"), SonarLintMode.Connected));
        activeSolutionBoundTracker.ClearReceivedCalls();

        var hotspotsControlViewModel = CreateTestSubject();

        _ = activeSolutionBoundTracker.Received(1).CurrentConfiguration;
        hotspotsControlViewModel.IsCloud.Should().BeTrue();
    }

    [TestMethod]
    public async Task UpdateHotspotsListAsync_UpdatesWhenManuallyTriggered()
    {
        var issueViz1 = Substitute.For<IAnalysisIssueVisualization>();
        var issueViz2 = Substitute.For<IAnalysisIssueVisualization>();
        var issueViz3 = Substitute.For<IAnalysisIssueVisualization>();

        hotspotsStore.GetAllLocalHotspots().Returns([new LocalHotspot(issueViz1, HotspotPriority.Medium, HotspotStatus.Safe)]);
        await testSubject.UpdateHotspotsListAsync();

        testSubject.Hotspots.Count.Should().Be(1);
        testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
        testSubject.Hotspots[0].HotspotPriority.Should().Be(HotspotPriority.Medium);
        testSubject.Hotspots[0].HotspotStatus.Should().Be(HotspotStatus.Safe);

        hotspotsStore.GetAllLocalHotspots().Returns([
            new LocalHotspot(issueViz1, HotspotPriority.Low, HotspotStatus.Fixed),
            new LocalHotspot(issueViz2, HotspotPriority.Medium, HotspotStatus.Acknowledged),
            new LocalHotspot(issueViz3, HotspotPriority.High, HotspotStatus.ToReview)
        ]);
        await testSubject.UpdateHotspotsListAsync();

        testSubject.Hotspots.Count.Should().Be(3);
        testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
        testSubject.Hotspots[0].HotspotPriority.Should().Be(HotspotPriority.Low);
        testSubject.Hotspots[0].HotspotStatus.Should().Be(HotspotStatus.Fixed);
        testSubject.Hotspots[1].Hotspot.Should().Be(issueViz2);
        testSubject.Hotspots[1].HotspotPriority.Should().Be(HotspotPriority.Medium);
        testSubject.Hotspots[1].HotspotStatus.Should().Be(HotspotStatus.Acknowledged);
        testSubject.Hotspots[2].Hotspot.Should().Be(issueViz3);
        testSubject.Hotspots[2].HotspotPriority.Should().Be(HotspotPriority.High);
        testSubject.Hotspots[2].HotspotStatus.Should().Be(HotspotStatus.ToReview);
    }

    [TestMethod]
    public async Task UpdateHotspotsListAsync_RunsOnBackgroundThread()
    {
        await testSubject.UpdateHotspotsListAsync();

        await threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>());
        hotspotsStore.Received(1).GetAllLocalHotspots();
    }

    [TestMethod]
    public async Task UpdateHotspotsListAsync_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        await testSubject.UpdateHotspotsListAsync();

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.Hotspots)));
    }

    [TestMethod]
    public void IssueChangedEventRaised_RunsOnBackgroundThread()
    {
        RaiseStoreIssuesChangedEvent();

        threadHandling.Received(1).RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>());
        hotspotsStore.Received(1).GetAllLocalHotspots();
    }

    [TestMethod]
    public void Dispose_UnregisterFromHotspotsCollectionChanges()
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();

        testSubject.Dispose();

        originalCollection.Add(issueViz);
        testSubject.Hotspots.Count.Should().Be(0);
    }

    [TestMethod]
    public void Dispose_UnregisterEvents()
    {
        testSubject.Dispose();

        selectionService.Received(1).SelectedIssueChanged -= Arg.Any<EventHandler>();
        activeDocumentTracker.Received(1).ActiveDocumentChanged -= Arg.Any<EventHandler<ActiveDocumentChangedEventArgs>>();
    }

    [TestMethod]
    public async Task SelectionChanged_SelectedHotspotExistsInList_HotspotSelected()
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        hotspotsStore.GetAllLocalHotspots().Returns([new LocalHotspot(issueViz, default, default)]);
        await testSubject.UpdateHotspotsListAsync();

        RaiseStoreIssuesChangedEvent(issueViz);
        testSubject.SelectedHotspot.Should().BeNull();

        RaiseSelectionChangedEvent(issueViz);
        testSubject.SelectedHotspot.Should().NotBeNull();
        testSubject.SelectedHotspot.Hotspot.Should().Be(issueViz);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SelectionChanged_SelectedHotspotIsNotInList_SelectionSetToNull(bool isSelectedNull)
    {
        var oldSelection = new HotspotViewModel(Substitute.For<IAnalysisIssueVisualization>(), default, default);
        testSubject.SelectedHotspot = oldSelection;
        testSubject.SelectedHotspot.Should().Be(oldSelection);

        var selectedIssue = isSelectedNull ? null : Substitute.For<IAnalysisIssueVisualization>();

        RaiseSelectionChangedEvent(selectedIssue);

        testSubject.SelectedHotspot.Should().BeNull();
    }

    [TestMethod]
    public void SelectionChanged_SelectedHotspotExistsInList_RaisesPropertyChanged()
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        RaiseSelectionChangedEvent(issueViz);

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(HotspotsControlViewModel.SelectedHotspot)));
        eventHandler.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SelectionChanged_SelectedHotspotIsNotInList_RaisesPropertyChanged(bool isSelectedNull)
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        eventHandler.ReceivedCalls().Should().BeEmpty();

        var selectedIssue = isSelectedNull ? null : Substitute.For<IAnalysisIssueVisualization>();

        RaiseSelectionChangedEvent(selectedIssue);

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(HotspotsControlViewModel.SelectedHotspot)));
        eventHandler.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Verify that there is no callback, selection service -> property -> selection service")]
    public void SelectionChanged_HotspotSelected_SelectionServiceNotCalledAgain()
    {
        var selectedIssue = Substitute.For<IAnalysisIssueVisualization>();
        originalCollection.Add(selectedIssue);

        RaiseSelectionChangedEvent(selectedIssue);

        selectionService.DidNotReceive().SelectedIssue = Arg.Any<IAnalysisIssueVisualization>();
    }

    [TestMethod]
    public void NavigateCommand_CanExecute_NullParameter_False()
    {
        var result = testSubject.NavigateCommand.CanExecute(null);

        result.Should().BeFalse();
        locationNavigator.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void NavigateCommand_CanExecute_ParameterIsNotHotspotViewModel_False()
    {
        var result = testSubject.NavigateCommand.CanExecute("something");

        result.Should().BeFalse();
        locationNavigator.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void NavigateCommand_CanExecute_ParameterIsHotspotViewModel_True()
    {
        var result = testSubject.NavigateCommand.CanExecute(Substitute.For<IHotspotViewModel>());

        result.Should().BeTrue();
        locationNavigator.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void NavigateCommand_Execute_LocationNavigated()
    {
        var hotspot = Substitute.For<IAnalysisIssueVisualization>();
        var viewModel = Substitute.For<IHotspotViewModel>();
        viewModel.Hotspot.Returns(hotspot);

        testSubject.NavigateCommand.Execute(viewModel);

        locationNavigator.Received(1).TryNavigatePartial(hotspot);
        locationNavigator.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void SetSelectedHotspot_HotspotSet()
    {
        var selection = new HotspotViewModel(Substitute.For<IAnalysisIssueVisualization>(), default, default);
        testSubject.SelectedHotspot = selection;

        testSubject.SelectedHotspot.Should().Be(selection);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SetSelectedHotspot_SelectionChanged_SelectionServiceIsCalled(bool isSelectedNull)
    {
        var oldSelection = isSelectedNull ? new HotspotViewModel(Substitute.For<IAnalysisIssueVisualization>(), default, default) : null;
        var newSelection = isSelectedNull ? null : new HotspotViewModel(Substitute.For<IAnalysisIssueVisualization>(), default, default);

        testSubject.SelectedHotspot = oldSelection;
        selectionService.ClearReceivedCalls();

        testSubject.SelectedHotspot = newSelection;

        selectionService.Received(1).SelectedIssue = newSelection?.Hotspot;
        selectionService.ReceivedCalls().Should().HaveCount(1);
    }

    [TestMethod]
    public void SetSelectedHotspot_ValueIsTheSame_SelectionServiceNotCalled()
    {
        var selection = new HotspotViewModel(Substitute.For<IAnalysisIssueVisualization>(), default, default);
        testSubject.SelectedHotspot = selection;
        selectionService.ClearReceivedCalls();

        testSubject.SelectedHotspot = selection;

        selectionService.ReceivedCalls().Should().BeEmpty();
    }

    [TestMethod]
    public void NavigateToRuleDescriptionCommand_IsNotNull() => testSubject.NavigateToRuleDescriptionCommand.Should().BeSameAs(navigateToRuleDescriptionCommand);

    [TestMethod]
    public void ConnectionInfo_Set_RaisesPropertyChanged()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        var cloudBindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarCloud("my org"), SonarLintMode.Connected);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.IsCloud)));
    }

    [TestMethod]
    [DataRow(SonarLintMode.Connected)]
    [DataRow(SonarLintMode.LegacyConnected)]
    public void SolutionBindingChanged_BindingToCloud_IsCloudIsTrue(SonarLintMode sonarLintMode)
    {
        var cloudBindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarCloud("my org"), sonarLintMode);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeTrue();
    }

    [TestMethod]
    [DataRow(SonarLintMode.Connected)]
    [DataRow(SonarLintMode.LegacyConnected)]
    public void SolutionBindingChanged_BindingToServer_IsCloudIsFalse(SonarLintMode sonarLintMode)
    {
        var cloudBindingConfiguration = CreateBindingConfiguration(new ServerConnection.SonarQube(new Uri("C:\\")), sonarLintMode);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeFalse();
    }

    [TestMethod]
    public void SolutionBindingChanged_Standalone_IsCloudIsFalse()
    {
        var cloudBindingConfiguration = new BindingConfiguration(null, SonarLintMode.Standalone, string.Empty);

        activeSolutionBoundTracker.SolutionBindingChanged += Raise.EventWith(new ActiveSolutionBindingEventArgs(cloudBindingConfiguration));

        testSubject.IsCloud.Should().BeFalse();
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_ChangeStatusPermitted_ReturnsListOfAllowedStatuses()
    {
        var allowedStatuses = new List<HotspotStatus> { HotspotStatus.Fixed, HotspotStatus.ToReview };
        await MockSelectedHotspot("ServerKey");
        MockChangeStatusPermitted(testSubject.SelectedHotspot.Hotspot.Issue.IssueServerKey, allowedStatuses);

        var result = await testSubject.GetAllowedStatusesAsync();

        result.Should().BeEquivalentTo(allowedStatuses);
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task GetAllowedStatusesAsync_ChangeStatusNotPermitted_ShowsMessageBoxAndReturnsNull()
    {
        var reason = "Not permitted";
        await MockSelectedHotspot("ServerKey");
        MockChangeStatusNotPermitted(testSubject.SelectedHotspot.Hotspot.Issue.IssueServerKey, reason);

        var result = await testSubject.GetAllowedStatusesAsync();

        result.Should().BeNull();
        messageBox.Received(1).Show(Arg.Is<string>(x => x == string.Format(Resources.ReviewHotspotWindow_CheckReviewPermittedFailureMessage, reason)),
            Arg.Is<string>(x => x == Resources.ReviewHotspotWindow_FailureTitle), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    [DataRow(HotspotStatus.Fixed)]
    [DataRow(HotspotStatus.Safe)]
    public async Task ChangeHotspotStatusAsync_Succeeds_HotspotStatusFixed_RemovesViewModel(HotspotStatus newStatus)
    {
        var hotspotKey = "ServerKey";
        await MockSelectedHotspot(hotspotKey);
        MockReviewHotspot(hotspotKey, newStatus, true);

        await testSubject.ChangeHotspotStatusAsync(newStatus);

        testSubject.Hotspots.Should().NotContain(x => x.Hotspot.Issue.IssueServerKey == hotspotKey);
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    [DataRow(HotspotStatus.Fixed)]
    [DataRow(HotspotStatus.Safe)]
    public async Task ChangeHotspotStatusAsync_Succeeds_HotspotStatusFixed_RaisesEvents(HotspotStatus newStatus)
    {
        var hotspotKey = "ServerKey";
        await MockSelectedHotspot(hotspotKey);
        MockReviewHotspot(hotspotKey, newStatus, true);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        await testSubject.ChangeHotspotStatusAsync(newStatus);

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.Hotspots)));
    }

    [TestMethod]
    [DataRow(HotspotStatus.ToReview)]
    [DataRow(HotspotStatus.Acknowledged)]
    public async Task ChangeHotspotStatusAsync_Succeeds_HotspotStatusNotFixed_DoesNotRemoveViewModel(HotspotStatus newStatus)
    {
        var hotspotKey = "ServerKey";
        await MockSelectedHotspot(hotspotKey);
        MockReviewHotspot(hotspotKey, newStatus, true);

        await testSubject.ChangeHotspotStatusAsync(newStatus);

        testSubject.Hotspots.Should().Contain(x => x.Hotspot.Issue.IssueServerKey == hotspotKey);
        messageBox.DidNotReceive().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    [DataRow(HotspotStatus.ToReview)]
    [DataRow(HotspotStatus.Acknowledged)]
    public async Task ChangeHotspotStatusAsync_Succeeds_HotspotStatusNotFixed_DoesNotRaiseEvents(HotspotStatus newStatus)
    {
        var hotspotKey = "ServerKey";
        await MockSelectedHotspot(hotspotKey);
        MockReviewHotspot(hotspotKey, newStatus, true);
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        await testSubject.ChangeHotspotStatusAsync(newStatus);

        eventHandler.DidNotReceive().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.Hotspots)));
    }

    [TestMethod]
    [DataRow(HotspotStatus.Fixed)]
    [DataRow(HotspotStatus.ToReview)]
    [DataRow(HotspotStatus.Acknowledged)]
    [DataRow(HotspotStatus.Safe)]
    public async Task ChangeHotspotStatusAsync_Fails_ShowsMessageBox(HotspotStatus newStatus)
    {
        var hotspotKey = "ServerKey";
        await MockSelectedHotspot(hotspotKey);
        MockReviewHotspot(hotspotKey, newStatus, false);

        await testSubject.ChangeHotspotStatusAsync(newStatus);

        messageBox.Received(1).Show(Arg.Is<string>(x => x == Resources.ReviewHotspotWindow_ReviewFailureMessage), Arg.Is<string>(x => x == Resources.ReviewHotspotWindow_FailureTitle),
            MessageBoxButton.OK, MessageBoxImage.Error);
        testSubject.Hotspots.Should().Contain(x => x.Hotspot.Issue.IssueServerKey == hotspotKey);
    }

    [TestMethod]
    public async Task ViewHotspotInBrowserAsync_CallsReviewHotspotsService()
    {
        var hotspotKey = "ServerKey";
        await MockSelectedHotspot(hotspotKey);

        await testSubject.ViewHotspotInBrowserAsync();

        await reviewHotspotsService.Received(1).OpenHotspotAsync(hotspotKey);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromActiveSolutionBoundTrackerEvents()
    {
        testSubject.Dispose();

        activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
    }

    [TestMethod]
    public void SelectedLocationFilter_Set_RaisesPropertyChanged()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedLocationFilter = HotspotsControlViewModel.LocationFilters.Single(x => x.LocationFilter == LocationFilter.OpenDocuments);

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.SelectedLocationFilter)));
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.Hotspots)));
    }

    [TestMethod]
    public async Task SelectedLocationFilter_OpenDocumentsFilter_ShowsAllHotspots()
    {
        MockActiveDocument();
        await MockHotspotLists(CreateMockedLocalHotspots(CurrentDocumentPath), CreateMockedLocalHotspots(OtherDocument), CreateMockedLocalHotspots(CurrentDocumentPath));

        testSubject.SelectedLocationFilter = GetLocationFilter(LocationFilter.OpenDocuments);

        testSubject.Hotspots.Should().HaveCount(3);
        testSubject.Hotspots.Where(x => x.Hotspot.FilePath == CurrentDocumentPath).Should().HaveCount(2);
        testSubject.Hotspots.Where(x => x.Hotspot.FilePath != CurrentDocumentPath).Should().HaveCount(1);
    }

    [TestMethod]
    public async Task SelectedLocationFilter_CurrentDocumentFilter_ShowsOnlyHotspotsForActiveDocument()
    {
        MockActiveDocument();
        var localHotspot = CreateMockedLocalHotspots(CurrentDocumentPath);
        var localHotspot2 = CreateMockedLocalHotspots(CurrentDocumentPath);
        await MockHotspotLists(localHotspot, CreateMockedLocalHotspots(OtherDocument), localHotspot2);

        testSubject.SelectedLocationFilter = GetLocationFilter(LocationFilter.CurrentDocument);

        testSubject.Hotspots.Should().HaveCount(2);
        testSubject.Hotspots.Should().Contain(x => x.Hotspot == localHotspot.Visualization);
        testSubject.Hotspots.Should().Contain(x => x.Hotspot == localHotspot2.Visualization);
    }

    [TestMethod]
    public async Task SelectedLocationFilter_NoFilter_ShowsAllHotspots()
    {
        MockActiveDocument();
        await MockHotspotLists(CreateMockedLocalHotspots(CurrentDocumentPath), CreateMockedLocalHotspots(OtherDocument), CreateMockedLocalHotspots(CurrentDocumentPath));

        testSubject.SelectedLocationFilter = null;

        testSubject.Hotspots.Should().HaveCount(3);
    }

    [TestMethod]
    public async Task ActiveDocumentChanged_RaisesEventsAndFiltersHotspotsCorrectly()
    {
        var localHotspot = CreateMockedLocalHotspots(OtherDocument);
        await MockHotspotLists(CreateMockedLocalHotspots(CurrentDocumentPath), localHotspot, CreateMockedLocalHotspots(CurrentDocumentPath));

        MockActiveDocument(localHotspot.Visualization.FilePath);

        testSubject.Hotspots.Should().HaveCount(1);
        testSubject.Hotspots.Should().Contain(x => x.Hotspot == localHotspot.Visualization);
    }

    [TestMethod]
    public void ActiveDocumentChanged_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        MockActiveDocument(OtherDocument);

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.Hotspots)));
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void UpdatePriorityFilter_ShouldUpdateSelection(bool isSelected)
    {
        var filterToUpdate = testSubject.PriorityFilters[0];

        testSubject.UpdatePriorityFilter(filterToUpdate, isSelected);

        filterToUpdate.IsSelected.Should().Be(isSelected);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void UpdatePriorityFilter_RaisesEvents(bool isSelected)
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.UpdatePriorityFilter(testSubject.PriorityFilters[1], isSelected);

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.Hotspots)));
    }

    [TestMethod]
    [DataRow(HotspotPriority.High)]
    [DataRow(HotspotPriority.Medium)]
    [DataRow(HotspotPriority.Low)]
    public async Task UpdatePriorityFilter_OneFilterSelected_ShowsOnlyHotspotWithThatPriority(HotspotPriority priority)
    {
        await MockHotspotLists(
            CreateMockedLocalHotspots(priority: HotspotPriority.Low),
            CreateMockedLocalHotspots(priority: HotspotPriority.High),
            CreateMockedLocalHotspots(priority: HotspotPriority.Medium)
        );
        UpdateAllPriorityFilters(isSelected: false);

        testSubject.UpdatePriorityFilter(GetPriorityFilter(priority), true);

        testSubject.Hotspots.Should().HaveCount(1);
        testSubject.Hotspots.All(x => x.HotspotPriority == priority).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(HotspotPriority.High, HotspotPriority.Low)]
    [DataRow(HotspotPriority.Medium, HotspotPriority.High)]
    [DataRow(HotspotPriority.Low, HotspotPriority.Medium)]
    public async Task UpdatePriorityFilter_TwoFiltersSelected_ShowsHotspotWithMatchingPriorities(HotspotPriority priority1, HotspotPriority priority2)
    {
        await MockHotspotLists(
            CreateMockedLocalHotspots(priority: HotspotPriority.Low),
            CreateMockedLocalHotspots(priority: HotspotPriority.High),
            CreateMockedLocalHotspots(priority: HotspotPriority.Medium)
        );
        UpdateAllPriorityFilters(isSelected: false);

        testSubject.UpdatePriorityFilter(GetPriorityFilter(priority1), true);
        testSubject.UpdatePriorityFilter(GetPriorityFilter(priority2), true);

        testSubject.Hotspots.Should().HaveCount(2);
        testSubject.Hotspots.All(x => x.HotspotPriority == priority1 || x.HotspotPriority == priority2).Should().BeTrue();
    }

    [TestMethod]
    [DataRow(LocationFilter.CurrentDocument, HotspotPriority.High, 1)]
    [DataRow(LocationFilter.CurrentDocument, HotspotPriority.Medium, 1)]
    [DataRow(LocationFilter.OpenDocuments, HotspotPriority.High, 2)]
    [DataRow(LocationFilter.OpenDocuments, HotspotPriority.Medium, 2)]
    public async Task Hotspots_LocationAndPriorityFiltersAreBothApplied(LocationFilter filter, HotspotPriority priority, int expectedHotspots)
    {
        MockActiveDocument();
        await MockHotspotLists(
            CreateMockedLocalHotspots(filePath: CurrentDocumentPath, priority: HotspotPriority.High),
            CreateMockedLocalHotspots(filePath: CurrentDocumentPath, priority: HotspotPriority.Medium),
            CreateMockedLocalHotspots(filePath: OtherDocument, priority: HotspotPriority.High),
            CreateMockedLocalHotspots(filePath: OtherDocument, priority: HotspotPriority.Medium)
        );
        UpdateAllPriorityFilters(isSelected: false);

        testSubject.SelectedLocationFilter = GetLocationFilter(filter);
        testSubject.UpdatePriorityFilter(GetPriorityFilter(priority), true);

        testSubject.Hotspots.Should().HaveCount(expectedHotspots);
        testSubject.Hotspots.All(x => x.HotspotPriority == priority).Should().BeTrue();
    }

    private void MockTestSubject()
    {
        originalCollection = [];
        var readOnlyWrapper = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection);
        hotspotsStore.GetAllLocalHotspots().Returns(readOnlyWrapper.Select(x => new LocalHotspot(x, default, default)).ToList());

        threadHandling.When(x => x.RunOnBackgroundThread(Arg.Any<Func<Task<bool>>>())).Do(x =>
        {
            var func = x.Arg<Func<Task<bool>>>();
            func();
        });
    }

    private void RaiseStoreIssuesChangedEvent(params IAnalysisIssueVisualization[] issueVizs)
    {
        hotspotsStore.GetAllLocalHotspots().Returns(issueVizs.Select(x => new LocalHotspot(x, default, default)).ToList());
        RaiseIssuesChangedEvent(issueVizs);
    }

    private void RaiseIssuesChangedEvent(params IAnalysisIssueVisualization[] issueVizs) =>
        hotspotsStore.IssuesChanged += Raise.Event<EventHandler<IssuesChangedEventArgs>>(new IssuesChangedEventArgs(null, issueVizs));

    private void RaiseSelectionChangedEvent(IAnalysisIssueVisualization selectedIssue)
    {
        selectionService.SelectedIssue.Returns(selectedIssue);
        selectionService.SelectedIssueChanged += Raise.Event<EventHandler>(selectionService, EventArgs.Empty);
    }

    private static BindingConfiguration CreateBindingConfiguration(ServerConnection serverConnection, SonarLintMode mode) =>
        new(new BoundServerProject("my solution", "my project", serverConnection), mode, string.Empty);

    private void MockChangeStatusPermitted(string hotspotKey, List<HotspotStatus> allowedStatuses) =>
        reviewHotspotsService.CheckReviewHotspotPermittedAsync(hotspotKey).Returns(new ReviewHotspotPermittedArgs(allowedStatuses));

    private void MockChangeStatusNotPermitted(string hotspotKey, string reason) =>
        reviewHotspotsService.CheckReviewHotspotPermittedAsync(hotspotKey).Returns(new ReviewHotspotNotPermittedArgs(reason));

    private void MockReviewHotspot(string hotspotKey, HotspotStatus newStatus, bool succeeded) => reviewHotspotsService.ReviewHotspotAsync(hotspotKey, newStatus).Returns(succeeded);

    private async Task MockSelectedHotspot(string hotspotKey, HotspotStatus status = default)
    {
        var analysisIssueVisualization = Substitute.For<IAnalysisIssueVisualization>();
        analysisIssueVisualization.Issue.IssueServerKey.Returns(hotspotKey);

        hotspotsStore.GetAllLocalHotspots().Returns([new LocalHotspot(analysisIssueVisualization, default, status)]);
        await testSubject.UpdateHotspotsListAsync();
        testSubject.SelectedHotspot = testSubject.Hotspots.Single();
    }

    private async Task MockHotspotLists(params LocalHotspot[] localHotspots)
    {
        hotspotsStore.GetAllLocalHotspots().Returns(localHotspots);
        await testSubject.UpdateHotspotsListAsync();
    }

    private LocalHotspot CreateMockedLocalHotspots(string filePath = null, HotspotPriority priority = default)
    {
        var issueViz = Substitute.For<IAnalysisIssueVisualization>();
        issueViz.FilePath.Returns(filePath ?? string.Empty);

        return new LocalHotspot(issueViz, priority, default);
    }

    private void MockActiveDocument(string filePath = CurrentDocumentPath)
    {
        var textDocument = Substitute.For<ITextDocument>();
        textDocument.FilePath.Returns(filePath);
        activeDocumentTracker.ActiveDocumentChanged += Raise.EventWith(new ActiveDocumentChangedEventArgs(textDocument));
    }

    private void UpdateAllPriorityFilters(bool isSelected) => testSubject.PriorityFilters.ToList().ForEach(x => x.IsSelected = isSelected);

    private PriorityFilterViewModel GetPriorityFilter(HotspotPriority priority) => testSubject.PriorityFilters.Single(x => x.HotspotPriority == priority);

    private LocationFilterViewModel GetLocationFilter(LocationFilter location) => HotspotsControlViewModel.LocationFilters.Single(x => x.LocationFilter == location);

    private HotspotsControlViewModel CreateTestSubject() =>
        new(hotspotsStore, navigateToRuleDescriptionCommand, locationNavigator, selectionService, threadHandling, activeSolutionBoundTracker,
            reviewHotspotsService, messageBox, activeDocumentLocator, activeDocumentTracker);
}
