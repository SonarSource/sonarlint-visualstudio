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
using SonarLint.VisualStudio.SLCore.Common.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots.ReviewHotspot;

public class ReviewHotspotsViewModel : ViewModelBase
{
    private readonly IReadOnlyList<StatusViewModel> allStatusViewModels =
    [
        new(HotspotStatus.TO_REVIEW, Resources.ReviewHotspotWindow_ToReviewTitle, Resources.ReviewHotspotWindow_ToReviewContent),
        new(HotspotStatus.ACKNOWLEDGED, Resources.ReviewHotspotWindow_AcknowledgeTitle, Resources.ReviewHotspotWindow_AcknowledgeContent),
        new(HotspotStatus.FIXED, Resources.ReviewHotspotWindow_FixedTitle, Resources.ReviewHotspotWindow_FixedContent),
        new(HotspotStatus.SAFE, Resources.ReviewHotspotWindow_SafeTitle, Resources.ReviewHotspotWindow_SafeContent)
    ];
    private StatusViewModel selectedStatusViewModel;

    public ReviewHotspotsViewModel(HotspotStatus currentStatus, IEnumerable<HotspotStatus> allowedStatuses)
    {
        InitializeStatuses(allowedStatuses);
        InitializeCurrentStatus(currentStatus);
    }

    public StatusViewModel SelectedStatusViewModel
    {
        get => selectedStatusViewModel;
        set
        {
            selectedStatusViewModel = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsSubmitButtonEnabled));
        }
    }

    public bool IsSubmitButtonEnabled => SelectedStatusViewModel != null;

    public ObservableCollection<StatusViewModel> AllowedStatusViewModels { get; set; } = [];

    private void InitializeStatuses(IEnumerable<HotspotStatus> allowedStatuses)
    {
        AllowedStatusViewModels.Clear();
        allStatusViewModels.ToList().ForEach(vm => vm.IsChecked = false);
        allStatusViewModels.Where(x => allowedStatuses.Contains(x.HotspotStatus)).ToList().ForEach(vm => AllowedStatusViewModels.Add(vm));
    }

    private void InitializeCurrentStatus(HotspotStatus currentStatus)
    {
        SelectedStatusViewModel = AllowedStatusViewModels.FirstOrDefault(x => x.HotspotStatus == currentStatus);
        if (SelectedStatusViewModel == null)
        {
            return;
        }
        SelectedStatusViewModel.IsChecked = true;
    }
}
