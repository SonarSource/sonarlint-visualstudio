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
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;
using SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;
using SonarLint.VisualStudio.IssueVisualization.Security.IssuesStore;

namespace SonarLint.VisualStudio.IssueVisualization.Security.ReportView.Hotspots;

internal interface IHotspotsReportViewModel : IDisposable
{
    ObservableCollection<IGroupViewModel> GetHotspotsGroupViewModels();

    Task ShowHotspotInBrowserAsync(LocalHotspot localHotspot);

    event EventHandler HotspotsChanged;
}

[Export(typeof(IHotspotsReportViewModel))]
[PartCreationPolicy(CreationPolicy.Shared)]
internal sealed class HotspotsReportViewModel : IHotspotsReportViewModel
{
    private readonly ILocalHotspotsStore hotspotsStore;
    private readonly IReviewHotspotsService reviewHotspotsService;

    [ImportingConstructor]
    public HotspotsReportViewModel(ILocalHotspotsStore hotspotsStore, IReviewHotspotsService reviewHotspotsService)
    {
        this.hotspotsStore = hotspotsStore;
        this.reviewHotspotsService = reviewHotspotsService;
        hotspotsStore.IssuesChanged += HotspotsStore_IssuesChanged;
    }

    public void Dispose() => hotspotsStore.IssuesChanged -= HotspotsStore_IssuesChanged;

    public event EventHandler HotspotsChanged;

    public ObservableCollection<IGroupViewModel> GetHotspotsGroupViewModels()
    {
        var hotspots = hotspotsStore.GetAllLocalHotspots().Select(x => new HotspotViewModel(x));
        return GetGroupViewModel(hotspots);
    }

    public async Task ShowHotspotInBrowserAsync(LocalHotspot localHotspot) => await reviewHotspotsService.OpenHotspotAsync(localHotspot.Visualization.Issue.IssueServerKey);

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

    private void HotspotsStore_IssuesChanged(object sender, IssuesChangedEventArgs e) => HotspotsChanged?.Invoke(this, EventArgs.Empty);
}
