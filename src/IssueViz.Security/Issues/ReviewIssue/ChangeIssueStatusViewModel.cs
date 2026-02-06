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

using SonarLint.VisualStudio.IssueVisualization.Security.ReviewStatus;
using SonarLint.VisualStudio.SLCore.Service.Issue.Models;

namespace SonarLint.VisualStudio.IssueVisualization.Security.Issues;

internal class ChangeIssueStatusViewModel(ResolutionStatus? currentStatus, IEnumerable<ResolutionStatus> allowedStatuses)
    : ChangeStatusViewModel<ResolutionStatus>(currentStatus,
        GetAllowedStatuses(allowedStatuses), showComment: true)
{
    private static readonly IReadOnlyList<StatusViewModel<ResolutionStatus>> StatusViewModels =
    [
        new(ResolutionStatus.ACCEPT, Resources.ReviewIssueWindow_AcceptTitle,
            Resources.ReviewIssueWindow_AcceptContent, isCommentRequired: false),
        new(ResolutionStatus.WONT_FIX, Resources.ReviewIssueWindow_WontFixTitle,
            Resources.ReviewIssueWindow_WontFixContent, isCommentRequired: false),
        new(ResolutionStatus.FALSE_POSITIVE, Resources.ReviewIssueWindow_FalsePositiveTitle,
            Resources.ReviewIssueWindow_FalsePositiveContent, isCommentRequired: false)
    ];

    private static List<StatusViewModel<ResolutionStatus>> GetAllowedStatuses(IEnumerable<ResolutionStatus> allowedStatuses) =>
        StatusViewModels.Where(vm => allowedStatuses.Any(status => vm.GetCurrentStatus<ResolutionStatus>() == status)).ToList();
}
