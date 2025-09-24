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

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class GroupDependencyRiskViewModelTest
{
    private GroupDependencyRiskViewModel testSubject;
    private IDependencyRisksStore dependencyRisksStore;
    private PropertyChangedEventHandler eventHandler;
    private readonly IDependencyRisk risk1 = CreateDependencyRisk();
    private readonly IDependencyRisk risk2 = CreateDependencyRisk();
    private readonly IDependencyRisk risk3 = CreateDependencyRisk();
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
        risks = [risk1, risk2, risk3];
    }

    [TestMethod]
    public void Ctor_HasPropertiesInitialized()
    {
        testSubject.Title.Should().Be(Resources.DependencyRisksGroupTitle);
        testSubject.AllIssues.Should().BeEmpty();
    }

    [TestMethod]
    public void InitializeRisks_InitializesRisks()
    {
        var dependencyRisk = CreateDependencyRisk();
        var dependencyRisk2 = CreateDependencyRisk();
        MockRisksInStore(dependencyRisk, dependencyRisk2);
        dependencyRisksStore.ClearReceivedCalls();

        testSubject.InitializeRisks();

        dependencyRisksStore.Received(1).GetAll();
        testSubject.AllIssues.Should().HaveCount(2);
        testSubject.AllIssues.Should().ContainSingle(vm => ((DependencyRiskViewModel)vm).DependencyRisk == dependencyRisk);
        testSubject.AllIssues.Should().ContainSingle(vm => ((DependencyRiskViewModel)vm).DependencyRisk == dependencyRisk2);
    }

    [TestMethod]
    public void InitializeRisks_DefaultFilters_FilteredRisksContainsOnlyOpen()
    {
        MockRisksInStore(risksOld);

        testSubject.InitializeRisks();

        VerifyRisks(risksOld);
        VerifyFilteredRisks(risksOld);
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void InitializeRisks_NoRisks_FilteredRisksIsEmpty()
    {
        MockRisksInStore([]);

        testSubject.InitializeRisks();

        VerifyRisks();
        VerifyFilteredRisks();
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void InitializeRisks_PassAllFilter_FilteredRisksContainsAll()
    {
        SetInitialRisks(risksOld);
        MockRisksInStore(risks);

        testSubject.InitializeRisks();

        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
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

    private void VerifyUpdatedBothRiskLists()
    {
        dependencyRisksStore.Received().GetAll();
        ReceivedEvent(nameof(testSubject.AllIssues));
    }

    private void SetInitialRisks(IDependencyRisk[] state)
    {
        MockRisksInStore(state);
        testSubject.InitializeRisks();
        dependencyRisksStore.ClearReceivedCalls();
        eventHandler.ClearReceivedCalls();
    }

    private void VerifyRisks(params IDependencyRisk[] state) => testSubject.AllIssues.Select(x => ((DependencyRiskViewModel)x).DependencyRisk).Should().BeEquivalentTo(state);

    private void VerifyFilteredRisks(params IDependencyRisk[] state) => testSubject.AllIssues.Select(x => ((DependencyRiskViewModel)x).DependencyRisk).Should().BeEquivalentTo(state);

    private void ReceivedEvent(string eventName, int count = 1) => eventHandler.Received(count).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == eventName));

    private void MockRisksInStore(params IDependencyRisk[] dependencyRisks) => dependencyRisksStore.GetAll().Returns(dependencyRisks);

    private static IDependencyRisk CreateDependencyRisk(Guid? id = null, DependencyRiskStatus status = DependencyRiskStatus.Open)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Status.Returns(status);
        risk.Transitions.Returns([]);
        return risk;
    }
}
