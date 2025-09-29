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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

internal abstract class IssuesReportViewModelBase : IDisposable
{
    private readonly IIssuesStore issuesStore;
    private readonly IThreadHandling threadHandling;
    private bool disposed;

    protected IssuesReportViewModelBase(IIssuesStore issuesStore, IThreadHandling threadHandling)
    {
        this.issuesStore = issuesStore;
        this.threadHandling = threadHandling;
        issuesStore.IssuesChanged += IssueStore_OnIssuesChanged;
    }

    public event EventHandler<IssuesChangedEventArgs> IssuesChanged;

    public ObservableCollection<IGroupViewModel> GetGroupViewModels()
    {
        var issueViewModels = GetIssueViewModels();
        return GroupIssueViewModels(issueViewModels);
    }

    private void IssueStore_OnIssuesChanged(object sender, IssuesChangedEventArgs e) => threadHandling.RunOnUIThread(() => IssuesChanged?.Invoke(this, e));

    protected abstract IEnumerable<IIssueViewModel> GetIssueViewModels();

    private ObservableCollection<IGroupViewModel> GroupIssueViewModels(IEnumerable<IIssueViewModel> issueViewModels)
    {
        var issuesByFileGrouping = issueViewModels.GroupBy(vm => vm.FilePath);
        var groupViewModels = new ObservableCollection<IGroupViewModel>();
        foreach (var group in issuesByFileGrouping)
        {
            groupViewModels.Add(new GroupFileViewModel(group.Key, new List<IIssueViewModel>(group)));
        }

        return groupViewModels;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }
        if (disposing)
        {
            issuesStore.IssuesChanged -= IssueStore_OnIssuesChanged;
        }
        disposed = true;
    }
}
