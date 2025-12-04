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
using NSubstitute.ClearExtensions;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;
using SonarLint.VisualStudio.TestInfrastructure;

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Filters;

[TestClass]
public class ReportViewFilterViewModelTest
{
    private IFocusOnNewCodeServiceUpdater focusOnNewCodeService;
    private NoOpThreadHandler threadHandling;
    private ReportViewFilterViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        focusOnNewCodeService = Substitute.For<IFocusOnNewCodeServiceUpdater>();
        CreateTestSubject();
    }

    private void CreateTestSubject()
    {
        threadHandling = Substitute.ForPartsOf<NoOpThreadHandler>();
        testSubject = new ReportViewFilterViewModel(focusOnNewCodeService, threadHandling);
    }

    [TestMethod]
    public void Ctor_InitializesLocationFilters()
    {
        testSubject.LocationFilters.Should().HaveCount(2);
        testSubject.LocationFilters[0].LocationFilter.Should().Be(LocationFilter.CurrentDocument);
        testSubject.LocationFilters[0].DisplayName.Should().Be(Resources.HotspotsControl_CurrentDocumentFilter);
        testSubject.LocationFilters[1].LocationFilter.Should().Be(LocationFilter.OpenDocuments);
        testSubject.LocationFilters[1].DisplayName.Should().Be(Resources.HotspotsControl_OpenDocumentsFilter);

        testSubject.SelectedLocationFilter.Should().NotBeNull();
        testSubject.SelectedLocationFilter.LocationFilter.Should().Be(LocationFilter.OpenDocuments);

        testSubject.ShowAdvancedFilters.Should().BeFalse();
    }

    [TestMethod]
    public void Ctor_InitializesIssueTypeFilters()
    {
        testSubject.IssueTypeFilters.Should().HaveCount(4);
        testSubject.IssueTypeFilters[0].IssueType.Should().Be(IssueType.Issue);
        testSubject.IssueTypeFilters[1].IssueType.Should().Be(IssueType.SecurityHotspot);
        testSubject.IssueTypeFilters[2].IssueType.Should().Be(IssueType.TaintVulnerability);
        testSubject.IssueTypeFilters[3].IssueType.Should().Be(IssueType.DependencyRisk);

        testSubject.IssueTypeFilters.All(x => x.IsSelected).Should().BeTrue();
    }

    [TestMethod]
    public void Ctor_InitializesStatusFilters()
    {
        testSubject.StatusFilters.Should().BeEquivalentTo(
        [
            DisplayStatus.Open,
            DisplayStatus.Resolved,
            DisplayStatus.Any
        ], options => options.WithStrictOrdering());

        testSubject.SelectedStatusFilter.Should().Be(DisplayStatus.Open);
    }

    [TestMethod]
    public void Ctor_InitializesSeverityFilters()
    {
        testSubject.SeverityFilters.Should().BeEquivalentTo(
        [
            DisplaySeverity.Blocker,
            DisplaySeverity.High,
            DisplaySeverity.Medium,
            DisplaySeverity.Low,
            DisplaySeverity.Info,
        ], options => options.WithStrictOrdering());

        testSubject.SelectedSeverityFilter.Should().Be(DisplaySeverity.Info);
    }

    [TestMethod]
    public void Ctor_SubscribesForNewCodeUpdatesAndWaitsForServiceInitialization()
    {
        focusOnNewCodeService.ClearSubstitute();
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        var tcs = new TaskCompletionSource<byte>();
        focusOnNewCodeService.InitializationProcessor.InitializeAsync().Returns(tcs.Task);

        CreateTestSubject();
        testSubject.PropertyChanged += eventHandler;
        tcs.SetResult(0);

        focusOnNewCodeService.Received(1).Changed += Arg.Any<EventHandler<NewCodeStatusChangedEventArgs>>();
        focusOnNewCodeService.Received().InitializationProcessor.InitializeAsync();
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.SelectedNewCodeFilter)));
    }

    [TestMethod]
    public void SelectedLocationFilter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedLocationFilter = testSubject.LocationFilters[1];

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.SelectedLocationFilter)));
    }

    [TestMethod]
    public void ShowAdvancedFilters_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.ShowAdvancedFilters = true;

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.ShowAdvancedFilters)));
    }

    [TestMethod]
    public void SelectedStatusFilter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedStatusFilter = DisplayStatus.Open;

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.SelectedStatusFilter)));
    }

    [TestMethod]
    public void SelectedSeverityFilter_RaisesEvents()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedSeverityFilter = DisplaySeverity.Info;

        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.SelectedSeverityFilter)));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void SelectedNewCodeFilter_DoesNotRaiseEventsDirectly(bool focusOnNewCodeState)
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        testSubject.SelectedNewCodeFilter = focusOnNewCodeState;

        focusOnNewCodeService.Received().SetPreference(focusOnNewCodeState);
    }

    [DataTestMethod]
    public void SelectedNewCodeFilter_FocusOnNewCodeUpdated_RaisesEvent()
    {
        var eventHandler = Substitute.For<PropertyChangedEventHandler>();
        testSubject.PropertyChanged += eventHandler;

        focusOnNewCodeService.Changed += Raise.EventWith(new NewCodeStatusChangedEventArgs(new FocusOnNewCodeStatus(true)));

        threadHandling.Received(1).RunOnUIThreadAsync(Arg.Any<Action>());
        eventHandler.Received(1).Invoke(Arg.Any<object>(), Arg.Is<PropertyChangedEventArgs>(p => p.PropertyName == nameof(testSubject.SelectedNewCodeFilter)));
    }

    [DataTestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void ClearAll_SetsAllFiltersToDefaultValued(bool focusOnNewCodeState)
    {
        focusOnNewCodeService.Current.Returns(new FocusOnNewCodeStatus(focusOnNewCodeState));
        testSubject.IssueTypeFilters.ToList().ForEach(vm => vm.IsSelected = false);
        testSubject.SelectedLocationFilter = testSubject.LocationFilters.Single(f => f.LocationFilter == LocationFilter.CurrentDocument);
        testSubject.SelectedSeverityFilter = DisplaySeverity.Info;
        testSubject.SelectedStatusFilter = DisplayStatus.Resolved;
        testSubject.SelectedNewCodeFilter = true;

        testSubject.ClearAllFilters();

        testSubject.IssueTypeFilters.All(vm => vm.IsSelected).Should().BeTrue();
        testSubject.SelectedLocationFilter.LocationFilter.Should().Be(LocationFilter.OpenDocuments);
        testSubject.SelectedSeverityFilter.Should().Be(DisplaySeverity.Info);
        testSubject.SelectedStatusFilter.Should().Be(DisplayStatus.Open);
        testSubject.SelectedNewCodeFilter.Should().Be(focusOnNewCodeState);
    }

    [TestMethod]
    public void Dispose_Unsubscribes()
    {
        testSubject.Dispose();

        focusOnNewCodeService.Received().Changed -= Arg.Any<EventHandler<NewCodeStatusChangedEventArgs>>();
    }
}
