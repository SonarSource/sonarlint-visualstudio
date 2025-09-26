﻿/*
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

namespace SonarLint.VisualStudio.IssueVisualization.Security.UnitTests.ReportView.Filters;

[TestClass]
public class ReportViewFilterViewModelTest
{
    private ReportViewFilterViewModel testSubject;

    [TestInitialize]
    public void TestInitialize()
    {
        testSubject = new ReportViewFilterViewModel();
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
        testSubject.IssueTypeFilters.Should().HaveCount(3);
        testSubject.IssueTypeFilters[0].IssueType.Should().Be(IssueType.SecurityHotspot);
        testSubject.IssueTypeFilters[1].IssueType.Should().Be(IssueType.TaintVulnerability);
        testSubject.IssueTypeFilters[2].IssueType.Should().Be(IssueType.DependencyRisk);

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
            DisplaySeverity.Any,
            DisplaySeverity.Blocker,
            DisplaySeverity.High,
            DisplaySeverity.Medium,
            DisplaySeverity.Low,
            DisplaySeverity.Info,
        ], options => options.WithStrictOrdering());

        testSubject.SelectedSeverityFilter.Should().Be(DisplaySeverity.Any);
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
}
