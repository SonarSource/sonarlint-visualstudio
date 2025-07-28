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
using System.Windows;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.Telemetry;
using SonarLint.VisualStudio.IssueVisualization.Security.DependencyRisks;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView;

[TestClass]
public class ReportViewModelTest
{
    private ReportViewModel testSubject;
    private IActiveSolutionBoundTracker activeSolutionBoundTracker;
    private IDependencyRisksStore dependencyRisksStore;
    private IShowDependencyRiskInBrowserHandler showDependencyRiskInBrowserHandler;
    private IChangeDependencyRiskStatusHandler changeDependencyRiskStatusHandler;
    private IMessageBox messageBox;
    private ITelemetryManager telemetryManager;
    private IThreadHandling threadHandling;
    private readonly IDependencyRisk openRisk = CreateDependencyRisk(isResolved: false);
    private readonly IDependencyRisk openRisk2 = CreateDependencyRisk(isResolved: false);
    private readonly IDependencyRisk resolvedRisk =  CreateDependencyRisk(isResolved: true);
    private readonly IDependencyRisk resolvedRisk2 =  CreateDependencyRisk(isResolved: true);
    private IDependencyRisk[] risks;
    private IDependencyRisk[] risks2;
    private PropertyChangedEventHandler eventHandler;

    [TestInitialize]
    public void Initialize()
    {
        activeSolutionBoundTracker = Substitute.For<IActiveSolutionBoundTracker>();
        dependencyRisksStore = Substitute.For<IDependencyRisksStore>();
        showDependencyRiskInBrowserHandler = Substitute.For<IShowDependencyRiskInBrowserHandler>();
        changeDependencyRiskStatusHandler = Substitute.For<IChangeDependencyRiskStatusHandler>();
        messageBox = Substitute.For<IMessageBox>();
        telemetryManager = Substitute.For<ITelemetryManager>();
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        risks = [openRisk, resolvedRisk];
        risks2 = [openRisk2, resolvedRisk2];

        testSubject = CreateTestSubject();
        eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.GroupDependencyRisk.PropertyChanged += eventHandler;
    }

    [TestMethod]
    public void Class_InheritsFromServerViewModel() => testSubject.Should().BeAssignableTo<ServerViewModel>();

    [TestMethod]
    public void Ctor_InitializesDependencyRisks()
    {
        var dependencyRisk = CreateDependencyRisk();
        MockRisksInStore(dependencyRisk);

        testSubject = CreateTestSubject();

        testSubject.GroupDependencyRisk.Should().NotBeNull();
        testSubject.GroupDependencyRisk.Risks.Should().ContainSingle(vm => vm.DependencyRisk == dependencyRisk);
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromEvents()
    {
        MockRisksInStore(CreateDependencyRisk(), CreateDependencyRisk());

        testSubject.Dispose();

        dependencyRisksStore.Received(1).DependencyRisksChanged -= Arg.Any<EventHandler>();
    }

    [TestMethod]
    public void ShowInBrowser_CallsHandler()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);

        testSubject.ShowInBrowser(dependencyRisk);

