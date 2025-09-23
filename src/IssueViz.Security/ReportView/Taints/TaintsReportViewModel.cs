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
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.IssueVisualization.Helpers;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;

internal interface ITaintsReportViewModel : IDisposable
{
    void ShowTaintInBrowser(ITaintIssue taintIssue);

    ObservableCollection<IGroupViewModel> GetTaintsGroupViewModels();

    event EventHandler<IssuesChangedEventArgs> IssuesChanged;
}

[Export(typeof(ITaintsReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TaintsReportViewModel : IssuesReportViewModelBase, ITaintsReportViewModel
{
    private readonly ITaintStore taintsStore;
    private readonly IShowInBrowserService showInBrowserService;

    [ImportingConstructor]
    public TaintsReportViewModel(ITaintStore taintsStore, IShowInBrowserService showInBrowserService, IThreadHandling threadHandling) : base(taintsStore, threadHandling)
    {
        this.taintsStore = taintsStore;
        this.showInBrowserService = showInBrowserService;
    }

    public void ShowTaintInBrowser(ITaintIssue taintIssue) => showInBrowserService.ShowIssue(taintIssue.IssueServerKey);

    public ObservableCollection<IGroupViewModel> GetTaintsGroupViewModels() => GetGroupViewModels();

    protected override IEnumerable<IIssueViewModel> GetIssueViewModels() => taintsStore.GetAll().Select(x => new TaintViewModel(x));
}
