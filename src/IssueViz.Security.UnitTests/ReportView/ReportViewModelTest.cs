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

        testSubject = CreateTestSubject();
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

    [DataTestMethod]
    [DataRow(true, true, false, true)]
    [DataRow(true, false, false, true)]
    [DataRow(false, true, true, true)]
    public void FlipAndUpdateResolutionFilter_OpenFilter_AsExpected(bool open, bool resolved, bool expectedOpen, bool expectedResolved)
    {
        testSubject.ResolutionFilterOpen.IsSelected = open;
        testSubject.ResolutionFilterResolved.IsSelected = resolved;
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.GroupDependencyRisk.PropertyChanged += eventHandler;

        testSubject.FlipAndUpdateResolutionFilter(testSubject.ResolutionFilterOpen);

        testSubject.ResolutionFilterOpen.IsSelected.Should().Be(expectedOpen);
        testSubject.ResolutionFilterResolved.IsSelected.Should().Be(expectedResolved);
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.GroupDependencyRisk.FilteredRisks)));
    }

    [DataTestMethod]
    [DataRow(true, true, true, false)]
    [DataRow(true, false, true, true)]
    [DataRow(false, true, true, false)]
    public void FlipAndUpdateResolutionFilter_ResolvedFilter_AsExpected(bool open, bool resolved, bool expectedOpen, bool expectedResolved)
    {
        testSubject.ResolutionFilterOpen.IsSelected = open;
        testSubject.ResolutionFilterResolved.IsSelected = resolved;
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.GroupDependencyRisk.PropertyChanged += eventHandler;

        testSubject.FlipAndUpdateResolutionFilter(testSubject.ResolutionFilterResolved);

        testSubject.ResolutionFilterOpen.IsSelected.Should().Be(expectedOpen);
        testSubject.ResolutionFilterResolved.IsSelected.Should().Be(expectedResolved);
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(x => x.PropertyName == nameof(testSubject.GroupDependencyRisk.FilteredRisks)));
    }

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
