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
using System.Globalization;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Filters;

[TestClass]
public class IssueTypeFilterToTextConverterTest
{
    private IssueTypeFilterToTextConverter testSubject;
    private IReportViewModel reportViewModel;
    private IIssueTypeFilterViewModel issueTypeFilterViewModel;

    [TestInitialize]
    public void TestInitialize()
    {
        reportViewModel = Substitute.For<IReportViewModel>();
        issueTypeFilterViewModel = Substitute.For<IIssueTypeFilterViewModel>();
        testSubject = new IssueTypeFilterToTextConverter();
    }

    [TestMethod]
    public void Convert_WhenLessParametersThanExpected_ReturnsNull()
    {
        var result = testSubject.Convert([reportViewModel], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_WhenMoreParametersThanExpected_ReturnsLocalization()
    {
        reportViewModel.GroupViewModels.Returns([]);

        var result = testSubject.Convert([issueTypeFilterViewModel, reportViewModel, default, default], null, null, CultureInfo.InvariantCulture);

        result.Should().NotBeNull();
    }

    [TestMethod]
    public void Convert_WhenIssueTypeFilterViewModelNotProvided_ReturnsNull()
    {
        var result = testSubject.Convert([null, reportViewModel], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    public void Convert_WhenReportViewModelNotProvided_ReturnsNull()
    {
        var result = testSubject.Convert([issueTypeFilterViewModel, null], null, null, CultureInfo.InvariantCulture);

        result.Should().BeNull();
    }

    [TestMethod]
    [DataRow("No Dependency Risks", 0, 0, 1)]
    [DataRow("1 Dependency Risk", 1, 2, 6)]
    [DataRow("8 Dependency Risks", 8, 2, 0)]
    public void Convert_DependencyRisk_ReturnsExpectedLocalization(
        string expectedLocalization,
        int risksCount,
        int hotspotsCount,
        int taintsCount)
    {
        MockGroupViewModel(risksCount, hotspotsCount, taintsCount);
        MockIssueTypeFilter(IssueType.DependencyRisk);

        var result = testSubject.Convert([issueTypeFilterViewModel, reportViewModel], null, null, null);

        result.Should().Be(expectedLocalization);
    }

    [TestMethod]
    [DataRow("No Security Hotspots", 20, 0, 1)]
    [DataRow("1 Security Hotspot", 0, 1, 6)]
    [DataRow("14 Security Hotspots", 28, 14, 0)]
    public void Convert_SecurityHotspot_ReturnsExpectedLocalization(
        string expectedLocalization,
        int risksCount,
        int hotspotsCount,
        int taintsCount)
    {
        MockGroupViewModel(risksCount, hotspotsCount, taintsCount);
        MockIssueTypeFilter(IssueType.SecurityHotspot);

        var result = testSubject.Convert([issueTypeFilterViewModel, reportViewModel], null, null, null);

        result.Should().Be(expectedLocalization);
    }

    [TestMethod]
    [DataRow("No Taint Vulnerabilities", 20, 1, 0)]
    [DataRow("1 Taint Vulnerability", 9, 0, 1)]
    [DataRow("13 Taint Vulnerabilities", 0, 1, 13)]
    public void Convert_TaintVulnerability_ReturnsExpectedLocalization(
        string expectedLocalization,
        int risksCount,
        int hotspotsCount,
        int taintsCount)
    {
        MockGroupViewModel(risksCount, hotspotsCount, taintsCount);
        MockIssueTypeFilter(IssueType.TaintVulnerability);

        var result = testSubject.Convert([issueTypeFilterViewModel, reportViewModel], null, null, null);

        result.Should().Be(expectedLocalization);
    }

    [TestMethod]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Action act = () => testSubject.ConvertBack("value", null, null, null);
        act.Should().Throw<NotImplementedException>();
    }

    private void MockIssueTypeFilter(IssueType type) => issueTypeFilterViewModel.IssueType.Returns(type);

    private void MockGroupViewModel(int risksCount, int hotspotCount, int taintCount)
    {
        var risks = new ObservableCollection<IIssueViewModel>();
        var hotspots = new ObservableCollection<IIssueViewModel>();
        var taints = new ObservableCollection<IIssueViewModel>();

        for (var i = 0; i < risksCount; i++)
        {
            risks.Add(CreateMockedIssueViewModel(IssueType.DependencyRisk));
        }
        for (var i = 0; i < hotspotCount; i++)
        {
            hotspots.Add(CreateMockedIssueViewModel(IssueType.SecurityHotspot));
        }
        for (var i = 0; i < taintCount; i++)
        {
            taints.Add(CreateMockedIssueViewModel(IssueType.TaintVulnerability));
        }

        var groupVm = Substitute.For<IGroupViewModel>();
        var filteredIssues = new ObservableCollection<IIssueViewModel>(risks.Union(hotspots).Union(taints));
        groupVm.FilteredIssues.Returns(filteredIssues);
        reportViewModel.GroupViewModels.Returns([groupVm]);
    }

    private static IIssueViewModel CreateMockedIssueViewModel(IssueType issueType)
    {
        var issueViewModel = Substitute.For<IIssueViewModel>();
        issueViewModel.IssueType.Returns(issueType);
        return issueViewModel;
    }
}
