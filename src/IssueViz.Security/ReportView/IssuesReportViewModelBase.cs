/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Models;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView;

public class ViewModelAnalysisIssuesChangedEventArgs(IReadOnlyCollection<IIssueViewModel> addedIssues, HashSet<Guid> removedIssues)
    : EventArgs
{
    public HashSet<Guid> RemovedIssues { get; } = removedIssues;

    public IReadOnlyCollection<IIssueViewModel> AddedIssues { get; } = addedIssues;
}

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

    public event EventHandler<ViewModelAnalysisIssuesChangedEventArgs> IssuesChanged;

    private void IssueStore_OnIssuesChanged(object sender, IssuesChangedEventArgs e)
    {
        var added = e.AddedIssues.Select(CreateViewModel).ToList();
        var removed = e.RemovedIssues.Select(x => x.IssueId).ToHashSet();

        threadHandling.RunOnUIThread(() => IssuesChanged?.Invoke(this, new ViewModelAnalysisIssuesChangedEventArgs(added, removed)));
    }

    protected abstract IIssueViewModel CreateViewModel(IAnalysisIssueVisualization issue);

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
