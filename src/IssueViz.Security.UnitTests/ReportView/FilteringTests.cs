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
    private const string CsharpFilePath = "C:\\source\\myProj\\myFile.cs";
    private const string TsFilePath = "C:\\source\\myProj\\myTaint.ts";
    private readonly IIssueViewModel csharpHotspotInfo = CreateMockedIssueViewModel(IssueType.SecurityHotspot, CsharpFilePath, DisplaySeverity.Info, DisplayStatus.Open);
    private readonly IIssueViewModel csharpTaintLow = CreateMockedIssueViewModel(IssueType.TaintVulnerability, CsharpFilePath, DisplaySeverity.Low, DisplayStatus.Resolved);
    private readonly IIssueViewModel tsHotspotMedium = CreateMockedIssueViewModel(IssueType.SecurityHotspot, TsFilePath, DisplaySeverity.Medium, DisplayStatus.Resolved);
    private readonly IIssueViewModel tsTaintHigh = CreateMockedIssueViewModel(IssueType.TaintVulnerability, TsFilePath, DisplaySeverity.High, DisplayStatus.Open);
    private readonly IDependencyRisk dependencyRisk = CreateDependencyRisk(severity: DependencyRiskImpactSeverity.Blocker, status: DependencyRiskStatus.Open);
    private IIssueViewModel dependencyRiskIssue;
    private IActiveDocumentLocator activeDocumentLocator;
    private IActiveDocumentTracker activeDocumentTracker;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;

    private IDependencyRisksReportViewModel dependencyRisksReportViewModel;
    private IDependencyRisksStore dependencyRisksStore;
    private PropertyChangedEventHandler eventHandler;
    private IHotspotsReportViewModel hotspotsReportViewModel;
    private ILocationNavigator locationNavigator;
    private INavigateToRuleDescriptionCommand navigateToRuleDescriptionCommand;
    private IIssueSelectionService selectionService;
    private ITaintsReportViewModel taintsReportViewModel;
    private ITelemetryManager telemetryManager;
    private ReportViewModel testSubject;
    private IThreadHandling threadHandling;

    [TestInitialize]
    public void Initialize()
    {
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        hotspotsReportViewModel = Substitute.For<IHotspotsReportViewModel>();
        taintsReportViewModel = Substitute.For<ITaintsReportViewModel>();
        dependencyRisksReportViewModel = Substitute.For<IDependencyRisksReportViewModel>();
        navigateToRuleDescriptionCommand = Substitute.For<INavigateToRuleDescriptionCommand>();
        locationNavigator = Substitute.For<ILocationNavigator>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        selectionService = Substitute.For<IIssueSelectionService>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        activeDocumentLocator = Substitute.For<IActiveDocumentLocator>();
        activeDocumentTracker = Substitute.For<IActiveDocumentTracker>();
        eventHandler = Substitute.For<PropertyChangedEventHandler>();
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns([]);
        taintsReportViewModel.GetTaintsGroupViewModels().Returns([]);

        var csharpGroup = new ObservableCollection<IGroupViewModel>([CreateGroupFileViewModel(CsharpFilePath, csharpHotspotInfo, csharpTaintLow)]);
        var tsGroup = new ObservableCollection<IGroupViewModel>([CreateGroupFileViewModel(TsFilePath, tsHotspotMedium, tsTaintHigh)]);
        var dependencyRiskGroup = CreateGroupDependencyRiskViewModel(dependencyRisk);
        dependencyRiskIssue = dependencyRiskGroup.FilteredIssues.Single();
        hotspotsReportViewModel.GetHotspotsGroupViewModels().Returns(csharpGroup);
        taintsReportViewModel.GetTaintsGroupViewModels().Returns(tsGroup);
        dependencyRisksReportViewModel.GetDependencyRisksGroup().Returns(dependencyRiskGroup);
        CreateTestSubject();
        ClearCallsForReportsViewModels();
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
        ApplyLocationFilter(LocationFilter.CurrentDocument);

        testSubject.ApplyFilter();

        VerifyOnlyGroupForCurrentDocumentIsShown();
    }

    [TestMethod]
    public void ApplyFilter_LocationFilterIsOpenDocuments_ShowsAllGroups()
    {
        ApplyLocationFilter(LocationFilter.OpenDocuments);

        testSubject.ApplyFilter();

        VerifyAllIssuesAreShown();
    }

    [TestMethod]
    public void ApplyFilter_LocationFilterChanged_ShowsGroupsCorrectly()
    {
        ApplyLocationFilter(LocationFilter.CurrentDocument);
        testSubject.ApplyFilter();
        VerifyOnlyGroupForCurrentDocumentIsShown();

        ApplyLocationFilter(LocationFilter.OpenDocuments);
        testSubject.ApplyFilter();
        VerifyAllIssuesAreShown();
    }

    [TestMethod]
    public void ActiveDocumentChanged_LocationFilterIsCurrentDocument_ReappliesFilter()
    {
        ApplyLocationFilter(LocationFilter.CurrentDocument);

        activeDocumentTracker.ActiveDocumentChanged += Raise.EventWith(new ActiveDocumentChangedEventArgs(MockTextDocument(TsFilePath)));

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyIsExpectedGroup(TsFilePath, tsTaintHigh, tsHotspotMedium);
        VerifyHasGroupsUpdated();
    }

    [TestMethod]
    public void ApplyFilter_IssueTypeFilterIsHotspot_FiltersIssuesInGroups()
    {
        ClearFilter();
        SetIssueTypeFilter(IssueType.SecurityHotspot, isSelected: true);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        VerifyIsExpectedGroup(TsFilePath, tsHotspotMedium);
        VerifyIsExpectedGroup(CsharpFilePath, csharpHotspotInfo);
    }

    [TestMethod]
    public void ApplyFilter_IssueTypeFilterIsHotspotAndTaint_FiltersIssuesInGroups()
    {
        ClearFilter();
        SetIssueTypeFilter(IssueType.SecurityHotspot, isSelected: true);
        SetIssueTypeFilter(IssueType.TaintVulnerability, isSelected: true);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        VerifyIsExpectedGroup(TsFilePath, tsHotspotMedium, tsTaintHigh);
        VerifyIsExpectedGroup(CsharpFilePath, csharpHotspotInfo, csharpTaintLow);
    }

    [TestMethod]
    public void ApplyFilter_IssueTypeFilterIsHotspotAndDependencyRisk_FiltersIssuesInGroups()
    {
        ClearFilter();
        SetIssueTypeFilter(IssueType.SecurityHotspot, isSelected: true);
        SetIssueTypeFilter(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(3);
        VerifyIsExpectedGroup(TsFilePath, tsHotspotMedium);
        VerifyIsExpectedGroup(CsharpFilePath, csharpHotspotInfo);
        VerifyIsExpectedGroup(null, dependencyRiskIssue);
    }

    [TestMethod]
    public void ApplyFilter_IssueTypeFilterIsTaint_FiltersIssuesInGroups()
    {
        ClearFilter();
        SetIssueTypeFilter(IssueType.TaintVulnerability, isSelected: true);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        VerifyIsExpectedGroup(TsFilePath, tsTaintHigh);
        VerifyIsExpectedGroup(CsharpFilePath, csharpTaintLow);
    }

    [TestMethod]
    public void ApplyFilter_IssueTypeFilterIsTaintAndDependencyRisk_FiltersIssuesInGroups()
    {
        ClearFilter();
        SetIssueTypeFilter(IssueType.TaintVulnerability, isSelected: true);
        SetIssueTypeFilter(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(3);
        VerifyIsExpectedGroup(TsFilePath, tsTaintHigh);
        VerifyIsExpectedGroup(CsharpFilePath, csharpTaintLow);
        VerifyIsExpectedGroup(null, dependencyRiskIssue);
    }

    [TestMethod]
    public void ApplyFilter_IssueTypeFilterIsDependencyRisk_FiltersIssuesInGroups()
    {
        ClearFilter();
        SetIssueTypeFilter(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyIsExpectedGroup(null, dependencyRiskIssue);
    }

    [TestMethod]
    [DataRow(DisplaySeverity.Info)]
    [DataRow(DisplaySeverity.Low)]
    [DataRow(DisplaySeverity.Medium)]
    [DataRow(DisplaySeverity.High)]
    [DataRow(DisplaySeverity.Blocker)]
    public void ApplyFilter_SeverityFilterSelected_ShowsOnlyRisksWithThatSeverity(DisplaySeverity selectedSeverityFilter)
    {
        MockSeverityFilter(selectedSeverityFilter);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        testSubject.FilteredGroupViewModels.Should().Contain(g => g.FilteredIssues.SingleOrDefault(vm => vm.DisplaySeverity == selectedSeverityFilter) != null);
    }

    [TestMethod]
    public void ApplyFilter_SeverityFilterNotSelected_ShowsAllRisks()
    {
        MockSeverityFilter(displaySeverity: DisplaySeverity.Any);

        testSubject.ApplyFilter();

        VerifyAllIssuesAreShown();
    }

    [TestMethod]
    public void ApplyFilter_OpenStatusFilterSelected_ShowsOnlyRisksWithStatusOpen()
    {
        MockStatusFilter(DisplayStatus.Open);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(3);
        testSubject.FilteredGroupViewModels.Should().Contain(g => g.FilteredIssues.SingleOrDefault(vm => vm.Status == DisplayStatus.Open) != null);
    }

    [TestMethod]
    public void ApplyFilter_ResolvedStatusFilterSelected_ShowsOnlyRisksWithStatusResolved()
    {
        MockStatusFilter(DisplayStatus.Resolved);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(2);
        testSubject.FilteredGroupViewModels.Should().Contain(g => g.FilteredIssues.SingleOrDefault(vm => vm.Status == DisplayStatus.Resolved) != null);
    }

    [TestMethod]
    public void ApplyFilter_StatusFilterNotSelected_ShowsAllRisks()
    {
        MockStatusFilter(displayStatus: null);

        testSubject.ApplyFilter();

        VerifyAllIssuesAreShown();
    }

    [TestMethod]
    public void ApplyFilter_MixedFilters_FiltersCorrectly()
    {
        ClearFilter();
        MockSeverityFilter(displaySeverity: DisplaySeverity.Blocker);
        MockStatusFilter(DisplayStatus.Open);
        SetIssueTypeFilter(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter();

        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        testSubject.FilteredGroupViewModels.Should().Contain(g =>
            g.FilteredIssues.SingleOrDefault(vm => vm.DisplaySeverity == DisplaySeverity.Blocker && vm.Status == DisplayStatus.Open && vm.IssueType == IssueType.DependencyRisk) != null);
    }

    [TestMethod]
    public void ActiveDocumentChanged_LocationFilterIsOpenDocumentsDocument_DoesNotReapply()
    {
        ApplyLocationFilter(LocationFilter.OpenDocuments);

        activeDocumentTracker.ActiveDocumentChanged += Raise.EventWith(new ActiveDocumentChangedEventArgs(MockTextDocument(TsFilePath)));

        eventHandler.DidNotReceiveWithAnyArgs().Invoke(default, default);
    }

    private void CreateTestSubject()
    {
        MockActiveDocument();
        var reportViewModel = new ReportViewModel(activeSolutionBoundTracker,
            navigateToRuleDescriptionCommand,
            locationNavigator,
            hotspotsReportViewModel,
            dependencyRisksReportViewModel,
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

    private void SetIssueTypeFilter(IssueType issueType, bool isSelected)
    {
        var issueTypeFilter = testSubject.ReportViewFilter.IssueTypeFilters.Single(f => f.IssueType == issueType);
        issueTypeFilter.IsSelected = isSelected;
    }

    private void VerifyHasGroupsUpdated() => eventHandler.Received().Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasGroups)));

    private void MockActiveDocument(string filePath = CsharpFilePath)
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

    private IGroupViewModel CreateGroupFileViewModel(string filePath, params IIssueViewModel[] filteredIssueViewModels) =>
        new GroupFileViewModel(filePath, filteredIssueViewModels.ToList(), threadHandling);

    private IGroupViewModel CreateGroupDependencyRiskViewModel(params IDependencyRisk[] dependencyRisks)
    {
        dependencyRisksStore.GetAll().Returns(dependencyRisks);
        var groupVm = new GroupDependencyRiskViewModel(dependencyRisksStore);
        groupVm.InitializeRisks();
        return groupVm;
    }

    private static IIssueViewModel CreateMockedIssueViewModel(
        IssueType issueType,
        string filePath,
        DisplaySeverity severity,
        DisplayStatus status)
    {
        var analysisIssueViewModel = Substitute.For<IIssueViewModel>();
        analysisIssueViewModel.IssueType.Returns(issueType);
        analysisIssueViewModel.FilePath.Returns(filePath);
        analysisIssueViewModel.DisplaySeverity.Returns(severity);
        analysisIssueViewModel.Status.Returns(status);
        return analysisIssueViewModel;
    }

    private void ClearCallsForReportsViewModels()
    {
        dependencyRisksStore.ClearReceivedCalls();
        taintsReportViewModel.ClearReceivedCalls();
        hotspotsReportViewModel.ClearReceivedCalls();
    }

    private void VerifyAllIssuesAreShown()
    {
        testSubject.FilteredGroupViewModels.Should().HaveCount(3);
        testSubject.FilteredGroupViewModels.Should().Contain(g => g.FilePath == null && g.FilteredIssues.SequenceEqual(new List<IIssueViewModel> { dependencyRiskIssue }));
        VerifyIsExpectedGroup(CsharpFilePath, csharpTaintLow, csharpHotspotInfo);
        VerifyIsExpectedGroup(TsFilePath, tsTaintHigh, tsHotspotMedium);
    }

    private void VerifyOnlyGroupForCurrentDocumentIsShown()
    {
        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyIsExpectedGroup(CsharpFilePath, csharpTaintLow, csharpHotspotInfo);
    }

    private void VerifyIsExpectedGroup(string filePath, params IIssueViewModel[] expectedIssueViewModels) =>
        testSubject.FilteredGroupViewModels.Should().Contain(g =>
            g.FilePath == filePath && g.FilteredIssues.Count == expectedIssueViewModels.Length && expectedIssueViewModels.All(issue => g.FilteredIssues.Contains(issue)));

    private static IDependencyRisk CreateDependencyRisk(
        Guid? id = null,
        bool isResolved = false,
        DependencyRiskImpactSeverity severity = default,
        DependencyRiskStatus status = default)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Transitions.Returns([]);
        risk.Status.Returns(status);
        risk.Severity.Returns(severity);
        return risk;
    }

    private void MockSeverityFilter(DisplaySeverity displaySeverity) => testSubject.ReportViewFilter.SelectedSeverityFilter = displaySeverity;

    private void MockStatusFilter(DisplayStatus? displayStatus) => testSubject.ReportViewFilter.SelectedStatusFilter = displayStatus;

    private void ClearFilter() => testSubject.ReportViewFilter.IssueTypeFilters.ToList().ForEach(f => f.IsSelected = false);
}
