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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class GroupDependencyRiskViewModelTest
{
    private GroupDependencyRiskViewModel testSubject;
    private IDependencyRisksStore dependencyRisksStore;
    private PropertyChangedEventHandler eventHandler;
    private IFocusOnNewCodeServiceUpdater focusOnNewCodeService;
    private ReportViewFilterViewModel reportViewFilterViewModel;
    private readonly IDependencyRisk riskInfo = CreateDependencyRisk(severity: DependencyRiskImpactSeverity.Info);
    private readonly IDependencyRisk riskLow = CreateDependencyRisk(status: DependencyRiskStatus.Open, severity: DependencyRiskImpactSeverity.Low);
    private readonly IDependencyRisk riskMedium = CreateDependencyRisk(status: DependencyRiskStatus.Confirmed, severity: DependencyRiskImpactSeverity.Medium);
    private readonly IDependencyRisk riskHigh = CreateDependencyRisk(status: DependencyRiskStatus.Accepted, severity: DependencyRiskImpactSeverity.High);
    private readonly IDependencyRisk riskBlocker = CreateDependencyRisk(status: DependencyRiskStatus.Safe, severity: DependencyRiskImpactSeverity.Blocker);
    private readonly IDependencyRisk fixedRisk = CreateDependencyRisk(status: DependencyRiskStatus.Fixed);
    private IDependencyRisk[] risksOld;
    private IDependencyRisk[] risks;

    [TestInitialize]
    public void Initialize()
    {
        focusOnNewCodeService = Substitute.For<IFocusOnNewCodeServiceUpdater>();
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(false));
        reportViewFilterViewModel = new(focusOnNewCodeService, Substitute.ForPartsOf<NoOpThreadHandler>());
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        testSubject = new(dependencyRisksStore);
        eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;
        risksOld = [CreateDependencyRisk(), CreateDependencyRisk()];
        risks = [riskInfo, riskLow, riskMedium, riskHigh, riskBlocker];
    }

    [TestMethod]
    public void Ctor_HasPropertiesInitialized()
    {
        testSubject.Title.Should().Be(Resources.DependencyRisksGroupTitle);
        testSubject.AllIssues.Should().BeEmpty();
        testSubject.FilteredIssues.Should().NotBeSameAs(testSubject.AllIssues);
        testSubject.FilteredIssues.Should().BeEmpty();
    }

    [TestMethod]
    public void InitializeRisks_InitializesRisks()
    {
        MockRisksInStore(risksOld);

        testSubject.InitializeRisks();

        VerifyRisks(risksOld);
        VerifyFilteredRisks(risksOld);
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void InitializeRisks_FixedRisks_FilteredRisksDoNotContainRisksAlreadyFixed()
    {
        var risksWithFixed = risks.ToList();
        risksWithFixed.Add(fixedRisk);
        MockRisksInStore(risksWithFixed.ToArray());

        testSubject.InitializeRisks();

        testSubject.AllIssues.Should().NotContain(vm => ((DependencyRiskViewModel)vm).DependencyRisk.Status == DependencyRiskStatus.Fixed);
        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
    }

    [TestMethod]
    public void ApplyFilter_DependencyRiskFilterSelected_ShowsAllRisks()
    {
        SetInitialRisks(risks);
        MockIssueTypeFilter(isSelected: true);
        MockStatusFilter(DisplayStatus.Any);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
    }

    [TestMethod]
    public void ApplyFilter_DependencyRiskFilterNotSelected_RemovesAllRisksFromFilteredLists()
    {
        SetInitialRisks(risks);
        MockIssueTypeFilter(isSelected: false);
        MockStatusFilter(DisplayStatus.Any);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        VerifyRisks(risks);
        VerifyFilteredRisks([]);
    }

    [TestMethod]
    [DataRow(DisplaySeverity.Info, DependencyRiskImpactSeverity.Info)]
    [DataRow(DisplaySeverity.Low, DependencyRiskImpactSeverity.Low)]
    [DataRow(DisplaySeverity.Medium, DependencyRiskImpactSeverity.Medium)]
    [DataRow(DisplaySeverity.High, DependencyRiskImpactSeverity.High)]
    [DataRow(DisplaySeverity.Blocker, DependencyRiskImpactSeverity.Blocker)]
    public void ApplyFilter_SeverityFilterSelected_ShowsOnlyRisksWithThatSeverityOrHigher(DisplaySeverity selectedSeverityFilter, DependencyRiskImpactSeverity expectedSeverity)
    {
        SetInitialRisks(risks);
        MockSeverityFilter(selectedSeverityFilter);
        MockStatusFilter(DisplayStatus.Any);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        var expectedFilteredRisks = risks.Where(r => r.Severity >= expectedSeverity).ToArray();
        VerifyRisks(risks);
        VerifyFilteredRisks(expectedFilteredRisks);
    }

    [TestMethod]
    public void ApplyFilter_SeverityFilterNotSelected_ShowsAllRisks()
    {
        SetInitialRisks(risks);
        MockSeverityFilter(displaySeverity: DisplaySeverity.Info);
        MockStatusFilter(DisplayStatus.Any);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
    }

    [TestMethod]
    public void ApplyFilter_DependencyRiskFilterNotSelectedAndSeveritySelected_RemovesAllRisksFromFilteredLists()
    {
        SetInitialRisks(risks);
        MockIssueTypeFilter(isSelected: false);
        MockSeverityFilter(DisplaySeverity.Blocker);
        MockStatusFilter(DisplayStatus.Any);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        VerifyRisks(risks);
        VerifyFilteredRisks([]);
    }

    [TestMethod]
    public void ApplyFilter_OpenStatusFilterSelected_ShowsOnlyRisksWithStatusOpen()
    {
        SetInitialRisks(risks);
        MockStatusFilter(DisplayStatus.Open);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        var expectedFilteredRisks = risks.Where(r => r.Status is DependencyRiskStatus.Open or DependencyRiskStatus.Confirmed).ToArray();
        VerifyRisks(risks);
        VerifyFilteredRisks(expectedFilteredRisks);
    }

    [TestMethod]
    public void ApplyFilter_ResolvedStatusFilterSelected_ShowsOnlyRisksWithStatusResolved()
    {
        SetInitialRisks(risks);
        MockStatusFilter(DisplayStatus.Resolved);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        var expectedFilteredRisks = risks.Where(r => r.Status is DependencyRiskStatus.Accepted or DependencyRiskStatus.Safe or DependencyRiskStatus.Fixed).ToArray();
        VerifyRisks(risks);
        VerifyFilteredRisks(expectedFilteredRisks);
    }


    [DataRow(true)]
    [DataRow(false)]
    [DataTestMethod]
    public void ApplyFilter_NewCodeSelected_DoesNotAffect(bool isOnNewCode)
    {
        SetInitialRisks(risks);
        MockStatusFilter(displayStatus: DisplayStatus.Any);
        MockNewCodeFilter(isOnNewCode);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.AllIssues.Should().HaveCount(5);
        testSubject.PreFilteredIssues.Should().HaveCount(5);
        testSubject.FilteredIssues.Should().HaveCount(5);
    }

    [TestMethod]
    public void ApplyFilter_StatusFilterNotSelected_ShowsAllRisks()
    {
        SetInitialRisks(risks);
        MockStatusFilter(displayStatus: DisplayStatus.Any);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
    }

    [TestMethod]
    public void ApplyFilter_StatusAndSeverityFilter_AppliesStatusAsPreFilter()
    {
        SetInitialRisks(risks);
        MockStatusFilter(DisplayStatus.Resolved);
        MockSeverityFilter(DisplaySeverity.Blocker);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.AllIssues.Should().HaveCount(5);
        testSubject.PreFilteredIssues.Should().HaveCount(2);
        testSubject.FilteredIssues.Should().HaveCount(1);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.Status == DisplayStatus.Resolved && i.DisplaySeverity == DisplaySeverity.Blocker);
    }

    [TestMethod]
    public void ApplyFilter_SeverityFilter_NoPreFiltering()
    {
        SetInitialRisks(risks);
        MockStatusFilter(DisplayStatus.Any);
        MockSeverityFilter(DisplaySeverity.Blocker);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        testSubject.AllIssues.Should().HaveCount(5);
        testSubject.PreFilteredIssues.Should().HaveCount(5);
        testSubject.FilteredIssues.Should().HaveCount(1);
        testSubject.FilteredIssues.Should().OnlyContain(i => i.DisplaySeverity == DisplaySeverity.Blocker);
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

    private void VerifyUpdatedBothRiskLists()
    {
        dependencyRisksStore.Received().GetAll();
        ReceivedEvent(nameof(testSubject.FilteredIssues));
    }

    private void SetInitialRisks(IDependencyRisk[] state)
    {
        MockRisksInStore(state);
        testSubject.InitializeRisks();
        dependencyRisksStore.ClearReceivedCalls();
        eventHandler.ClearReceivedCalls();
    }

    private void VerifyRisks(params IDependencyRisk[] state) => testSubject.AllIssues.Select(x => ((DependencyRiskViewModel)x).DependencyRisk).Should().BeEquivalentTo(state);

    private void VerifyFilteredRisks(params IDependencyRisk[] state) => testSubject.FilteredIssues.Select(x => ((DependencyRiskViewModel)x).DependencyRisk).Should().BeEquivalentTo(state);

    private void ReceivedEvent(string eventName, int count = 1) => eventHandler.Received(count).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == eventName));

    private void MockRisksInStore(params IDependencyRisk[] dependencyRisks) => dependencyRisksStore.GetAll().Returns(dependencyRisks);

    private static IDependencyRisk CreateDependencyRisk(Guid? id = null, DependencyRiskStatus status = DependencyRiskStatus.Open, DependencyRiskImpactSeverity severity = default)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Status.Returns(status);
        risk.Severity.Returns(severity);
        risk.Transitions.Returns([]);
        return risk;
    }

    private void MockIssueTypeFilter(bool isSelected)
    {
        var dependencyRiskFilter = reportViewFilterViewModel.IssueTypeFilters.Single(f => f.IssueType == IssueType.DependencyRisk);
        dependencyRiskFilter.IsSelected = isSelected;
    }

    private void MockSeverityFilter(DisplaySeverity displaySeverity) => reportViewFilterViewModel.SelectedSeverityFilter = displaySeverity;

    private void MockStatusFilter(DisplayStatus displayStatus) => reportViewFilterViewModel.SelectedStatusFilter = displayStatus;

    private void MockNewCodeFilter(bool isOnNewCode) => reportViewFilterViewModel.SelectedNewCodeFilter = isOnNewCode;
}
