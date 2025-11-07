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

using System.ComponentModel;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class GroupFileViewModelTest
{
    private const string filePath = "c:\\myDir\\myFile.cs";
    private readonly IIssueViewModel hotspotInfo = CreateMockedIssueType(IssueType.SecurityHotspot, DisplaySeverity.Info, DisplayStatus.Open);
    private readonly IIssueViewModel hotspotLow = CreateMockedIssueType(IssueType.SecurityHotspot, DisplaySeverity.Low, DisplayStatus.Resolved);
    private readonly IIssueViewModel taintHigh = CreateMockedIssueType(IssueType.TaintVulnerability, DisplaySeverity.High, DisplayStatus.Open);
    private readonly IIssueViewModel taintMedium = CreateMockedIssueType(IssueType.TaintVulnerability, DisplaySeverity.Medium, DisplayStatus.Resolved);
    private readonly IIssueViewModel taintBlocker = CreateMockedIssueType(IssueType.TaintVulnerability, DisplaySeverity.Blocker, DisplayStatus.Resolved);
    private readonly ReportViewFilterViewModel reportViewFilterViewModel = new();
    private List<IIssueViewModel> allIssues;
    private PropertyChangedEventHandler eventHandler;
    private GroupFileViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
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

        testSubject.FilteredIssues.Should().HaveCount(3);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.Status == DisplayStatus.Resolved);
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

    private static IIssueViewModel CreateMockedIssueType(IssueType issueType, DisplaySeverity severity = DisplaySeverity.Info, DisplayStatus status = DisplayStatus.Open)
    {
        var issueHotspot = Substitute.For<IIssueViewModel>();
        issueHotspot.IssueType.Returns(issueType);
        issueHotspot.DisplaySeverity.Returns(severity);
        issueHotspot.Status.Returns(status);
        return issueHotspot;
    }

    private void MockIssueTypeFilter(IssueType issueType, bool isSelected)
    {
        var dependencyRiskFilter = reportViewFilterViewModel.IssueTypeFilters.Single(f => f.IssueType == issueType);
        dependencyRiskFilter.IsSelected = isSelected;
    }

    private void MockSeverityFilter(DisplaySeverity displaySeverity) => reportViewFilterViewModel.SelectedSeverityFilter = displaySeverity;

    private void MockStatusFilter(DisplayStatus displayStatus) => reportViewFilterViewModel.SelectedStatusFilter = displayStatus;

    private void ClearFilter() => reportViewFilterViewModel.IssueTypeFilters.ToList().ForEach(f => f.IsSelected = false);

    private void VerifyAllIssuesUnchanged() => testSubject.AllIssues.Should().BeSameAs(allIssues);

    private void ReceivedEvent(string eventName, int count = 1) => eventHandler.Received(count).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == eventName));
}
