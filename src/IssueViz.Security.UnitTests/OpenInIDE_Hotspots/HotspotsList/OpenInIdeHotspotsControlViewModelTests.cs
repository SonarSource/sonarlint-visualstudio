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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE_Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.OpenInIDE_Hotspots.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.OpenInIDE_Hotspots.HotspotsList
{
    [TestClass]
    public class OpenInIDEHotspotsControlViewModelTests
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
            var store = new Mock<IOpenInIDEHotspotsStore>();
            var testSubject = CreateTestSubject(hotspotsStore: store);

            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz3 = Mock.Of<IAnalysisIssueVisualization>();

            RaiseStoreIssuesChangedEvent(store, issueViz1);

            testSubject.Hotspots.Count.Should().Be(1);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);

            RaiseStoreIssuesChangedEvent(store, issueViz1, issueViz2, issueViz3);

            testSubject.Hotspots.Count.Should().Be(3);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[1].Hotspot.Should().Be(issueViz2);
            testSubject.Hotspots[2].Hotspot.Should().Be(issueViz3);

            RaiseStoreIssuesChangedEvent(store, issueViz1, issueViz3);

            testSubject.Hotspots.Count.Should().Be(2);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[1].Hotspot.Should().Be(issueViz3);
        }

        [TestMethod]
        public void Ctor_InitializeListWithHotspotsStoreCollection()
        {
            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> { issueViz1, issueViz2 };

            var testSubject = CreateTestSubject(storeHotspots);

            testSubject.Hotspots.Count.Should().Be(2);
            testSubject.Hotspots.First().Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots.Last().Hotspot.Should().Be(issueViz2);
        }

        [TestMethod]
        public void NavigateToRuleDescriptionCommand_IsNotNull()
        {
            var navigateToRuleDescriptionCommand = Mock.Of<INavigateToRuleDescriptionCommand>();
            var testSubject = CreateTestSubject(navigateToRuleDescriptionCommand: navigateToRuleDescriptionCommand);

            testSubject.NavigateToRuleDescriptionCommand.Should()
                .BeSameAs(navigateToRuleDescriptionCommand);
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
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(storeHotspots, selectionService: selectionService.Object);

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

            var oldSelection = new OpenInIDEHotspotViewModel(Mock.Of<IAnalysisIssueVisualization>());
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
                    args.PropertyName == nameof(IOpenInIDEHotspotsControlViewModel.SelectedHotspot))), Times.Once);

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
                    args.PropertyName == nameof(IOpenInIDEHotspotsControlViewModel.SelectedHotspot))), Times.Once);

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
            var result = testSubject.NavigateCommand.CanExecute(Mock.Of<IOpenInIDEHotspotViewModel>());
            result.Should().BeTrue();

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_Execute_LocationNavigated()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var hotspot = Mock.Of<IAnalysisIssueVisualization>();
            var viewModel = new Mock<IOpenInIDEHotspotViewModel>();
            viewModel.Setup(x => x.Hotspot).Returns(hotspot);

            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);
            testSubject.NavigateCommand.Execute(viewModel.Object);

            locationNavigator.Verify(x => x.TryNavigate(hotspot), Times.Once);
            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void RemoveCommand_CanExecute_NullParameter_False()
        {
            var hotspotsStore = new Mock<IOpenInIDEHotspotsStore>();

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            var result = testSubject.RemoveCommand.CanExecute(null);
            result.Should().BeFalse();

            hotspotsStore.Verify(x => x.Remove(It.IsAny<IAnalysisIssueVisualization>()), Times.Never);
        }

        [TestMethod]
        public void RemoveCommand_CanExecute_ParameterIsNotHotspotViewModel_False()
        {
            var hotspotsStore = new Mock<IOpenInIDEHotspotsStore>();

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            var result = testSubject.RemoveCommand.CanExecute("something");
            result.Should().BeFalse();

            hotspotsStore.Verify(x => x.Remove(It.IsAny<IAnalysisIssueVisualization>()), Times.Never);
        }

        [TestMethod]
        public void RemoveCommand_CanExecute_ParameterIsHotspotViewModel_True()
        {
            var hotspotsStore = new Mock<IOpenInIDEHotspotsStore>();

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            var result = testSubject.RemoveCommand.CanExecute(Mock.Of<IOpenInIDEHotspotViewModel>());
            result.Should().BeTrue();

            hotspotsStore.Verify(x => x.Remove(It.IsAny<IAnalysisIssueVisualization>()), Times.Never);
        }

        [TestMethod]
        public void RemoveCommand_Execute_HotspotRemoved()
        {
            var hotspotsStore = new Mock<IOpenInIDEHotspotsStore>();

            var hotspot = Mock.Of<IAnalysisIssueVisualization>();
            var viewModel = new Mock<IOpenInIDEHotspotViewModel>();
            viewModel.Setup(x => x.Hotspot).Returns(hotspot);

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            testSubject.RemoveCommand.Execute(viewModel.Object);

            hotspotsStore.Verify(x => x.Remove(hotspot), Times.Once);
        }

        [TestMethod]
        public void SetSelectedHotspot_HotspotSet()
        {
            var testSubject = CreateTestSubject();

            var selection = new OpenInIDEHotspotViewModel(Mock.Of<IAnalysisIssueVisualization>());
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

            var oldSelection = isSelectedNull ? new OpenInIDEHotspotViewModel(Mock.Of<IAnalysisIssueVisualization>()) : null;
            var newSelection = isSelectedNull ? null : new OpenInIDEHotspotViewModel(Mock.Of<IAnalysisIssueVisualization>());

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

            var selection = new OpenInIDEHotspotViewModel(Mock.Of<IAnalysisIssueVisualization>());
            testSubject.SelectedHotspot = selection;

            selectionService.Reset();

            testSubject.SelectedHotspot = selection;

            selectionService.VerifyNoOtherCalls();
        }

        private static OpenInIDEHotspotsControlViewModel CreateTestSubject(ObservableCollection<IAnalysisIssueVisualization> originalCollection = null,
            ILocationNavigator locationNavigator = null,
            Mock<IOpenInIDEHotspotsStore> hotspotsStore = null,
            IIssueSelectionService selectionService = null,
            INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand = null)
        {
            originalCollection ??= new ObservableCollection<IAnalysisIssueVisualization>();
            var readOnlyWrapper = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection);

            hotspotsStore ??= new Mock<IOpenInIDEHotspotsStore>();
            hotspotsStore.Setup(x => x.GetAll()).Returns(readOnlyWrapper);

            selectionService ??= Mock.Of<IIssueSelectionService>();
            navigateToRuleDescriptionCommand ??= Mock.Of<INavigateToRuleDescriptionCommand>();

            return new OpenInIDEHotspotsControlViewModel(hotspotsStore.Object, locationNavigator, selectionService, navigateToRuleDescriptionCommand);
        }

        private static void RaiseStoreIssuesChangedEvent(Mock<IOpenInIDEHotspotsStore> store, params IAnalysisIssueVisualization[] issueVizs)
        {
            store.Setup(x => x.GetAll()).Returns(issueVizs);
            store.Raise(x => x.IssuesChanged += null, null, new IssuesStore.IssuesChangedEventArgs(null, null));
        }

        private static void RaiseSelectionChangedEvent(Mock<IIssueSelectionService> selectionService, IAnalysisIssueVisualization selectedIssue)
        {
            selectionService.Setup(x => x.SelectedIssue).Returns(selectedIssue);
            selectionService.Raise(x => x.SelectedIssueChanged += null, EventArgs.Empty);
        }
    }
}
