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

using System.Collections.ObjectModel;
using SonarLint.VisualStudio.Core.WPF;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

/// <summary>
/// Used to display status info in the UI in a uniform way for different <see cref="IssueType"/>
/// </summary>
public enum DisplayStatus
{
    Open,
    Resolved
}

internal class ReportViewFilterViewModel : ViewModelBase
{
    private LocationFilterViewModel selectedLocationFilter;
    private DisplaySeverity selectedSeverityFilter = DisplaySeverity.Any;
    private DisplayStatus? selectedStatusFilter;
    private bool showAdvancedFilters;

    public ObservableCollection<LocationFilterViewModel> LocationFilters { get; } =
    [
        new(LocationFilter.CurrentDocument, Resources.HotspotsControl_CurrentDocumentFilter),
        new(LocationFilter.OpenDocuments, Resources.HotspotsControl_OpenDocumentsFilter)
    ];
    public ObservableCollection<DisplayStatus> StatusFilters { get; } = new(Enum.GetValues(typeof(DisplayStatus)).Cast<DisplayStatus>());
    public ObservableCollection<DisplaySeverity> SeverityFilters { get; } = new(Enum.GetValues(typeof(DisplaySeverity)).Cast<DisplaySeverity>().Reverse());
    public ObservableCollection<IssueTypeFilterViewModel> IssueTypeFilters { get; } =
        new(Enum.GetValues(typeof(IssueType)).Cast<IssueType>().Select(x => new IssueTypeFilterViewModel(x)));

    public LocationFilterViewModel SelectedLocationFilter
    {
        get => selectedLocationFilter;
        set
        {
            selectedLocationFilter = value;
            RaisePropertyChanged();
        }
    }

    public DisplayStatus? SelectedStatusFilter
    {
        get => selectedStatusFilter;
        set
        {
            selectedStatusFilter = value;
            RaisePropertyChanged();
        }
    }

    public DisplaySeverity SelectedSeverityFilter
    {
        get => selectedSeverityFilter;
        set
        {
            selectedSeverityFilter = value;
            RaisePropertyChanged();
        }
    }

    public bool ShowAdvancedFilters
    {
        get => showAdvancedFilters;
        set
        {
            showAdvancedFilters = value;
            RaisePropertyChanged();
        }
    }

    public ReportViewFilterViewModel()
    {
        SelectedLocationFilter = LocationFilters.Single(x => x.LocationFilter == LocationFilter.OpenDocuments);
    }
}
