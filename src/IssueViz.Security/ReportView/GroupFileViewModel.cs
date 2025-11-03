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
using System.IO;
using SonarLint.VisualStudio.Core.WPF;
using SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Filters;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal sealed class GroupFileViewModel : ViewModelBase, IGroupViewModel
{
    private bool isExpanded = true;

    public GroupFileViewModel(string filePath, List<IIssueViewModel> issues)
    {
        Title = Path.GetFileName(filePath);
        FilePath = filePath;
        AllIssues = issues;
        FilteredIssues = new ObservableCollection<IIssueViewModel>(issues);
    }

    public string Title { get; }
    public string FilePath { get; }
    public List<IIssueViewModel> AllIssues { get; }
    public ObservableCollection<IIssueViewModel> FilteredIssues { get; }

    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            isExpanded = value;
            RaisePropertyChanged();
        }
    }

    public void ApplyFilter(ReportViewFilterViewModel reportViewFilter)
    {
        var issueTypesToShow = reportViewFilter.IssueTypeFilters.Where(x => x.IsSelected).Select(x => x.IssueType);
        var filteredIssues = AllIssues.Where(issue => issueTypesToShow.Contains(issue.IssueType));

        filteredIssues = filteredIssues
            .Where(issue => DisplaySeverityComparer.Instance.Compare(issue.DisplaySeverity, reportViewFilter.SelectedSeverityFilter) >= 0)
            .OrderByDescending(issue => issue.DisplaySeverity, DisplaySeverityComparer.Instance);

        if (reportViewFilter.SelectedStatusFilter != DisplayStatus.Any)
        {
            filteredIssues = filteredIssues.Where(vm => vm.Status == reportViewFilter.SelectedStatusFilter);
        }

        FilteredIssues.Clear();
        filteredIssues.ToList().ForEach(issue => FilteredIssues.Add(issue));
    }

    public void Dispose() { }
}
