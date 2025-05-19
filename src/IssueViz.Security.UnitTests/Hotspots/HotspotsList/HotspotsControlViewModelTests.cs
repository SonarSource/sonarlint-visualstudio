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
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.HotspotsList
{
    [TestClass]
    public class HotspotsControlViewModelTests
    {
        private HotspotsControlViewModel testSubject;
        private ILocalHotspotsStore hotspotsStore;
        private INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
        private ILocationNavigator locationNavigator;
        private IIssueSelectionService selectionService;
        private IThreadHandling threadHandling;
        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private ObservableCollection<IAnalysisIssueVisualization> originalCollection;

        [TestInitialize]
        public void TestInitialize()
        {
            hotspotsStore = Substitute.For<ILocalHotspotsStore>();
            navigateToRuleDescriptionCommand = Substitute.For<INavigateToRuleDescriptionCommand>();
            locationNavigator = Substitute.For<ILocationNavigator>();
            selectionService = Substitute.For<IIssueSelectionService>();
            threadHandling = Substitute.For<IThreadHandling>();
            activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
            testSubject = new HotspotsControlViewModel(hotspotsStore, navigateToRuleDescriptionCommand, locationNavigator, selectionService, threadHandling, activeSolutionBoundTracker);

            MockTestSubject();
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
                new LocalHotspot(issueViz3, HotspotPriority.High, HotspotStatus.Acknowledge)
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
            testSubject.Hotspots[2].HotspotStatus.Should().Be(HotspotStatus.Acknowledge);
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
                new LocalHotspot(issueViz2, HotspotPriority.Medium, HotspotStatus.Acknowledge),
                new LocalHotspot(issueViz3, HotspotPriority.High, HotspotStatus.ToReview)
            ]);
            await testSubject.UpdateHotspotsListAsync();

            testSubject.Hotspots.Count.Should().Be(3);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[0].HotspotPriority.Should().Be(HotspotPriority.Low);
            testSubject.Hotspots[0].HotspotStatus.Should().Be(HotspotStatus.Fixed);
            testSubject.Hotspots[1].Hotspot.Should().Be(issueViz2);
            testSubject.Hotspots[1].HotspotPriority.Should().Be(HotspotPriority.Medium);
            testSubject.Hotspots[1].HotspotStatus.Should().Be(HotspotStatus.Acknowledge);
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
        public void IssueChangedEventRaised_RunsOnBackgroundThread()
        {
            RaiseStoreIssuesChangedEvent(Array.Empty<IAnalysisIssueVisualization>());

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
        public void Ctor_RegisterToSelectionChangedEvent()
        {
            selectionService.Received(1).SelectedIssueChanged += Arg.Any<EventHandler>();
            selectionService.ReceivedCalls().Should().HaveCount(1);
        }

        [TestMethod]
        public void Dispose_UnregisterFromSelectionChangedEvent()
        {
            testSubject.Dispose();

            selectionService.Received(1).SelectedIssueChanged -= Arg.Any<EventHandler>();
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

            eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(IHotspotsControlViewModel.SelectedHotspot)));
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

            eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(IHotspotsControlViewModel.SelectedHotspot)));
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
        public void Dispose_UnsubscribesFromActiveSolutionBoundTrackerEvents()
        {
            testSubject.Dispose();

            activeSolutionBoundTracker.ReceivedWithAnyArgs(1).SolutionBindingChanged -= Arg.Any<EventHandler<ActiveSolutionBindingEventArgs>>();
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
    }
}
