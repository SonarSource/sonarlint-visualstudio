/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class GroupFileViewModelTest
{
    private const string filePath = "c:\\myDir\\myFile.cs";
    private readonly IIssueViewModel hotspotInfo = CreateMockedIssue(IssueType.SecurityHotspot, DisplaySeverity.Info, DisplayStatus.Open, true);
    private readonly IIssueViewModel hotspotLow = CreateMockedIssue(IssueType.SecurityHotspot, DisplaySeverity.Low, DisplayStatus.Resolved, false);
    private readonly IIssueViewModel taintHigh = CreateMockedIssue(IssueType.TaintVulnerability, DisplaySeverity.High, DisplayStatus.Open, true);
    private readonly IIssueViewModel taintMedium = CreateMockedIssue(IssueType.TaintVulnerability, DisplaySeverity.Medium, DisplayStatus.Resolved, false);
    private readonly IIssueViewModel taintBlocker = CreateMockedIssue(IssueType.TaintVulnerability, DisplaySeverity.Blocker, DisplayStatus.Resolved, true);
    private ReportViewFilterViewModel reportViewFilterViewModel;
    private List<IIssueViewModel> allIssues;
    private PropertyChangedEventHandler eventHandler;
    private IFocusOnNewCodeServiceUpdater focusOnNewCodeService;
    private GroupFileViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        focusOnNewCodeService = Substitute.For<IFocusOnNewCodeServiceUpdater>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));
        reportViewFilterViewModel = new(focusOnNewCodeService, Substitute.ForPartsOf<NoOpThreadHandler>());
        allIssues = [hotspotInfo, hotspotLow, taintMedium, taintHigh, taintBlocker];
        testSubject = new GroupFileViewModel(filePath, allIssues);
        eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        MockStatusFilter(DisplayStatus.Any); // tests were written with this assumption, changing the tests would take too much time
    }

    [TestMethod]
    public void Ctor_InitializesPropertiesAsExpected()
    {
        testSubject.Title.Should().Be("myFile.cs");
        testSubject.FilePath.Should().Be(filePath);
        testSubject.AllIssues.Should().BeSameAs(allIssues);
        testSubject.FilteredIssues.Should().NotBeSameAs(testSubject.AllIssues);
        testSubject.FilteredIssues.Should().BeEquivalentTo(testSubject.AllIssues);
    }

    [TestMethod]
    public void ApplyFilter_HotspotFilterSelected_FilteredIssuesHasOnlyTaints()
    {
        ClearFilter();
        MockIssueTypeFilter(IssueType.SecurityHotspot, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().HaveCount(2);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.IssueType == IssueType.SecurityHotspot);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_TaintFilterSelected_FilteredIssuesHasOnlyHotspots()
    {
        ClearFilter();
        MockIssueTypeFilter(IssueType.TaintVulnerability, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().HaveCount(3);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.IssueType == IssueType.TaintVulnerability);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_DependencyRisksSelected_FilteredIssuesHasNoIssues()
    {
        ClearFilter();
        MockIssueTypeFilter(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEmpty();
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_AllTypesSelected_FilteredIssuesHasAllIssues()
    {
        MockIssueTypeFilter(IssueType.SecurityHotspot, isSelected: true);
        MockIssueTypeFilter(IssueType.TaintVulnerability, isSelected: true);
        MockIssueTypeFilter(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEquivalentTo(testSubject.AllIssues);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_NoTypesSelected_FilteredIssuesHasNoIssues()
    {
        ClearFilter();
        MockIssueTypeFilter(IssueType.SecurityHotspot, isSelected: false);
        MockIssueTypeFilter(IssueType.TaintVulnerability, isSelected: false);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEmpty();
        VerifyAllIssuesUnchanged();
    }

    [DataRow(true, 3)]
    [DataRow(false, 5)]
    [DataTestMethod]
    public void ApplyFilter_NewCodeSelected_AppliesAccordingly(bool isOnNewCode, int filteredCount)
    {
        MockNewCodeFilter(isOnNewCode);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.AllIssues.Should().HaveCount(5);
        testSubject.PreFilteredIssues.Should().HaveCount(5);
        testSubject.FilteredIssues.Should().HaveCount(filteredCount);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    [DataRow(DisplaySeverity.Info)]
    [DataRow(DisplaySeverity.Low)]
    [DataRow(DisplaySeverity.Medium)]
    [DataRow(DisplaySeverity.High)]
    [DataRow(DisplaySeverity.Blocker)]
    public void ApplyFilter_SeverityFilterSelected_ShowsOnlyRisksWithThatSeverityOrHigher(DisplaySeverity selectedSeverityFilter)
    {
        MockSeverityFilter(selectedSeverityFilter);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().OnlyContain(i => i.DisplaySeverity >= selectedSeverityFilter);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_SeverityFilterNotSelected_ShowsAllRisks()
    {
        MockSeverityFilter(displaySeverity: DisplaySeverity.Info);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEquivalentTo(testSubject.AllIssues);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_MixedFilters_FiltersCorrectly()
    {
        ClearFilter();
        MockSeverityFilter(displaySeverity: DisplaySeverity.Low);
        MockIssueTypeFilter(IssueType.SecurityHotspot, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().HaveCount(1);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.DisplaySeverity == DisplaySeverity.Low && i.IssueType == IssueType.SecurityHotspot);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_OpenStatusFilterSelected_ShowsOnlyRisksWithStatusOpen()
    {
        MockStatusFilter(DisplayStatus.Open);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().HaveCount(2);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.Status == DisplayStatus.Open);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_ResolvedStatusFilterSelected_ShowsOnlyRisksWithStatusResolved()
    {
        MockStatusFilter(DisplayStatus.Resolved);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.PreFilteredIssues.Should().HaveCount(3);
        testSubject.FilteredIssues.Should().HaveCount(3);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.Status == DisplayStatus.Resolved);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_StatusAndSeverityFilter_AppliesStatusAsPreFilter()
    {
        MockStatusFilter(DisplayStatus.Resolved);
        MockSeverityFilter(DisplaySeverity.Blocker);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.AllIssues.Should().HaveCount(5);
        testSubject.PreFilteredIssues.Should().HaveCount(3);
        testSubject.FilteredIssues.Should().HaveCount(1);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.Status == DisplayStatus.Resolved && i.DisplaySeverity == DisplaySeverity.Blocker);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_TypeAndSeverityFilter_NoPreFiltering()
    {
        MockIssueTypeFilter(IssueType.TaintVulnerability, true);
        MockSeverityFilter(DisplaySeverity.Blocker);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.AllIssues.Should().HaveCount(5);
        testSubject.PreFilteredIssues.Should().HaveCount(5);
        testSubject.FilteredIssues.Should().HaveCount(1);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.IssueType == IssueType.TaintVulnerability && i.DisplaySeverity == DisplaySeverity.Blocker);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_NewCodeAndSeverityFilter_NoPreFiltering()
    {
        MockNewCodeFilter(true);
        MockSeverityFilter(DisplaySeverity.Medium);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.AllIssues.Should().HaveCount(5);
        testSubject.PreFilteredIssues.Should().HaveCount(5);
        testSubject.FilteredIssues.Should().HaveCount(2);
        testSubject.FilteredIssues.Should().BeEquivalentTo(taintBlocker, taintHigh);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_StatusFilterNotSelected_ShowsAllRisks()
    {
        MockStatusFilter(displayStatus: DisplayStatus.Any);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEquivalentTo(testSubject.AllIssues);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void IsExpanded_DefaultValue_IsTrue()
    {
        var result = testSubject.IsExpanded;

        result.Should().BeTrue();
    }

    [TestMethod]
    public void IsExpanded_SetToFalse_ValueIsFalse()
    {
        testSubject.IsExpanded = false;

        testSubject.IsExpanded.Should().BeFalse();
    }

    [TestMethod]
    public void IsExpanded_SetToTrue_ValueIsTrue()
    {
        testSubject.IsExpanded = false;

        testSubject.IsExpanded = true;

        testSubject.IsExpanded.Should().BeTrue();
    }

    [TestMethod]
    public void IsExpanded_SetValue_RaisesPropertyChanged()
    {
        testSubject.IsExpanded = true;

        ReceivedEvent(nameof(testSubject.IsExpanded));
    }

    [TestMethod]
    public void ApplyFilter_SortsFilteredIssuesBySeverityLineAndColumn()
    {
        var sortedIssues = new List<IIssueViewModel>
        {
            CreateMockedIssue(DisplaySeverity.Blocker, null, null),
            CreateMockedIssue(DisplaySeverity.Blocker, 1, 1),
            CreateMockedIssue(DisplaySeverity.Blocker, 1, 2),
            CreateMockedIssue(DisplaySeverity.Medium, 1, 1),
            CreateMockedIssue(DisplaySeverity.Info, 1, 2),
            CreateMockedIssue(DisplaySeverity.Info, 10, 1),
        };

        testSubject.AllIssues.Clear();
        testSubject.AllIssues.Add(sortedIssues[2]);
        testSubject.AllIssues.Add(sortedIssues[3]);
        testSubject.AllIssues.Add(sortedIssues[1]);
        testSubject.AllIssues.Add(sortedIssues[5]);
        testSubject.AllIssues.Add(sortedIssues[0]);
        testSubject.AllIssues.Add(sortedIssues[4]);
        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEquivalentTo(sortedIssues, options => options.WithStrictOrdering());
    }

    private static IIssueViewModel CreateMockedIssue(
        DisplaySeverity severity,
        int? line,
        int? column)
    {
        var mockIssue = Substitute.For<IIssueViewModel>();
        mockIssue.DisplaySeverity.Returns(severity);
        mockIssue.Line.Returns(line);
        mockIssue.Column.Returns(column);
        return mockIssue;
    }

    private static IIssueViewModel CreateMockedIssue(
        IssueType issueType,
        DisplaySeverity severity,
        DisplayStatus status,
        bool isOnNewCode)
    {
        var issueHotspot = Substitute.For<IIssueViewModel>();
        issueHotspot.IssueType.Returns(issueType);
        issueHotspot.DisplaySeverity.Returns(severity);
        issueHotspot.Status.Returns(status);
        issueHotspot.IsOnNewCode.Returns(isOnNewCode);
        return issueHotspot;
    }

    private void MockIssueTypeFilter(IssueType issueType, bool isSelected)
    {
        var dependencyRiskFilter = reportViewFilterViewModel.IssueTypeFilters.Single(f => f.IssueType == issueType);
        dependencyRiskFilter.IsSelected = isSelected;
    }

    private void MockSeverityFilter(DisplaySeverity displaySeverity) => reportViewFilterViewModel.SelectedSeverityFilter = displaySeverity;

    private void MockNewCodeFilter(bool isOnNewCode) => focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(isOnNewCode));

    private void MockStatusFilter(DisplayStatus displayStatus) => reportViewFilterViewModel.SelectedStatusFilter = displayStatus;

    private void ClearFilter() => reportViewFilterViewModel.IssueTypeFilters.ToList().ForEach(f => f.IsSelected = false);

    private void VerifyAllIssuesUnchanged() => testSubject.AllIssues.Should().BeSameAs(allIssues);

    private void ReceivedEvent(string eventName, int count = 1) => eventHandler.Received(count).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == eventName));
}
