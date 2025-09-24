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
using Microsoft.VisualStudio.Text;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.Infrastructure.VS;
using SonarLint.VisualStudio.Infrastructure.VS.DocumentEvents;
using SonarLint.VisualStudio.IssueVisualization.Editor;
using SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels.Commands;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;
using SonarLint.VisualStudio.IssueVisualization.Selection;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class FilteringTests
{
    private const string CurrentDocumentPath = "C:\\source\\myProj\\myFile.cs";
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
    private IIssueSelectionService selectionService;
    private IActiveDocumentLocator activeDocumentLocator;
    private IActiveDocumentTracker activeDocumentTracker;

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
        selectionService = Substitute.For<IIssueSelectionService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        activeDocumentLocator = Substitute.For<IActiveDocumentLocator>();
        activeDocumentTracker = Substitute.For<IActiveDocumentTracker>();
        eventHandler = Substitute.For<PropertyChangedEventHandler>();
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns([]);
        taintsReportViewModel.GetTaintsGroupViewModels().Returns([]);

        CreateTestSubject();
    }

    [TestMethod]
    public void ApplyFilter_RaisesEvents()
    {
        eventHandler.ClearReceivedCalls();

        testSubject.ApplyFilter();

        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void ApplyFilter_LocationFilterIsCurrentDocument_RemovesGroupsThatAreNotForTheCurrentDocument()
    {
        var hotspotsGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel(CurrentDocumentPath), CreateMockedGroupViewModel("myTaint.ts")]);
        var taintGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel("MyTaint.js"), CreateMockedGroupViewModel(CurrentDocumentPath)]);
        var dependencyRisks = new List<IDependencyRisk>([CreateDependencyRisk()]);
        InitializeTestSubjectWithInitialGroups(hotspotsGroups, taintGroups, dependencyRisks);

        ApplyLocationFilter(LocationFilter.CurrentDocument);
        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        testSubject.FilteredGroupViewModels.All(group => group.FilePath == CurrentDocumentPath && group is not GroupDependencyRiskViewModel).Should().BeTrue();
    }

    [TestMethod]
    public void ApplyFilter_LocationFilterIsOpenDocuments_ShowsAllGroups()
    {
        MockActiveDocument();
        var hotspotsGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel(CurrentDocumentPath), CreateMockedGroupViewModel("myTaint.ts")]);
        var taintGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel("MyTaint.js"), CreateMockedGroupViewModel(CurrentDocumentPath)]);
        var dependencyRisks = new List<IDependencyRisk>([CreateDependencyRisk()]);
        InitializeTestSubjectWithInitialGroups(hotspotsGroups, taintGroups, dependencyRisks);

        ApplyLocationFilter(LocationFilter.OpenDocuments);
        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(5);
        testSubject.FilteredGroupViewModels.Should().Contain(hotspotsGroups);
        testSubject.FilteredGroupViewModels.Should().Contain(taintGroups);
        testSubject.FilteredGroupViewModels.Should().Contain(g => g is GroupDependencyRiskViewModel);
    }

    [TestMethod]
    public void ApplyFilter_LocationFilterChanged_ShowsGroupsCorrectly()
    {
        MockActiveDocument();
        var hotspotsGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel(CurrentDocumentPath), CreateMockedGroupViewModel("myTaint.ts")]);
        var taintGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel("MyTaint.js"), CreateMockedGroupViewModel(CurrentDocumentPath)]);
        var dependencyRisks = new List<IDependencyRisk>([CreateDependencyRisk()]);
        InitializeTestSubjectWithInitialGroups(hotspotsGroups, taintGroups, dependencyRisks);

        ApplyLocationFilter(LocationFilter.CurrentDocument);
        testSubject.ApplyFilter();
        testSubject.FilteredGroupViewModels.Should().HaveCount(2);

        ApplyLocationFilter(LocationFilter.OpenDocuments);
        testSubject.ApplyFilter();
        testSubject.FilteredGroupViewModels.Should().HaveCount(5);
        testSubject.FilteredGroupViewModels.Should().Contain(hotspotsGroups);
        testSubject.FilteredGroupViewModels.Should().Contain(taintGroups);
        testSubject.FilteredGroupViewModels.Should().Contain(g => g is GroupDependencyRiskViewModel);
    }

    [TestMethod]
    public void ActiveDocumentChanged_LocationFilterIsCurrentDocument_ReappliesFilter()
    {
        var newFileName = "C://somePath/MyTaint.js";
        var taintGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel(newFileName), CreateMockedGroupViewModel(CurrentDocumentPath)]);
        InitializeTestSubjectWithInitialGroups([], taintGroups, []);

        ApplyLocationFilter(LocationFilter.CurrentDocument);
        activeDocumentTracker.ActiveDocumentChanged += Raise.EventWith(new ActiveDocumentChangedEventArgs(MockTextDocument(newFileName)));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        testSubject.FilteredGroupViewModels.Should().ContainSingle(group => group.FilePath == newFileName);
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void ActiveDocumentChanged_LocationFilterIsOpenDocumentsDocument_DoesNotReapply()
    {
        var newFileName = "C://somePath/MyTaint.js";
        var taintGroups = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel(newFileName), CreateMockedGroupViewModel(CurrentDocumentPath)]);
        InitializeTestSubjectWithInitialGroups([], taintGroups, []);

        ApplyLocationFilter(LocationFilter.OpenDocuments);
        activeDocumentTracker.ActiveDocumentChanged += Raise.EventWith(new ActiveDocumentChangedEventArgs(MockTextDocument(newFileName)));

        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    private void CreateTestSubject()
    {
        MockActiveDocument();
        var reportViewModel = new ReportViewModel(activeSolutionBoundTracker,
            navigateToRuleDescriptionCommand,
            locationNavigator,
            hotspotsReportViewModel,
            new DependencyRisksReportViewModel(dependencyRisksStore, showDependencyRiskInBrowserHandler, changeDependencyRiskStatusHandler, messageBox),
            taintsReportViewModel,
            telemetryManager,
            selectionService,
            activeDocumentLocator,
            activeDocumentTracker,
            threadHandling);
        reportViewModel.PropertyChanged += eventHandler;
        testSubject = reportViewModel;
    }

    private void ApplyLocationFilter(LocationFilter filter) =>
        testSubject.ReportViewFilter.SelectedLocationFilter = testSubject.ReportViewFilter.LocationFilters.Single(f => f.LocationFilter == filter);

    private void VerifyHasGroupsUpdated() => eventHandler.Received().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasGroups)));

    private void InitializeTestSubjectWithInitialGroups(
        ObservableCollection<IGroupViewModel> hotspotGroups,
        ObservableCollection<IGroupViewModel> taintGroups,
        IEnumerable<IDependencyRisk> dependencyRisks)
    {
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns(hotspotGroups);
        taintsReportViewModel.GetTaintsGroupViewModels().Returns(taintGroups);
        dependencyRisksStore.GetAll().Returns(dependencyRisks);
        CreateTestSubject();
        ClearCallsForReportsViewModels();
    }

    private void MockActiveDocument(string filePath = CurrentDocumentPath)
    {
        var textDocument = MockTextDocument(filePath);
        activeDocumentLocator.FindActiveDocument().Returns(textDocument);
    }

    private static ITextDocument MockTextDocument(string filePath)
    {
        var textDocument = Substitute.For<ITextDocument>();
        textDocument.FilePath.Returns(filePath);
        return textDocument;
    }

    private static IGroupViewModel CreateMockedGroupViewModel(string filePath)
    {
        var group = Substitute.For<IGroupViewModel>();
        group.FilePath.Returns(filePath);
        return group;
    }

    private void ClearCallsForReportsViewModels()
    {
        dependencyRisksStore.ClearReceivedCalls();
        taintsReportViewModel.ClearReceivedCalls();
        hotspotsReportViewModel.ClearReceivedCalls();
    }

    private static IDependencyRisk CreateDependencyRisk(Guid? id = null, bool isResolved = false)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Transitions.Returns([]);
        risk.Status.Returns(isResolved ? DependencyRiskStatus.Accepted : DependencyRiskStatus.Open);
        return risk;
    }
}
