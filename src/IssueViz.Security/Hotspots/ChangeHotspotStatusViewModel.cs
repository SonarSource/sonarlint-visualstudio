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

using SonarLint.VisualStudio.Core.Analysis;
using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Hotspots;

internal class ChangeHotspotStatusViewModel(HotspotStatus currentStatus, IEnumerable<HotspotStatus> allowedStatuses)
    : ChangeStatusViewModel<HotspotStatus>(currentStatus,
        GetAllowedStatuses(allowedStatuses), showComment: false)
{
    private static readonly IReadOnlyList<StatusViewModel<HotspotStatus>> StatusViewModels =
    [
        new(HotspotStatus.ToReview, Resources.ReviewHotspotWindow_ToReviewTitle,
            Resources.ReviewHotspotWindow_ToReviewContent, isCommentRequired: false),
        new(HotspotStatus.Acknowledged, Resources.ReviewHotspotWindow_AcknowledgeTitle,
            Resources.ReviewHotspotWindow_AcknowledgeContent, isCommentRequired: false),
        new(HotspotStatus.Fixed, Resources.ReviewHotspotWindow_FixedTitle,
            Resources.ReviewHotspotWindow_FixedContent, isCommentRequired: false),
        new(HotspotStatus.Safe, Resources.ReviewHotspotWindow_SafeTitle,
            Resources.ReviewHotspotWindow_SafeContent, isCommentRequired: false)
    ];

    private static List<StatusViewModel<HotspotStatus>> GetAllowedStatuses(IEnumerable<HotspotStatus> allowedStatuses) =>
        StatusViewModels.Where(vm => allowedStatuses.Any(hotspotStatus => vm.GetCurrentStatus<HotspotStatus>() == hotspotStatus)).ToList();
}