        showDependencyRiskInBrowserHandler.Received(1).ShowInBrowser(riskId);
    }

    [TestMethod]
    public async Task ChangeStatusAsync_CallsHandler_Success()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);
        var transition = DependencyRiskTransition.Accept;
        var comment = "test comment";
        changeDependencyRiskStatusHandler.ChangeStatusAsync(riskId, transition, comment).Returns(true);

        await testSubject.ChangeStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.Received(1).ChangeStatusAsync(riskId, transition, comment);
        messageBox.DidNotReceiveWithAnyArgs().Show(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<MessageBoxButton>(), Arg.Any<MessageBoxImage>());
    }

    [TestMethod]
    public async Task ChangeStatusAsync_CallsHandler_Failure_ShowsMessageBox()
    {
        var riskId = Guid.NewGuid();
        var dependencyRisk = CreateDependencyRisk(riskId);
        const DependencyRiskTransition transition = DependencyRiskTransition.Accept;
        const string comment = "test comment";
        changeDependencyRiskStatusHandler.ChangeStatusAsync(riskId, transition, comment).Returns(false);

        await testSubject.ChangeStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.Received(1).ChangeStatusAsync(riskId, transition, comment);
        messageBox.Received(1).Show(Resources.DependencyRiskStatusChangeFailedTitle, Resources.DependencyRiskStatusChangeError, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    public async Task ChangeStatusAsync_NullTransition_DoesNotCallHandler_ShowsMessageBox()
    {
        var dependencyRisk = CreateDependencyRisk();
        DependencyRiskTransition? transition = null;
        const string comment = "test comment";

        await testSubject.ChangeStatusAsync(dependencyRisk, transition, comment);

        await changeDependencyRiskStatusHandler.DidNotReceiveWithAnyArgs().ChangeStatusAsync(Arg.Any<Guid>(), Arg.Any<DependencyRiskTransition>(), Arg.Any<string>());
        messageBox.Received(1).Show(Resources.DependencyRiskStatusChangeFailedTitle, Resources.DependencyRiskNullTransitionError, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [TestMethod]
    public void ResolutionFilterOpen_ExpectedResolutionAndDefaultSelection()
    {
        testSubject.ResolutionFilterOpen.IsResolved.Should().BeFalse();
        testSubject.ResolutionFilterOpen.IsSelected.Should().BeTrue();
    }

    [TestMethod]
    public void ResolutionFilterResolved_ExpectedResolutionAndDefaultSelection()
    {
        testSubject.ResolutionFilterResolved.IsResolved.Should().BeTrue();
        testSubject.ResolutionFilterResolved.IsSelected.Should().BeFalse();
    }

    [TestMethod]
    public void InitializeRisks_DefaultFilters_FilteredRisksContainsOnlyOpen()
    {
        MockRisksInStore(risks);

        testSubject.GroupDependencyRisk.InitializeRisks();

        VerifyRisks(risks);
        VerifyFilteredRisks(openRisk);
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void InitializeRisks_NoRisks_FilteredRisksIsEmpty()
    {
        MockRisksInStore([]);

        testSubject.GroupDependencyRisk.InitializeRisks();

        VerifyRisks();
        VerifyFilteredRisks();
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void InitializeRisks_DefaultFilters_NewRisks_FilteredRisksContainsOnlyOpen()
    {
        SetInitialRisks(risks);
        MockRisksInStore(risks2);

        testSubject.GroupDependencyRisk.InitializeRisks();

        VerifyRisks(risks2);
        VerifyFilteredRisks(openRisk2);
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void InitializeRisks_OnlyResolvedSelected_FilteredRisksContainsOnlyResolved()
    {
        SetInitialRisks(risks);
        testSubject.ResolutionFilterOpen.IsSelected = false;
        testSubject.ResolutionFilterResolved.IsSelected = true;
        MockRisksInStore(risks2);

        testSubject.GroupDependencyRisk.InitializeRisks();

        VerifyRisks(risks2);
        VerifyFilteredRisks(resolvedRisk2);
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void InitializeRisks_BothFiltersSelected_FilteredRisksContainsAllRisks()
    {
        SetInitialRisks(risks);
        testSubject.ResolutionFilterOpen.IsSelected = true;
        testSubject.ResolutionFilterResolved.IsSelected = true;
        MockRisksInStore(risks2);

        testSubject.GroupDependencyRisk.InitializeRisks();

        VerifyRisks(risks2);
        VerifyFilteredRisks(risks2);
        VerifyUpdatedBothRiskLists();
    }

    [TestMethod]
    public void FlipAndUpdateResolutionFilter_DisableOpenWhenResolvedWasNotSelected_FilteredContainsOnlyResolved()
    {
        SetInitialRisks(risks);

        testSubject.FlipAndUpdateResolutionFilter(testSubject.ResolutionFilterOpen);

        testSubject.ResolutionFilterOpen.IsSelected.Should().BeFalse();
        testSubject.ResolutionFilterResolved.IsSelected.Should().BeTrue();
        VerifyRisks(risks);
        VerifyFilteredRisks(resolvedRisk);
        VerifyOnlyUpdatedFilteredRiskList();
    }

    [TestMethod]
    public void FlipAndUpdateResolutionFilter_DisableResolvedWhenOpenWasNotSelected_FilteredContainsOnlyResolved()
    {
        SetInitialRisks(risks);
        testSubject.ResolutionFilterOpen.IsSelected = false;
        testSubject.ResolutionFilterResolved.IsSelected = true;

        testSubject.FlipAndUpdateResolutionFilter(testSubject.ResolutionFilterResolved);

        testSubject.ResolutionFilterOpen.IsSelected.Should().BeTrue();
        testSubject.ResolutionFilterResolved.IsSelected.Should().BeFalse();
        VerifyRisks(risks);
        VerifyFilteredRisks(openRisk);
        VerifyOnlyUpdatedFilteredRiskList();
    }

    [TestMethod]
    public void UpdateResolutionFilter_EnableResolvedWhenOpenSelected_FilteredContainsAll()
    {
        SetInitialRisks(risks);
        testSubject.ResolutionFilterOpen.IsSelected = true;
        testSubject.ResolutionFilterResolved.IsSelected = false;

        testSubject.FlipAndUpdateResolutionFilter(testSubject.ResolutionFilterResolved);

        testSubject.ResolutionFilterOpen.IsSelected.Should().BeTrue();
        testSubject.ResolutionFilterResolved.IsSelected.Should().BeTrue();
        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
        VerifyOnlyUpdatedFilteredRiskList();
    }

    [TestMethod]
    public void UpdateResolutionFilter_EnableOpenWhenResolveSelected_FilteredContainsAll()
    {
        SetInitialRisks(risks);
        testSubject.ResolutionFilterOpen.IsSelected = false;
        testSubject.ResolutionFilterResolved.IsSelected = true;

        testSubject.FlipAndUpdateResolutionFilter(testSubject.ResolutionFilterOpen);

        testSubject.ResolutionFilterOpen.IsSelected.Should().BeTrue();
        testSubject.ResolutionFilterResolved.IsSelected.Should().BeTrue();
        VerifyRisks(risks);
        VerifyFilteredRisks(risks);
        VerifyOnlyUpdatedFilteredRiskList();
    }

    private void VerifyOnlyUpdatedFilteredRiskList()
    {
        dependencyRisksStore.DidNotReceiveWithAnyArgs().GetAll();
        ReceivedEvent(nameof(testSubject.GroupDependencyRisk.FilteredRisks));
        DidNotReceiveEvent(nameof(testSubject.GroupDependencyRisk.Risks));
        DidNotReceiveEvent(nameof(testSubject.GroupDependencyRisk.HasRisks));
    }

    private void VerifyUpdatedBothRiskLists()
    {
        dependencyRisksStore.Received().GetAll();
        ReceivedEvent(nameof(testSubject.GroupDependencyRisk.FilteredRisks));
        ReceivedEvent(nameof(testSubject.GroupDependencyRisk.Risks));
        ReceivedEvent(nameof(testSubject.GroupDependencyRisk.HasRisks));
    }

    private void SetInitialRisks(IDependencyRisk[] state)
    {
        MockRisksInStore(state);
        testSubject.GroupDependencyRisk.InitializeRisks();
        dependencyRisksStore.ClearReceivedCalls();
        eventHandler.ClearReceivedCalls();
    }

    private void VerifyRisks(params IDependencyRisk[] state) =>
        testSubject.GroupDependencyRisk.Risks.Select(x => x.DependencyRisk).Should().BeEquivalentTo(state);

    private void VerifyFilteredRisks(params IDependencyRisk[] state) =>
        testSubject.GroupDependencyRisk.FilteredRisks.Select(x => x.DependencyRisk).Should().BeEquivalentTo(state);

    private void DidNotReceiveEvent(string eventName) => ReceivedEvent(eventName, 0);

    private void ReceivedEvent(string eventName, int count = 1) => eventHandler.Received(count).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == eventName));

    private ReportViewModel CreateTestSubject() =>
        new(activeSolutionBoundTracker,
            dependencyRisksStore,
            showDependencyRiskInBrowserHandler,
            changeDependencyRiskStatusHandler,
            messageBox,
            telemetryManager,
            threadHandling);

    private static IDependencyRisk CreateDependencyRisk(Guid? id = null, bool isResolved = false)
    {
        var risk = Substitute.For<IDependencyRisk>();
        risk.Id.Returns(id ?? Guid.NewGuid());
        risk.Transitions.Returns([]);
        risk.Status.Returns(isResolved ? DependencyRiskStatus.Accepted : DependencyRiskStatus.Open);
        return risk;
    }

    private void MockRisksInStore(params IDependencyRisk[] dependencyRisks) => dependencyRisksStore.GetAll().Returns(dependencyRisks);
}
