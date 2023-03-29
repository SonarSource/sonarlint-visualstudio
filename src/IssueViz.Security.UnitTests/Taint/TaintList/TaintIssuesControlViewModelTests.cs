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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Input;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text;
using Moq;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.TestInfrastructure;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.TaintList.ViewModels;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarQube.Client;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.Taint.TaintList
{
    [TestClass]
    public class TaintIssuesControlViewModelTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            // The ViewModel needs to be created on the UI thread
            ThreadHelper.SetCurrentThreadAsUIThread();
        }

        [TestMethod]
        public void Ctor_RegisterToStoreCollectionChanges()
        {
            var store = new Mock<ITaintStore>();
            var testSubject = CreateTestSubject(store: store);

            var issueViz1 = CreateIssueViz();
            var issueViz2 = CreateIssueViz();
            var issueViz3 = CreateIssueViz();

            RaiseStoreIssuesChangedEvent(store, issueViz1);

            var issues = GetSourceItems(testSubject);
            issues.Count.Should().Be(1);
            issues[0].TaintIssueViz.Should().Be(issueViz1);

            RaiseStoreIssuesChangedEvent(store, issueViz1, issueViz2, issueViz3);

            issues.Count.Should().Be(3);
            issues[0].TaintIssueViz.Should().Be(issueViz1);
            issues[1].TaintIssueViz.Should().Be(issueViz2);
            issues[2].TaintIssueViz.Should().Be(issueViz3);

            RaiseStoreIssuesChangedEvent(store, issueViz1, issueViz3);

            issues.Count.Should().Be(2);
            issues[0].TaintIssueViz.Should().Be(issueViz1);
            issues[1].TaintIssueViz.Should().Be(issueViz3);
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
        public void Ctor_InitializeListWithStoreCollection()
        {
            var issueViz1 = CreateIssueViz();
            var issueViz2 = CreateIssueViz();
            var storeCollection = new[] { issueViz1, issueViz2 };

            var testSubject = CreateTestSubject(storeCollection);

            var issues = GetSourceItems(testSubject);
            issues.Count.Should().Be(2);
            issues.First().TaintIssueViz.Should().Be(issueViz1);
            issues.Last().TaintIssueViz.Should().Be(issueViz2);
        }

        [TestMethod]
        public void Ctor_DefaultSortOrder_IsByCreationTimestampDescending()
        {
            const string filePath = "file1.txt";
            var oldestIssueViz = CreateIssueViz(filePath, created: DateTimeOffset.Parse("2010-01-31T01:02:03+0000"));
            var middleIssueViz = CreateIssueViz(filePath, created: DateTimeOffset.Parse("2012-06-30T01:02:03+0000"));
            var newestIssueViz = CreateIssueViz(filePath, created: DateTimeOffset.Parse("2020-09-01T01:02:03+0000"));
            var storeCollection = new[] { middleIssueViz, oldestIssueViz, newestIssueViz };

            var locator = CreateLocatorAndSetActiveDocument(filePath);

            var testSubject = CreateTestSubject(storeCollection, activeDocumentLocator: locator);

            // Check source collection ordering (should be in creation order)
            var issues = GetSourceItems(testSubject);
            issues.Count.Should().Be(3);
            issues.Select(x => x.TaintIssueViz).Should().ContainInOrder(middleIssueViz, oldestIssueViz, newestIssueViz);

            // Check the view ordering (should be sorted)
            var sortedIssueVizs = GetIssueVizsFromView(testSubject);
            sortedIssueVizs.Count.Should().Be(3);

            sortedIssueVizs.Should().ContainInOrder(newestIssueViz, middleIssueViz, oldestIssueViz);
        }

        [TestMethod]
        public void Ctor_NoActiveDocument_NoIssuesDisplayed()
        {
            var storeCollection = new[] { CreateIssueViz() };

            var locator = CreateLocatorAndSetActiveDocument(null);

            var testSubject = CreateTestSubject(storeCollection, activeDocumentLocator: locator);
            CheckExpectedSourceIssueCount(testSubject, 1);

            VerifyFilterIsNotNull(testSubject);

            var filteredItems = GetIssueVizsFromView(testSubject);
            filteredItems.Count().Should().Be(0);
        }

        [TestMethod]
        public void Ctor_ActiveDocumentExists_SuppressedIssuesAreFilteredOut()
        {
            var location1 = CreateLocationViz("current.cpp");
            var issueViz1 = CreateIssueViz(null, locations: new[] { location1 }, isSuppressed: true);

            var location2 = CreateLocationViz(null);
            var issueViz2 = CreateIssueViz("current.cpp", locations: new[] { location2 }, isSuppressed: false);

            var storeCollection = new[] { issueViz1, issueViz2 };

            var locator = CreateLocatorAndSetActiveDocument("current.cpp");

            var testSubject = CreateTestSubject(storeCollection, activeDocumentLocator: locator);

            CheckExpectedSourceIssueCount(testSubject, 2);

            VerifyFilterIsNotNull(testSubject);

            var filteredItems = GetIssueVizsFromView(testSubject);
            filteredItems.Count.Should().Be(1);
            filteredItems[0].Should().Be(issueViz2);
        }

        [TestMethod]
        public void Ctor_ActiveDocumentExists_IssuesFilteredForActiveFilePath()
        {
            var location1 = CreateLocationViz("current.cpp");
            var issueViz1 = CreateIssueViz(null, locations: new[] {location1});

            var location2 = CreateLocationViz(null);
            var issueViz2 = CreateIssueViz("current.cpp", locations: new[] {location2});

            var location3 = CreateLocationViz("someOtherFile.cpp");
            var issueViz3 = CreateIssueViz(null, locations: new[] { location3 });

            var storeCollection = new[] { issueViz1, issueViz2, issueViz3 };

            var locator = CreateLocatorAndSetActiveDocument("current.cpp");

            var testSubject = CreateTestSubject(storeCollection, activeDocumentLocator: locator);

            CheckExpectedSourceIssueCount(testSubject, 3);

            VerifyFilterIsNotNull(testSubject);

            var filteredItems = GetIssueVizsFromView(testSubject);
            filteredItems.Count.Should().Be(2);
            filteredItems[0].Should().Be(issueViz1);
            filteredItems[1].Should().Be(issueViz2);
        }

        [TestMethod]
        public void Ctor_RegisterToActiveDocumentChanges()
        {
            var activeDocumentTracker = new Mock<IActiveDocumentTracker>();
            activeDocumentTracker.SetupAdd(x => x.ActiveDocumentChanged += null);

            CreateTestSubject(activeDocumentTracker: activeDocumentTracker.Object);

            activeDocumentTracker.VerifyAdd(x => x.ActiveDocumentChanged += It.IsAny<EventHandler<ActiveDocumentChangedEventArgs>>(), Times.Once);
            activeDocumentTracker.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(ServerType.SonarCloud, nameof(ServerType.SonarCloud))]
        [DataRow(ServerType.SonarQube, nameof(ServerType.SonarQube))]
        [DataRow(null, "")]
        public void Ctor_ExpectedServerTypeSet(ServerType? serverType, string expectedValue)
        {
            var sonarQubeServiceMock = new Mock<ISonarQubeService>();
            SetServerType(serverType, sonarQubeServiceMock);

            var testSubject = CreateTestSubject(sonarQubeService: sonarQubeServiceMock.Object);

            testSubject.ServerType.Should().Be(expectedValue);
        }

        [TestMethod]
        public void Dispose_UnregisterFromActiveDocumentChanges()
        {
            var activeDocumentTracker = new Mock<IActiveDocumentTracker>();
            activeDocumentTracker.SetupRemove(x => x.ActiveDocumentChanged -= null);

            var testSubject = CreateTestSubject(activeDocumentTracker: activeDocumentTracker.Object);
            activeDocumentTracker.Invocations.Clear();

            testSubject.Dispose();

            activeDocumentTracker.VerifyRemove(x => x.ActiveDocumentChanged -= It.IsAny<EventHandler<ActiveDocumentChangedEventArgs>>(), Times.Once);
            activeDocumentTracker.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Dispose_UnregisterFromStoreCollectionChanges()
        {
            var store = new Mock<ITaintStore>();
            var testSubject = CreateTestSubject(store: store);

            testSubject.Dispose();

            RaiseStoreIssuesChangedEvent(store, CreateIssueViz());

            CheckExpectedSourceIssueCount(testSubject, 0);
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
        public void ShowVisualizationPaneCommand_CanExecute_NullParameter_False()
        {
            var menuCommandService = new Mock<IMenuCommandService>();
            var testSubject = CreateTestSubject(menuCommandService: menuCommandService.Object);

            VerifyCommandExecution(testSubject.ShowVisualizationPaneCommand, null, false);

            menuCommandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowVisualizationPaneCommand_CanExecute_ParameterIsNotTaintViewModel_False()
        {
            var menuCommandService = new Mock<IMenuCommandService>();
            var testSubject = CreateTestSubject(menuCommandService: menuCommandService.Object);

            VerifyCommandExecution(testSubject.ShowVisualizationPaneCommand, "something", false);

            menuCommandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowVisualizationPaneCommand_CanExecute_ParameterIsTaintViewModel_True()
        {
            var menuCommandService = new Mock<IMenuCommandService>();
            var testSubject = CreateTestSubject(menuCommandService: menuCommandService.Object);

            VerifyCommandExecution(testSubject.ShowVisualizationPaneCommand, Mock.Of<ITaintIssueViewModel>(), true);

            menuCommandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowVisualizationPaneCommand_Execute_LocationNavigated()
        {
            var menuCommandService = new Mock<IMenuCommandService>();

            var viewModel = Mock.Of<ITaintIssueViewModel>();

            var testSubject = CreateTestSubject(menuCommandService: menuCommandService.Object);
            testSubject.ShowVisualizationPaneCommand.Execute(viewModel);

            var cmdID = new CommandID(IssueVisualization.Commands.Constants.CommandSetGuid, IssueVisualization.Commands.Constants.ViewToolWindowCommandId);

            menuCommandService.Verify(x => x.GlobalInvoke(cmdID), Times.Once);
            menuCommandService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowVisualizationPaneCommand_Execute_TelemetryUpdated()
        {
            var menuCommandService = new Mock<IMenuCommandService>();

            var viewModel = Mock.Of<ITaintIssueViewModel>();

            var telemetryManager = new Mock<ITelemetryManager>();
            var testSubject = CreateTestSubject(telemetryManager: telemetryManager.Object);

            testSubject.ShowVisualizationPaneCommand.Execute(viewModel);

            telemetryManager.Verify(x => x.TaintIssueInvestigatedLocally(), Times.Once);
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_NullParameter_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);

            VerifyCommandExecution(testSubject.NavigateCommand, null, false);

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_ParameterIsNotTaintViewModel_False()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);

            VerifyCommandExecution(testSubject.NavigateCommand, "something", false);

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_CanExecute_ParameterIsTaintViewModel_True()
        {
            var locationNavigator = new Mock<ILocationNavigator>();
            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);

            VerifyCommandExecution(testSubject.NavigateCommand, Mock.Of<ITaintIssueViewModel>(), true);

            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void NavigateCommand_Execute_LocationNavigated()
        {
            var locationNavigator = new Mock<ILocationNavigator>();

            var issueViz = CreateIssueViz();
            var viewModel = new Mock<ITaintIssueViewModel>();
            viewModel.Setup(x => x.TaintIssueViz).Returns(issueViz);

            var testSubject = CreateTestSubject(locationNavigator: locationNavigator.Object);
            testSubject.NavigateCommand.Execute(viewModel.Object);

            locationNavigator.Verify(x => x.TryNavigate(issueViz), Times.Once);
            locationNavigator.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void Navigate_Execute_TelemetryUpdated()
        {
            var issueViz = CreateIssueViz(issueKey: "issue key 123");
            var viewModel = new Mock<ITaintIssueViewModel>();
            viewModel.Setup(x => x.TaintIssueViz).Returns(issueViz);

            var telemetryManager = new Mock<ITelemetryManager>();
            var testSubject = CreateTestSubject(telemetryManager: telemetryManager.Object);

            testSubject.NavigateCommand.Execute(viewModel.Object);

            telemetryManager.Verify(x => x.TaintIssueInvestigatedLocally(), Times.Once);
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowInBrowserCommand_CanExecute_NullParameter_False()
        {
            var showInBrowserService = new Mock<IShowInBrowserService>();
            var testSubject = CreateTestSubject(showInBrowserService: showInBrowserService.Object);

            VerifyCommandExecution(testSubject.ShowInBrowserCommand, null, false);

            showInBrowserService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowInBrowserCommand_CanExecute_ParameterIsNotTaintViewModel_False()
        {
            var showInBrowserService = new Mock<IShowInBrowserService>();
            var testSubject = CreateTestSubject(showInBrowserService: showInBrowserService.Object);

            VerifyCommandExecution(testSubject.ShowInBrowserCommand, "something", false);

            showInBrowserService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowInBrowserCommand_CanExecute_ParameterIsTaintViewModel_True()
        {
            var showInBrowserService = new Mock<IShowInBrowserService>();
            var testSubject = CreateTestSubject(showInBrowserService: showInBrowserService.Object);

            VerifyCommandExecution(testSubject.ShowInBrowserCommand, Mock.Of<ITaintIssueViewModel>(), true);

            showInBrowserService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowInBrowserCommand_Execute_BrowserServiceIsCalled()
        {
            var issueViz = CreateIssueViz(issueKey: "issue key 123");
            var viewModel = new Mock<ITaintIssueViewModel>();
            viewModel.Setup(x => x.TaintIssueViz).Returns(issueViz);

            var showInBrowserService = new Mock<IShowInBrowserService>();
            var testSubject = CreateTestSubject(showInBrowserService: showInBrowserService.Object);

            testSubject.ShowInBrowserCommand.Execute(viewModel.Object);

            showInBrowserService.Verify(x => x.ShowIssue("issue key 123"), Times.Once);
            showInBrowserService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ShowInBrowserCommand_Execute_TelemetryUpdated()
        {
            var issueViz = CreateIssueViz(issueKey: "issue key 123");
            var viewModel = new Mock<ITaintIssueViewModel>();
            viewModel.Setup(x => x.TaintIssueViz).Returns(issueViz);

            var telemetryManager = new Mock<ITelemetryManager>();
            var testSubject = CreateTestSubject(telemetryManager: telemetryManager.Object);

            testSubject.ShowInBrowserCommand.Execute(viewModel.Object);

            telemetryManager.Verify(x => x.TaintIssueInvestigatedRemotely(), Times.Once);
            telemetryManager.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void ActiveDocumentChanged_NoActiveDocument_NoIssuesDisplayed()
        {
            var storeCollection = new[] { CreateIssueViz() };

            var activeDocumentTracker = new Mock<IActiveDocumentTracker>();

            var testSubject = CreateTestSubject(storeCollection, activeDocumentTracker: activeDocumentTracker.Object);

            activeDocumentTracker.Raise(x => x.ActiveDocumentChanged += null, new ActiveDocumentChangedEventArgs(null));

            CheckExpectedSourceIssueCount(testSubject, 1);

            VerifyFilterIsNotNull(testSubject);

            var filteredItems = GetIssueVizsFromView(testSubject);
            filteredItems.Count().Should().Be(0);
        }

        [TestMethod]
        public void ActiveDocumentChanged_ActiveDocumentExists_IssuesFilteredForActiveFilePath()
        {
            var issueViz1 = CreateIssueViz("test1.cpp");
            var issueViz2 = CreateIssueViz("test2.cpp");
            var storeCollection = new[] { issueViz1, issueViz2 };

            var activeDocumentTracker = new Mock<IActiveDocumentTracker>();

            var testSubject = CreateTestSubject(storeCollection, activeDocumentTracker: activeDocumentTracker.Object);

            var activeDocument = new Mock<ITextDocument>();
            activeDocument.Setup(x => x.FilePath).Returns("test2.cpp");

            activeDocumentTracker.Raise(x => x.ActiveDocumentChanged += null, new ActiveDocumentChangedEventArgs(activeDocument.Object));

            CheckExpectedSourceIssueCount(testSubject, 2);

            VerifyFilterIsNotNull(testSubject);

            var filteredItems = GetIssueVizsFromView(testSubject);
            filteredItems.Count().Should().Be(1);
            filteredItems[0].Should().Be(issueViz2);
        }

        [TestMethod]
        public void HasServerIssues_IssuesExist_True()
        {
            var storeCollection = new[] { Mock.Of<IAnalysisIssueVisualization>() };

            var testSubject = CreateTestSubject(storeCollection);

            testSubject.HasServerIssues.Should().BeTrue();
        }

        [TestMethod]
        public void HasServerIssues_NoIssues_False()
        {
            var storeCollection = Array.Empty<IAnalysisIssueVisualization>();

            var testSubject = CreateTestSubject(storeCollection);

            testSubject.HasServerIssues.Should().BeFalse();
        }

        [TestMethod]
        public void HasServerIssues_IssuesChanged_RaisesPropertyChanged()
        {
            var store = new Mock<ITaintStore>();
            var testSubject = CreateTestSubject(store: store);

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            RaiseStoreIssuesChangedEvent(store, Mock.Of<IAnalysisIssueVisualization>());

            VerifyPropertyChangedWasRaised(eventHandler, nameof(testSubject.HasServerIssues));
            eventHandler.Reset();

            RaiseStoreIssuesChangedEvent(store);

            VerifyPropertyChangedWasRaised(eventHandler, nameof(testSubject.HasServerIssues));
        }

        [TestMethod]
        public void SelectionChanged_SelectedIssueExistsInList_IssueSelected()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            var storeIssues = new[] { issueViz };
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(storeIssues, selectionService: selectionService.Object);

            testSubject.SelectedIssue.Should().BeNull();

            RaiseSelectionChangedEvent(selectionService, issueViz);

            testSubject.SelectedIssue.Should().NotBeNull();
            testSubject.SelectedIssue.TaintIssueViz.Should().Be(issueViz);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectionChanged_SelectedIssueIsNotInList_SelectionSetToNull(bool isSelectedNull)
        {
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var oldSelection = new TaintIssueViewModel(Mock.Of<IAnalysisIssueVisualization>());
            testSubject.SelectedIssue = oldSelection;
            testSubject.SelectedIssue.Should().Be(oldSelection);

            var selectedIssue = isSelectedNull ? null : Mock.Of<IAnalysisIssueVisualization>();

            RaiseSelectionChangedEvent(selectionService, selectedIssue);

            testSubject.SelectedIssue.Should().BeNull();
        }

        [TestMethod]
        public void SelectionChanged_SelectedIssueExistsInList_RaisesPropertyChanged()
        {
            var issueViz = Mock.Of<IAnalysisIssueVisualization>();
            var storeHotspots = new[] {issueViz};
            var selectionService = new Mock<IIssueSelectionService>();

            var testSubject = CreateTestSubject(storeHotspots, selectionService: selectionService.Object);

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            eventHandler.VerifyNoOtherCalls();

            RaiseSelectionChangedEvent(selectionService, issueViz);

            eventHandler.Verify(x => x(testSubject,
                It.Is((PropertyChangedEventArgs args) =>
                    args.PropertyName == nameof(testSubject.SelectedIssue))), Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SelectionChanged_SelectedIssueIsNotInList_RaisesPropertyChanged(bool isSelectedNull)
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
                    args.PropertyName == nameof(testSubject.SelectedIssue))), Times.Once);

            eventHandler.VerifyNoOtherCalls();
        }

        [TestMethod]
        [Microsoft.VisualStudio.TestTools.UnitTesting.Description("Verify that there is no callback, selection service -> property -> selection service")]
        public void SelectionChanged_IssueSelected_SelectionServiceNotCalledAgain()
        {
            var selectedIssue = Mock.Of<IAnalysisIssueVisualization>();
            var storeIssues = new [] { selectedIssue };
            var selectionService = new Mock<IIssueSelectionService>();

            CreateTestSubject(storeIssues, selectionService: selectionService.Object);

            RaiseSelectionChangedEvent(selectionService, selectedIssue);

            selectionService.VerifySet(x => x.SelectedIssue = It.IsAny<IAnalysisIssueVisualization>(), Times.Never);
        }

        [TestMethod]
        public void SetSelectedIssue_IssueSet()
        {
            var testSubject = CreateTestSubject();

            var selection = new TaintIssueViewModel(Mock.Of<IAnalysisIssueVisualization>());
            testSubject.SelectedIssue = selection;

            testSubject.SelectedIssue.Should().Be(selection);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void SetSelectedIssue_SelectionChanged_SelectionServiceIsCalled(bool isSelectedNull)
        {
            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var oldSelection = isSelectedNull ? new TaintIssueViewModel(Mock.Of<IAnalysisIssueVisualization>()) : null;
            var newSelection = isSelectedNull ? null : new TaintIssueViewModel(Mock.Of<IAnalysisIssueVisualization>());

            testSubject.SelectedIssue = oldSelection;

            selectionService.Reset();

            testSubject.SelectedIssue = newSelection;

            selectionService.VerifySet(x => x.SelectedIssue = newSelection?.TaintIssueViz, Times.Once);
            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void SetSelectedIssue_ValueIsTheSame_SelectionServiceNotCalled()
        {
            var selectionService = new Mock<IIssueSelectionService>();
            var testSubject = CreateTestSubject(selectionService: selectionService.Object);

            var selection = new TaintIssueViewModel(Mock.Of<IAnalysisIssueVisualization>());
            testSubject.SelectedIssue = selection;

            selectionService.Reset();

            testSubject.SelectedIssue = selection;

            selectionService.VerifyNoOtherCalls();
        }

        [TestMethod]
        public void AnalysisInformation_NoAnalysisInformation_Null()
        {
            var store = new Mock<ITaintStore>();
            SetupAnalysisInformation(store, null);

            var testSubject = CreateTestSubject(store: store);

            testSubject.AnalysisInformation.Should().BeNull();
        }

        [TestMethod]
        public void AnalysisInformation_HasAnalysisInformation_PropertySet()
        {
            var store = new Mock<ITaintStore>();
            var analysisInformation = new AnalysisInformation("some branch", default);
            SetupAnalysisInformation(store, analysisInformation);

            var testSubject = CreateTestSubject(store: store);

            testSubject.AnalysisInformation.Should().BeSameAs(analysisInformation);
        }

        [TestMethod]
        public void AnalysisInformation_IssuesChanged_RaisesPropertyChanged()
        {
            var store = new Mock<ITaintStore>();
            var testSubject = CreateTestSubject(store: store);

            var eventHandler = new Mock<PropertyChangedEventHandler>();
            testSubject.PropertyChanged += eventHandler.Object;

            var analysisInformation = new AnalysisInformation("some branch", default);

            SetupAnalysisInformation(store, analysisInformation);
            RaiseStoreIssuesChangedEvent(store);

            VerifyPropertyChangedWasRaised(eventHandler, nameof(testSubject.AnalysisInformation));

            testSubject.AnalysisInformation.Should().BeSameAs(analysisInformation);
        }

        [TestMethod]
        [DataRow(null, ServerType.SonarCloud, nameof(ServerType.SonarCloud), true)]
        [DataRow(null, null, "", false)]
        [DataRow(ServerType.SonarCloud, null, "", true)]
        [DataRow(ServerType.SonarCloud, ServerType.SonarCloud, nameof(ServerType.SonarCloud), false)]
        [DataRow(ServerType.SonarCloud, ServerType.SonarQube, nameof(ServerType.SonarQube), true)]
        public void ActiveDocChanged_ExpectedServerTypeSetOnlyWhenItIsChanged(
            ServerType? originalServerType,
            ServerType? newServerType, 
            string expectedValue,
            bool expectedRaiseEvent)
        {
            var activeDocumentTrackerMock = new Mock<IActiveDocumentTracker>();
            var sonarQubeServiceMock = new Mock<ISonarQubeService>();
            SetServerType(originalServerType, sonarQubeServiceMock);

            var testSubject = CreateTestSubject(activeDocumentTracker: activeDocumentTrackerMock.Object, sonarQubeService:sonarQubeServiceMock.Object);
            var eventCount = 0;
            testSubject.PropertyChanged += (sender, args) => {
                if (args is { PropertyName: nameof(testSubject.ServerType) })
                {
                    eventCount++;
                }
            };
            SetServerType(newServerType, sonarQubeServiceMock);

            RaiseActiveDocumentChangedEvent(activeDocumentTrackerMock);

            testSubject.ServerType.Should().Be(expectedValue);
            eventCount.Should().Be(expectedRaiseEvent ? 1 : 0);
        }

        private static TaintIssuesControlViewModel CreateTestSubject(
            IAnalysisIssueVisualization[] issueVizs = null,
            ILocationNavigator locationNavigator = null,
            Mock<ITaintStore> store = null,
            IActiveDocumentTracker activeDocumentTracker = null,
            IActiveDocumentLocator activeDocumentLocator = null,
            ITelemetryManager telemetryManager = null,
            IShowInBrowserService showInBrowserService = null,
            IIssueSelectionService selectionService = null,
            IMenuCommandService menuCommandService = null,
            ISonarQubeService sonarQubeService = null)
        {
            issueVizs ??= Array.Empty<IAnalysisIssueVisualization>();
            store ??= new Mock<ITaintStore>();
            store.Setup(x => x.GetAll()).Returns(issueVizs);

            activeDocumentTracker ??= Mock.Of<IActiveDocumentTracker>();
            activeDocumentLocator ??= Mock.Of<IActiveDocumentLocator>();
            showInBrowserService ??= Mock.Of<IShowInBrowserService>();
            locationNavigator ??= Mock.Of<ILocationNavigator>();
            telemetryManager ??= Mock.Of<ITelemetryManager>();
            selectionService ??= Mock.Of<IIssueSelectionService>();
            menuCommandService ??= Mock.Of<IMenuCommandService>();
            sonarQubeService ??= Mock.Of<ISonarQubeService>();

            return new TaintIssuesControlViewModel(store.Object,
                locationNavigator,
                activeDocumentTracker,
                activeDocumentLocator,
                showInBrowserService,
                telemetryManager,
                selectionService,
                Mock.Of<ICommand>(),
                menuCommandService,
                sonarQubeService);
        }

        private static void SetServerType(ServerType? serverType, Mock<ISonarQubeService> sonarQubeServiceMock)
        {
            sonarQubeServiceMock.Setup(x => x.GetServerInfo())
                .Returns(serverType.HasValue ? new ServerInfo(null, serverType.Value) : null);
        }

        private static IActiveDocumentLocator CreateLocatorAndSetActiveDocument(string activeFilePath)
        {
            var activeDocument = new Mock<ITextDocument>();
            activeDocument.Setup(x => x.FilePath).Returns(activeFilePath);

            var activeDocumentLocator = new Mock<IActiveDocumentLocator>();
            activeDocumentLocator.Setup(x => x.FindActiveDocument()).Returns(activeDocument.Object);

            return activeDocumentLocator.Object;
        }

        private IAnalysisIssueVisualization CreateIssueViz(string filePath = "test.cpp", string issueKey = "issue key",
            DateTimeOffset created = default, bool isSuppressed = false, params IAnalysisIssueLocationVisualization[] locations)
        {
            var issue = new Mock<ITaintIssue>();
            issue.Setup(x => x.IssueKey).Returns(issueKey);
            issue.Setup(x => x.CreationTimestamp).Returns(created);

            var issueViz = new Mock<IAnalysisIssueVisualization>();
            issueViz.Setup(x => x.CurrentFilePath).Returns(filePath);
            issueViz.Setup(x => x.Issue).Returns(issue.Object);
            issueViz.Setup(x => x.IsSuppressed).Returns(isSuppressed);

            var flowViz = new Mock<IAnalysisIssueFlowVisualization>();
            flowViz.Setup(x => x.Locations).Returns(locations);
            issueViz.Setup(x => x.Flows).Returns(new[] {flowViz.Object});

            return issueViz.Object;
        }

        private IAnalysisIssueLocationVisualization CreateLocationViz(string filePath)
        {
            var locationViz = new Mock<IAnalysisIssueLocationVisualization>();
            locationViz.Setup(x => x.CurrentFilePath).Returns(filePath);

            return locationViz.Object;
        }

        private void VerifyCommandExecution(ICommand command, object parameter, bool canExecute)
        {
            var result = command.CanExecute(parameter);
            result.Should().Be(canExecute);
        }

        private static void VerifyFilterIsNotNull(TaintIssuesControlViewModel controlViewModel) =>
            controlViewModel.IssuesView.Filter.Should().NotBeNull();

        /// <summary>
        /// Returns the filtered and sorted list of issue viz items
        /// that will be displayed in the grid
        /// </summary>
        private static IList<IAnalysisIssueVisualization> GetIssueVizsFromView(TaintIssuesControlViewModel controlViewModel)
        {
            var taintIssueVizs = controlViewModel.IssuesView.OfType<ITaintIssueViewModel>()
                .Select(x => x.TaintIssueViz)
                .ToList();
            // All items should be issue viz instances
            controlViewModel.IssuesView.OfType<object>().Count().Should().Be(taintIssueVizs.Count);

            return taintIssueVizs;
        }

        private static void RaiseStoreIssuesChangedEvent(Mock<ITaintStore> store, params IAnalysisIssueVisualization[] issueVizs)
        {
            store.Setup(x => x.GetAll()).Returns(issueVizs);
            store.Raise(x => x.IssuesChanged += null, null, null);
        }

        private static void CheckExpectedSourceIssueCount(ITaintIssuesControlViewModel controlViewModel, int expected) =>
            GetSourceItems(controlViewModel).Count.Should().Be(expected);

        private static ObservableCollection<ITaintIssueViewModel> GetSourceItems(ITaintIssuesControlViewModel controlViewModel) =>
            (ObservableCollection<ITaintIssueViewModel>)controlViewModel.IssuesView.SourceCollection;

        private static void RaiseSelectionChangedEvent(Mock<IIssueSelectionService> selectionService, IAnalysisIssueVisualization selectedIssue)
        {
            selectionService.Setup(x => x.SelectedIssue).Returns(selectedIssue);
            selectionService.Raise(x => x.SelectedIssueChanged += null, EventArgs.Empty);
        }

        private static void RaiseActiveDocumentChangedEvent(Mock<IActiveDocumentTracker> activeDocTracker)
        {
            activeDocTracker.Raise(x => x.ActiveDocumentChanged += null, new ActiveDocumentChangedEventArgs(Mock.Of<ITextDocument>()));
        }

        private void VerifyPropertyChangedWasRaised(Mock<PropertyChangedEventHandler> eventHandler, string expectedProperty)
        {
            eventHandler.Verify(x => x(It.IsAny<object>(),
                    It.Is((PropertyChangedEventArgs e) => e.PropertyName == expectedProperty)),
                Times.Once);
        }

        private void SetupAnalysisInformation(Mock<ITaintStore> store, AnalysisInformation analysisInformation)
        {
            store.Setup(x => x.GetAnalysisInformation()).Returns(analysisInformation);
        }
    }
}
