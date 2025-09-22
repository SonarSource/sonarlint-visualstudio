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

    event EventHandler<IssuesChangedEventArgs> IssuesChanged;
}

[Export(typeof(ITaintsReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class TaintsReportViewModel : IssuesReportViewModelBase, ITaintsReportViewModel
{
    private readonly ITaintStore taintsStore;

    [ImportingConstructor]
    public TaintsReportViewModel(ITaintStore taintsStore) : base(taintsStore)
    {
        this.taintsStore = taintsStore;
    }

    public ObservableCollection<IGroupViewModel> GetTaintsGroupViewModels() => GetGroupViewModels();

    protected override IEnumerable<IIssueViewModel> GetIssueViewModels() => taintsStore.GetAll().Select(x => new TaintViewModel(x));
}
