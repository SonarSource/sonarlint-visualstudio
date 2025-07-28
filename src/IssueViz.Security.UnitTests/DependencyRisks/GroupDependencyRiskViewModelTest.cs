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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.DependencyRisks;

[TestClass]
public class GroupDependencyRiskViewModelTest
{
    private GroupDependencyRiskViewModel testSubject;
    private IDependencyRisksStore dependencyRisksStore;
    private IThreadHandling threadHandling;
    private ITelemetryManager telemetryManager;

    [TestInitialize]
    public void Initialize()
    {
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        testSubject = new(dependencyRisksStore, telemetryManager, threadHandling);
    }

    [TestMethod]
    public void Ctor_HasPropertiesInitialized()
    {
        GroupDependencyRiskViewModel.Title.Should().Be(Resources.DependencyRisksGroupTitle);
        testSubject.Risks.Should().BeEmpty();
    }

    [TestMethod]
    public void Ctor_SubscribesToEvents() => dependencyRisksStore.Received().DependencyRisksChanged += Arg.Any<EventHandler>();

    [TestMethod]
    public void InitializeRisks_ExecutesOnUIThread()
    {
        testSubject.InitializeRisks();

        threadHandling.Received(1).RunOnUIThread(Arg.Any<Action>());
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
        testSubject.Risks.Should().HaveCount(2);
        testSubject.Risks.Should().ContainSingle(vm => vm.DependencyRisk == dependencyRisk);
        testSubject.Risks.Should().ContainSingle(vm => vm.DependencyRisk == dependencyRisk2);
    }

    [TestMethod]
    public void InitializeRisks_RaisesPropertyChanged()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.InitializeRisks();

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.HasRisks)));
    }

    [TestMethod]
    public void HasRisks_ReturnsTrue_WhenThereAreRisks()
    {
        MockRisksInStore(CreateDependencyRisk());

        testSubject.InitializeRisks();

        testSubject.HasRisks.Should().BeTrue();
    }

    [TestMethod]
    public void HasRisks_ReturnsFalse_WhenThereAreNoRisks() => testSubject.HasRisks.Should().BeFalse();

    [TestMethod]
    public void DependencyRisksChanged_RefreshesRisks()
    {
        var dependencyRisk = CreateDependencyRisk();
        MockRisksInStore(dependencyRisk);
        dependencyRisksStore.ClearReceivedCalls();

        dependencyRisksStore.DependencyRisksChanged += Raise.Event<EventHandler>();

        dependencyRisksStore.Received(1).GetAll();
        testSubject.Risks.Should().ContainSingle(vm => vm.DependencyRisk == dependencyRisk);
    }

    [TestMethod]
    public void SelectedItem_Initially_IsNull()
    {
        testSubject.SelectedItem.Should().BeNull();
    }

    [TestMethod]
    public void SelectedItem_SetToValue_CallsTelemetry()
    {
        var risk = CreateDependencyRisk();
        var riskViewModel = new DependencyRiskViewModel(risk);

        testSubject.SelectedItem = riskViewModel;

        testSubject.SelectedItem.Should().BeSameAs(riskViewModel);
        telemetryManager.Received(1).DependencyRiskInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToSameValue_DoesNotCallTelemetry()
    {
        var riskViewModel = new DependencyRiskViewModel(CreateDependencyRisk());
        testSubject.SelectedItem = riskViewModel;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = riskViewModel;

        telemetryManager.DidNotReceive().DependencyRiskInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToDifferentValue_CallsTelemetry()
    {
        var riskViewModel1 = new DependencyRiskViewModel(CreateDependencyRisk());
        var riskViewModel2 = new DependencyRiskViewModel(CreateDependencyRisk());
        testSubject.SelectedItem = riskViewModel1;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = riskViewModel2;

        testSubject.SelectedItem.Should().BeSameAs(riskViewModel2);
        telemetryManager.Received(1).DependencyRiskInvestigatedLocally();
    }

    [TestMethod]
    public void SelectedItem_SetToNull_DoesNotCallTelemetry()
    {
        var riskViewModel1 = new DependencyRiskViewModel(CreateDependencyRisk());
        testSubject.SelectedItem = riskViewModel1;
        telemetryManager.ClearReceivedCalls();

        testSubject.SelectedItem = null;

        testSubject.SelectedItem.Should().BeNull();
        telemetryManager.DidNotReceive().DependencyRiskInvestigatedLocally();
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        testSubject.Dispose();

        dependencyRisksStore.Received(1).DependencyRisksChanged -= Arg.Any<EventHandler>();
    }

    private IDependencyRisk CreateDependencyRisk()
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Transitions.Returns([]);
        return risk;
    }

    private void MockRisksInStore(params IDependencyRisk[] dependencyRisks) => dependencyRisksStore.GetAll().Returns(dependencyRisks);
}
