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
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class GroupDependencyRiskViewModelTest
{
    private GroupDependencyRiskViewModel testSubject;
    private IDependencyRisksStore dependencyRisksStore;
    private PropertyChangedEventHandler eventHandler;
    private readonly ReportViewFilterViewModel reportViewFilterViewModel = new();
    private readonly IDependencyRisk riskInfo = CreateDependencyRisk(severity: DependencyRiskImpactSeverity.Info);
    private readonly IDependencyRisk riskLow = CreateDependencyRisk(severity: DependencyRiskImpactSeverity.Low);
    private readonly IDependencyRisk riskMedium = CreateDependencyRisk(severity: DependencyRiskImpactSeverity.Medium);
    private readonly IDependencyRisk riskHigh = CreateDependencyRisk(severity: DependencyRiskImpactSeverity.High);
    private readonly IDependencyRisk riskBlocker = CreateDependencyRisk(severity: DependencyRiskImpactSeverity.Blocker);
    private readonly IDependencyRisk fixedRisk = CreateDependencyRisk(status: DependencyRiskStatus.Fixed);
    private IDependencyRisk[] risksOld;
    private IDependencyRisk[] risks;

    [TestInitialize]
    public void Initialize()
    {
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

        testSubject.ApplyFilter(reportViewFilterViewModel);

        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
    }

    [TestMethod]
    public void ApplyFilter_DependencyRiskFilterNotSelected_RemovesAllRisksFromFilteredLists()
    {
        SetInitialRisks(risks);
        MockIssueTypeFilter(isSelected: false);

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
    public void ApplyFilter_SeverityFilterSelected_ShowsOnlyRisksWithThatSeverity(DisplaySeverity selectedSeverityFilter, DependencyRiskImpactSeverity expectedSeverity)
    {
        SetInitialRisks(risks);
        MockSeverityFilter(selectedSeverityFilter);

        testSubject.ApplyFilter(reportViewFilterViewModel);

        var expectedFilteredRisks = risks.Where(r => r.Severity == expectedSeverity).ToArray();
        VerifyRisks(risks);
        VerifyFilteredRisks(expectedFilteredRisks);
    }

    [TestMethod]
    public void ApplyFilter_SeverityFilterNotSelected_ShowsAllRisks()
    {
        SetInitialRisks(risks);
        MockSeverityFilter(displaySeverity: null);

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

        testSubject.ApplyFilter(reportViewFilterViewModel);

        VerifyRisks(risks);
        VerifyFilteredRisks([]);
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

    private void MockSeverityFilter(DisplaySeverity? displaySeverity) => reportViewFilterViewModel.SelectedSeverityFilter = displaySeverity;
}
