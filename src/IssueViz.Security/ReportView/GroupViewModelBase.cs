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
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal abstract class GroupViewModelBase(string title, string filePath) : ViewModelBase, IGroupViewModel
{
    private bool isExpanded = true;
    public string Title { get; } = title;
    public string FilePath { get; } = filePath;

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            isExpanded = value;
            RaisePropertyChanged();
        }
    }

    public abstract List<IIssueViewModel> AllIssues { get; }
    public abstract List<IIssueViewModel> PreFilteredIssues { get; protected set; }
    public abstract ObservableCollection<IIssueViewModel> FilteredIssues { get; }

    public void ApplyFilter(ReportViewFilterViewModel reportViewFilter)
    {
        IEnumerable<IIssueViewModel> issuesToShow = PreFilteredIssues = PreFilter(reportViewFilter, AllIssues).ToList();

        issuesToShow = FilterIssues(reportViewFilter, issuesToShow);

        issuesToShow = OrderIssues(issuesToShow);

        FilteredIssues.Clear();
        issuesToShow.ToList().ForEach(issue => FilteredIssues.Add(issue));
    }

    private static IEnumerable<IIssueViewModel> OrderIssues(IEnumerable<IIssueViewModel> issuesToShow) => issuesToShow.OrderByDescending(vm => vm.DisplaySeverity, DisplaySeverityComparer.Instance);

    private static IEnumerable<IIssueViewModel> PreFilter(ReportViewFilterViewModel reportViewFilter, IEnumerable<IIssueViewModel> issuesToShow)
    {
        if (reportViewFilter.SelectedStatusFilter != DisplayStatus.Any)
        {
            issuesToShow = issuesToShow.Where(vm => vm.Status == reportViewFilter.SelectedStatusFilter);
        }
        return issuesToShow;
    }

    private static IEnumerable<IIssueViewModel> FilterIssues(ReportViewFilterViewModel reportViewFilter, IEnumerable<IIssueViewModel> filteredIssues)
    {
        var issueTypesToShow = reportViewFilter.IssueTypeFilters.Where(x => x.IsSelected).Select(x => x.IssueType);
        filteredIssues = filteredIssues.Where(issue => issueTypesToShow.Contains(issue.IssueType));

        if (reportViewFilter.SelectedSeverityFilter != DisplaySeverity.Info)
        {
            filteredIssues = filteredIssues.Where(vm => DisplaySeverityComparer.Instance.Compare(vm.DisplaySeverity, reportViewFilter.SelectedSeverityFilter) >= 0);
        }
        if (reportViewFilter.SelectedNewCodeFilter)
        {
            filteredIssues = filteredIssues.Where(vm => vm.IsOnNewCode);
        }

        return filteredIssues;
    }

    public void Dispose() { }
}
