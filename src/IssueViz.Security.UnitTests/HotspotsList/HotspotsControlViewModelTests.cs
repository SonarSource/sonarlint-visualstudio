/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.HotspotsList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Security.SelectionService;
using SonarLint.VisualStudio.IssueVisualization.Security.Store;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.HotspotsList
{
    [TestClass]
    public class HotspotsControlViewModelTests
    {
        [TestMethod]
        public void Ctor_RegisterToHotspotsCollectionChanges()
        {
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization>();
            var testSubject = CreateTestSubject(storeHotspots);

            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz3 = Mock.Of<IAnalysisIssueVisualization>();

            storeHotspots.Add(issueViz1);

            testSubject.Hotspots.Count.Should().Be(1);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);

            storeHotspots.Add(issueViz2);
            storeHotspots.Add(issueViz3);

            testSubject.Hotspots.Count.Should().Be(3);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[1].Hotspot.Should().Be(issueViz2);
            testSubject.Hotspots[2].Hotspot.Should().Be(issueViz3);

            storeHotspots.Remove(issueViz2);

            testSubject.Hotspots.Count.Should().Be(2);
            testSubject.Hotspots[0].Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots[1].Hotspot.Should().Be(issueViz3);
        }

        [TestMethod]
        public void Ctor_InitializeListWithHotspotsStoreCollection()
        {
            var issueViz1 = Mock.Of<IAnalysisIssueVisualization>();
            var issueViz2 = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> {issueViz1, issueViz2};

            var testSubject = CreateTestSubject(storeHotspots);

            testSubject.Hotspots.Count.Should().Be(2);
            testSubject.Hotspots.First().Hotspot.Should().Be(issueViz1);
            testSubject.Hotspots.Last().Hotspot.Should().Be(issueViz2);
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
            var selectionService = new Mock<IHotspotsSelectionService>();
            selectionService.SetupAdd(x => x.SelectionChanged += null);

            CreateTestSubject(selectionService: selectionService.Object);

            selectionService.VerifyAdd(x => x.SelectionChanged += It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once());
            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromSelectionChangedEvent()
        {
            var selectionService = new Mock<IHotspotsSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            selectionService.Reset();
            selectionService.SetupRemove(x => x.SelectionChanged -= null);

            testSubject.Dispose();

            selectionService.VerifyRemove(x => x.SelectionChanged -= It.IsAny<EventHandler<SelectionChangedEventArgs>>(), Times.Once());
        }

        [TestMethod]
        public void SelectionChanged_SelectedHotspotExistsInList_HotspotSelected()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> {issueViz};
            var selectionService = new Mock<IHotspotsSelectionService>();

            var testSubject = CreateTestSubject(storeHotspots, selectionService: selectionService.Object);

            testSubject.SelectedHotspot.Should().BeNull();

            selectionService.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(issueViz));

            testSubject.SelectedHotspot.Should().NotBeNull();
            testSubject.SelectedHotspot.Hotspot.Should().Be(issueViz);
        }

        [TestMethod]
        public void SelectionChanged_SelectedHotspotIsNotInList_SelectionSetToNull()
        {
            var selectionService = new Mock<IHotspotsSelectionService>();

            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            testSubject.SelectedHotspot.Should().BeNull();

            selectionService.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(Mock.Of<IAnalysisIssueVisualization>()));

            testSubject.SelectedHotspot.Should().BeNull();
        }

        [TestMethod]
        public void SelectionChanged_SelectedHotspotExistsInList_RaisesPropertyChanged()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new ObservableCollection<IAnalysisIssueVisualization> { issueViz };
            var selectionService = new Mock<IHotspotsSelectionService>();

            var testSubject = CreateTestSubject(storeHotspots, selectionService: selectionService.Object);

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            selectionService.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(issueViz));

            eventHandler.Verify(x => x(testSubject,
                It.Is((PropertyChangedEventArgs args) =>
                    args.PropertyName == nameof(IHotspotsControlViewModel.SelectedHotspot))), Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SelectionChanged_SelectedHotspotIsNotInList_RaisesPropertyChanged()
        {
            var selectionService = new Mock<IHotspotsSelectionService>();

            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            selectionService.Raise(x => x.SelectionChanged += null, new SelectionChangedEventArgs(Mock.Of<IAnalysisIssueVisualization>()));

            eventHandler.Verify(x => x(testSubject,
                It.Is((PropertyChangedEventArgs args) =>
                    args.PropertyName == nameof(IHotspotsControlViewModel.SelectedHotspot))), Times.Once);

            eventHandler.VerifyNoOtherCalls();
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
        public void RemoveCommand_CanExecute_NullParameter_False()
        {
            var hotspotsStore = new Mock<IHotspotsStore>();

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            var result = testSubject.RemoveCommand.CanExecute(null);
            result.Should().BeFalse();

            hotspotsStore.Verify(x => x.Remove(It.IsAny<IAnalysisIssueVisualization>()), Times.Never);
        }

        [TestMethod]
        public void RemoveCommand_CanExecute_ParameterIsNotHotspotViewModel_False()
        {
            var hotspotsStore = new Mock<IHotspotsStore>();

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            var result = testSubject.RemoveCommand.CanExecute("something");
            result.Should().BeFalse();

            hotspotsStore.Verify(x => x.Remove(It.IsAny<IAnalysisIssueVisualization>()), Times.Never);
        }

        [TestMethod]
        public void RemoveCommand_CanExecute_ParameterIsHotspotViewModel_True()
        {
            var hotspotsStore = new Mock<IHotspotsStore>();

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            var result = testSubject.RemoveCommand.CanExecute(Mock.Of<IHotspotViewModel>());
            result.Should().BeTrue();

            hotspotsStore.Verify(x => x.Remove(It.IsAny<IAnalysisIssueVisualization>()), Times.Never);
        }

        [TestMethod]
        public void RemoveCommand_Execute_HotspotRemoved()
        {
            var hotspotsStore = new Mock<IHotspotsStore>();

            var hotspot = Mock.Of<IAnalysisIssueVisualization>();
            var viewModel = new Mock<IHotspotViewModel>();
            viewModel.Setup(x => x.Hotspot).Returns(hotspot);

            var testSubject = CreateTestSubject(hotspotsStore: hotspotsStore);
            testSubject.RemoveCommand.Execute(viewModel.Object);

            hotspotsStore.Verify(x => x.Remove(hotspot), Times.Once);
        }

        private static HotspotsControlViewModel CreateTestSubject(ObservableCollection<IAnalysisIssueVisualization> originalCollection = null,
            ILocationNavigator locationNavigator = null,
            Mock<IHotspotsStore> hotspotsStore = null,
            IHotspotsSelectionService selectionService = null)
        {
            originalCollection ??= new ObservableCollection<IAnalysisIssueVisualization>();
            var readOnlyWrapper = new ReadOnlyObservableCollection<IAnalysisIssueVisualization>(originalCollection);

            hotspotsStore ??= new Mock<IHotspotsStore>();
            hotspotsStore.Setup(x => x.GetAll()).Returns(readOnlyWrapper);

            selectionService ??= Mock.Of<IHotspotsSelectionService>();

            return new HotspotsControlViewModel(hotspotsStore.Object, locationNavigator, selectionService);
        }
    }
}
