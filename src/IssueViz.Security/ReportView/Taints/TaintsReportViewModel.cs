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
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;
using SonarLint.VisualStudio.IssueVisualization.Security.Taint;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Taints;

internal interface ITaintsReportViewModel : IDisposable
{
    ObservableCollection<IGroupViewModel> GetTaintsGroupViewModels();

    event EventHandler TaintsChanged;
}

[Export(typeof(ITaintsReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TaintsReportViewModel : ITaintsReportViewModel
{
    private readonly ITaintStore taintsStore;

    // TODO by SLVS-2525 introduce base view model to avoid duplication with HotspotsReportViewModel (and future IssuesReportViewModel)
    [ImportingConstructor]
    public TaintsReportViewModel(ITaintStore taintsStore)
    {
        this.taintsStore = taintsStore;
        taintsStore.IssuesChanged += TaintsStore_IssuesChanged;
    }

    public void Dispose() => taintsStore.IssuesChanged -= TaintsStore_IssuesChanged;

    public event EventHandler TaintsChanged;

    public ObservableCollection<IGroupViewModel> GetTaintsGroupViewModels()
    {
        var taints = taintsStore.GetAll().Select(x => new IssueViewModel(x));
        return GetGroupViewModel(taints);
    }

    private static ObservableCollection<IGroupViewModel> GetGroupViewModel(IEnumerable<IIssueViewModel> issueViewModels)
    {
        var issuesByFileGrouping = issueViewModels.GroupBy(vm => vm.FilePath);
        var groupViewModels = new ObservableCollection<IGroupViewModel>();
        foreach (var group in issuesByFileGrouping)
        {
            groupViewModels.Add(new GroupFileViewModel(group.Key, new ObservableCollection<IIssueViewModel>(group)));
        }

        return groupViewModels;
    }

    private void TaintsStore_IssuesChanged(object sender, IssuesChangedEventArgs e) => TaintsChanged?.Invoke(this, EventArgs.Empty);
}
