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
    private readonly IIssueViewModel csharpHotspot = CreateMockedIssueViewModel(IssueType.SecurityHotspot, CsharpFilePath);
    private readonly IIssueViewModel csharpTaint = CreateMockedIssueViewModel(IssueType.TaintVulnerability, CsharpFilePath);
    private readonly IIssueViewModel dependencyRiskIssue = CreateMockedIssueViewModel(IssueType.DependencyRisk, null);
    private readonly IIssueViewModel tsHotspot = CreateMockedIssueViewModel(IssueType.SecurityHotspot, TsFilePath);
    private readonly IIssueViewModel tsTaint = CreateMockedIssueViewModel(IssueType.TaintVulnerability, TsFilePath);
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

        var csharpGroup = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel(CsharpFilePath, csharpHotspot, csharpTaint)]);
        var tsGroup = new ObservableCollection<IGroupViewModel>([CreateMockedGroupViewModel(TsFilePath, tsTaint, tsHotspot)]);
        var dependencyRiskGroup = CreateMockedGroupViewModel(null, dependencyRiskIssue);
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
        VerifyIsExpectedGroup(TsFilePath, tsTaint, tsHotspot);
        VerifyHasGroupsUpdated();
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

    private static IGroupViewModel CreateMockedGroupViewModel(string filePath, params IIssueViewModel[] filteredIssueViewModels)
    {
        var group = Substitute.For<IGroupViewModel>();
        group.FilePath.Returns(filePath);
        group.FilteredIssues.Returns(new ObservableCollection<IIssueViewModel>(filteredIssueViewModels));
        return group;
    }

    private static IIssueViewModel CreateMockedIssueViewModel(IssueType issueType, string filePath)
    {
        var analysisIssueViewModel = Substitute.For<IIssueViewModel>();
        analysisIssueViewModel.IssueType.Returns(issueType);
        analysisIssueViewModel.FilePath.Returns(filePath);
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
        VerifyIsExpectedGroup(CsharpFilePath, csharpTaint, csharpHotspot);
        VerifyIsExpectedGroup(TsFilePath, tsTaint, tsHotspot);
    }

    private void VerifyOnlyGroupForCurrentDocumentIsShown()
    {
        testSubject.FilteredGroupViewModels.Should().HaveCount(1);
        VerifyIsExpectedGroup(CsharpFilePath, csharpTaint, csharpHotspot);
    }

    private void VerifyIsExpectedGroup(string filePath, params IIssueViewModel[] expectedIssueViewModels) =>
        testSubject.FilteredGroupViewModels.Should().Contain(g =>
            g.FilePath == filePath && g.FilteredIssues.Count == expectedIssueViewModels.Length && expectedIssueViewModels.All(issue => g.FilteredIssues.Contains(issue)));
}
