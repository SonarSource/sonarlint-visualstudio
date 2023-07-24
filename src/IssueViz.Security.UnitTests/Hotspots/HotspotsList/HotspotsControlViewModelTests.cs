/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2023 SonarSource SA
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

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.Models;
using SonarLint.VisualStudio.IssueVisualization.Selection;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Hotspots.HotspotsList
{
    [TestClass]
    public class HotspotsControlViewModelTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            // HotspotsControlViewModel needs to be created on the UI thread
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void Ctor_RegisterToStoreCollectionChanges()
        {
            var store = new Mock<ILocalHotspotsStore>();
            var testSubject = CreateTestSubject(hotspotsStore: store);

            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz3 = Mock.Of<IAnalysisIssueVisualization>();

            RaiseStoreIssuesChangedEvent(store, issueViz1);

            testSubject.Hotspots.Count.Should().Be(1);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[0].HotspotPriority.Should().Be(default(HotspotPriority));

            store.Setup(x => x.GetAllLocalHotspots()).Returns(new[] { new LocalHotspot(issueViz1, HotspotPriority.Low), new LocalHotspot(issueViz2, HotspotPriority.Medium), new LocalHotspot(issueViz3, HotspotPriority.High) });
            store.Raise(x => x.IssuesChanged += null, null, new IssuesStore.IssuesChangedEventArgs(null, null));

            testSubject.Hotspots.Count.Should().Be(3);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[0].HotspotPriority.Should().Be(HotspotPriority.Low);
            testSubject.Hotspots[1].Hotspot.Should().Be(issueViz2);
            testSubject.Hotspots[1].HotspotPriority.Should().Be(HotspotPriority.Medium);
            testSubject.Hotspots[2].Hotspot.Should().Be(issueViz3);
            testSubject.Hotspots[2].HotspotPriority.Should().Be(HotspotPriority.High);
        }

        [TestMethod]
        public async Task UpdateHotspotsListAsync_UpdatesWhenManuallyTriggered()
        {
            var store = new Mock<ILocalHotspotsStore>();
            var testSubject = CreateTestSubject(hotspotsStore: store);

            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz3 = Mock.Of<IAnalysisIssueVisualization>();

            store.Setup(x => x.GetAllLocalHotspots()).Returns(new[] { new LocalHotspot(issueViz1, HotspotPriority.Medium) });
            await testSubject.UpdateHotspotsListAsync();

            testSubject.Hotspots.Count.Should().Be(1);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[0].HotspotPriority.Should().Be(HotspotPriority.Medium);
            
            store.Setup(x => x.GetAllLocalHotspots()).Returns(new[] { new LocalHotspot(issueViz1, HotspotPriority.Low), new LocalHotspot(issueViz2, HotspotPriority.Medium), new LocalHotspot(issueViz3, HotspotPriority.High) });
            await testSubject.UpdateHotspotsListAsync();

            testSubject.Hotspots.Count.Should().Be(3);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[0].HotspotPriority.Should().Be(HotspotPriority.Low);
            testSubject.Hotspots[1].Hotspot.Should().Be(issueViz2);
            testSubject.Hotspots[1].HotspotPriority.Should().Be(HotspotPriority.Medium);
            testSubject.Hotspots[2].Hotspot.Should().Be(issueViz3);
            testSubject.Hotspots[2].HotspotPriority.Should().Be(HotspotPriority.High);
        }

        [TestMethod]
        public async Task UpdateHotspotsListAsync_RunsOnBackgroundThread()
        {
            var store = new Mock<ILocalHotspotsStore>();
            var threadHandling = new Mock<IThreadHandling>();
            SetupGetAllOnBackgroundThreadForUpdate(threadHandling, store);

            var testSubject = CreateTestSubject(hotspotsStore: store, threadHandling: threadHandling.Object);

            await testSubject.UpdateHotspotsListAsync();
            
            threadHandling.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()), Times.Once);
            store.Verify(x => x.GetAllLocalHotspots(), Times.Once);
        }

        [TestMethod]
        public void IssueChangedEventRaised_RunsOnBackgroundThread()
        {
            var store = new Mock<ILocalHotspotsStore>();
            var threadHandling = new Mock<IThreadHandling>();
            SetupGetAllOnBackgroundThreadForUpdate(threadHandling, store);
            
            var testSubject = CreateTestSubject(hotspotsStore: store, threadHandling: threadHandling.Object);

            RaiseStoreIssuesChangedEvent(store, Array.Empty<IAnalysisIssueVisualization>());
            
            threadHandling.Verify(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()), Times.Once);
            store.Verify(x => x.GetAllLocalHotspots(), Times.Once);
        }

        [TestMethod]
        public void Dispose_UnregisterFromHotspotsCollectionChanges()
        {
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(storeHotspots);

            testSubject.Dispose();

            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            storeHotspots.Add(issueViz);

            testSubject.Hotspots.Count.Should().Be(0);
        }

        [TestMethod]
        public void Ctor_RegisterToSelectionChangedEvent()
        {
            var selectionService = new Mock<IIssueSelectionService>();
            selectionService.SetupAdd(x => x.SelectedIssueChanged += null);

            CreateTestSubject(selectionService: selectionService.Object);

            selectionService.VerifyAdd(x => x.SelectedIssueChanged += It.IsAny<EventHandler>(), Times.Once());
            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromSelectionChangedEvent()
        {
            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            selectionService.Reset();
            selectionService.SetupRemove(x => x.SelectedIssueChanged -= null);

            testSubject.Dispose();

            selectionService.VerifyRemove(x => x.SelectedIssueChanged -= It.IsAny<EventHandler>(), Times.Once());
        }

        [TestMethod]
        public void SelectionChanged_SelectedHotspotExistsInList_HotspotSelected()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> { issueViz };
            var hotspotStore = new Mock<ILocalHotspotsStore>();
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(storeHotspots, hotspotsStore:hotspotStore, selectionService: selectionService.Object);

            RaiseStoreIssuesChangedEvent(hotspotStore, issueViz);

            testSubject.SelectedHotspot.Should().BeNull();

            RaiseSelectionChangedEvent(selectionService, issueViz);

            testSubject.SelectedHotspot.Should().NotBeNull();
            testSubject.SelectedHotspot.Hotspot.Should().Be(issueViz);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectionChanged_SelectedHotspotIsNotInList_SelectionSetToNull(bool isSelectedNull)
        {
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var oldSelection = new HotspotViewModel(Mock.Of<IAnalysisIssueVisualization>(), default);
            testSubject.SelectedHotspot = oldSelection;
            testSubject.SelectedHotspot.Should().Be(oldSelection);

            var selectedIssue = isSelectedNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            RaiseSelectionChangedEvent(selectionService, selectedIssue);

            testSubject.SelectedHotspot.Should().BeNull();
        }

        [TestMethod]
        public void SelectionChanged_SelectedHotspotExistsInList_RaisesPropertyChanged()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> { issueViz };
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(storeHotspots, selectionService: selectionService.Object);

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            RaiseSelectionChangedEvent(selectionService, issueViz);

            eventHandler.Verify(x => x(testSubject,
                It.Is((PropertyChangedEventArgs args) =>
                    args.PropertyName == nameof(IHotspotsControlViewModel.SelectedHotspot))), Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectionChanged_SelectedHotspotIsNotInList_RaisesPropertyChanged(bool isSelectedNull)
        {
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            var selectedIssue = isSelectedNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            RaiseSelectionChangedEvent(selectionService, selectedIssue);

            eventHandler.Verify(x => x(testSubject,
                It.Is((PropertyChangedEventArgs args) =>
                    args.PropertyName == nameof(IHotspotsControlViewModel.SelectedHotspot))), Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Verify that there is no callback, selection service -> property -> selection service")]
        public void SelectionChanged_HotspotSelected_SelectionServiceNotCalledAgain()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> { selectedIssue };
            var selectionService = new Mock<IIssueSelectionService>();

            CreateTestSubject(storeHotspots, selectionService: selectionService.Object);

            RaiseSelectionChangedEvent(selectionService, selectedIssue);

            selectionService.VerifySet(x=> x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never);
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_NullParameter_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);
            var result = testSubject.NavigateCommand.CanExecute(null);
            result.Should().BeFalse();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_ParameterIsNotHotspotViewModel_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);
            var result = testSubject.NavigateCommand.CanExecute("something");
            result.Should().BeFalse();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_ParameterIsHotspotViewModel_True()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);
            var result = testSubject.NavigateCommand.CanExecute(Mock.Of<IHotspotViewModel>());
            result.Should().BeTrue();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_Execute_LocationNavigated()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var hotspot = Mock.Of<IAnalysisIssueVisualization>();
            var viewModel = new Mock<IHotspotViewModel>();
            viewModel.Setup(x => x.Hotspot).Returns(hotspot);

            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);
            testSubject.NavigateCommand.Execute(viewModel.Object);

            locationNavigator.Verify(x => x.TryNavigate(hotspot), Times.Once);
            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SetSelectedHotspot_HotspotSet()
        {
            var testSubject = CreateTestSubject();

            var selection = new HotspotViewModel(Mock.Of<IAnalysisIssueVisualization>(), default);
            testSubject.SelectedHotspot = selection;

            testSubject.SelectedHotspot.Should().Be(selection);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedHotspot_SelectionChanged_SelectionServiceIsCalled(bool isSelectedNull)
        {
            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var oldSelection = isSelectedNull ? new HotspotViewModel(Mock.Of<IAnalysisIssueVisualization>(), default) : null;
            var newSelection = isSelectedNull ? null : new HotspotViewModel(Mock.Of<IAnalysisIssueVisualization>(), default);

            testSubject.SelectedHotspot = oldSelection;

            selectionService.Reset();

            testSubject.SelectedHotspot = newSelection;

            selectionService.VerifySet(x=> x.SelectedIssue = newSelection?.Hotspot, Times.Once);
            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SetSelectedHotspot_ValueIsTheSame_SelectionServiceNotCalled()
        {
            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var selection = new HotspotViewModel(Mock.Of<IAnalysisIssueVisualization>(), default);
            testSubject.SelectedHotspot = selection;

            selectionService.Reset();

            testSubject.SelectedHotspot = selection;

            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateToRuleDescriptionCommand_IsNotNull()
        {
            var navigateToRuleDescriptionCommand = Mock.Of<INavigateToRuleDescriptionCommand>();
            var testSubject = CreateTestSubject(navigateToRuleDescriptionCommand: navigateToRuleDescriptionCommand);

            testSubject.NavigateToRuleDescriptionCommand.Should()
                .BeSameAs(navigateToRuleDescriptionCommand);
        }

        private static HotspotsControlViewModel CreateTestSubject(
            ObservableCollection<IAnalysisIssueVisualization> originalCollection = null,
            ILocationNavigator locationNavigator = null,
            Mock<ILocalHotspotsStore> hotspotsStore = null,
            IIssueSelectionService selectionService = null,
            IThreadHandling threadHandling = null,
            INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand = null)
        {
            originalCollection ??= new ObservableCollection<IAnalysisIssueVisualization>();
            var readOnlyWrapper = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection);

            hotspotsStore ??= new Mock<ILocalHotspotsStore>();
            hotspotsStore.Setup(x => x.GetAllLocalHotspots()).Returns(readOnlyWrapper.Select(x => new LocalHotspot(x, default)).ToList());

            selectionService ??= Mock.Of<IIssueSelectionService>();
            navigateToRuleDescriptionCommand ??= Mock.Of<INavigateToRuleDescriptionCommand>();

            return new HotspotsControlViewModel(hotspotsStore.Object,
                locationNavigator,
                selectionService,
                threadHandling ?? new NoOpThreadHandler(), 
                navigateToRuleDescriptionCommand);
        }

        private static void RaiseStoreIssuesChangedEvent(Mock<ILocalHotspotsStore> store, params IAnalysisIssueVisualization[] issueVizs)
        {
            store.Setup(x => x.GetAllLocalHotspots()).Returns(issueVizs.Select(x => new LocalHotspot(x, default)).ToList());
            store.Raise(x => x.IssuesChanged += null, null, new IssuesStore.IssuesChangedEventArgs(null, null));
        }

        private static void RaiseSelectionChangedEvent(Mock<IIssueSelectionService> selectionService, IAnalysisIssueVisualization selectedIssue)
        {
            selectionService.Setup(x => x.SelectedIssue).Returns(selectedIssue);
            selectionService.Raise(x => x.SelectedIssueChanged += null, EventArgs.Empty);
        }
        
        private static void SetupGetAllOnBackgroundThreadForUpdate(Mock<IThreadHandling> threadHandling, Mock<ILocalHotspotsStore> store)
        {
            var callOrder = new MockSequence();
            threadHandling
                .InSequence(callOrder)
                .Setup(x => x.RunOnBackgroundThread(It.IsAny<Func<Task<bool>>>()))
                .Callback((Func<Task<bool>> f) => f());
            store
                .InSequence(callOrder)
                .Setup(x => x.GetAllLocalHotspots())
                .Returns(Array.Empty<LocalHotspot>());
        }
    }
}
