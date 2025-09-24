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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class GroupFileViewModelTest
{
    private const string filePath = "c:\\myDir\\myFile.cs";
    private IThreadHandling threadHandling;
    private readonly IIssueViewModel hotspot = CreateMockedIssueType(IssueType.SecurityHotspot);
    private readonly IIssueViewModel taint = CreateMockedIssueType(IssueType.TaintVulnerability);
    private readonly ReportViewFilterViewModel reportViewFilterViewModel = new();
    private List<IIssueViewModel> allIssues;
    private GroupFileViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        threadHandling = Substitute.For<IThreadHandling>();
        allIssues = [hotspot, taint];
        testSubject = new GroupFileViewModel(filePath, allIssues, threadHandling);
        ClearFilter();
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
        MockFilterViewModel(IssueType.SecurityHotspot, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().HaveCount(1);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.IssueType == IssueType.SecurityHotspot);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_TaintFilterSelected_FilteredIssuesHasOnlyHotspots()
    {
        MockFilterViewModel(IssueType.TaintVulnerability, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().HaveCount(1);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.IssueType == IssueType.TaintVulnerability);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_DependencyRisksSelected_FilteredIssuesHasNoIssues()
    {
        MockFilterViewModel(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEmpty();
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_AllTypesSelected_FilteredIssuesHasAllIssues()
    {
        MockFilterViewModel(IssueType.SecurityHotspot, isSelected: true);
        MockFilterViewModel(IssueType.TaintVulnerability, isSelected: true);
        MockFilterViewModel(IssueType.DependencyRisk, isSelected: true);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEquivalentTo(testSubject.AllIssues);
        VerifyAllIssuesUnchanged();
    }

    [TestMethod]
    public void ApplyFilter_NoTypesSelected_FilteredIssuesHasNoIssues()
    {
        MockFilterViewModel(IssueType.SecurityHotspot, isSelected: false);
        MockFilterViewModel(IssueType.TaintVulnerability, isSelected: false);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.FilteredIssues.Should().BeEmpty();
        VerifyAllIssuesUnchanged();
    }

    private static IIssueViewModel CreateMockedIssueType(IssueType issueType)
    {
        var issueHotspot = Substitute.For<IIssueViewModel>();
        issueHotspot.IssueType.Returns(issueType);
        return issueHotspot;
    }

    private void MockFilterViewModel(IssueType issueType, bool isSelected)
    {
        var dependencyRiskFilter = reportViewFilterViewModel.IssueTypeFilters.Single(f => f.IssueType == issueType);
        dependencyRiskFilter.IsSelected = isSelected;
    }

    private void ClearFilter() => reportViewFilterViewModel.IssueTypeFilters.ToList().ForEach(f => f.IsSelected = false);

    private void VerifyAllIssuesUnchanged() => testSubject.AllIssues.Should().BeSameAs(allIssues);
}
